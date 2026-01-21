using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Terminal.Components;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using System.Collections.Generic;

namespace Nop.Plugin.Payments.Terminal;

/// <summary>
/// Terminal payment processor
/// </summary>
public class TerminalPaymentProcessor : BasePlugin, IPaymentMethod
{
    #region Fields

    protected readonly TerminalPaymentSettings _terminalPaymentSettings;
    protected readonly ILocalizationService _localizationService;
    protected readonly IOrderTotalCalculationService _orderTotalCalculationService;
    protected readonly ISettingService _settingService;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public TerminalPaymentProcessor(TerminalPaymentSettings terminalPaymentSettings,
        ILocalizationService localizationService,
        IOrderTotalCalculationService orderTotalCalculationService,
        ISettingService settingService,
        IShoppingCartService shoppingCartService,
        IWebHelper webHelper)
    {
        _terminalPaymentSettings = terminalPaymentSettings;
        _localizationService = localizationService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _settingService = settingService;
        _shoppingCartService = shoppingCartService;
        _webHelper = webHelper;
    }

    #endregion

    #region Methods

    public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        return Task.FromResult(new ProcessPaymentResult());
    }

    public Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
    {
        return Task.CompletedTask;
    }

    public async Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
    {
        if (_terminalPaymentSettings.ShippableProductRequired && !await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
            return true;

        return false;
    }

    public async Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
    {
        return await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart,
            _terminalPaymentSettings.AdditionalFee, _terminalPaymentSettings.AdditionalFeePercentage);
    }

    public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
    {
        return Task.FromResult(new CapturePaymentResult { Errors = new[] { "Capture method not supported" } });
    }

    public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
    {
        return Task.FromResult(new RefundPaymentResult { Errors = new[] { "Refund method not supported" } });
    }

    public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
    {
        return Task.FromResult(new VoidPaymentResult { Errors = new[] { "Void method not supported" } });
    }

    public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        return Task.FromResult(new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } });
    }

    public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
    {
        return Task.FromResult(new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } });
    }

    public Task<bool> CanRePostProcessPaymentAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return Task.FromResult(false);
    }

    public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
    {
        return Task.FromResult<IList<string>>(new List<string>());
    }

    public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
    {
        return Task.FromResult(new ProcessPaymentRequest());
    }

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/PaymentTerminal/Configure";
    }

    public Type GetPublicViewComponent()
    {
        return typeof(TerminalViewComponent);
    }

    public override async Task InstallAsync()
    {
        var settings = new TerminalPaymentSettings
        {
            DescriptionText = "<p>Оплата заказа производится банковской картой через терминал в точке выдачи или при доставке.</p><p>Пожалуйста, убедитесь, что у вас есть карта соответствующей платёжной системы. При необходимости вы можете изменить этот текст в административной панели.</p>"
        };
        await _settingService.SaveSettingAsync(settings);

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Payment.Terminal.AdditionalFee"] = "Дополнительная комиссия",
            ["Plugins.Payment.Terminal.AdditionalFee.Hint"] = "Фиксированная сумма, добавляемая к заказу.",
            ["Plugins.Payment.Terminal.AdditionalFeePercentage"] = "Дополнительная комиссия, %",
            ["Plugins.Payment.Terminal.AdditionalFeePercentage.Hint"] = "Если включено, комиссия рассчитывается в процентах от суммы заказа.",
            ["Plugins.Payment.Terminal.DescriptionText"] = "Инструкции по оплате по терминалу",
            ["Plugins.Payment.Terminal.DescriptionText.Hint"] = "Текст, который увидит покупатель при выборе оплаты по терминалу.",
            ["Plugins.Payment.Terminal.PaymentMethodDescription"] = "Оплата банковской картой через терминал",
            ["Plugins.Payment.Terminal.ShippableProductRequired"] = "Требуется физическая доставка",
            ["Plugins.Payment.Terminal.ShippableProductRequired.Hint"] = "Показывать метод только если корзина содержит доставляемые товары."
        });

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<TerminalPaymentSettings>();
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payment.Terminal");

        await base.UninstallAsync();
    }

    public async Task<string> GetPaymentMethodDescriptionAsync()
    {
        return await _localizationService.GetResourceAsync("Plugins.Payment.Terminal.PaymentMethodDescription");
    }

    #endregion

    #region Properties

    public bool SupportCapture => false;

    public bool SupportPartiallyRefund => false;

    public bool SupportRefund => false;

    public bool SupportVoid => false;

    public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

    public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

    public bool SkipPaymentInfo => false;

    #endregion
}
