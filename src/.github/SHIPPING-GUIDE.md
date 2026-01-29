# Руководство по системе доставки nopCommerce

## Общая схема работы доставки

### Основные сущности

В nopCommerce доставка основана на концепции **Shipment** (отгрузка):

- **Order** (Заказ) - главная сущность, содержит информацию о заказе и его статусе доставки
- **Shipment** (Отгрузка) - представляет физическую отправку товаров
- **ShipmentItem** (Элемент отгрузки) - связывает конкретные товары с отгрузкой

### Жизненный цикл доставки

```
Заказ создан (NotYetShipped)
    ↓
Создание Shipment (отгрузки)
    ↓
Варианты:
    A) Обычная доставка:
       → SetAsShipped (отправлено)
       → SetAsDelivered (доставлено)
    
    B) Самовывоз (PickupInStore):
       → SetAsReadyForPickup (готово к выдаче)
       → SetAsDelivered (получено)
```

### Статусы доставки заказа

Определены в `Libraries/Nop.Core/Domain/Shipping/ShippingStatus.cs`:

| Статус | Значение | Описание |
|--------|----------|----------|
| `ShippingNotRequired` | 10 | Доставка не требуется |
| `NotYetShipped` | 20 | Еще не отправлен |
| `PartiallyShipped` | 25 | Частично отправлен (товары отправляются несколькими партиями) |
| `Shipped` | 30 | Отправлен |
| `Delivered` | 40 | Доставлен |

### Данные Shipment

Основная модель: `Libraries/Nop.Core/Domain/Shipping/Shipment.cs`

```csharp
public partial class Shipment : BaseEntity
{
    public int OrderId { get; set; }              // Связь с заказом
    public string TrackingNumber { get; set; }    // Номер отслеживания
    public decimal? TotalWeight { get; set; }     // Общий вес отгрузки
    public DateTime? ShippedDateUtc { get; set; } // Дата отправки
    public DateTime? ReadyForPickupDateUtc { get; set; } // Дата готовности к самовывозу
    public DateTime? DeliveryDateUtc { get; set; } // Дата доставки
    public string AdminComment { get; set; }       // Комментарий администратора
    public DateTime CreatedOnUtc { get; set; }     // Дата создания отгрузки
}
```

## Административная панель управления

### Страница ShipmentList (Admin/Order/ShipmentList)

**Назначение**: Центральная панель управления всеми отгрузками в магазине.

**Расположение файлов**:
- View: `Presentation/Nop.Web/Areas/Admin/Views/Order/ShipmentList.cshtml`
- Controller: `Presentation/Nop.Web/Areas/Admin/Controllers/OrderController.cs`
- Factory: `Presentation/Nop.Web/Areas/Admin/Factories/OrderModelFactory.cs`
- Service: `Libraries/Nop.Services/Shipping/ShipmentService.cs`

### Колонки таблицы

- **ID** - идентификатор отгрузки
- **CustomOrderNumber** - пользовательский номер заказа
- **PickupInStore** - является ли самовывозом (да/нет)
- **TrackingNumber** - номер отслеживания посылки
- **TotalWeight** - общий вес
- **ShippedDate** - дата отправки
- **ReadyForPickupDate** - дата готовности к самовывозу
- **DeliveryDate** - дата доставки
- **View** - кнопка просмотра деталей отгрузки

### Фильтры поиска

**По датам**:
- `StartDate` - дата создания от
- `EndDate` - дата создания до

**По географии**:
- `CountryId` - страна доставки
- `StateProvinceId` - регион/штат
- `County` - округ
- `City` - город

**По параметрам**:
- `TrackingNumber` - номер отслеживания
- `WarehouseId` - склад отправки

**По статусам**:
- `LoadNotShipped` - только не отправленные
- `LoadNotReadyForPickup` - только не готовые к самовывозу
- `LoadNotDelivered` - только не доставленные

### Массовые операции

**Печать документов**:
- **Print Packaging Slips - All** - печать упаковочных листов для всех отфильтрованных отгрузок
- **Print Packaging Slips - Selected** - печать для выбранных отгрузок

**Изменение статусов**:
- **Ship Selected** - пометить выбранные как "отправленные"
- **Ready for Pickup Selected** - пометить как "готово к самовывозу"
- **Delivery Selected** - пометить как "доставлено"

### Вложенная таблица товаров

