using Microsoft.Extensions.Logging;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Misc.TelegramNotifications.Services;
using Nop.Services.Configuration;
using Nop.Services.Events;
using Nop.Services.Orders;

namespace Nop.Plugin.Misc.TelegramNotifications.Infrastructure;

/// <summary>
/// Represents plugin event consumer
/// </summary>
public class EventConsumer : 
    IConsumer<OrderPlacedEvent>,
    IConsumer<OrderStatusChangedEvent>,
    IConsumer<OrderPaidEvent>,
    IConsumer<ShipmentSentEvent>,
    IConsumer<ShipmentDeliveredEvent>
{
    private readonly TelegramNotificationService _telegramNotificationService;
    private readonly ISettingService _settingService;
    private readonly IOrderService _orderService;
    private readonly ILogger<EventConsumer> _logger;

    public EventConsumer(
        TelegramNotificationService telegramNotificationService,
        ISettingService settingService,
        IStoreContext storeContext,
        IOrderService orderService,
        ILogger<EventConsumer> logger)
    {
        _telegramNotificationService = telegramNotificationService;
        _settingService = settingService;
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Handle order placed event
    /// </summary>
    public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        var settings = await _settingService.LoadSettingAsync<TelegramNotificationsSettings>(eventMessage.Order.StoreId);

        if (!settings.Enabled || !settings.NotifyOnOrderPlaced)
            return;

        await _telegramNotificationService.SendOrderStatusNotificationAsync(eventMessage.Order);
    }

    /// <summary>
    /// Handle order status changed event
    /// </summary>
    public async Task HandleEventAsync(OrderStatusChangedEvent eventMessage)
    {
        var settings = await _settingService.LoadSettingAsync<TelegramNotificationsSettings>(eventMessage.Order.StoreId);

        if (!settings.Enabled)
            return;

        var order = eventMessage.Order;
        var previousStatus = eventMessage.PreviousOrderStatus;

        // Check specific notification settings
        var shouldNotify = order.OrderStatus switch
        {
            OrderStatus.Cancelled => settings.NotifyOnOrderCancelled,
            OrderStatus.Complete => settings.NotifyOnOrderCompleted,
            _ => settings.NotifyOnOrderStatusChanged
        };

        if (!shouldNotify)
            return;

        await _telegramNotificationService.SendOrderStatusNotificationAsync(order, previousStatus);
    }

    /// <summary>
    /// Handle order paid event
    /// </summary>
    public async Task HandleEventAsync(OrderPaidEvent eventMessage)
    {
        var settings = await _settingService.LoadSettingAsync<TelegramNotificationsSettings>(eventMessage.Order.StoreId);

        if (!settings.Enabled || !settings.NotifyOnOrderPaid)
            return;

        // Don't send notifications if order is already completed
        if (eventMessage.Order.OrderStatus == OrderStatus.Complete)
            return;

        await _telegramNotificationService.SendOrderStatusNotificationAsync(eventMessage.Order);
    }

    /// <summary>
    /// Handle shipment sent event
    /// </summary>
    public async Task HandleEventAsync(ShipmentSentEvent eventMessage)
    {
        _logger.LogInformation("ShipmentSentEvent received for shipment {ShipmentId}", eventMessage.Shipment.Id);
        
        var order = await _orderService.GetOrderByIdAsync(eventMessage.Shipment.OrderId);
        if (order == null)
        {
            _logger.LogWarning("Order not found for shipment {ShipmentId}", eventMessage.Shipment.Id);
            return;
        }

        var settings = await _settingService.LoadSettingAsync<TelegramNotificationsSettings>(order.StoreId);
        _logger.LogInformation("Settings loaded: Enabled={Enabled}, NotifyOnShipmentSent={NotifyOnShipmentSent}", 
            settings.Enabled, settings.NotifyOnShipmentSent);

        if (!settings.Enabled || !settings.NotifyOnShipmentSent)
            return;

        await _telegramNotificationService.SendShipmentNotificationAsync(order, eventMessage.Shipment, isDelivered: false);
    }

    /// <summary>
    /// Handle shipment delivered event
    /// </summary>
    public async Task HandleEventAsync(ShipmentDeliveredEvent eventMessage)
    {
        _logger.LogInformation("ShipmentDeliveredEvent received for shipment {ShipmentId}", eventMessage.Shipment.Id);
        
        var order = await _orderService.GetOrderByIdAsync(eventMessage.Shipment.OrderId);
        if (order == null)
        {
            _logger.LogWarning("Order not found for shipment {ShipmentId}", eventMessage.Shipment.Id);
            return;
        }

        var settings = await _settingService.LoadSettingAsync<TelegramNotificationsSettings>(order.StoreId);
        _logger.LogInformation("Settings loaded: Enabled={Enabled}, NotifyOnShipmentDelivered={NotifyOnShipmentDelivered}", 
            settings.Enabled, settings.NotifyOnShipmentDelivered);

        if (!settings.Enabled || !settings.NotifyOnShipmentDelivered)
            return;

        await _telegramNotificationService.SendShipmentNotificationAsync(order, eventMessage.Shipment, isDelivered: true);
    }
}
