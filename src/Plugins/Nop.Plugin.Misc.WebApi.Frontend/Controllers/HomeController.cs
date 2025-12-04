using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.WebApi.Frontend.Configuration;
using Nop.Plugin.Misc.WebApi.Frontend.DTOs;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Seo;
using Nop.Services.Stores;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Simplified public API for home page data compatible with NopStation Cart API routes
/// </summary>
[ApiController]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
[Route("public-api/home")]
public class HomeController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;
    private readonly IManufacturerService _manufacturerService;
    private readonly IUrlRecordService _urlRecordService;
    private readonly IWorkContext _workContext;
    private readonly IStoreContext _storeContext;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly CustomerSettings _customerSettings;
    private readonly CatalogSettings _catalogSettings;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly OrderSettings _orderSettings;
    private readonly ISettingService _settingService;

    public HomeController(
        ICategoryService categoryService,
        IProductService productService,
        IManufacturerService manufacturerService,
        IUrlRecordService urlRecordService,
        IWorkContext workContext,
        IStoreContext storeContext,
        IShoppingCartService shoppingCartService,
        CustomerSettings customerSettings,
        CatalogSettings catalogSettings,
        ShoppingCartSettings shoppingCartSettings,
        OrderSettings orderSettings,
        ISettingService settingService)
    {
        _categoryService = categoryService;
        _productService = productService;
        _manufacturerService = manufacturerService;
        _urlRecordService = urlRecordService;
        _workContext = workContext;
        _storeContext = storeContext;
        _shoppingCartService = shoppingCartService;
        _customerSettings = customerSettings;
        _catalogSettings = catalogSettings;
        _shoppingCartSettings = shoppingCartSettings;
        _orderSettings = orderSettings;
        _settingService = settingService;
    }

    /// <summary>
    /// GET /home/homepagecategorieswithproducts
    /// Returns home page categories with a few products for each (minimal fields).
    /// </summary>
    [HttpGet("homepagecategorieswithproducts")]
    [ProducesResponseType(typeof(ApiResponse<IList<HomeCategoryWithProductsDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHomepageCategoriesWithProducts()
    {
        var categories = await _categoryService.GetAllCategoriesDisplayedOnHomepageAsync();

        var result = new List<HomeCategoryWithProductsDto>();

        foreach (var category in categories)
        {
            var seName = await _urlRecordService.GetSeNameAsync(category, 0, true, false);

            // take a few products from this category
            var products = await _productService.SearchProductsAsync(
                pageIndex: 0,
                pageSize: 10,
                categoryIds: new List<int> { category.Id },
                manufacturerIds: null,
                storeId: 0,
                vendorId: 0,
                warehouseId: 0,
                productType: null,
                visibleIndividuallyOnly: true);

            var productDtos = new List<ProductSummaryDto>();
            foreach (var product in products)
            {
                var productSeName = await _urlRecordService.GetSeNameAsync(product, 0, true, false);

                productDtos.Add(new ProductSummaryDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    SeName = productSeName,
                    ShortDescription = product.ShortDescription,
                    Sku = product.Sku
                });
            }

            result.Add(new HomeCategoryWithProductsDto
            {
                Id = category.Id,
                Name = category.Name,
                SeName = seName,
                Products = productDtos
            });
        }

        return Ok(new ApiResponse<IList<HomeCategoryWithProductsDto>> { Data = result });
    }

    /// <summary>
    /// GET /home/manufacturers
    /// Returns a simple list of manufacturers for the home page.
    /// </summary>
    [HttpGet("manufacturers")]
    [ProducesResponseType(typeof(ApiResponse<IList<ManufacturerSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHomepageManufacturers()
    {
        // take first page of manufacturers
        var manufacturersPaged = await _manufacturerService.GetAllManufacturersAsync();
        var manufacturers = manufacturersPaged;

        var data = new List<ManufacturerSummaryDto>();
        foreach (var m in manufacturers)
        {
            var seName = await _urlRecordService.GetSeNameAsync(m, 0, true, false);
            data.Add(new ManufacturerSummaryDto
            {
                Id = m.Id,
                Name = m.Name,
                SeName = seName
            });
        }

        return Ok(new ApiResponse<IList<ManufacturerSummaryDto>> { Data = data });
    }

    /// <summary>
    /// GET /home/featureproducts
    /// Returns products marked as displayed on home page (simplified featured products).
    /// </summary>
    [HttpGet("featureproducts")]
    [ProducesResponseType(typeof(ApiResponse<IList<ProductSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHomepageFeaturedProducts()
    {
        var products = await _productService.GetAllProductsDisplayedOnHomepageAsync();

        var data = new List<ProductSummaryDto>();
        foreach (var product in products)
        {
            var seName = await _urlRecordService.GetSeNameAsync(product, 0, true, false);
            data.Add(new ProductSummaryDto
            {
                Id = product.Id,
                Name = product.Name,
                SeName = seName,
                ShortDescription = product.ShortDescription,
                Sku = product.Sku
            });
        }

        return Ok(new ApiResponse<IList<ProductSummaryDto>> { Data = data });
    }

    /// <summary>
    /// GET /home/bestsellerproducts
    /// Simplified: for now, reuse products displayed on home page.
    /// </summary>
    [HttpGet("bestsellerproducts")]
    [ProducesResponseType(typeof(ApiResponse<IList<ProductSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHomepageBestSellerProducts()
    {
        var products = await _productService.GetAllProductsDisplayedOnHomepageAsync();

        var data = new List<ProductSummaryDto>();
        foreach (var product in products)
        {
            var seName = await _urlRecordService.GetSeNameAsync(product, 0, true, false);
            data.Add(new ProductSummaryDto
            {
                Id = product.Id,
                Name = product.Name,
                SeName = seName,
                ShortDescription = product.ShortDescription,
                Sku = product.Sku
            });
        }

        return Ok(new ApiResponse<IList<ProductSummaryDto>> { Data = data });
    }

    /// <summary>
    /// GET /home/categorytree
    /// Returns a simplified category tree (id, name, seName, children).
    /// </summary>
    [HttpGet("categorytree")]
    [ProducesResponseType(typeof(ApiResponse<IList<CategoryTreeNodeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryTree()
    {
        var allCategories = await _categoryService.GetAllCategoriesAsync(showHidden: false);
        var lookup = allCategories.GroupBy(c => c.ParentCategoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        async Task<CategoryTreeNodeDto> BuildNodeAsync(Category category)
        {
            var seName = await _urlRecordService.GetSeNameAsync(category, 0, true, false);

            var children = lookup.ContainsKey(category.Id)
                ? await Task.WhenAll(lookup[category.Id].Select(BuildNodeAsync))
                : Array.Empty<CategoryTreeNodeDto>();

            return new CategoryTreeNodeDto
            {
                Id = category.Id,
                Name = category.Name,
                SeName = seName,
                SubCategories = children.ToList()
            };
        }

        var rootCategories = lookup.ContainsKey(0)
            ? lookup[0]
            : new List<Category>();

        var tree = await Task.WhenAll(rootCategories.Select(BuildNodeAsync));

        return Ok(new ApiResponse<IList<CategoryTreeNodeDto>> { Data = tree.ToList() });
    }

    /// <summary>
    /// GET /home/applandingsetting
    /// Returns app landing settings for mobile app homepage.
    /// Uses mobile app specific settings instead of website settings.
    /// </summary>
    [HttpGet("applandingsetting")]
    [ProducesResponseType(typeof(ApiResponse<AppLandingSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppLandingSetting()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var wishlist = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, store.Id);

        // Load mobile app specific settings
        var mobileAppSettings = await _settingService.LoadSettingAsync<MobileAppSettings>();

        var data = new AppLandingSettingsDto
        {
            ShowHomepageSlider = false,
            // Use mobile app specific settings instead of website settings
            ShowFeaturedProducts = mobileAppSettings.ShowFeaturedProducts,
            ShowBestsellersOnHomepage = mobileAppSettings.ShowBestsellersOnHomepage,
            ShowHomepageCategoryProducts = mobileAppSettings.ShowHomepageCategoryProducts,
            ShowManufacturers = mobileAppSettings.ShowManufacturers,
            Rtl = (await _workContext.GetWorkingLanguageAsync()).Rtl,
            AndroidVersion = string.Empty,
            AndriodForceUpdate = false,
            PlayStoreUrl = string.Empty,
            IOSVersion = string.Empty,
            IOSForceUpdate = false,
            AppStoreUrl = string.Empty,
            LogoUrl = string.Empty,
            TotalShoppingCartProducts = cart.Sum(i => i.Quantity),
            TotalWishListProducts = wishlist.Sum(i => i.Quantity),
            NewProductsEnabled = _catalogSettings.NewProductsEnabled,
            RecentlyViewedProductsEnabled = _catalogSettings.RecentlyViewedProductsEnabled,
            CompareProductsEnabled = _catalogSettings.CompareProductsEnabled,
            AllowCustomersToUploadAvatars = _customerSettings.AllowCustomersToUploadAvatars,
            AnonymousCheckoutAllowed = _orderSettings.AnonymousCheckoutAllowed
        };

        return Ok(new ApiResponse<AppLandingSettingsDto> { Data = data });
    }
}


