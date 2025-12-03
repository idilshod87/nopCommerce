using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Plugin.Misc.WebApi.Frontend.DTOs;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Models.Customer;
using Nop.Web.Models.Common;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Public API for customer account info and avatar, aligned with NopStation Cart API routes.
/// </summary>
[ApiController]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
[Route("public-api/customer")]
public class CustomerController : ControllerBase
{
    private readonly IWorkContext _workContext;
    private readonly ICustomerService _customerService;
    private readonly IAddressService _addressService;
    private readonly ICustomerModelFactory _customerModelFactory;
    private readonly CustomerSettings _customerSettings;
    private readonly ILocalizationService _localizationService;
    private readonly IPictureService _pictureService;
    private readonly IDownloadService _downloadService;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly MediaSettings _mediaSettings;

    public CustomerController(
        IWorkContext workContext,
        ICustomerService customerService,
        IAddressService addressService,
        ICustomerModelFactory customerModelFactory,
        CustomerSettings customerSettings,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IDownloadService downloadService,
        IGenericAttributeService genericAttributeService,
        MediaSettings mediaSettings)
    {
        _workContext = workContext;
        _customerService = customerService;
        _addressService = addressService;
        _customerModelFactory = customerModelFactory;
        _customerSettings = customerSettings;
        _localizationService = localizationService;
        _pictureService = pictureService;
        _downloadService = downloadService;
        _genericAttributeService = genericAttributeService;
        _mediaSettings = mediaSettings;
    }

