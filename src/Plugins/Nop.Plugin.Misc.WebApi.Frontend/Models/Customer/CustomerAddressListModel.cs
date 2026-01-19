using Nop.Web.Framework.Models;
using Nop.Web.Models.Common;

namespace Nop.Plugin.Misc.WebApi.Frontend.Models.Customer;

/// <summary>
/// Copy of Nop.Web.Models.Customer.CustomerAddressListModel
/// extended with default billing and shipping address identifiers
/// for use in WebApi.Frontend responses.
/// </summary>
public partial record CustomerAddressListModel : BaseNopModel
{
    public CustomerAddressListModel()
    {
        Addresses = new List<AddressModel>();
    }

    public IList<AddressModel> Addresses { get; set; }

    /// <summary>
    /// Id of the customer's current/default billing address (if any).
    /// </summary>
    public int? DefaultBillingAddressId { get; set; }

    /// <summary>
    /// Id of the customer's current/default shipping address (if any).
    /// </summary>
    public int? DefaultShippingAddressId { get; set; }
}
