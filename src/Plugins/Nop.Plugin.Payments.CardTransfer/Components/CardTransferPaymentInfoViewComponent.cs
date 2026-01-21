using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.CardTransfer.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.CardTransfer.Components;

public class CardTransferViewComponent : NopViewComponent
{
    protected readonly CardTransferPaymentSettings _cardTransferPaymentSettings;
    protected readonly ILocalizationService _localizationService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    public CardTransferViewComponent(CardTransferPaymentSettings cardTransferPaymentSettings,
        ILocalizationService localizationService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _cardTransferPaymentSettings = cardTransferPaymentSettings;
        _localizationService = localizationService;
        _storeContext = storeContext;
        _workContext = workContext;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var store = await _storeContext.GetCurrentStoreAsync();

        var model = new PaymentInfoModel
        {
            DescriptionText = await _localizationService.GetLocalizedSettingAsync(_cardTransferPaymentSettings,
                x => x.DescriptionText, (await _workContext.GetWorkingLanguageAsync()).Id, store.Id)
        };

        return View("~/Plugins/Payments.CardTransfer/Views/PaymentInfo.cshtml", model);
    }
}
