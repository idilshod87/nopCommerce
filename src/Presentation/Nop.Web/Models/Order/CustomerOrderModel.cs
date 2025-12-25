using Nop.Core.Domain.Orders;
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Order;

public partial record CustomerOrderModel : BaseNopEntityModel
{
    public string CustomOrderNumber { get; set; }
    public string OrderTotal { get; set; }
    public bool IsReturnRequestAllowed { get; set; }
    public OrderStatus OrderStatusEnum { get; set; }
    public string OrderStatus { get; set; }
    public string PaymentStatus { get; set; }
    public string ShippingStatus { get; set; }
    public DateTime CreatedOn { get; set; }

    // Новые поля для истории заказов
    public string VendorName { get; set; } // Поставщик (первый из товаров заказа, если есть)
    public int VendorId { get; set; } // ID поставщика (первого товара)
    public int ItemCount { get; set; } // Количество позиций в заказе
    public decimal OrderTotalValue { get; set; } // Итоговая сумма (числом)
    public string CurrencyCode { get; set; } // Код валюты
}