    private async Task<Customer?> GetCurrentRegisteredCustomerAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return null;
        return customer;
    }

    #region Info

    /// <summary>
    /// GET /customer/info
    /// Returns current customer account info (CustomerInfoModel).
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(ApiResponse<CustomerInfoModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetInfo()
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized();

        var model = new CustomerInfoModel();
        model = await _customerModelFactory.PrepareCustomerInfoModelAsync(model, customer, false);

        return Ok(new ApiResponse<CustomerInfoModel> { Data = model });
    }

    /// <summary>
    /// POST /customer/info
    /// Updates basic customer account info. Body: CustomerInfoModel (subset is enough).
    /// </summary>
    [HttpPost("info")]
    [ProducesResponseType(typeof(ApiResponse<CustomerInfoModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateInfo([FromBody] CustomerInfoModel model)
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized();

        if (model == null)
            return BadRequest(new { Message = "Model is required" });

        // Update basic profile fields (simplified subset of CustomerController.Info)
        if (_customerSettings.FirstNameEnabled)
            customer.FirstName = model.FirstName;
        if (_customerSettings.LastNameEnabled)
            customer.LastName = model.LastName;
        if (_customerSettings.GenderEnabled)
            customer.Gender = model.Gender;
        if (_customerSettings.DateOfBirthEnabled)
            customer.DateOfBirth = model.ParseDateOfBirth();
        if (_customerSettings.CompanyEnabled)
            customer.Company = model.Company;
        if (_customerSettings.StreetAddressEnabled)
            customer.StreetAddress = model.StreetAddress;
        if (_customerSettings.StreetAddress2Enabled)
            customer.StreetAddress2 = model.StreetAddress2;
        if (_customerSettings.ZipPostalCodeEnabled)
            customer.ZipPostalCode = model.ZipPostalCode;
        if (_customerSettings.CityEnabled)
            customer.City = model.City;
        if (_customerSettings.CountyEnabled)
            customer.County = model.County;
        if (_customerSettings.CountryEnabled)
            customer.CountryId = model.CountryId;
        if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
            customer.StateProvinceId = model.StateProvinceId;
        if (_customerSettings.PhoneEnabled)
            customer.Phone = model.Phone;
        if (_customerSettings.FaxEnabled)
            customer.Fax = model.Fax;

        await _customerService.UpdateCustomerAsync(customer);

        var updated = await _customerModelFactory.PrepareCustomerInfoModelAsync(new CustomerInfoModel(), customer, false);
        return Ok(new ApiResponse<CustomerInfoModel> { Data = updated });
    }

    #endregion

    #region Avatar

    /// <summary>
    /// GET /customer/avatar
    /// Returns avatar URL and related settings (CustomerAvatarModel).
    /// </summary>
    [HttpGet("avatar")]
    [ProducesResponseType(typeof(ApiResponse<CustomerAvatarModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAvatar()
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized();

        if (!_customerSettings.AllowCustomersToUploadAvatars)
            return Ok(new ApiResponse<CustomerAvatarModel> { Data = new CustomerAvatarModel { AvatarUrl = string.Empty } });

        var model = new CustomerAvatarModel();
        model = await _customerModelFactory.PrepareCustomerAvatarModelAsync(model);

        return Ok(new ApiResponse<CustomerAvatarModel> { Data = model });
    }

        /// <summary>
        /// POST /customer/uploadavatar
        /// Multipart/form-data with file field "file".
        /// </summary>
        [HttpPost("uploadavatar")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized();

        if (!_customerSettings.AllowCustomersToUploadAvatars)
            return BadRequest(new { Message = "Avatar upload is disabled" });

        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "No file uploaded" });

        var contentType = file.ContentType?.ToLowerInvariant();
        if (contentType != null && !contentType.Equals("image/jpeg") && !contentType.Equals("image/gif") && !contentType.Equals("image/png"))
        {
            var msg = await _localizationService.GetResourceAsync("Account.Avatar.UploadRules");
            return BadRequest(new { Message = msg });
        }

        // size check
        var avatarMaxSize = _customerSettings.AvatarMaximumSizeBytes;
        if (file.Length > avatarMaxSize)
        {
            var msg = string.Format(await _localizationService.GetResourceAsync("Account.Avatar.MaximumUploadedFileSize"), avatarMaxSize);
            return BadRequest(new { Message = msg });
        }

        var currentAvatarId = await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.AvatarPictureIdAttribute);
        var customerAvatar = await _pictureService.GetPictureByIdAsync(currentAvatarId);

        var fileBinary = await _downloadService.GetDownloadBitsAsync(file);
        if (customerAvatar != null)
            customerAvatar = await _pictureService.UpdatePictureAsync(customerAvatar.Id, fileBinary, contentType, null);
        else
            customerAvatar = await _pictureService.InsertPictureAsync(fileBinary, contentType, null);

        var customerAvatarId = customerAvatar?.Id ?? 0;
        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AvatarPictureIdAttribute, customerAvatarId);

        var avatarUrl = await _pictureService.GetPictureUrlAsync(
            customerAvatarId,
            _mediaSettings.AvatarPictureSize,
            false);

        return Ok(new ApiResponse<string> { Data = avatarUrl });
    }

    /// <summary>
    /// POST /customer/removeavatar
    /// </summary>
    [HttpPost("removeavatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveAvatar()
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized();

        if (!_customerSettings.AllowCustomersToUploadAvatars)
            return BadRequest(new { Message = "Avatar upload is disabled" });

        var avatarId = await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.AvatarPictureIdAttribute);
        var customerAvatar = await _pictureService.GetPictureByIdAsync(avatarId);
        if (customerAvatar != null)
            await _pictureService.DeletePictureAsync(customerAvatar);

        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AvatarPictureIdAttribute, 0);

        return Ok(new ApiResponse<OperationResultDto>
        {
            Data = new OperationResultDto { Success = true }
        });
    }

    #endregion

    #region Addresses

    /// <summary>
    /// GET /customer/addresses
    /// Returns list of customer addresses.
    /// </summary>
    [HttpGet("addresses")]
    [ProducesResponseType(typeof(ApiResponse<CustomerAddressListModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAddresses()
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized();

        var model = await _customerModelFactory.PrepareCustomerAddressListModelAsync();
        return Ok(new ApiResponse<CustomerAddressListModel> { Data = model });
    }

    /// <summary>
    /// POST /customer/addressadd
    /// Adds new address for current customer.
    /// Body: AddressModel (упрощённо, без custom attributes).
    /// </summary>
    [HttpPost("addressadd")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddressAdd([FromBody] AddressModel model)
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized();

        if (model == null)
            return BadRequest(new { Message = "Address model is required" });

        var address = model.ToEntity();
        address.CustomAttributes = string.Empty;
        address.CreatedOnUtc = DateTime.UtcNow;

        if (address.CountryId == 0)
            address.CountryId = null;
        if (address.StateProvinceId == 0)
            address.StateProvinceId = null;

        await _addressService.InsertAddressAsync(address);
        await _customerService.InsertCustomerAddressAsync(customer, address);

        return Ok(new ApiResponse<OperationResultDto>
        {
            Data = new OperationResultDto { Success = true }
        });
    }

    /// <summary>
    /// POST /customer/addressedit/{addressId}
    /// Updates existing address of current customer.
    /// Body: AddressModel.
    /// </summary>
    [HttpPost("addressedit/{addressId:int}")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddressEdit(int addressId, [FromBody] AddressModel model)
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized();

        if (model == null)
            return BadRequest(new { Message = "Address model is required" });

        var address = await _customerService.GetCustomerAddressAsync(customer.Id, addressId);
        if (address == null)
            return BadRequest(new { Message = "Address not found" });

        address = model.ToEntity(address);
        if (address.CountryId == 0)
            address.CountryId = null;
        if (address.StateProvinceId == 0)
            address.StateProvinceId = null;

        await _addressService.UpdateAddressAsync(address);

        return Ok(new ApiResponse<OperationResultDto>
        {
            Data = new OperationResultDto { Success = true }
        });
    }

    /// <summary>
    /// POST /customer/addressdelete/{addressId}
    /// Deletes address of current customer.
    /// </summary>
    [HttpPost("addressdelete/{addressId:int}")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddressDelete(int addressId)
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized();

        var address = await _customerService.GetCustomerAddressAsync(customer.Id, addressId);
        if (address != null)
        {
            await _customerService.RemoveCustomerAddressAsync(customer, address);
            await _customerService.UpdateCustomerAsync(customer);
            await _addressService.DeleteAddressAsync(address);
        }

        return Ok(new ApiResponse<OperationResultDto>
        {
            Data = new OperationResultDto { Success = true }
        });
    }

    #endregion

    #region Downloadable products

    /// <summary>
    /// GET /customer/downloadableproducts
    /// Returns list of downloadable products for current customer.
    /// </summary>
    [HttpGet("downloadableproducts")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDownloadableProductsModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDownloadableProducts()
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized();

        var model = await _customerModelFactory.PrepareCustomerDownloadableProductsModelAsync();
        return Ok(new ApiResponse<CustomerDownloadableProductsModel> { Data = model });
    }

    #endregion
}

