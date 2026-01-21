using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.CardTransfer.Models;

public record PaymentInfoModel : BaseNopModel
{
    public string DescriptionText { get; set; }
}
