using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Vendors;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Seo;
using Nop.Services.Vendors;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Misc.WebApi.Frontend.Factories;

/// <summary>
/// Represents the API catalog model factory implementation
/// </summary>
public class ApiCatalogModelFactory : IApiCatalogModelFactory
{
    #region Fields

    private readonly ICatalogModelFactory _catalogModelFactory;
    private readonly ICategoryService _categoryService;
    private readonly ILocalizationService _localizationService;
    private readonly IManufacturerService _manufacturerService;
    private readonly IProductModelFactory _productModelFactory;
    private readonly IProductService _productService;
    private readonly IStoreContext _storeContext;
    private readonly IUrlRecordService _urlRecordService;
    private readonly IVendorService _vendorService;
    private readonly IWorkContext _workContext;
    private readonly CatalogSettings _catalogSettings;
    private readonly VendorSettings _vendorSettings;

    #endregion

    #region Ctor

    public ApiCatalogModelFactory(
        ICatalogModelFactory catalogModelFactory,
        ICategoryService categoryService,
        ILocalizationService localizationService,
        IManufacturerService manufacturerService,
        IProductModelFactory productModelFactory,
        IProductService productService,
        IStoreContext storeContext,
        IUrlRecordService urlRecordService,
        IVendorService vendorService,
        IWorkContext workContext,
        CatalogSettings catalogSettings,
        VendorSettings vendorSettings)
    {
        _catalogModelFactory = catalogModelFactory;
        _categoryService = categoryService;
        _localizationService = localizationService;
        _manufacturerService = manufacturerService;
        _productModelFactory = productModelFactory;
        _productService = productService;
        _storeContext = storeContext;
        _urlRecordService = urlRecordService;
        _vendorService = vendorService;
        _workContext = workContext;
        _catalogSettings = catalogSettings;
        _vendorSettings = vendorSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepare the search model with support for multiple categories, manufacturers, and vendors
    /// </summary>
    public virtual async Task<SearchModel> PrepareSearchModelAsync(
        SearchModel model,
        CatalogProductsCommand command,
        IList<int> categoryIds = null,
        IList<int> manufacturerIds = null,
        IList<int> vendorIds = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(command);

        categoryIds ??= new List<int>();
        manufacturerIds ??= new List<int>();
        vendorIds ??= new List<int>();

        var currentStore = await _storeContext.GetCurrentStoreAsync();
        var workingLanguage = await _workContext.GetWorkingLanguageAsync();

        // Prepare available categories
        var categoriesModels = new List<SearchModel.CategoryModel>();
        var allCategories = await _categoryService.GetAllCategoriesAsync(storeId: currentStore.Id);
        
        foreach (var c in allCategories)
        {
            // Generate full category name (breadcrumb)
            var categoryBreadcrumb = string.Empty;
            var breadcrumb = await _categoryService.GetCategoryBreadCrumbAsync(c, allCategories);
            for (var i = 0; i <= breadcrumb.Count - 1; i++)
            {
                categoryBreadcrumb += await _localizationService.GetLocalizedAsync(breadcrumb[i], x => x.Name);
                if (i != breadcrumb.Count - 1)
                    categoryBreadcrumb += " >> ";
            }

            categoriesModels.Add(new SearchModel.CategoryModel
            {
                Id = c.Id,
                Breadcrumb = categoryBreadcrumb
            });
        }

        if (categoriesModels.Any())
        {
            // First empty entry
            model.AvailableCategories.Add(new SelectListItem
            {
                Value = "0",
                Text = await _localizationService.GetResourceAsync("Common.All")
            });
            
            // All other categories
            foreach (var c in categoriesModels)
            {
                model.AvailableCategories.Add(new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Breadcrumb,
                    Selected = model.cid == c.Id
                });
            }
        }

        // Prepare available manufacturers
        var manufacturers = await _manufacturerService.GetAllManufacturersAsync(storeId: currentStore.Id);
        if (manufacturers.Any())
        {
            model.AvailableManufacturers.Add(new SelectListItem
            {
                Value = "0",
                Text = await _localizationService.GetResourceAsync("Common.All")
            });
            
            foreach (var m in manufacturers)
            {
                model.AvailableManufacturers.Add(new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = await _localizationService.GetLocalizedAsync(m, x => x.Name),
                    Selected = model.mid == m.Id
                });
            }
        }

