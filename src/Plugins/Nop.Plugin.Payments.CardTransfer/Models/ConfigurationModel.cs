using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System.Collections.Generic;

namespace Nop.Plugin.Payments.CardTransfer.Models;

public record ConfigurationModel : BaseNopModel, ILocalizedModel<ConfigurationModel.ConfigurationLocalizedModel>
{
    public ConfigurationModel()
    {
        Locales = new List<ConfigurationLocalizedModel>();
    }

    public int ActiveStoreScopeConfiguration { get; set; }

    [NopResourceDisplayName("Plugins.Payment.CardTransfer.DescriptionText")]
    public string DescriptionText { get; set; }
    public bool DescriptionText_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payment.CardTransfer.AdditionalFee")]
    public decimal AdditionalFee { get; set; }
    public bool AdditionalFee_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payment.CardTransfer.AdditionalFeePercentage")]
    public bool AdditionalFeePercentage { get; set; }
    public bool AdditionalFeePercentage_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payment.CardTransfer.ShippableProductRequired")]
    public bool ShippableProductRequired { get; set; }
    public bool ShippableProductRequired_OverrideForStore { get; set; }

    public IList<ConfigurationLocalizedModel> Locales { get; set; }

    public class ConfigurationLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [NopResourceDisplayName("Plugins.Payment.CardTransfer.DescriptionText")]
        public string DescriptionText { get; set; }
    }
}
