using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.WebApi.Frontend.Models;

/// <summary>
/// Model for mobile app settings configuration
/// </summary>
public record MobileAppSettingsModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.Misc.WebApi.Frontend.MobileApp.ShowFeaturedProducts")]
    public bool ShowFeaturedProducts { get; set; } = true;

    [NopResourceDisplayName("Plugins.Misc.WebApi.Frontend.MobileApp.ShowBestsellersOnHomepage")]
    public bool ShowBestsellersOnHomepage { get; set; } = true;

    [NopResourceDisplayName("Plugins.Misc.WebApi.Frontend.MobileApp.ShowHomepageCategoryProducts")]
    public bool ShowHomepageCategoryProducts { get; set; } = true;

    [NopResourceDisplayName("Plugins.Misc.WebApi.Frontend.MobileApp.ShowManufacturers")]
    public bool ShowManufacturers { get; set; } = true;
}

