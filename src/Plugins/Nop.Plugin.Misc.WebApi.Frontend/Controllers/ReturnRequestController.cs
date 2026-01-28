using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Http;
using Nop.Core.Http.Extensions;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.WebApi.Frontend.DTOs;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Models.Order;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Public API for return requests, aligned with NopStation Cart API routes.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
[Route("public-api/returnrequest")]
public class ReturnRequestController : ControllerBase
{
    #region Fields

    private readonly ICustomerService _customerService;
    private readonly ICustomNumberFormatter _customNumberFormatter;
    private readonly IDownloadService _downloadService;
    private readonly ILocalizationService _localizationService;
    private readonly INopFileProvider _fileProvider;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly IOrderService _orderService;
    private readonly IReturnRequestModelFactory _returnRequestModelFactory;
    private readonly IReturnRequestService _returnRequestService;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly IWorkflowMessageService _workflowMessageService;
    private readonly LocalizationSettings _localizationSettings;
    private readonly OrderSettings _orderSettings;

    #endregion

    public ReturnRequestController(
        ICustomerService customerService,
        ICustomNumberFormatter customNumberFormatter,
        IDownloadService downloadService,
        ILocalizationService localizationService,
        INopFileProvider fileProvider,
        IOrderProcessingService orderProcessingService,
        IOrderService orderService,
        IReturnRequestModelFactory returnRequestModelFactory,
        IReturnRequestService returnRequestService,
        IStoreContext storeContext,
        IWorkContext workContext,
        IWorkflowMessageService workflowMessageService,
        LocalizationSettings localizationSettings,
        OrderSettings orderSettings)
    {
        _customerService = customerService;
        _customNumberFormatter = customNumberFormatter;
        _downloadService = downloadService;
        _localizationService = localizationService;
        _fileProvider = fileProvider;
        _orderProcessingService = orderProcessingService;
        _orderService = orderService;
        _returnRequestModelFactory = returnRequestModelFactory;
        _returnRequestService = returnRequestService;
        _storeContext = storeContext;
        _workContext = workContext;
        _workflowMessageService = workflowMessageService;
        _localizationSettings = localizationSettings;
        _orderSettings = orderSettings;
    }