        // Prepare available vendors
        model.asv = _vendorSettings.AllowSearchByVendor;
        if (model.asv)
        {
            var vendors = await _vendorService.GetAllVendorsAsync();
            if (vendors.Any())
            {
                model.AvailableVendors.Add(new SelectListItem
                {
                    Value = "0",
                    Text = await _localizationService.GetResourceAsync("Common.All")
                });
                
                foreach (var vendor in vendors)
                {
                    model.AvailableVendors.Add(new SelectListItem
                    {
                        Value = vendor.Id.ToString(),
                        Text = await _localizationService.GetLocalizedAsync(vendor, x => x.Name),
                        Selected = model.vid == vendor.Id
                    });
                }
            }
        }

        // Prepare catalog products model with extended search
        model.CatalogProductsModel = await PrepareSearchProductsModelAsync(
            model, 
            command, 
            categoryIds, 
            manufacturerIds, 
            vendorIds);

        // Collect unique vendors from found products
        if (model.CatalogProductsModel?.Products != null && model.CatalogProductsModel.Products.Any())
        {
            var productVendorIds = model.CatalogProductsModel.Products
                .Where(p => p.VendorId > 0)
                .Select(p => p.VendorId)
                .Distinct()
                .ToList();

            foreach (var vendorId in productVendorIds)
            {
                var vendor = await _vendorService.GetVendorByIdAsync(vendorId);
                if (vendor != null && !vendor.Deleted && vendor.Active)
                {
                    model.FoundVendors.Add(new VendorBriefInfoModel
                    {
                        Id = vendor.Id,
                        Name = await _localizationService.GetLocalizedAsync(vendor, x => x.Name),
                        SeName = await _urlRecordService.GetSeNameAsync(vendor),
                    });
                }
            }
        }

