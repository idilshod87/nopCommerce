#nullable enable
using System.Collections.Generic;

namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

public class StringResourceItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class AppLandingSettingsDto
{
    public bool ShowHomepageSlider { get; set; }
    public bool ShowFeaturedProducts { get; set; }
    public bool ShowBestsellersOnHomepage { get; set; }
    public bool ShowHomepageCategoryProducts { get; set; }
    public bool ShowManufacturers { get; set; }
    public bool Rtl { get; set; }
    public string AndroidVersion { get; set; } = string.Empty;
    public bool AndriodForceUpdate { get; set; }
    public string PlayStoreUrl { get; set; } = string.Empty;
    public string IOSVersion { get; set; } = string.Empty;
    public bool IOSForceUpdate { get; set; }
    public string AppStoreUrl { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public int TotalShoppingCartProducts { get; set; }
    public int TotalWishListProducts { get; set; }
    public bool NewProductsEnabled { get; set; }
    public bool RecentlyViewedProductsEnabled { get; set; }
    public bool CompareProductsEnabled { get; set; }
    public bool AllowCustomersToUploadAvatars { get; set; }
    public bool AnonymousCheckoutAllowed { get; set; }
}


