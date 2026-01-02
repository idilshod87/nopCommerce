using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.BankTransfer.Components;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using System.Collections.Generic;

namespace Nop.Plugin.Payments.BankTransfer;

/// <summary>
/// Bank transfer payment processor
/// </summary>
public class BankTransferPaymentProcessor : BasePlugin, IPaymentMethod
{
    #region Fields

    protected readonly BankTransferPaymentSettings _bankTransferPaymentSettings;
    protected readonly ILocalizationService _localizationService;
    protected readonly IOrderTotalCalculationService _orderTotalCalculationService;
    protected readonly ISettingService _settingService;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public BankTransferPaymentProcessor(BankTransferPaymentSettings bankTransferPaymentSettings,
        ILocalizationService localizationService,
        IOrderTotalCalculationService orderTotalCalculationService,
        ISettingService settingService,
        IShoppingCartService shoppingCartService,
        IWebHelper webHelper)
    {
        _bankTransferPaymentSettings = bankTransferPaymentSettings;
        _localizationService = localizationService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _settingService = settingService;
        _shoppingCartService = shoppingCartService;
        _webHelper = webHelper;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Process a payment
    /// </summary>
    /// <param name="processPaymentRequest">Payment info required for an order processing</param>
    /// <returns>A task that represents the asynchronous operation The task result contains the process payment result</returns>
    public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        return Task.FromResult(new ProcessPaymentResult());
    }

    /// <summary>
    /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
    /// </summary>
    public Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a value indicating whether payment method should be hidden during checkout
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    public async Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
    {
        if (_bankTransferPaymentSettings.ShippableProductRequired && !await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
            return true;

        return false;
    }

    /// <summary>
    /// Gets additional handling fee
    /// </summary>
    public async Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
    {
        return await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart,
            _bankTransferPaymentSettings.AdditionalFee, _bankTransferPaymentSettings.AdditionalFeePercentage);
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
        return $"{_webHelper.GetStoreLocation()}Admin/PaymentBankTransfer/Configure";
    }

    public Type GetPublicViewComponent()
    {
        return typeof(BankTransferViewComponent);
    }

    public override async Task InstallAsync()
    {
        var settings = new BankTransferPaymentSettings
        {
            DescriptionText = "<p>Оплатите заказ банковским переводом по следующим реквизитам:</p><p><strong>ООО \"Название компании\"</strong><br /><strong>ИНН: 0000000000</strong><br /><strong>Р/с: 00000000000000000000</strong><br /><strong>Банк: ПАО &quot;БАНК&quot;</strong><br /><strong>БИК: 000000000</strong></p><p>Укажите номер заказа в назначении платежа. Отгрузка производится после поступления средств.</p><p>Вы можете изменить этот текст в административной панели.</p>"
        };
        await _settingService.SaveSettingAsync(settings);

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Payment.BankTransfer.AdditionalFee"] = "Дополнительная комиссия",
            ["Plugins.Payment.BankTransfer.AdditionalFee.Hint"] = "Фиксированная сумма, добавляемая к заказу.",
            ["Plugins.Payment.BankTransfer.AdditionalFeePercentage"] = "Дополнительная комиссия, %",
            ["Plugins.Payment.BankTransfer.AdditionalFeePercentage.Hint"] = "Если включено, комиссия рассчитывается в процентах от суммы заказа.",
            ["Plugins.Payment.BankTransfer.DescriptionText"] = "Инструкции по оплате",
            ["Plugins.Payment.BankTransfer.DescriptionText.Hint"] = "Текст, который увидит покупатель при выборе оплаты перечислением.",
            ["Plugins.Payment.BankTransfer.PaymentMethodDescription"] = "Оплата перечислением на расчётный счёт",
            ["Plugins.Payment.BankTransfer.ShippableProductRequired"] = "Требуется физическая доставка",
            ["Plugins.Payment.BankTransfer.ShippableProductRequired.Hint"] = "Показывать метод только если корзина содержит доставляемые товары."
        });

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<BankTransferPaymentSettings>();
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payment.BankTransfer");

        await base.UninstallAsync();
    }

    public async Task<string> GetPaymentMethodDescriptionAsync()
    {
        return await _localizationService.GetResourceAsync("Plugins.Payment.BankTransfer.PaymentMethodDescription");
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
