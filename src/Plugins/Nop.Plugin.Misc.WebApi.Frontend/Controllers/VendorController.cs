using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Vendors;
using Nop.Plugin.Misc.WebApi.Frontend.DTOs;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Vendors;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Public API for vendors.
/// </summary>
[ApiController]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
[Route("public-api/vendor")]
public class VendorController : ControllerBase
{
    private readonly ICatalogModelFactory _catalogModelFactory;
    private readonly IVendorService _vendorService;
    private readonly IUrlRecordService _urlRecordService;
    private readonly ILocalizationService _localizationService;
    private readonly IPictureService _pictureService;
    private readonly VendorSettings _vendorSettings;
    private readonly MediaSettings _mediaSettings;

    public VendorController(
        ICatalogModelFactory catalogModelFactory,
        IVendorService vendorService,
        IUrlRecordService urlRecordService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        VendorSettings vendorSettings,
        MediaSettings mediaSettings)
    {
        _catalogModelFactory = catalogModelFactory;
        _vendorService = vendorService;
        _urlRecordService = urlRecordService;
        _localizationService = localizationService;
        _pictureService = pictureService;
        _vendorSettings = vendorSettings;
        _mediaSettings = mediaSettings;
    }

    /// <summary>
    /// GET /vendor/{id}
    /// Get products by vendor.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<VendorModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVendor(int id, [FromQuery] CatalogProductsCommand command)
    {
        var vendor = await _vendorService.GetVendorByIdAsync(id);
        if (vendor == null || !vendor.Active)
            return NotFound(new { Message = "Vendor not found" });

        var model = await _catalogModelFactory.PrepareVendorModelAsync(vendor, command);

        return Ok(new ApiResponse<VendorModel> { Data = model });
    }

    /// <summary>
    /// GET /vendor/all
    /// Get all vendors.
    /// </summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(ApiResponse<IList<VendorModel>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllVendors()
    {
        var models = await _catalogModelFactory.PrepareVendorAllModelsAsync();
        return Ok(new ApiResponse<IList<VendorModel>> { Data = models });
    }

    /// <summary>
    /// GET /vendor
    /// Get vendors with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedVendorsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVendors([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        // Validate pagination parameters
        if (pageNumber < 1)
            pageNumber = 1;
        if (pageSize < 1)
            pageSize = 10;
        if (pageSize > 100)
            pageSize = 100; // Limit max page size

        // Get paginated vendors (only active, not deleted)
        var vendors = await _vendorService.GetAllVendorsAsync(
            pageIndex: pageNumber - 1,
            pageSize: pageSize,
            showHidden: false);

        // Prepare vendor models
        var vendorModels = new List<VendorModel>();
        foreach (var vendor in vendors)
        {
            var vendorModel = new VendorModel
            {
                Id = vendor.Id,
                Name = await _localizationService.GetLocalizedAsync(vendor, x => x.Name),
                Description = await _localizationService.GetLocalizedAsync(vendor, x => x.Description),
                MetaKeywords = await _localizationService.GetLocalizedAsync(vendor, x => x.MetaKeywords),
                MetaDescription = await _localizationService.GetLocalizedAsync(vendor, x => x.MetaDescription),
                MetaTitle = await _localizationService.GetLocalizedAsync(vendor, x => x.MetaTitle),
                SeName = await _urlRecordService.GetSeNameAsync(vendor),
                AllowCustomersToContactVendors = _vendorSettings.AllowCustomersToContactVendors,
                PictureModel = await PrepareVendorPictureModelAsync(vendor)
            };

            vendorModels.Add(vendorModel);
        }

        // Create paginated response
        var result = new PaginatedVendorsDto
        {
            Vendors = vendorModels
        };
        result.LoadPagedList(vendors);

        return Ok(new ApiResponse<PaginatedVendorsDto> { Data = result });
    }

    /// <summary>
    /// Prepare vendor picture model
    /// </summary>
    private async Task<PictureModel> PrepareVendorPictureModelAsync(Vendor vendor)
    {
        var pictureSize = _mediaSettings.VendorThumbPictureSize;
        var picture = await _pictureService.GetPictureByIdAsync(vendor.PictureId);
        string fullSizeImageUrl, imageUrl;

        (fullSizeImageUrl, picture) = await _pictureService.GetPictureUrlAsync(picture);
        (imageUrl, _) = await _pictureService.GetPictureUrlAsync(picture, pictureSize);

        var localizedName = await _localizationService.GetLocalizedAsync(vendor, x => x.Name);

        return new PictureModel
        {
            FullSizeImageUrl = fullSizeImageUrl,
            ImageUrl = imageUrl,
            Title = string.Format(await _localizationService.GetResourceAsync("Media.Vendor.ImageLinkTitleFormat"), localizedName),
            AlternateText = string.Format(await _localizationService.GetResourceAsync("Media.Vendor.ImageAlternateTextFormat"), localizedName)
        };
    }
}