    private async Task<Customer?> GetCurrentRegisteredCustomerAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return null;
        return customer;
    }

    /// <summary>
    /// POST /returnrequest/returnrequest/{orderId}
    /// Create a return request for an order.
    /// </summary>
    [HttpPost("returnrequest/{orderId:int}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<SubmitReturnRequestModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitReturnRequest(int orderId, [FromBody] SubmitReturnRequestApiDto request)
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized(new { Message = "Authentication required" });

        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.Deleted || order.CustomerId != customer.Id)
            return NotFound(new { Message = "Order not found" });

        if (!await _orderProcessingService.IsReturnRequestAllowedAsync(order))
            return BadRequest(new { Message = "Return requests are not allowed for this order" });

        var count = 0;
        var downloadId = 0;

        if (_orderSettings.ReturnRequestsAllowFiles && request.UploadedFileGuid.HasValue)
        {
            var download = await _downloadService.GetDownloadByGuidAsync(request.UploadedFileGuid.Value);
            if (download != null)
                downloadId = download.Id;
        }

        var orderItems = await _orderService.GetOrderItemsAsync(order.Id, isNotReturnable: false);
        var itemsById = orderItems.ToDictionary(x => x.Id);

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                continue;

            if (!itemsById.TryGetValue(item.OrderItemId, out var orderItem))
                continue;

            var rrr = await _returnRequestService.GetReturnRequestReasonByIdAsync(request.ReturnRequestReasonId);
            var rra = await _returnRequestService.GetReturnRequestActionByIdAsync(request.ReturnRequestActionId);
            var store = await _storeContext.GetCurrentStoreAsync();

            var rr = new ReturnRequest
            {
                CustomNumber = string.Empty,
                StoreId = store.Id,
                OrderItemId = orderItem.Id,
                Quantity = item.Quantity,
                CustomerId = customer.Id,
                ReasonForReturn = rrr != null ? await _localizationService.GetLocalizedAsync(rrr, x => x.Name) : "not available",
                RequestedAction = rra != null ? await _localizationService.GetLocalizedAsync(rra, x => x.Name) : "not available",
                CustomerComments = request.Comments,
                UploadedFileId = downloadId,
                StaffNotes = string.Empty,
                ReturnRequestStatus = ReturnRequestStatus.Pending,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            await _returnRequestService.InsertReturnRequestAsync(rr);

            rr.CustomNumber = _customNumberFormatter.GenerateReturnRequestCustomNumber(rr);
            await _customerService.UpdateCustomerAsync(customer);
            await _returnRequestService.UpdateReturnRequestAsync(rr);

            await _workflowMessageService.SendNewReturnRequestStoreOwnerNotificationAsync(rr, orderItem, order, _localizationSettings.DefaultAdminLanguageId);
            await _workflowMessageService.SendNewReturnRequestCustomerNotificationAsync(rr, orderItem, order);

            count++;
        }

        var model = await _returnRequestModelFactory.PrepareSubmitReturnRequestModelAsync(new SubmitReturnRequestModel(), order);
        model.Comments = request.Comments ?? string.Empty;
        model.ReturnRequestReasonId = request.ReturnRequestReasonId;
        model.ReturnRequestActionId = request.ReturnRequestActionId;
        model.UploadedFileGuid = request.UploadedFileGuid ?? Guid.Empty;
        model.Result = count > 0
            ? await _localizationService.GetResourceAsync("ReturnRequests.Submitted")
            : await _localizationService.GetResourceAsync("ReturnRequests.NoItemsSubmitted");

        return Ok(new ApiResponse<SubmitReturnRequestModel> { Data = model });
    }

    /// <summary>
    /// GET /returnrequest/history
    /// Get return request history for current customer.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<CustomerReturnRequestsModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory()
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized(new { Message = "Authentication required" });

        var model = await _returnRequestModelFactory.PrepareCustomerReturnRequestsModelAsync();

        return Ok(new ApiResponse<CustomerReturnRequestsModel> { Data = model });
    }

    /// <summary>
    /// POST /returnrequest/uploadfile
    /// Upload a file for a return request.
    /// </summary>
    [HttpPost("uploadfile")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<UploadFileResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile()
    {
        if (!_orderSettings.ReturnRequestsEnabled || !_orderSettings.ReturnRequestsAllowFiles)
        {
            return BadRequest(new ApiResponse<UploadFileResultDto>
            {
                Data = new UploadFileResultDto
                {
                    Success = false,
                    Message = "File uploads for return requests are disabled",
                    DownloadGuid = Guid.Empty
                }
            });
        }

        var httpPostedFile = await Request.GetFirstOrDefaultFileAsync();
        if (httpPostedFile == null)
        {
            return BadRequest(new ApiResponse<UploadFileResultDto>
            {
                Data = new UploadFileResultDto
                {
                    Success = false,
                    Message = "No file uploaded",
                    DownloadGuid = Guid.Empty
                }
            });
        }

        var fileBinary = await _downloadService.GetDownloadBitsAsync(httpPostedFile);
        var fileName = _fileProvider.GetFileName(httpPostedFile.FileName);
        var contentType = httpPostedFile.ContentType;
        var fileExtension = _fileProvider.GetFileExtension(fileName);
        if (!string.IsNullOrEmpty(fileExtension))
            fileExtension = fileExtension.ToLowerInvariant();

        var validationFileMaximumSize = _orderSettings.ReturnRequestsFileMaximumSize;
        if (validationFileMaximumSize > 0)
        {
            var maxFileSizeBytes = validationFileMaximumSize * 1024;
            if (fileBinary.Length > maxFileSizeBytes)
            {
                var message = string.Format(await _localizationService.GetResourceAsync("ShoppingCart.MaximumUploadedFileSize"), validationFileMaximumSize);
                return BadRequest(new ApiResponse<UploadFileResultDto>
                {
                    Data = new UploadFileResultDto
                    {
                        Success = false,
                        Message = message,
                        DownloadGuid = Guid.Empty
                    }
                });
            }
        }

        var download = new Download
        {
            DownloadGuid = Guid.NewGuid(),
            UseDownloadUrl = false,
            DownloadUrl = string.Empty,
            DownloadBinary = fileBinary,
            ContentType = contentType,
            Filename = _fileProvider.GetFileNameWithoutExtension(fileName),
            Extension = fileExtension,
            IsNew = true
        };
        await _downloadService.InsertDownloadAsync(download);

        var downloadUrl = Url.RouteUrl(NopRouteNames.Standard.DOWNLOAD_GET_FILE_UPLOAD, new { downloadId = download.DownloadGuid });

        return Ok(new ApiResponse<UploadFileResultDto>
        {
            Data = new UploadFileResultDto
            {
                Success = true,
                Message = await _localizationService.GetResourceAsync("ShoppingCart.FileUploaded"),
                DownloadGuid = download.DownloadGuid,
                DownloadUrl = downloadUrl
            }
        });
    }
}


