using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.Terminal.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Terminal.Components;

public class TerminalViewComponent : NopViewComponent
{
    protected readonly TerminalPaymentSettings _terminalPaymentSettings;
    protected readonly ILocalizationService _localizationService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    public TerminalViewComponent(TerminalPaymentSettings terminalPaymentSettings,
        ILocalizationService localizationService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _terminalPaymentSettings = terminalPaymentSettings;
        _localizationService = localizationService;
        _storeContext = storeContext;
        _workContext = workContext;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var store = await _storeContext.GetCurrentStoreAsync();

        var model = new PaymentInfoModel
        {
            DescriptionText = await _localizationService.GetLocalizedSettingAsync(_terminalPaymentSettings,
                x => x.DescriptionText, (await _workContext.GetWorkingLanguageAsync()).Id, store.Id)
        };

        return View("~/Plugins/Payments.Terminal/Views/PaymentInfo.cshtml", model);
    }
}