        return model;
    }

    /// <summary>
    /// Prepare search products model with support for multiple filters
    /// </summary>
    protected virtual async Task<CatalogProductsModel> PrepareSearchProductsModelAsync(
        SearchModel searchModel,
        CatalogProductsCommand command,
        IList<int> categoryIds,
        IList<int> manufacturerIds,
        IList<int> vendorIds)
    {
        var model = new CatalogProductsModel
        {
            UseAjaxLoading = _catalogSettings.UseAjaxCatalogProductsLoading
        };

        // Delegate to catalog model factory for sorting, view modes, and page size
        // We can't directly access these protected methods, so we'll prepare them manually
        await PrepareSortingOptionsAsync(model, command);
        await PrepareViewModesAsync(model, command);
        await PreparePageSizeOptionsAsync(model, command);

        var searchTerms = searchModel.q == null ? string.Empty : searchModel.q.Trim();
        var currentStore = await _storeContext.GetCurrentStoreAsync();
        var workingLanguage = await _workContext.GetWorkingLanguageAsync();

        // API calls always perform search (no need to check HttpContext for 'q' parameter)
        var shouldSearch = true;

        IPagedList<Product> products = new PagedList<Product>(new List<Product>(), 0, 1);

        if (shouldSearch)
        {
            // Check minimum search term length only if search term is provided
            if (!string.IsNullOrEmpty(searchTerms) && 
                searchTerms.Length < _catalogSettings.ProductSearchTermMinimumLength)
            {
                model.WarningMessage = string.Format(
                    await _localizationService.GetResourceAsync("Search.SearchTermMinimumLengthIsNCharacters"),
                    _catalogSettings.ProductSearchTermMinimumLength);
            }
            else
            {
                // Expand category IDs to include subcategories
                var expandedCategoryIds = new List<int>();
                if (categoryIds != null && categoryIds.Any())
                {
                    foreach (var categoryId in categoryIds.Where(id => id > 0))
                    {
                        expandedCategoryIds.Add(categoryId);
                        if (searchModel.isc)
                        {
                            var childCategoryIds = await _categoryService.GetChildCategoryIdsAsync(categoryId, currentStore.Id);
                            expandedCategoryIds.AddRange(childCategoryIds);
                        }
                    }
                }

                // Filter out zero values from manufacturer IDs
                var validManufacturerIds = manufacturerIds?.Where(id => id > 0).ToList() ?? new List<int>();

                // Handle multiple vendors - for now we'll search across all vendor IDs
                // Note: The core search API supports only one vendor at a time, 
                // so we'll need to merge results if multiple vendors are specified
                IPagedList<Product> allProducts = null;

                if (vendorIds != null && vendorIds.Count > 1)
                {
                    // Multiple vendors: perform separate searches and merge
                    var allProductsList = new List<Product>();
                    var totalCount = 0;

                    foreach (var vendorId in vendorIds.Where(id => id > 0))
                    {
                        var vendorProducts = await _productService.SearchProductsAsync(
                            0,
                            int.MaxValue, // Get all to merge later
                            categoryIds: expandedCategoryIds,
                            manufacturerIds: validManufacturerIds,
                            storeId: currentStore.Id,
                            visibleIndividuallyOnly: true,
                            keywords: string.IsNullOrEmpty(searchTerms) ? null : searchTerms,
                            searchDescriptions: searchModel.sid,
                            searchProductTags: searchModel.sit,
                            languageId: workingLanguage.Id,
                            vendorId: vendorId);

                        allProductsList.AddRange(vendorProducts);
                        totalCount += vendorProducts.TotalCount;
                    }

                    // Remove duplicates and apply ordering
                    var distinctProducts = allProductsList
                        .GroupBy(p => p.Id)
                        .Select(g => g.First())
                        .ToList();

                    // Apply ordering
                    distinctProducts = ApplyOrdering(distinctProducts, (ProductSortingEnum)command.OrderBy);

                    // Apply pagination
                    var pagedProducts = distinctProducts
                        .Skip((command.PageNumber - 1) * command.PageSize)
                        .Take(command.PageSize)
                        .ToList();

                    allProducts = new PagedList<Product>(pagedProducts, command.PageNumber - 1, command.PageSize, distinctProducts.Count);
                }
                else
                {
                    // Single vendor or no vendor filter
                    var vendorId = vendorIds?.FirstOrDefault(id => id > 0) ?? 0;

                    products = await _productService.SearchProductsAsync(
                        command.PageNumber - 1,
                        command.PageSize,
                        categoryIds: expandedCategoryIds,
                        manufacturerIds: validManufacturerIds,
                        storeId: currentStore.Id,
                        visibleIndividuallyOnly: true,
                        keywords: string.IsNullOrEmpty(searchTerms) ? null : searchTerms,
                        searchDescriptions: searchModel.sid,
                        searchProductTags: searchModel.sit,
                        languageId: workingLanguage.Id,
                        orderBy: (ProductSortingEnum)command.OrderBy,
                        vendorId: vendorId);

                    allProducts = products;
                }

                products = allProducts;
            }
        }

        // Prepare product overview models
        var productModels = await _productModelFactory.PrepareProductOverviewModelsAsync(products);
        model.Products = productModels.ToList();

        // Set pagination properties from BasePageableModel
        model.PageNumber = products.PageIndex + 1;
        model.PageSize = products.PageSize;
        model.TotalItems = products.TotalCount;
        model.TotalPages = products.TotalPages;
        model.FirstItem = (products.PageIndex * products.PageSize) + 1;
        model.LastItem = Math.Min((products.PageIndex + 1) * products.PageSize, products.TotalCount);
        model.HasPreviousPage = products.HasPreviousPage;
        model.HasNextPage = products.HasNextPage;

        return model;
    }

    /// <summary>
    /// Apply ordering to product list
    /// </summary>
    protected virtual List<Product> ApplyOrdering(List<Product> products, ProductSortingEnum orderBy)
    {
        return orderBy switch
        {
            ProductSortingEnum.Position => products.OrderBy(p => p.DisplayOrder).ToList(),
            ProductSortingEnum.NameAsc => products.OrderBy(p => p.Name).ToList(),
            ProductSortingEnum.NameDesc => products.OrderByDescending(p => p.Name).ToList(),
            ProductSortingEnum.PriceAsc => products.OrderBy(p => p.Price).ToList(),
            ProductSortingEnum.PriceDesc => products.OrderByDescending(p => p.Price).ToList(),
            ProductSortingEnum.CreatedOn => products.OrderByDescending(p => p.CreatedOnUtc).ToList(),
            _ => products.OrderBy(p => p.DisplayOrder).ToList()
        };
    }

    /// <summary>
    /// Prepare sorting options
    /// </summary>
    protected virtual async Task PrepareSortingOptionsAsync(CatalogProductsModel model, CatalogProductsCommand command)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(command);

        // Get active sorting options
        var activeSortingOptionsIds = Enum.GetValues(typeof(ProductSortingEnum)).Cast<int>()
            .Except(_catalogSettings.ProductSortingEnumDisabled).ToList();

        // Order sorting options
        var orderedActiveSortingOptions = activeSortingOptionsIds
            .Select(id => new { Id = id, Order = _catalogSettings.ProductSortingEnumDisplayOrder.TryGetValue(id, out var order) ? order : id })
            .OrderBy(option => option.Order).ToList();

        // Set the default option
        model.OrderBy = command.OrderBy;
        command.OrderBy ??= orderedActiveSortingOptions.FirstOrDefault()?.Id ?? (int)ProductSortingEnum.Position;

        // Ensure that product sorting is enabled
        if (!_catalogSettings.AllowProductSorting)
            return;

        model.AllowProductSorting = true;

        // Prepare available sort options
        foreach (var option in orderedActiveSortingOptions)
        {
            model.AvailableSortOptions.Add(new SelectListItem
            {
                Text = await _localizationService.GetLocalizedEnumAsync((ProductSortingEnum)option.Id),
                Value = option.Id.ToString(),
                Selected = option.Id == (command.OrderBy ?? 0)
            });
        }
    }

    /// <summary>
    /// Prepare view modes
    /// </summary>
    protected virtual Task PrepareViewModesAsync(CatalogProductsModel model, CatalogProductsCommand command)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(command);

        model.AllowProductViewModeChanging = _catalogSettings.AllowProductViewModeChanging;
        model.ViewMode = !string.IsNullOrEmpty(command.ViewMode)
            ? command.ViewMode
            : _catalogSettings.DefaultViewMode;
        model.AllowCustomersToSelectPageSize = false;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Prepare page size options
    /// </summary>
    protected virtual Task PreparePageSizeOptionsAsync(CatalogProductsModel model, CatalogProductsCommand command)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(command);

        if (command.PageSize <= 0)
            command.PageSize = _catalogSettings.SearchPageProductsPerPage;
        if (command.PageNumber <= 0)
            command.PageNumber = 1;

        model.AllowCustomersToSelectPageSize = _catalogSettings.SearchPageAllowCustomersToSelectPageSize;
        
        if (model.AllowCustomersToSelectPageSize && _catalogSettings.SearchPagePageSizeOptions != null)
        {
            var pageSizes = _catalogSettings.SearchPagePageSizeOptions.Split(',');
            foreach (var pageSize in pageSizes)
            {
                if (!int.TryParse(pageSize, out var temp))
                    continue;

                if (model.PageSizeOptions.Any(x => x.Value == temp.ToString()))
                    continue;

                model.PageSizeOptions.Add(new SelectListItem
                {
                    Text = pageSize,
                    Value = temp.ToString(),
                    Selected = temp == command.PageSize
                });
            }

            if (!model.PageSizeOptions.Any())
            {
                model.PageSizeOptions.Add(new SelectListItem
                {
                    Text = command.PageSize.ToString(),
                    Value = command.PageSize.ToString(),
                    Selected = true
                });
            }
        }

        model.PageSize = command.PageSize;

        return Task.CompletedTask;
    }

    #endregion
}