При раскрытии строки отгрузки отображается список товаров:
- Название товара
- Склад отправки
- Количество в отгрузке
- Вес товара
- Размеры товара

## Рабочий процесс (Workflow)

### 1. Создание отгрузки

**URL**: `Admin/Order/AddShipment/{orderId}`

**Процесс**:
1. Администратор открывает заказ
2. Нажимает "Add Shipment"
3. Выбирает товары и количество для отгрузки
4. Для товаров с множественными складами выбирает склад
5. Указывает номер отслеживания (опционально)
6. Сохраняет

**Код** (`OrderController.cs`):
```csharp
[HttpPost]
public virtual async Task<IActionResult> AddShipment(ShipmentModel model, IFormCollection form, bool continueEditing)
{
    // Создание Shipment
    var shipment = new Shipment
    {
        OrderId = order.Id,
        TrackingNumber = model.TrackingNumber,
        TotalWeight = null,
        AdminComment = model.AdminComment,
        CreatedOnUtc = DateTime.UtcNow
    };
    
    // Добавление товаров
    foreach (var orderItem in orderItems)
    {
        shipmentItems.Add(new ShipmentItem
        {
            OrderItemId = orderItem.Id,
            Quantity = qtyToAdd,
            WarehouseId = warehouseId
        });
    }
    
    // Сохранение и публикация событий
    await _shipmentService.InsertShipmentAsync(shipment);
    await _eventPublisher.PublishAsync(new ShipmentCreatedEvent(shipment));
}
```

### 2. Обновление статуса отгрузки

**Для обычной доставки**:
```csharp
// Пометить как отправлено
[HttpPost]
public virtual async Task<IActionResult> SetAsShipped(int id)
{
    await _orderProcessingService.ShipAsync(shipment, notifyCustomer: true);
}

// Пометить как доставлено
[HttpPost]
public virtual async Task<IActionResult> SetAsDelivered(int id)
{
    await _orderProcessingService.DeliverAsync(shipment, notifyCustomer: true);
}
```

**Для самовывоза**:
```csharp
// Пометить как готово к выдаче
[HttpPost]
public virtual async Task<IActionResult> SetAsReadyForPickup(int id)
{
    await _orderProcessingService.ReadyForPickupAsync(shipment, notifyCustomer: true);
}
```

### 3. Просмотр деталей отгрузки

**URL**: `Admin/Order/ShipmentDetails/{id}`

**Функционал**:
- Просмотр информации об отгрузке
- Редактирование номера отслеживания
- Редактирование дат (отправки, готовности к выдаче, доставки)
- Добавление комментария администратора
- Удаление отгрузки
- Печать упаковочного листа

## Ключевые сервисы

### ShipmentService

**Интерфейс**: `Libraries/Nop.Services/Shipping/IShipmentService.cs`

**Основные методы**:

```csharp
// Поиск отгрузок с фильтрами
Task<IPagedList<Shipment>> GetAllShipmentsAsync(
    int vendorId = 0, 
    int warehouseId = 0,
    int shippingCountryId = 0,
    int shippingStateId = 0,
    string shippingCounty = null,
    string shippingCity = null,
    string trackingNumber = null,
    bool loadNotShipped = false,
    bool loadNotReadyForPickup = false,
    bool loadNotDelivered = false,
    int orderId = 0,
    DateTime? createdFromUtc = null, 
    DateTime? createdToUtc = null,
    int pageIndex = 0, 
    int pageSize = int.MaxValue);

// Получение отгрузок заказа
Task<IList<Shipment>> GetShipmentsByOrderIdAsync(
    int orderId, 
    bool? shipped = null, 
    bool? readyForPickup = null, 
    int vendorId = 0);

// Получение товаров отгрузки
Task<IList<ShipmentItem>> GetShipmentItemsByShipmentIdAsync(int shipmentId);

// CRUD операции
Task InsertShipmentAsync(Shipment shipment);
Task UpdateShipmentAsync(Shipment shipment);
Task DeleteShipmentAsync(Shipment shipment);
```

### OrderProcessingService

**Основные методы для управления статусами**:

```csharp
// Пометить как отправлено
Task ShipAsync(Shipment shipment, bool notifyCustomer);

// Пометить как готово к самовывозу
Task ReadyForPickupAsync(Shipment shipment, bool notifyCustomer);

// Пометить как доставлено
Task DeliverAsync(Shipment shipment, bool notifyCustomer);
```

