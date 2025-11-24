using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Sms.TelegramGateway.Models;

/// <summary>
/// Represents Telegram Gateway configuration model
/// </summary>
public record ConfigurationModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.Sms.TelegramGateway.Fields.BotToken")]
    public string BotToken { get; set; }

    [NopResourceDisplayName("Plugins.Sms.TelegramGateway.Fields.BotUsername")]
    public string BotUsername { get; set; }

    [NopResourceDisplayName("Plugins.Sms.TelegramGateway.Fields.WebhookSecretToken")]
    public string WebhookSecretToken { get; set; }

    [NopResourceDisplayName("Plugins.Sms.TelegramGateway.Fields.SessionTtlMinutes")]
    public int SessionTtlMinutes { get; set; }

    [NopResourceDisplayName("Plugins.Sms.TelegramGateway.Fields.VerificationCodeLength")]
    public int VerificationCodeLength { get; set; }

    [NopResourceDisplayName("Plugins.Sms.TelegramGateway.Fields.VerificationCodeTtlMinutes")]
    public int VerificationCodeTtlMinutes { get; set; }
}

