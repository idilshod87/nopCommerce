using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.TelegramNotifications;

/// <summary>
/// Represents plugin settings
/// </summary>
public class TelegramNotificationsSettings : ISettings
{
    /// <summary>
    /// Gets or sets the Telegram bot token
    /// </summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the chat ID to send notifications to
    /// </summary>
    public string ChatId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to send notifications when order is placed
    /// </summary>
    public bool NotifyOnOrderPlaced { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to send notifications when order status changes
    /// </summary>
    public bool NotifyOnOrderStatusChanged { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to send notifications when order is paid
    /// </summary>
    public bool NotifyOnOrderPaid { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to send notifications when order is cancelled
    /// </summary>
    public bool NotifyOnOrderCancelled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to send notifications when order is completed
    /// </summary>
    public bool NotifyOnOrderCompleted { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the plugin is enabled
    /// </summary>
    public bool Enabled { get; set; }
}
