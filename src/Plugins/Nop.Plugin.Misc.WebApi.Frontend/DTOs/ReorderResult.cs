namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

/// <summary>
/// Result model for reorder operation
/// </summary>
public class ReorderResult
{
    /// <summary>
    /// Shopping cart item ID
    /// </summary>
    public int CartItemId { get; set; }

    /// <summary>
    /// Indicates whether the item was already in the cart
    /// </summary>
    public bool WasAlreadyInCart { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; }
}
