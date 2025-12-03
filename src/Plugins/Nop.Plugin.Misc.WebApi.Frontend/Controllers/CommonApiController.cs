using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Plugin.Misc.WebApi.Frontend.DTOs;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Models.Directory;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Public API for common/settings endpoints (strings, app landing, language, currency).
/// </summary>
[ApiController]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
[Route("public-api/common")]
public class CommonController : ControllerBase
{
    private readonly ILocalizationService _localizationService;
    private readonly ILanguageService _languageService;
    private readonly IWorkContext _workContext;
    private readonly ICurrencyService _currencyService;
    private readonly IStoreContext _storeContext;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly CustomerSettings _customerSettings;
    private readonly CatalogSettings _catalogSettings;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly OrderSettings _orderSettings;
    private readonly IStoreService _storeService;
    private readonly ICountryModelFactory _countryModelFactory;

    public CommonController(
        ILocalizationService localizationService,
        ILanguageService languageService,
        IWorkContext workContext,
        ICurrencyService currencyService,
        IStoreContext storeContext,
        IGenericAttributeService genericAttributeService,
        IShoppingCartService shoppingCartService,
        CustomerSettings customerSettings,
        CatalogSettings catalogSettings,
        ShoppingCartSettings shoppingCartSettings,
        OrderSettings orderSettings,
        IStoreService storeService,
        ICountryModelFactory countryModelFactory)
    {
        _localizationService = localizationService;
        _languageService = languageService;
        _workContext = workContext;
        _currencyService = currencyService;
        _storeContext = storeContext;
        _genericAttributeService = genericAttributeService;
        _shoppingCartService = shoppingCartService;
        _customerSettings = customerSettings;
        _catalogSettings = catalogSettings;
        _shoppingCartSettings = shoppingCartSettings;
        _orderSettings = orderSettings;
        _storeService = storeService;
        _countryModelFactory = countryModelFactory;
    }

    /// <summary>
    /// GET /common/getstringresources/{languageId}
    /// Returns all string resources (key/value) for given language.
    /// </summary>
    [HttpGet("getstringresources/{languageId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IList<StringResourceItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStringResources(int languageId)
    {
        var resources = await _localizationService.GetAllResourceValuesAsync(languageId, null);
        var data = resources
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new StringResourceItemDto
            {
                Key = kvp.Key,
                Value = kvp.Value.Value
            })
            .ToList();

        return Ok(new ApiResponse<IList<StringResourceItemDto>> { Data = data });
    }

    /// <summary>
    /// GET /home/applandingsetting
    /// Simplified app landing settings for mobile app.
    /// </summary>
    [HttpGet("~/public-api/home/applandingsetting")]
    [ProducesResponseType(typeof(ApiResponse<AppLandingSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppLandingSetting()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var wishlist = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, store.Id);

        var data = new AppLandingSettingsDto
        {
            ShowHomepageSlider = false,
            ShowFeaturedProducts = true,
            ShowBestsellersOnHomepage = _catalogSettings.ShowBestsellersOnHomepage,
            ShowHomepageCategoryProducts = true,
            ShowManufacturers = true,
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

    /// <summary>
    /// POST /appstart
    /// Dummy endpoint to accept device registration (no-op on server).
    /// </summary>
    [HttpPost("~/public-api/appstart")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto>), StatusCodes.Status200OK)]
    public IActionResult AppStart()
    {
        return Ok(new ApiResponse<OperationResultDto>
        {
            Data = new OperationResultDto { Success = true }
        });
    }

    /// <summary>
    /// GET /common/setlanguage/{languageId}
    /// Sets working language for current customer/session.
    /// </summary>
    [HttpGet("setlanguage/{languageId:int}")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetLanguage(int languageId)
    {
        var language = await _languageService.GetLanguageByIdAsync(languageId);
        if (!language?.Published ?? false)
            language = await _workContext.GetWorkingLanguageAsync();

        await _workContext.SetWorkingLanguageAsync(language);

        return Ok(new ApiResponse<OperationResultDto>
        {
            Data = new OperationResultDto { Success = true }
        });
    }

    /// <summary>
    /// POST /common/setcurrency/{currencyId}
    /// Sets working currency for current customer/session.
    /// </summary>
    [HttpPost("setcurrency/{currencyId:int}")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetCurrency(int currencyId)
    {
        var currency = await _currencyService.GetCurrencyByIdAsync(currencyId);
        if (currency != null)
            await _workContext.SetWorkingCurrencyAsync(currency);

        return Ok(new ApiResponse<OperationResultDto>
        {
            Data = new OperationResultDto { Success = currency != null }
        });
    }

    /// <summary>
    /// GET /country/getstatesbycountryid/{countryId}
    /// Returns list of states/provinces for given country.
    /// </summary>
    [HttpGet("~/public-api/country/getstatesbycountryid/{countryId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IList<StateProvinceModel>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatesByCountryId(int countryId)
    {
        var model = await _countryModelFactory.GetStatesByCountryIdAsync(countryId, false);
        return Ok(new ApiResponse<IList<StateProvinceModel>> { Data = model });
    }
}


