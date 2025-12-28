using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Plugin.Misc.TelegramNotifications.Services;
using Nop.Services.Configuration;
using Nop.Services.Events;

namespace Nop.Plugin.Misc.TelegramNotifications.Infrastructure;

/// <summary>
/// Represents plugin event consumer
/// </summary>
public class EventConsumer : 
    IConsumer<OrderPlacedEvent>,
    IConsumer<OrderStatusChangedEvent>,
    IConsumer<OrderPaidEvent>
{
    private readonly TelegramNotificationService _telegramNotificationService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;

    public EventConsumer(
        TelegramNotificationService telegramNotificationService,
        ISettingService settingService,
        IStoreContext storeContext)
    {
        _telegramNotificationService = telegramNotificationService;
        _settingService = settingService;
        _storeContext = storeContext;
    }

    /// <summary>
    /// Handle order placed event
    /// </summary>
    public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        var storeId = (await _storeContext.GetCurrentStoreAsync()).Id;
        var settings = await _settingService.LoadSettingAsync<TelegramNotificationsSettings>(storeId);

        if (!settings.Enabled || !settings.NotifyOnOrderPlaced)
            return;

        await _telegramNotificationService.SendOrderStatusNotificationAsync(eventMessage.Order);
    }

    /// <summary>
    /// Handle order status changed event
    /// </summary>
    public async Task HandleEventAsync(OrderStatusChangedEvent eventMessage)
    {
        var storeId = (await _storeContext.GetCurrentStoreAsync()).Id;
        var settings = await _settingService.LoadSettingAsync<TelegramNotificationsSettings>(storeId);

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
        var storeId = (await _storeContext.GetCurrentStoreAsync()).Id;
        var settings = await _settingService.LoadSettingAsync<TelegramNotificationsSettings>(storeId);

        if (!settings.Enabled || !settings.NotifyOnOrderPaid)
            return;

        await _telegramNotificationService.SendOrderStatusNotificationAsync(eventMessage.Order);
    }
}
