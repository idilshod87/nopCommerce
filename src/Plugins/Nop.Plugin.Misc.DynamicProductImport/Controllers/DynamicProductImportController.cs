using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Vendors;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.DynamicProductImport.Domain;
using Nop.Plugin.Misc.DynamicProductImport.Models;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Plugin.Misc.DynamicProductImport.Services;

namespace Nop.Plugin.Misc.DynamicProductImport.Controllers;

[Area("Admin")]
[AutoValidateAntiforgeryToken]
public class DynamicProductImportController : BaseAdminController
{
    #region Fields

    private readonly IDynamicImportManager _importManager;
    private readonly ILocalizationService _localizationService;
    private readonly IMappingTemplateService _templateService;
    private readonly INopFileProvider _fileProvider;
    private readonly INotificationService _notificationService;
    private readonly IVendorService _vendorService;
    private readonly IWorkContext _workContext;
    private readonly VendorSettings _vendorSettings;

    private static readonly string[] RequiredFields = { "SKU", "Name", "Price", "StockQuantity", "Categories"};
    
    private static readonly string[] AvailableFields =
    {
        // Обязательные поля
        "SKU",
        "Name",
        "Price",
        "StockQuantity",
        
        // Категории и производители
        "Categories",
        "Manufacturers",
        
        // Описание
        "ShortDescription",
        "FullDescription",
        
        // Цены
        "OldPrice",
        "ProductCost",
        "MinimumCustomerEnteredPrice",
        "MaximumCustomerEnteredPrice",
        
        // Размеры и вес
        "Weight",
        "Length",
        "Width",
        "Height",
        
        // Идентификаторы
        "Gtin",
        "ManufacturerPartNumber",
        
        // Настройки товара
        "Published"
    };

    #endregion

    #region Ctor

    public DynamicProductImportController(
        IDynamicImportManager importManager,
        ILocalizationService localizationService,
        IMappingTemplateService templateService,
        INopFileProvider fileProvider,
        INotificationService notificationService,
        IVendorService vendorService,
        IWorkContext workContext,
        VendorSettings vendorSettings)
    {
        _importManager = importManager;
        _localizationService = localizationService;
        _templateService = templateService;
        _fileProvider = fileProvider;
        _notificationService = notificationService;
        _vendorService = vendorService;
        _workContext = workContext;
        _vendorSettings = vendorSettings;
    }

    #endregion

    #region Utilities

    protected virtual string GetDynamicImportDirectory()
    {
        var tempDirectory = _fileProvider.MapPath("~/App_Data/TempUploads");
        tempDirectory = _fileProvider.Combine(tempDirectory, "DynamicImport");
        _fileProvider.CreateDirectory(tempDirectory);

        return tempDirectory;
    }

    #endregion

    #region Methods

