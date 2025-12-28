using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.TelegramNotifications.Models;

/// <summary>
/// Represents configuration model
/// </summary>
public record ConfigurationModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.Misc.TelegramNotifications.Fields.BotToken")]
    public string BotToken { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Misc.TelegramNotifications.Fields.ChatId")]
    public string ChatId { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Misc.TelegramNotifications.Fields.Enabled")]
    public bool Enabled { get; set; }

    [NopResourceDisplayName("Plugins.Misc.TelegramNotifications.Fields.NotifyOnOrderPlaced")]
    public bool NotifyOnOrderPlaced { get; set; }

    [NopResourceDisplayName("Plugins.Misc.TelegramNotifications.Fields.NotifyOnOrderStatusChanged")]
    public bool NotifyOnOrderStatusChanged { get; set; }

    [NopResourceDisplayName("Plugins.Misc.TelegramNotifications.Fields.NotifyOnOrderPaid")]
    public bool NotifyOnOrderPaid { get; set; }

    [NopResourceDisplayName("Plugins.Misc.TelegramNotifications.Fields.NotifyOnOrderCancelled")]
    public bool NotifyOnOrderCancelled { get; set; }

    [NopResourceDisplayName("Plugins.Misc.TelegramNotifications.Fields.NotifyOnOrderCompleted")]
    public bool NotifyOnOrderCompleted { get; set; }
}
