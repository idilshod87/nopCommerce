using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Plugin.ExternalAuth.Telegram.Domain.Authentication;

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
    private readonly IPriceFormatter _priceFormatter;
    private readonly IWorkContext _workContext;
    private readonly IRepository<TelegramAuthSession> _telegramAuthSessionRepository;

    public TelegramNotificationService(
        IHttpClientFactory httpClientFactory,
        ILogger<TelegramNotificationService> logger,
        TelegramNotificationsSettings settings,
        ILocalizationService localizationService,
        ICustomerService customerService,
        ICountryService countryService,
        IPriceFormatter priceFormatter,
        IWorkContext workContext,
        IRepository<TelegramAuthSession> telegramAuthSessionRepository)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _settings = settings;
        _localizationService = localizationService;
        _customerService = customerService;
        _countryService = countryService;
        _priceFormatter = priceFormatter;
        _workContext = workContext;
        _telegramAuthSessionRepository = telegramAuthSessionRepository;
    }

    /// <summary>
    /// Send order status notification
    /// </summary>
    /// <param name="order">Order</param>
    /// <param name="previousStatus">Previous order status</param>
    public async Task SendOrderStatusNotificationAsync(Order order, OrderStatus? previousStatus = null)
    {
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.BotToken))
            return;

        try
        {
            // Get customer's Telegram Chat ID
            var chatId = await GetCustomerTelegramChatIdAsync(order.CustomerId);
            if (!chatId.HasValue)
            {
                _logger.LogWarning("Customer {CustomerId} does not have Telegram Chat ID. Skipping notification for order {OrderId}",
                    order.CustomerId, order.Id);
                return;
            }

            var message = await BuildOrderMessageAsync(order, previousStatus);
            await SendTelegramMessageAsync(chatId.Value, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Telegram notification for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Send shipment notification
    /// </summary>
    /// <param name="order">Order</param>
    /// <param name="shipment">Shipment</param>
    /// <param name="isDelivered">Is delivered</param>
    public async Task SendShipmentNotificationAsync(Order order, Shipment shipment, bool isDelivered)
    {
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.BotToken))
            return;

        try
        {
            var chatId = await GetCustomerTelegramChatIdAsync(order.CustomerId);
            if (!chatId.HasValue)
            {
                _logger.LogWarning("Customer {CustomerId} does not have Telegram Chat ID. Skipping shipment notification for order {OrderId}",
                    order.CustomerId, order.Id);
                return;
            }

            var message = await BuildShipmentMessageAsync(order, shipment, isDelivered);
            await SendTelegramMessageAsync(chatId.Value, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Telegram shipment notification for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Get customer's Telegram Chat ID from TelegramAuthSession
    /// </summary>
    private async Task<long?> GetCustomerTelegramChatIdAsync(int customerId)
    {
        try
        {
            var session = await _telegramAuthSessionRepository.Table
                .Where(s => s.CustomerId == customerId 
                    && s.TelegramChatId.HasValue 
                    && s.StatusId == (int)TelegramAuthStatus.Verified)
                .OrderByDescending(s => s.VerifiedOnUtc)
                .FirstOrDefaultAsync();

            return session?.TelegramChatId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Telegram Chat ID for customer {CustomerId}", customerId);
            return null;
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

        var formattedTotal = await _priceFormatter.FormatPriceAsync(order.OrderTotal, true, order.CustomerCurrencyCode, (await _workContext.GetWorkingLanguageAsync()).Id, false);
        sb.AppendLine($"💰 <b>Сумма заказа:</b> {formattedTotal}");
        
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
    /// Build shipment message
    /// </summary>
    private async Task<string> BuildShipmentMessageAsync(Order order, Shipment shipment, bool isDelivered)
    {
        var sb = new StringBuilder();
        
        var icon = isDelivered ? "📦✅" : "🚚";
        var title = isDelivered ? "Заказ доставлен" : "Заказ отправлен";
        
        sb.AppendLine($"{icon} <b>{title}</b>");
        sb.AppendLine();
        sb.AppendLine($"📋 <b>Номер заказа:</b> #{order.CustomOrderNumber}");
        
        if (!string.IsNullOrEmpty(shipment.TrackingNumber))
        {
            sb.AppendLine($"🔢 <b>Трек-номер:</b> {shipment.TrackingNumber}");
        }

        var formattedTotal = await _priceFormatter.FormatPriceAsync(order.OrderTotal, true, order.CustomerCurrencyCode, (await _workContext.GetWorkingLanguageAsync()).Id, false);
        sb.AppendLine($"💰 <b>Сумма заказа:</b> {formattedTotal}");
        
        if (isDelivered && shipment.DeliveryDateUtc.HasValue)
        {
            sb.AppendLine($"📅 <b>Дата доставки:</b> {shipment.DeliveryDateUtc.Value:dd.MM.yyyy HH:mm}");
        }
        else if (!isDelivered && shipment.ShippedDateUtc.HasValue)
        {
            sb.AppendLine($"📅 <b>Дата отправки:</b> {shipment.ShippedDateUtc.Value:dd.MM.yyyy HH:mm}");
        }
        
        sb.AppendLine();
        var statusMessage = isDelivered 
            ? "Ваш заказ был успешно доставлен!" 
            : "Ваш заказ в пути. Ожидайте доставку!";
        sb.AppendLine($"<i>{statusMessage}</i>");

        return sb.ToString();
    }

    /// <summary>
    /// Send message to Telegram
    /// </summary>
    private async Task SendTelegramMessageAsync(long chatId, string message)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://api.telegram.org/bot{_settings.BotToken}/sendMessage";

        var payload = new
        {
            chat_id = chatId,
            text = message,
            parse_mode = "HTML"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Telegram API returned {StatusCode}: {Body} for chat {ChatId}",
                response.StatusCode, responseBody, chatId);
        }
        else
        {
            _logger.LogInformation("Successfully sent Telegram notification to chat {ChatId}", chatId);
        }
    }
}