    [HttpPost]
    [CheckPermission(StandardPermission.Catalog.PRODUCTS_IMPORT_EXPORT)]
    public virtual async Task<IActionResult> ImportExcelMappingUpload(IFormFile importexcelfile)
    {
        var currentVendor = await _workContext.GetCurrentVendorAsync();
        if (currentVendor != null && !_vendorSettings.AllowVendorsToImportProducts)
            return AccessDeniedView();

        if (importexcelfile == null || importexcelfile.Length == 0)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Admin.Common.UploadFile") });

        var extension = _fileProvider.GetFileExtension(importexcelfile.FileName);
        if (!".xlsx".Equals(extension, StringComparison.InvariantCultureIgnoreCase))
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Admin.Common.ImportFromExcel.WrongFile") });

        var tempDirectory = GetDynamicImportDirectory();
        var fileId = Guid.NewGuid().ToString("N");
        var filePath = _fileProvider.Combine(tempDirectory, $"{fileId}.xlsx");

        try
        {
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await importexcelfile.CopyToAsync(stream);
            }

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
                return BadRequest(new { message = await _localizationService.GetResourceAsync("Admin.Common.ImportFromExcel.WrongFile") });

            var columns = new List<object>();
            var columnIndex = 1;
            while (true)
            {
                var headerCell = worksheet.Row(1).Cell(columnIndex);
                if (headerCell == null || string.IsNullOrEmpty(headerCell.GetString()))
                    break;

                columns.Add(new
                {
                    index = columnIndex,
                    name = headerCell.GetString(),
                    preview = worksheet.Row(2).Cell(columnIndex).GetString()
                });

                columnIndex++;
            }

            var vendors = new List<object>();
            if (currentVendor != null)
                vendors.Add(new { id = currentVendor.Id, name = currentVendor.Name });
            else
                vendors.AddRange((await _vendorService.GetAllVendorsAsync(showHidden: true)).Select(v => new { id = v.Id, name = v.Name }));

            return Json(new
            {
                success = true,
                fileId,
                columns,
                requiredFields = RequiredFields,
                availableFields = AvailableFields,
                vendors
            });
        }
        catch (Exception exc)
        {
            if (_fileProvider.FileExists(filePath))
                _fileProvider.DeleteFile(filePath);

            await _notificationService.ErrorNotificationAsync(exc);
            return BadRequest(new { success = false, message = exc.Message });
        }
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Catalog.PRODUCTS_IMPORT_EXPORT)]
    [IgnoreAntiforgeryToken]
    public virtual async Task<IActionResult> ImportExcelDynamic([FromBody] ProductImportDynamicRequestModel request)
    {
        var currentVendor = await _workContext.GetCurrentVendorAsync();
        if (currentVendor != null && !_vendorSettings.AllowVendorsToImportProducts)
            return AccessDeniedView();

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .SelectMany(x => x.Value.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                .ToList();
            return BadRequest(new { message = string.Join("; ", errors), errors = ModelState });
        }

        if (request == null)
            return BadRequest(new { message = "Request is null" });

        if (string.IsNullOrWhiteSpace(request.FileId))
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Admin.Common.UploadFile") });

        var vendorId = currentVendor?.Id ?? request.VendorId;
        if (!vendorId.HasValue || vendorId.Value <= 0)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Admin.Catalog.Products.DynamicImport.VendorNotAllowed") });

        if (currentVendor != null && vendorId.Value != currentVendor.Id)
            return Forbid();

        var mappings = request.Mappings ?? new List<ProductImportDynamicMappingModel>();

        var missingRequiredFields = RequiredFields.Where(r =>
            !mappings.Any(m => string.Equals(m.Property, r, StringComparison.InvariantCultureIgnoreCase) && m.ColumnIndex > 0)).ToList();

        if (missingRequiredFields.Any())
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Admin.Catalog.Products.DynamicImport.RequiredFieldMissing") });

        var duplicateColumns = mappings.Where(m => m.ColumnIndex > 0)
            .GroupBy(m => m.ColumnIndex)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateColumns != null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Admin.Catalog.Products.DynamicImport.DuplicateColumn") });

        var tempDirectory = GetDynamicImportDirectory();
        var filePath = _fileProvider.Combine(tempDirectory, $"{request.FileId}.xlsx");

        if (!_fileProvider.FileExists(filePath))
            return NotFound(new { message = await _localizationService.GetResourceAsync("Admin.Catalog.Products.DynamicImport.FileNotFound") });

        try
        {
            await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var importMappings = mappings.Select(m => new ImportProductMapping
                {
                    Property = m.Property,
                    ColumnIndex = m.ColumnIndex
                });

                await _importManager.ImportProductsFromXlsxAsync(stream, importMappings, vendorId);
            }

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Catalog.Products.Imported"));
            return Json(new { success = true });
        }
        catch (Exception exc)
        {
            await _notificationService.ErrorNotificationAsync(exc);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = exc.Message });
        }
        finally
        {
            if (_fileProvider.FileExists(filePath))
                _fileProvider.DeleteFile(filePath);
        }
    }

    [HttpGet]
    [CheckPermission(StandardPermission.Catalog.PRODUCTS_IMPORT_EXPORT)]
    public virtual async Task<IActionResult> GetMappingTemplates(int vendorId = 0)
    {
        var currentVendor = await _workContext.GetCurrentVendorAsync();
        if (currentVendor != null)
            vendorId = currentVendor.Id;

        var templates = await _templateService.GetTemplatesAsync(vendorId, includeSystemTemplates: true);
        
        return Json(new
        {
            success = true,
            templates = templates.Select(t => new
            {
                id = t.Id,
                name = t.Name,
                isSystem = t.IsSystemTemplate,
                vendorId = t.VendorId
            })
        });
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Catalog.PRODUCTS_IMPORT_EXPORT)]
    public virtual async Task<IActionResult> SaveMappingTemplate([FromBody] SaveMappingTemplateModel model)
    {
        var currentVendor = await _workContext.GetCurrentVendorAsync();
        
        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Admin.Catalog.Products.DynamicImport.Template.NameRequired") });

        if (model.Mappings == null || !model.Mappings.Any())
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Admin.Catalog.Products.DynamicImport.Template.MappingsRequired") });

        // Поставщики не могут создавать системные шаблоны
        if (currentVendor != null && model.IsSystemTemplate)
            return BadRequest(new { message = "Vendors cannot create system templates" });

        var template = new MappingTemplate
        {
            Name = model.Name,
            VendorId = currentVendor?.Id ?? model.VendorId,
            IsSystemTemplate = model.IsSystemTemplate && currentVendor == null && model.VendorId == 0, // Только администраторы могут создавать системные шаблоны (без привязки к поставщику)
            MappingsJson = JsonSerializer.Serialize(model.Mappings),
            CreatedOnUtc = DateTime.UtcNow
        };

        await _templateService.InsertTemplateAsync(template);

        return Json(new
        {
            success = true,
            templateId = template.Id,
            message = await _localizationService.GetResourceAsync("Admin.Catalog.Products.DynamicImport.Template.Saved")
        });
    }

    [HttpGet]
    [CheckPermission(StandardPermission.Catalog.PRODUCTS_IMPORT_EXPORT)]
    public virtual async Task<IActionResult> GetMappingTemplate(int id)
    {
        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null)
            return NotFound(new { message = await _localizationService.GetResourceAsync("Admin.Catalog.Products.DynamicImport.Template.NotFound") });

        var currentVendor = await _workContext.GetCurrentVendorAsync();
        
        // Проверяем права доступа: системные доступны всем, свои только своим
        if (!template.IsSystemTemplate && currentVendor != null && template.VendorId != currentVendor.Id)
            return Forbid();

        var mappings = string.IsNullOrEmpty(template.MappingsJson)
            ? new List<ImportProductMapping>()
            : JsonSerializer.Deserialize<List<ImportProductMapping>>(template.MappingsJson);

        return Json(new
        {
            success = true,
            template = new
            {
                id = template.Id,
                name = template.Name,
                isSystem = template.IsSystemTemplate,
                mappings
            }
        });
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Catalog.PRODUCTS_IMPORT_EXPORT)]
    public virtual async Task<IActionResult> DeleteMappingTemplate(int id)
    {
        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null)
            return NotFound(new { message = await _localizationService.GetResourceAsync("Admin.Catalog.Products.DynamicImport.Template.NotFound") });

        var currentVendor = await _workContext.GetCurrentVendorAsync();

        // Системные шаблоны могут удалять только администраторы
        if (template.IsSystemTemplate && currentVendor != null)
            return Forbid();

        // Свои шаблоны могут удалять только их владельцы
        if (!template.IsSystemTemplate && currentVendor != null && template.VendorId != currentVendor.Id)
            return Forbid();

        await _templateService.DeleteTemplateAsync(template);

        return Json(new
        {
            success = true,
            message = await _localizationService.GetResourceAsync("Admin.Catalog.Products.DynamicImport.Template.Deleted")
        });
    }

    #endregion
}

