using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
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
    private readonly ICountryService _countryService;
    private readonly IStateProvinceService _stateProvinceService;

    public CommonController(
        ILocalizationService localizationService,
        ILanguageService languageService,
        IWorkContext workContext,
        ICurrencyService currencyService,
        ICountryModelFactory countryModelFactory,
        ICountryService countryService,
        IStateProvinceService stateProvinceService)
    {
        _localizationService = localizationService;
        _languageService = languageService;
        _workContext = workContext;
        _currencyService = currencyService;
        _countryModelFactory = countryModelFactory;
        _countryService = countryService;
        _stateProvinceService = stateProvinceService;
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

    /// <summary>
    /// GET /common/countries
    /// Returns paged list of countries with optional search by name or ISO codes.
    /// </summary>
    [HttpGet("countries")]
    [ProducesResponseType(typeof(PagedResultDto<CountryListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCountries([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;
        var countries = await _countryService.GetAllCountriesAsync(languageId, false);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            countries = countries
                .Where(c =>
                    (!string.IsNullOrEmpty(c.Name) && c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.TwoLetterIsoCode) && c.TwoLetterIsoCode.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.ThreeLetterIsoCode) && c.ThreeLetterIsoCode.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var totalCount = countries.Count;
        var pageIndex = page - 1;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedCountries = countries.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        var items = new List<CountryListItemDto>();
        foreach (var c in pagedCountries)
        {
            var localizedName = await _localizationService.GetLocalizedAsync(c, x => x.Name, languageId);
            items.Add(new CountryListItemDto
            {
                Id = c.Id,
                Name = localizedName,
                TwoLetterIsoCode = c.TwoLetterIsoCode ?? string.Empty,
                ThreeLetterIsoCode = c.ThreeLetterIsoCode ?? string.Empty,
                AllowsBilling = c.AllowsBilling,
                AllowsShipping = c.AllowsShipping,
                SubjectToVat = c.SubjectToVat
            });
        }

        var result = new PagedResultDto<List<CountryListItemDto>>
        {
            Data = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return Ok(result);
    }

    /// <summary>
    /// GET /common/stateprovinces
    /// Returns paged list of state/provinces optionally filtered by country.
    /// </summary>
    [HttpGet("stateprovinces")]
    [ProducesResponseType(typeof(PagedResultDto<StateProvinceListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStateProvinces([FromQuery] int? countryId = null, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;
        var states = countryId.HasValue && countryId.Value > 0
            ? await _stateProvinceService.GetStateProvincesByCountryIdAsync(countryId.Value, languageId, false)
            : await _stateProvinceService.GetStateProvincesAsync(false);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            states = states
                .Where(s => !string.IsNullOrEmpty(s.Name) && s.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var totalCount = states.Count;
        var pageIndex = page - 1;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedStates = states.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        var items = new List<StateProvinceListItemDto>();
        foreach (var s in pagedStates)
        {
            var localizedName = await _localizationService.GetLocalizedAsync(s, x => x.Name, languageId);
            items.Add(new StateProvinceListItemDto
            {
                Id = s.Id,
                CountryId = s.CountryId,
                Name = localizedName,
                Abbreviation = s.Abbreviation ?? string.Empty,
                Published = s.Published,
                DisplayOrder = s.DisplayOrder
            });
        }

        var result = new PagedResultDto<List<StateProvinceListItemDto>>
        {
            Data = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return Ok(result);
    }
}


