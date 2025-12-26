using Microsoft.AspNetCore.Mvc;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Misc.DynamicProductImport.Components;

/// <summary>
/// Represents a view component to render the dynamic import button on a product list view
/// </summary>
public class DynamicImportButtonViewComponent : NopViewComponent
{
    #region Fields

    protected readonly IPermissionService _permissionService;

    #endregion

    #region Ctor

    public DynamicImportButtonViewComponent(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Invoke the widget view component
    /// </summary>
    /// <param name="widgetZone">Widget zone</param>
    /// <param name="additionalData">Additional parameters</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the view component result
    /// </returns>
    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermission.Catalog.PRODUCTS_IMPORT_EXPORT))
            return Content(string.Empty);

        //ensure that it's a proper widget zone
        if (!widgetZone.Equals(AdminWidgetZones.ProductListButtons))
            return Content(string.Empty);

        //cast additionalData to ProductSearchModel
        if (additionalData is not ProductSearchModel model)
            return Content(string.Empty);

        return View("~/Plugins/Misc.DynamicProductImport/Views/Product/DynamicImportButton.cshtml", model);
    }

    #endregion
}