Эти методы автоматически:
- Обновляют даты в Shipment
- Изменяют статус заказа (Order.ShippingStatus)
- Отправляют уведомления клиенту (если указано)
- Публикуют события для интеграций
- Обновляют инвентарь на складах

## Важные особенности

### Частичные отгрузки (Partial Shipments)

Один заказ может иметь несколько отгрузок. Это используется когда:
- Товары находятся на разных складах
- Товары отправляются в разное время
- Товары частично доступны для отправки

Статус заказа становится `PartiallyShipped` если:
- Есть хотя бы одна отгрузка с датой отправки
- Но не все товары заказа отгружены

### Управление складами (Warehouse Management)

Если товар использует множественные склады (`Product.UseMultipleWarehouses = true`):
- При создании отгрузки администратор выбирает конкретный склад
- `ShipmentItem.WarehouseId` хранит информацию о складе отправки
- Система автоматически резервирует и списывает товары с указанного склада

### Vendor Filtering

Если в системе работают вендоры:
- Каждый вендор видит только отгрузки своих товаров
- Фильтрация происходит автоматически на уровне контроллера
- Используется `HasAccessToShipmentAsync()` для проверки прав

### События (Events)

Система публикует события для интеграций:

```csharp
// При создании отгрузки
public class ShipmentCreatedEvent
{
    public Shipment Shipment { get; }
}

// При установке номера отслеживания
public class ShipmentTrackingNumberSetEvent
{
    public Shipment Shipment { get; }
}
```

Плагины могут подписываться на эти события для:
- Отправки уведомлений (email, SMS, Telegram)
- Интеграции с транспортными компаниями
- Обновления внешних систем учета

## Расширение функционала

### Добавление пользовательских полей к Shipment

Пример из плагина `Nop.Plugin.Misc.Metrx`:

```csharp
// 1. Создать доменную модель
public class VendorDeliveryDate : BaseEntity
{
    public int VendorId { get; set; }
    public DateTime? DeliveryDate { get; set; }
}

// 2. Создать ViewComponent
public class VendorDeliveryDateAdminViewComponent : NopViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        // Логика отображения
    }
}

// 3. Зарегистрировать в widget zone
// AdminWidgetZones.OrderShipmentListButtons
```

### Widget Zones для кастомизации

Доступные зоны на странице ShipmentList:
- `AdminWidgetZones.OrderShipmentListButtons` - кнопки в шапке таблицы

## Лучшие практики

1. **Всегда проверяйте права доступа** через `HasAccessToShipmentAsync()` для вендоров
2. **Используйте события** вместо прямой модификации Order.ShippingStatus
3. **Не забывайте про инвентарь** - при удалении отгрузки нужно возвращать товары на склад
4. **Используйте транзакции** при создании отгрузки с несколькими товарами
5. **Логируйте изменения** через OrderNote для аудита действий

## Связанные файлы

### Основные файлы

- `Libraries/Nop.Core/Domain/Shipping/Shipment.cs` - модель Shipment
- `Libraries/Nop.Core/Domain/Shipping/ShipmentItem.cs` - модель ShipmentItem
- `Libraries/Nop.Core/Domain/Shipping/ShippingStatus.cs` - enum статусов
- `Libraries/Nop.Services/Shipping/ShipmentService.cs` - сервис работы с отгрузками
- `Libraries/Nop.Services/Orders/OrderProcessingService.cs` - обработка статусов заказа

### Административная панель

- `Presentation/Nop.Web/Areas/Admin/Controllers/OrderController.cs` - контроллер
- `Presentation/Nop.Web/Areas/Admin/Factories/OrderModelFactory.cs` - фабрика моделей
- `Presentation/Nop.Web/Areas/Admin/Views/Order/ShipmentList.cshtml` - список отгрузок
- `Presentation/Nop.Web/Areas/Admin/Views/Order/ShipmentDetails.cshtml` - детали отгрузки
- `Presentation/Nop.Web/Areas/Admin/Views/Order/AddShipment.cshtml` - создание отгрузки

### Модели

- `Presentation/Nop.Web/Areas/Admin/Models/Orders/ShipmentModel.cs`
- `Presentation/Nop.Web/Areas/Admin/Models/Orders/ShipmentListModel.cs`
- `Presentation/Nop.Web/Areas/Admin/Models/Orders/ShipmentSearchModel.cs`
- `Presentation/Nop.Web/Areas/Admin/Models/Orders/ShipmentItemModel.cs`
