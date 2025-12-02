using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.ExternalAuth.Telegram.Models;

/// <summary>
/// Represents Telegram Gateway configuration model
/// </summary>
public record ConfigurationModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.ExternalAuth.Telegram.Fields.BotToken")]
    public string BotToken { get; set; }

    [NopResourceDisplayName("Plugins.ExternalAuth.Telegram.Fields.BotUsername")]
    public string BotUsername { get; set; }

    [NopResourceDisplayName("Plugins.ExternalAuth.Telegram.Fields.WebhookSecretToken")]
    public string WebhookSecretToken { get; set; }

    [NopResourceDisplayName("Plugins.ExternalAuth.Telegram.Fields.SessionTtlMinutes")]
    public int SessionTtlMinutes { get; set; }

    [NopResourceDisplayName("Plugins.ExternalAuth.Telegram.Fields.VerificationCodeLength")]
    public int VerificationCodeLength { get; set; }

    [NopResourceDisplayName("Plugins.ExternalAuth.Telegram.Fields.VerificationCodeTtlMinutes")]
    public int VerificationCodeTtlMinutes { get; set; }
}

