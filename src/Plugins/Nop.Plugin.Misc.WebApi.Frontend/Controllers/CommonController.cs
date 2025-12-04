using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Localization;
using Nop.Plugin.Misc.WebApi.Frontend.DTOs;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Web.Factories;
using Nop.Web.Models.Directory;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Public API for common endpoints (strings, language, currency, country).
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
    private readonly ICountryModelFactory _countryModelFactory;

    public CommonController(
        ILocalizationService localizationService,
        ILanguageService languageService,
        IWorkContext workContext,
        ICurrencyService currencyService,
        ICountryModelFactory countryModelFactory)
    {
        _localizationService = localizationService;
        _languageService = languageService;
        _workContext = workContext;
        _currencyService = currencyService;
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


