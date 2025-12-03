#nullable enable
using System;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

public class ReturnRequestItemDto
{
    public int OrderItemId { get; set; }
    public int Quantity { get; set; }
}

public class SubmitReturnRequestApiDto
{
    public int ReturnRequestReasonId { get; set; }
    public int ReturnRequestActionId { get; set; }
    public string? Comments { get; set; }
    public Guid? UploadedFileGuid { get; set; }
    public IList<ReturnRequestItemDto> Items { get; set; } = new List<ReturnRequestItemDto>();
}

public class UploadFileResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid DownloadGuid { get; set; }
    public string? DownloadUrl { get; set; }
}


