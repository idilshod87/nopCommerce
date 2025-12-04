using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.WebApi.Frontend.Configuration;

/// <summary>
/// Settings for mobile app homepage display
/// </summary>
public class MobileAppSettings : ISettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to show featured products on mobile app homepage
    /// </summary>
    public bool ShowFeaturedProducts { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show bestsellers on mobile app homepage
    /// </summary>
    public bool ShowBestsellersOnHomepage { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show homepage category products on mobile app
    /// </summary>
    public bool ShowHomepageCategoryProducts { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show manufacturers on mobile app homepage
    /// </summary>
    public bool ShowManufacturers { get; set; } = true;
}

