using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Http;
using Nop.Services.Catalog;
using Nop.Services.Media;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Public API endpoints for product sample downloads.
/// Mirrors behavior of standard DownloadController.Sample, but under /api/download.
/// </summary>
[ApiController]
[IgnoreAntiforgeryToken]
[Route("public-api/download")]
public class DownloadController : ControllerBase
{
    private readonly IDownloadService _downloadService;
    private readonly IProductService _productService;

    public DownloadController(
        IDownloadService downloadService,
        IProductService productService)
    {
        _downloadService = downloadService;
            _productService = productService;
    }

    /// <summary>
    /// GET /api/download/sample/{productId}
    /// </summary>
    [HttpGet("sample/{productId:int}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Sample(int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null)
            return NotFound("Product not found.");

        if (!product.HasSampleDownload)
            return NotFound("Product doesn't have a sample download.");

        var download = await _downloadService.GetDownloadByIdAsync(product.SampleDownloadId);
        if (download == null)
            return NotFound("Sample download is not available any more.");

        if (download.UseDownloadUrl)
            return Redirect(download.DownloadUrl);

        if (download.DownloadBinary == null)
            return NotFound("Download data is not available any more.");

        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : product.Id.ToString();
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
            ? download.ContentType
            : MimeTypes.ApplicationOctetStream;

        return File(download.DownloadBinary, contentType, fileName + download.Extension);
    }

        /// <summary>
        /// GET /download/getfileupload/{downloadId}
        /// Returns previously uploaded file (by GUID).
        /// </summary>
        [HttpGet("getfileupload/{downloadId:guid}")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFileUpload(Guid downloadId)
        {
            var download = await _downloadService.GetDownloadByGuidAsync(downloadId);
            if (download == null)
                return NotFound("Download is not available any more.");

            if (download.UseDownloadUrl)
                return Redirect(download.DownloadUrl);

            if (download.DownloadBinary == null)
                return NotFound("Download data is not available any more.");

            var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : downloadId.ToString();
            var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
                ? download.ContentType
                : MimeTypes.ApplicationOctetStream;

            return File(download.DownloadBinary, contentType, fileName + download.Extension);
        }
}


