using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Vendors;
using Nop.Plugin.Misc.WebApi.Frontend.Models.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Vendors;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;

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
    private readonly IPictureService _pictureService;
    private readonly IProductModelFactory _productModelFactory;
    private readonly IProductService _productService;
    private readonly IStoreContext _storeContext;
    private readonly IUrlRecordService _urlRecordService;
    private readonly IVendorService _vendorService;
    private readonly IWorkContext _workContext;
    private readonly CatalogSettings _catalogSettings;
    private readonly MediaSettings _mediaSettings;
    private readonly VendorSettings _vendorSettings;

    #endregion

    #region Ctor

    public ApiCatalogModelFactory(
        ICatalogModelFactory catalogModelFactory,
        ICategoryService categoryService,
        ILocalizationService localizationService,
        IManufacturerService manufacturerService,
        IPictureService pictureService,
        IProductModelFactory productModelFactory,
        IProductService productService,
        IStoreContext storeContext,
        IUrlRecordService urlRecordService,
        IVendorService vendorService,
        IWorkContext workContext,
        CatalogSettings catalogSettings,
        MediaSettings mediaSettings,
        VendorSettings vendorSettings)
    {
        _catalogModelFactory = catalogModelFactory;
        _categoryService = categoryService;
        _localizationService = localizationService;
        _manufacturerService = manufacturerService;
        _pictureService = pictureService;
        _productModelFactory = productModelFactory;
        _productService = productService;
        _storeContext = storeContext;
        _urlRecordService = urlRecordService;
        _vendorService = vendorService;
        _workContext = workContext;
        _catalogSettings = catalogSettings;
        _mediaSettings = mediaSettings;
        _vendorSettings = vendorSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepare the search model with support for multiple categories, manufacturers, and vendors
    /// </summary>
    public virtual async Task<ApiSearchModel> PrepareSearchModelAsync(
        ApiSearchModel model,
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

        // Prepare available categories in hierarchical structure
        var allCategories = await _categoryService.GetAllCategoriesAsync(storeId: currentStore.Id);
        
        // Build hierarchical structure - only root categories (no parent)
        var rootCategories = allCategories.Where(c => c.ParentCategoryId == 0).OrderBy(c => c.DisplayOrder).ToList();
        
        foreach (var rootCategory in rootCategories)
        {
            var categoryModel = await BuildCategoryModelAsync(rootCategory, allCategories);
            model.AvailableCategories.Add(categoryModel);
        }

        // Prepare available manufacturers
        var manufacturers = await _manufacturerService.GetAllManufacturersAsync(storeId: currentStore.Id);
        foreach (var m in manufacturers)
        {
            model.AvailableManufacturers.Add(new ApiSearchModel.ManufacturerModel
            {
                Id = m.Id,
                Name = await _localizationService.GetLocalizedAsync(m, x => x.Name)
            });
        }

        // Prepare available vendors
        model.asv = _vendorSettings.AllowSearchByVendor;
        if (model.asv)
        {
            var vendors = await _vendorService.GetAllVendorsAsync();
            foreach (var vendor in vendors)
            {
                var vendorModel = new ApiSearchModel.VendorModel
                {
                    Id = vendor.Id,
                    Name = await _localizationService.GetLocalizedAsync(vendor, x => x.Name)
                };

                // Prepare picture model
                var picture = await _pictureService.GetPictureByIdAsync(vendor.PictureId);
                var pictureSize = _mediaSettings.VendorThumbPictureSize;
                var (imageUrl, _) = await _pictureService.GetPictureUrlAsync(picture, pictureSize);

                vendorModel.PictureModel = new PictureModel
                {
                    ImageUrl = imageUrl,
                    FullSizeImageUrl = (await _pictureService.GetPictureUrlAsync(picture)).Url,
                    Title = string.Format(await _localizationService.GetResourceAsync("Media.Vendor.ImageLinkTitleFormat"), vendorModel.Name),
                    AlternateText = string.Format(await _localizationService.GetResourceAsync("Media.Vendor.ImageAlternateTextFormat"), vendorModel.Name)
                };

                model.AvailableVendors.Add(vendorModel);
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
                    var vendorModel = new ApiSearchModel.VendorModel
                    {
                        Id = vendor.Id,
                        Name = await _localizationService.GetLocalizedAsync(vendor, x => x.Name)
                    };

                    // Prepare picture model
                    var picture = await _pictureService.GetPictureByIdAsync(vendor.PictureId);
                    var pictureSize = _mediaSettings.VendorThumbPictureSize;
                    var (imageUrl, _) = await _pictureService.GetPictureUrlAsync(picture, pictureSize);

                    vendorModel.PictureModel = new PictureModel
                    {
                        ImageUrl = imageUrl,
                        FullSizeImageUrl = (await _pictureService.GetPictureUrlAsync(picture)).Url,
                        Title = string.Format(await _localizationService.GetResourceAsync("Media.Vendor.ImageLinkTitleFormat"), vendorModel.Name),
                        AlternateText = string.Format(await _localizationService.GetResourceAsync("Media.Vendor.ImageAlternateTextFormat"), vendorModel.Name)
                    };

                    model.Vendors.Add(vendorModel);
                }
            }
        }

        return model;
    }

    /// <summary>
    /// Prepare search products model with support for multiple filters
    /// </summary>
    protected virtual async Task<CatalogProductsModel> PrepareSearchProductsModelAsync(
        ApiSearchModel searchModel,
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
        var productModels = await _productModelFactory.PrepareProductOverviewModelsAsync(
            products,
            prepareProductAttributes: true);
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

    /// <summary>
    /// Build category model recursively with subcategories
    /// </summary>
    protected virtual async Task<ApiSearchModel.CategoryModel> BuildCategoryModelAsync(Category category, IList<Category> allCategories)
    {
        var categoryModel = new ApiSearchModel.CategoryModel
        {
            Id = category.Id,
            Name = await _localizationService.GetLocalizedAsync(category, x => x.Name)
        };

        // Load subcategories recursively
        var subCategories = allCategories.Where(c => c.ParentCategoryId == category.Id).OrderBy(c => c.DisplayOrder).ToList();
        foreach (var subCategory in subCategories)
        {
            var subCategoryModel = await BuildCategoryModelAsync(subCategory, allCategories);
            categoryModel.SubCategories.Add(subCategoryModel);
        }

        return categoryModel;
    }

    #endregion
}
