using System.Text;
using Microsoft.Extensions.Logging;
using Nop.Core.Domain.Orders;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;

namespace Nop.Plugin.Misc.TelegramNotifications.Services;

/// <summary>
/// Represents Telegram notification service
/// </summary>
public class TelegramNotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramNotificationService> _logger;
    private readonly TelegramNotificationsSettings _settings;
    private readonly ILocalizationService _localizationService;
    private readonly ICustomerService _customerService;
    private readonly ICountryService _countryService;

    public TelegramNotificationService(
        IHttpClientFactory httpClientFactory,
        ILogger<TelegramNotificationService> logger,
        TelegramNotificationsSettings settings,
        ILocalizationService localizationService,
        ICustomerService customerService,
        ICountryService countryService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _settings = settings;
        _localizationService = localizationService;
        _customerService = customerService;
        _countryService = countryService;
    }

    /// <summary>
    /// Send order status notification
    /// </summary>
    /// <param name="order">Order</param>
    /// <param name="previousStatus">Previous order status</param>
    public async Task SendOrderStatusNotificationAsync(Order order, OrderStatus? previousStatus = null)
    {
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.BotToken) || string.IsNullOrWhiteSpace(_settings.ChatId))
            return;

        try
        {
            var message = await BuildOrderMessageAsync(order, previousStatus);
            await SendTelegramMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Telegram notification for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Build order message
    /// </summary>
    private async Task<string> BuildOrderMessageAsync(Order order, OrderStatus? previousStatus)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("🛒 <b>Уведомление о заказе</b>");
        sb.AppendLine();
        sb.AppendLine($"📋 <b>Номер заказа:</b> #{order.CustomOrderNumber}");
        
        if (previousStatus.HasValue)
        {
            var prevStatusName = await _localizationService.GetLocalizedEnumAsync(previousStatus.Value);
            var currentStatusName = await _localizationService.GetLocalizedEnumAsync(order.OrderStatus);
            sb.AppendLine($"📊 <b>Статус изменен:</b> {prevStatusName} → {currentStatusName}");
        }
        else
        {
            var statusName = await _localizationService.GetLocalizedEnumAsync(order.OrderStatus);
            sb.AppendLine($"📊 <b>Статус:</b> {statusName}");
        }

        sb.AppendLine($"💰 <b>Сумма заказа:</b> {order.OrderTotal:C}");
        
        // Customer information
        var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
        if (customer != null)
        {
            var customerName = $"{customer.FirstName} {customer.LastName}".Trim();
            if (string.IsNullOrEmpty(customerName))
                customerName = customer.Email;
            sb.AppendLine($"👤 <b>Клиент:</b> {customerName}");
        }
        
        sb.AppendLine($"📅 <b>Дата создания:</b> {order.CreatedOnUtc:dd.MM.yyyy HH:mm}");
        
        // Order status icon
        var statusIcon = order.OrderStatus switch
        {
            OrderStatus.Pending => "⏳",
            OrderStatus.Processing => "⚙️",
            OrderStatus.Complete => "✅",
            OrderStatus.Cancelled => "❌",
            _ => "📦"
        };
        
        sb.AppendLine();
        sb.AppendLine($"{statusIcon} <i>{GetStatusDescription(order.OrderStatus)}</i>");

        return sb.ToString();
    }

    /// <summary>
    /// Get status description
    /// </summary>
    private static string GetStatusDescription(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "Заказ ожидает обработки",
            OrderStatus.Processing => "Заказ обрабатывается",
            OrderStatus.Complete => "Заказ выполнен",
            OrderStatus.Cancelled => "Заказ отменен",
            _ => "Статус заказа изменен"
        };
    }

    /// <summary>
    /// Send message to Telegram
    /// </summary>
    private async Task SendTelegramMessageAsync(string message)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://api.telegram.org/bot{_settings.BotToken}/sendMessage";

        var payload = new
        {
            chat_id = _settings.ChatId,
            text = message,
            parse_mode = "HTML"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Telegram API returned {StatusCode}: {Body}", response.StatusCode, responseBody);
        }
    }
}
