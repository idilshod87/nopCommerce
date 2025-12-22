namespace Nop.Plugin.Misc.WebApi.Frontend;

/// <summary>
/// Represents plugin constants
/// </summary>
public class WebApiFrontendDefaults
{
    /// <summary>
    /// Gets a plugin system name
    /// </summary>
    public static string SystemName => "Misc.WebApi.Frontend";

    /// <summary>
    /// Gets the generic attribute key used to store vendor payment selections for the frontend API.
    /// </summary>
    public static string VendorPaymentMethodsAttribute => "WebApi.SelectedVendorPaymentMethods";

    /// <summary>
    /// Gets the custom value key used when persisting vendor payment selections inside process payment requests.
    /// </summary>
    public static string VendorPaymentMethodsCustomValue => "WebApi.VendorPaymentMethods";
}