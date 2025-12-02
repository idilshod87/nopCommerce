using Nop.Core;
using Nop.Core.Domain.Customers;

namespace Nop.Plugin.Api.Domain;

/// <summary>
/// Represents an external login (e.g., Telegram, Google)
/// </summary>
public class CustomerLogin : BaseEntity
{
    public string LoginProvider { get; set; }
    public string ProviderKey { get; set; }
    public string ProviderDisplayName { get; set; }
    public int CustomerId { get; set; }

    public virtual Customer Customer { get; set; }
}
