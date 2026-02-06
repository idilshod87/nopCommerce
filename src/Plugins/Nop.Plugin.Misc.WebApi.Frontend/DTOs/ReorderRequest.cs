namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

/// <summary>
/// Request model for reordering an item from a previous order
/// </summary>
public class ReorderRequest
{
    /// <summary>
    /// The order ID to reorder from
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// The order item ID to reorder
    /// </summary>
    public int OrderItemId { get; set; }
}
