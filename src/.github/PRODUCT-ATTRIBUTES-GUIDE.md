# Руководство по атрибутам товаров (Product Attributes) в nopCommerce

## Общая схема работы атрибутов товаров

### Назначение

Атрибуты товаров позволяют создавать вариации товаров с различными характеристиками (размер, цвет, материал и т.д.), которые могут влиять на:
- **Цену** товара (добавочная стоимость)
- **Вес** товара
- **SKU/GTIN/MPN** (артикул может меняться в зависимости от выбранных атрибутов)
- **Изображение** товара (разные картинки для разных вариантов)
- **Наличие** товара (через комбинации атрибутов)

### Трёхуровневая архитектура

```
1. ProductAttribute (Глобальный атрибут)
   ↓
2. ProductAttributeMapping (Привязка к товару)
   ↓
3. ProductAttributeValue (Конкретные значения)
```

## Структура данных

### 1. ProductAttribute (Глобальный атрибут)

**Файл**: `Libraries/Nop.Core/Domain/Catalog/ProductAttribute.cs`

```csharp
public partial class ProductAttribute : BaseEntity, ILocalizedEntity
{
    public string Name { get; set; }        // Название атрибута (например, "Размер", "Цвет")
    public string Description { get; set; } // Описание атрибута
}
```

**Особенности**:
- Атрибуты создаются **глобально** в системе через `Admin → Catalog → Attributes → Product attributes`
- Один атрибут может быть использован в разных товарах
- Поддерживает локализацию (разные языки)

**Примеры**: "Цвет", "Размер", "Материал", "Память", "Процессор"

### 2. ProductAttributeMapping (Привязка к товару)

**Файл**: `Libraries/Nop.Core/Domain/Catalog/ProductAttributeMapping.cs`

```csharp
public partial class ProductAttributeMapping : BaseEntity, ILocalizedEntity
{
    public int ProductId { get; set; }                    // ID товара
    public int ProductAttributeId { get; set; }           // ID глобального атрибута
    public string TextPrompt { get; set; }                // Текст подсказки
    public bool IsRequired { get; set; }                  // Обязателен ли
    public int AttributeControlTypeId { get; set; }       // Тип контрола
    public int DisplayOrder { get; set; }                 // Порядок отображения
    
    // Валидация
    public int? ValidationMinLength { get; set; }
    public int? ValidationMaxLength { get; set; }
    public string ValidationFileAllowedExtensions { get; set; }
    public int? ValidationFileMaximumSize { get; set; }
    
    public string DefaultValue { get; set; }              // Значение по умолчанию
    
    // Условная видимость
    public string ConditionAttributeXml { get; set; }     // Условие отображения
    
    public AttributeControlType AttributeControlType { get; set; }
}
```

**Назначение**:
- Связывает глобальный атрибут с конкретным товаром
- Определяет **как** атрибут будет отображаться (dropdown, radio, checkbox и т.д.)
- Устанавливает правила валидации
- Может зависеть от других атрибутов (условная видимость)

### 3. ProductAttributeValue (Значения атрибута)

**Файл**: `Libraries/Nop.Core/Domain/Catalog/ProductAttributeValue.cs`

```csharp
public partial class ProductAttributeValue : BaseEntity, ILocalizedEntity
{
    public int ProductAttributeMappingId { get; set; }    // ID маппинга
    public int AttributeValueTypeId { get; set; }          // Тип значения
    public int AssociatedProductId { get; set; }           // Связанный товар
    public string Name { get; set; }                       // Название значения
    
    // Визуальные элементы
    public string ColorSquaresRgb { get; set; }           // Цвет для ColorSquares
    public int ImageSquaresPictureId { get; set; }        // Картинка для ImageSquares
    
    // Влияние на цену и вес
    public decimal PriceAdjustment { get; set; }          // Добавка к цене
    public bool PriceAdjustmentUsePercentage { get; set; } // Процент или фикс.сумма
    public decimal WeightAdjustment { get; set; }         // Добавка к весу
    public decimal Cost { get; set; }                     // Себестоимость
    
    // Для связанных товаров
    public bool CustomerEntersQty { get; set; }           // Клиент вводит количество
    public int Quantity { get; set; }                     // Количество по умолчанию
    
    public bool IsPreSelected { get; set; }               // Выбрано по умолчанию
    public int DisplayOrder { get; set; }                 // Порядок отображения
    
    public AttributeValueType AttributeValueType { get; set; }
}
```

**Примеры значений**:
- Для атрибута "Размер": "S", "M", "L", "XL"
- Для атрибута "Цвет": "Красный", "Синий", "Зелёный"
- Для атрибута "Память": "64GB (+$50)", "128GB (+$100)", "256GB (+$200)"

## Типы контролов (AttributeControlType)

**Файл**: `Libraries/Nop.Core/Domain/Catalog/AttributeControlType.cs`

| Тип | Значение | Описание | Применение |
|-----|----------|----------|------------|
| `DropdownList` | 1 | Выпадающий список | Выбор одного из многих (размер, цвет) |
| `RadioList` | 2 | Радио-кнопки | Выбор одного варианта с визуальным списком |
| `Checkboxes` | 3 | Чекбоксы | Выбор нескольких вариантов |
| `TextBox` | 4 | Текстовое поле | Ввод текста (гравировка, имя) |
| `MultilineTextbox` | 10 | Многострочное поле | Ввод длинного текста (послание) |
| `Datepicker` | 20 | Выбор даты | Дата доставки, дата на подарке |
| `FileUpload` | 30 | Загрузка файла | Загрузка дизайна, фото |
| `ColorSquares` | 40 | Цветные квадраты | Визуальный выбор цвета |
| `ImageSquares` | 50 | Картинки-квадраты | Визуальный выбор с изображениями |

## Хранение выбранных атрибутов в корзине

### Формат AttributesXml

Выбранные клиентом атрибуты хранятся в **XML-формате** в поле `ShoppingCartItem.AttributesXml`:

**Файл**: `Libraries/Nop.Core/Domain/Orders/ShoppingCartItem.cs`

```csharp
public partial class ShoppingCartItem : BaseEntity
{
    public int ProductId { get; set; }
    public string AttributesXml { get; set; }   // ← Атрибуты в XML
    public int Quantity { get; set; }
    // ... другие поля
}
```

### Пример XML атрибутов

```xml
<Attributes>
    <ProductAttribute ID="1">
        <ProductAttributeValue>
            <Value>5</Value>        <!-- ID значения атрибута -->
        </ProductAttributeValue>
    </ProductAttribute>
    <ProductAttribute ID="2">
        <ProductAttributeValue>
            <Value>12</Value>
        </ProductAttributeValue>
    </ProductAttribute>
    <ProductAttribute ID="3">
        <ProductAttributeValue>
            <Value>Текст для гравировки</Value>
        </ProductAttributeValue>
    </ProductAttribute>
</Attributes>
```

**Расшифровка**:
- `ID="1"` - ProductAttributeMappingId (привязка атрибута "Цвет" к этому товару)
- `Value="5"` - ProductAttributeValueId (значение "Красный")
- `ID="2"` - ProductAttributeMappingId (привязка атрибута "Размер")
- `Value="12"` - ProductAttributeValueId (значение "XL")
- `ID="3"` - ProductAttributeMappingId (текстовое поле "Гравировка")
- `Value="..."` - Текст, введённый клиентом

## Workflow: Добавление товара в корзину с атрибутами

### 1. Отображение товара с атрибутами

**Controller**: `Presentation/Nop.Web/Controllers/ProductController.cs`

```csharp
public virtual async Task<IActionResult> ProductDetails(int productId, int updatecartitemid = 0)
{
    var model = await _productModelFactory.PrepareProductDetailsModelAsync(
        product, 
        updatecartitem, 
        isAssociatedProduct: false);
    
    // model.ProductAttributes содержит все атрибуты товара
    return View(model);
}
```

**Factory**: `Presentation/Nop.Web/Factories/ProductModelFactory.cs`

```csharp
protected virtual async Task<IList<ProductDetailsModel.ProductAttributeModel>> 
    PrepareProductAttributeModelsAsync(Product product, ShoppingCartItem updatecartitem)
{
    var model = new List<ProductDetailsModel.ProductAttributeModel>();
    
    // Получаем все атрибуты товара
    var productAttributeMapping = await _productAttributeService
        .GetProductAttributeMappingsByProductIdAsync(product.Id);
    
    foreach (var attribute in productAttributeMapping)
    {
        var attributeModel = new ProductDetailsModel.ProductAttributeModel
        {
            Id = attribute.Id,
            ProductId = product.Id,
            ProductAttributeId = attribute.ProductAttributeId,
            Name = await _localizationService.GetLocalizedAsync(attribute.ProductAttribute, x => x.Name),
            TextPrompt = await _localizationService.GetLocalizedAsync(attribute, x => x.TextPrompt),
            IsRequired = attribute.IsRequired,
            AttributeControlType = attribute.AttributeControlType,
            DefaultValue = attribute.DefaultValue
        };
        
        // Заполняем значения атрибута
        if (attribute.ShouldHaveValues())
        {
            var attributeValues = await _productAttributeService
                .GetProductAttributeValuesAsync(attribute.Id);
            
            foreach (var attributeValue in attributeValues)
            {
                var valueModel = new ProductDetailsModel.ProductAttributeValueModel
                {
                    Id = attributeValue.Id,
                    Name = await _localizationService.GetLocalizedAsync(attributeValue, x => x.Name),
                    ColorSquaresRgb = attributeValue.ColorSquaresRgb,
                    IsPreSelected = attributeValue.IsPreSelected,
                    PriceAdjustment = priceAdjustmentValue,
                    PriceAdjustmentValue = priceAdjustmentValue
                };
                
                attributeModel.Values.Add(valueModel);
            }
        }
        
        model.Add(attributeModel);
    }
    
    return model;
}
```

### 2. Обработка изменения атрибутов (динамическое обновление цены)

**Endpoint**: `POST /ShoppingCart/ProductDetails_AttributeChange`

```csharp
[HttpPost]
public virtual async Task<IActionResult> ProductDetails_AttributeChange(
    int productId, 
    bool validateAttributeConditions,
    bool loadPicture, 
    IFormCollection form)
{
    var product = await _productService.GetProductByIdAsync(productId);
    
    // Парсим выбранные атрибуты из формы
    var errors = new List<string>();
    var attributeXml = await _productAttributeParser
        .ParseProductAttributesAsync(product, form, errors);
    
    // Получаем новую цену с учётом атрибутов
    var (finalPrice, _, _) = await _shoppingCartService.GetUnitPriceAsync(
        product,
        currentCustomer,
        currentStore,
        ShoppingCartType.ShoppingCart,
        1, 
        attributeXml,  // ← Атрибуты влияют на цену
        0,
        rentalStartDate, 
        rentalEndDate, 
        true);
    
    // Получаем новые SKU/GTIN/MPN
    var sku = await _productService.FormatSkuAsync(product, attributeXml);
    
    // Получаем новое изображение (если атрибут меняет картинку)
    var picture = await _pictureService.GetProductPictureAsync(product, attributeXml);
    
    return Json(new
    {
        productId,
        gtin,
        mpn,
        sku,
        price = await _priceFormatter.FormatPriceAsync(finalPrice),
        pictureDefaultSizeUrl,
        pictureFullSizeUrl,
        enabledattributemappingids,
        disabledattributemappingids
    });
}
```

### 3. Парсинг атрибутов из формы

**Service**: `Libraries/Nop.Services/Catalog/ProductAttributeParser.cs`

```csharp
public virtual async Task<string> ParseProductAttributesAsync(
    Product product, 
    IFormCollection form, 
    List<string> errors)
{
    var attributesXml = string.Empty;
    
    var productAttributes = await _productAttributeService
        .GetProductAttributeMappingsByProductIdAsync(product.Id);
    
    foreach (var attribute in productAttributes)
    {
        var controlId = $"{NopCatalogDefaults.ProductAttributePrefix}{attribute.Id}";
        
        switch (attribute.AttributeControlType)
        {
            case AttributeControlType.DropdownList:
            case AttributeControlType.RadioList:
            case AttributeControlType.ColorSquares:
            case AttributeControlType.ImageSquares:
            {
                var ctrlAttributes = form[controlId];
                if (!StringValues.IsNullOrEmpty(ctrlAttributes))
                {
                    var selectedAttributeId = int.Parse(ctrlAttributes);
                    if (selectedAttributeId > 0)
                    {
                        // Добавляем выбранное значение в XML
                        attributesXml = AddProductAttribute(
                            attributesXml, 
                            attribute, 
                            selectedAttributeId.ToString());
                    }
                }
                break;
            }
            
            case AttributeControlType.Checkboxes:
            {
                var cblAttributes = form[controlId];
                if (!StringValues.IsNullOrEmpty(cblAttributes))
                {
                    foreach (var item in cblAttributes.ToString()
                        .Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var selectedAttributeId = int.Parse(item);
                        if (selectedAttributeId > 0)
                        {
                            attributesXml = AddProductAttribute(
                                attributesXml, 
                                attribute, 
                                selectedAttributeId.ToString());
                        }
                    }
                }
                break;
            }
            
            case AttributeControlType.TextBox:
            case AttributeControlType.MultilineTextbox:
            {
                var ctrlAttributes = form[controlId];
                if (!StringValues.IsNullOrEmpty(ctrlAttributes))
                {
                    var enteredText = ctrlAttributes.ToString().Trim();
                    
                    attributesXml = AddProductAttribute(
                        attributesXml, 
                        attribute, 
                        enteredText);
                }
                break;
            }
            
            case AttributeControlType.Datepicker:
            {
                var day = form[controlId + "_day"];
                var month = form[controlId + "_month"];
                var year = form[controlId + "_year"];
                
                if (int.TryParse(day, out var selectedDay) &&
                    int.TryParse(month, out var selectedMonth) &&
                    int.TryParse(year, out var selectedYear))
                {
                    var selectedDate = new DateTime(selectedYear, selectedMonth, selectedDay);
                    attributesXml = AddProductAttribute(
                        attributesXml, 
                        attribute, 
                        selectedDate.ToString("D"));
                }
                break;
            }
            
            case AttributeControlType.FileUpload:
            {
                var downloadGuid = form[controlId];
                if (!StringValues.IsNullOrEmpty(downloadGuid))
                {
                    attributesXml = AddProductAttribute(
                        attributesXml, 
                        attribute, 
                        downloadGuid);
                }
                break;
            }
        }
    }
    
    return attributesXml;
}
```

### 4. Добавление товара в корзину

**Controller**: `Presentation/Nop.Web/Controllers/ShoppingCartController.cs`

```csharp
[HttpPost]
public virtual async Task<IActionResult> AddProductToCart_Details(
    int productId, 
    int shoppingCartTypeId, 
    IFormCollection form)
{
    var product = await _productService.GetProductByIdAsync(productId);
    
    var addToCartWarnings = new List<string>();
    
    // Парсим атрибуты из формы
    var attributes = await _productAttributeParser
        .ParseProductAttributesAsync(product, form, addToCartWarnings);
    
    // Парсим количество
    var quantity = _productAttributeParser.ParseEnteredQuantity(product, form);
    
    // Добавляем в корзину
    await _shoppingCartService.AddToCartAsync(
        customer: await _workContext.GetCurrentCustomerAsync(),
        product: product,
        shoppingCartType: (ShoppingCartType)shoppingCartTypeId,
        storeId: (await _storeContext.GetCurrentStoreAsync()).Id,
        attributesXml: attributes,  // ← Сохраняем атрибуты в XML
        quantity: quantity);
    
    return Json(new { success = true });
}
```

### 5. Валидация атрибутов

**Service**: `Libraries/Nop.Services/Orders/ShoppingCartService.cs`

```csharp
public virtual async Task<IList<string>> GetShoppingCartItemAttributeWarningsAsync(
    Customer customer,
    ShoppingCartType shoppingCartType,
    Product product,
    int quantity = 1,
    string attributesXml = null,
    bool ignoreNonCombinableAttributes = false,
    bool ignoreConditionMet = false)
{
    var warnings = new List<string>();
    
    var attributes = await _productAttributeParser
        .ParseProductAttributeMappingsAsync(attributesXml);
    
    // Проверяем обязательные атрибуты
    foreach (var attribute in requiredAttributes)
    {
        if (!attributes.Any(a => a.Id == attribute.Id))
        {
            warnings.Add(string.Format(
                await _localizationService.GetResourceAsync("ShoppingCart.SelectAttribute"),
                await _localizationService.GetLocalizedAsync(attribute.ProductAttribute, a => a.Name)));
        }
    }
    
    // Валидация текстовых полей
    foreach (var attribute in attributes)
    {
        if (attribute.AttributeControlType == AttributeControlType.TextBox ||
            attribute.AttributeControlType == AttributeControlType.MultilineTextbox)
        {
            var enteredText = _productAttributeParser
                .ParseValues(attributesXml, attribute.Id).FirstOrDefault();
            
            if (attribute.ValidationMinLength.HasValue &&
                enteredText?.Length < attribute.ValidationMinLength.Value)
            {
                warnings.Add(string.Format(
                    "Minimum length is {0}", 
                    attribute.ValidationMinLength));
            }
        }
    }
    
    return warnings;
}
```

## Отображение атрибутов в корзине

### Форматирование атрибутов для отображения

**Service**: `Libraries/Nop.Services/Catalog/ProductAttributeFormatter.cs`

```csharp
public virtual async Task<string> FormatAttributesAsync(
    Product product, 
    string attributesXml)
{
    var customer = await _workContext.GetCurrentCustomerAsync();
    var result = new StringBuilder();
    
    var attributes = await _productAttributeParser
        .ParseProductAttributeMappingsAsync(attributesXml);
    
    for (var i = 0; i < attributes.Count; i++)
    {
        var attribute = attributes[i];
        var attributeName = await _localizationService
            .GetLocalizedAsync(attribute.ProductAttribute, a => a.Name);
        
        result.Append($"<strong>{attributeName}:</strong> ");
        
        if (attribute.ShouldHaveValues())
        {
            var attributeValues = await _productAttributeParser
                .ParseProductAttributeValuesAsync(attributesXml, attribute.Id);
            
            for (var j = 0; j < attributeValues.Count; j++)
            {
                var valueStr = await _localizationService
                    .GetLocalizedAsync(attributeValues[j], a => a.Name);
                
                result.Append(valueStr);
                
                // Показываем цену, если есть
                if (_shoppingCartSettings.RenderAssociatedAttributeValueQuantity &&
                    attributeValues[j].AttributeValueType == AttributeValueType.AssociatedToProduct)
                {
                    if (attributeValues[j].Quantity > 0)
                        result.Append($" (x{attributeValues[j].Quantity})");
                }
                
                if (j != attributeValues.Count - 1)
                    result.Append(", ");
            }
        }
        else
        {
            // Текстовый атрибут
            var valuesStr = _productAttributeParser
                .ParseValues(attributesXml, attribute.Id);
            
            for (var j = 0; j < valuesStr.Count; j++)
            {
                result.Append(WebUtility.HtmlEncode(valuesStr[j]));
                
                if (j != valuesStr.Count - 1)
                    result.Append(", ");
            }
        }
        
        if (i != attributes.Count - 1)
            result.Append("<br />");
    }
    
    return result.ToString();
}
```

### Модель корзины

**Factory**: `Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs`

```csharp
protected virtual async Task<ShoppingCartModel.ShoppingCartItemModel> 
    PrepareShoppingCartItemModelAsync(
        IList<ShoppingCartItem> cart, 
        ShoppingCartItem sci)
{
    var product = await _productService.GetProductByIdAsync(sci.ProductId);
    
    var cartItemModel = new ShoppingCartModel.ShoppingCartItemModel
    {
        Id = sci.Id,
        ProductId = sci.ProductId,
        ProductName = await _localizationService.GetLocalizedAsync(product, x => x.Name),
        Quantity = sci.Quantity,
        
        // Форматированные атрибуты для отображения
        AttributeInfo = await _productAttributeFormatter
            .FormatAttributesAsync(product, sci.AttributesXml)
    };
    
    // Цена с учётом атрибутов
    var (unitPrice, _, _) = await _shoppingCartService.GetUnitPriceAsync(sci, true);
    var (shoppingCartUnitPriceWithDiscountBase, _) = await _taxService
        .GetProductPriceAsync(product, unitPrice);
    
    cartItemModel.UnitPrice = await _priceFormatter
        .FormatPriceAsync(shoppingCartUnitPriceWithDiscountBase);
    
    return cartItemModel;
}
```

## Расчёт цены с учётом атрибутов

**Service**: `Libraries/Nop.Services/Catalog/PriceCalculationService.cs`

```csharp
public virtual async Task<decimal> GetProductAttributeValuePriceAdjustmentAsync(
    Product product,
    ProductAttributeValue attributeValue,
    Customer customer,
    Store store,
    decimal productPrice = decimal.Zero,
    int quantity = 1)
{
    if (attributeValue == null)
        return decimal.Zero;
    
    var adjustment = decimal.Zero;
    
    switch (attributeValue.AttributeValueType)
    {
        case AttributeValueType.Simple:
        {
            // Простая добавка к цене
            if (attributeValue.PriceAdjustmentUsePercentage)
            {
                // Процент от базовой цены
                adjustment = productPrice * attributeValue.PriceAdjustment / 100;
            }
            else
            {
                // Фиксированная сумма
                adjustment = attributeValue.PriceAdjustment;
            }
            break;
        }
        
        case AttributeValueType.AssociatedToProduct:
        {
            // Связанный товар (например, добавить аксессуар)
            var associatedProduct = await _productService
                .GetProductByIdAsync(attributeValue.AssociatedProductId);
            
            if (associatedProduct != null)
            {
                var qty = attributeValue.Quantity;
                var (associatedProductPrice, _, _) = await GetFinalPriceAsync(
                    associatedProduct,
                    customer,
                    store,
                    includeDiscounts: true);
                
                adjustment = associatedProductPrice * qty;
            }
            break;
        }
    }
    
    return adjustment;
}
```

## Комбинации атрибутов

### ProductAttributeCombination

Позволяет создавать предопределённые комбинации атрибутов с:
- Уникальным SKU
- Отдельным остатком
- Индивидуальной ценой
- Собственными изображениями

**Файл**: `Libraries/Nop.Core/Domain/Catalog/ProductAttributeCombination.cs`

```csharp
public partial class ProductAttributeCombination : BaseEntity
{
    public int ProductId { get; set; }
    public string AttributesXml { get; set; }      // Комбинация атрибутов
    public int StockQuantity { get; set; }         // Остаток для этой комбинации
    public bool AllowOutOfStockOrders { get; set; }
    public string Sku { get; set; }                // Уникальный SKU
    public string ManufacturerPartNumber { get; set; }
    public string Gtin { get; set; }
    public decimal? OverriddenPrice { get; set; }  // Цена для комбинации
    public int? NotifyAdminForQuantityBelow { get; set; }
}
```

**Пример**: 
- Товар: "Футболка"
- Комбинация 1: Размер=S, Цвет=Красный → SKU: "TSHIRT-S-RED", Stock: 50
- Комбинация 2: Размер=M, Цвет=Синий → SKU: "TSHIRT-M-BLUE", Stock: 30

## Условная видимость атрибутов

Атрибуты могут отображаться в зависимости от выбора других атрибутов через поле `ConditionAttributeXml`.

**Пример**:
```
Атрибут: "Тип футболки"
  - Обычная
  - С персонализацией

Атрибут: "Текст гравировки" (показывается только если выбрано "С персонализацией")
```

**Проверка условия**:

```csharp
public virtual async Task<bool?> IsConditionMetAsync(
    ProductAttributeMapping pam,
    string selectedAttributesXml)
{
    if (string.IsNullOrEmpty(pam.ConditionAttributeXml))
        return null; // Нет условия
    
    var currentAttributesXml = pam.ConditionAttributeXml;
    var conditionAttributes = await ParseProductAttributeMappingsAsync(currentAttributesXml);
    
    foreach (var attribute in conditionAttributes)
    {
        var conditionValues = await ParseProductAttributeValuesAsync(currentAttributesXml);
        var selectedValues = await ParseProductAttributeValuesAsync(selectedAttributesXml);
        
        // Проверяем, что выбранные значения соответствуют условию
        if (!conditionValues.All(cv => selectedValues.Any(sv => sv.Id == cv.Id)))
            return false;
    }
    
    return true;
}
```

## Связь с заказами

Когда заказ создаётся из корзины, `AttributesXml` копируется в `OrderItem`:

```csharp
public partial class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string AttributesXml { get; set; }  // ← Атрибуты сохраняются
    public int Quantity { get; set; }
    public decimal UnitPriceInclTax { get; set; }
    public decimal UnitPriceExclTax { get; set; }
}
```

Это позволяет:
- Сохранить историю того, что именно было куплено
- Отобразить детали заказа в личном кабинете и админке
- Повторить заказ с теми же атрибутами

## Практические примеры

### Пример 1: Футболка с размером и цветом

**Настройка**:

1. Создать глобальный атрибут "Размер" (`Admin → Catalog → Attributes → Product attributes`)
2. Создать глобальный атрибут "Цвет"
3. В товаре "Футболка" добавить атрибут "Размер":
   - Тип контрола: `DropdownList`
   - Обязательный: `Да`
   - Значения: S, M, L, XL (каждое с `PriceAdjustment = 0`)
4. Добавить атрибут "Цвет":
   - Тип контрола: `ColorSquares`
   - Обязательный: `Да`
   - Значения:
     - Красный (RGB: #FF0000)
     - Синий (RGB: #0000FF)
     - Зелёный (RGB: #00FF00)

**Результат в корзине**:

```xml
<Attributes>
    <ProductAttribute ID="1"><!-- Размер -->
        <ProductAttributeValue>
            <Value>7</Value><!-- M -->
        </ProductAttributeValue>
    </ProductAttribute>
    <ProductAttribute ID="2"><!-- Цвет -->
        <ProductAttributeValue>
            <Value>15</Value><!-- Красный -->
        </ProductAttributeValue>
    </ProductAttribute>
</Attributes>
```

### Пример 2: Ноутбук с конфигурацией

**Настройка**:

1. Атрибут "Процессор":
   - Тип: `RadioList`
   - Значения:
     - Intel i5 (базовая цена)
     - Intel i7 (+$150)
     - Intel i9 (+$300)
2. Атрибут "Память":
   - Тип: `RadioList`
   - Значения:
     - 8GB
     - 16GB (+$80)
     - 32GB (+$200)
3. Атрибут "SSD":
   - Тип: `RadioList`
   - Значения:
     - 256GB
     - 512GB (+$100)
     - 1TB (+$250)

**Расчёт цены**:
- Базовая цена: $1000
- Выбрано: i7 (+$150), 16GB (+$80), 512GB (+$100)
- **Итоговая цена: $1330**

### Пример 3: Подарочная карта

**Настройка**:

1. Атрибут "Имя получателя":
   - Тип: `TextBox`
   - Обязательный: `Да`
   - Валидация: мин. 2 символа, макс. 50
2. Атрибут "Email получателя":
   - Тип: `TextBox`
   - Обязательный: `Да`
3. Атрибут "Сообщение":
   - Тип: `MultilineTextbox`
   - Обязательный: `Нет`
   - Валидация: макс. 500 символов
4. Атрибут "Дата доставки":
   - Тип: `Datepicker`
   - Обязательный: `Да`

**Результат в корзине**:

```xml
<Attributes>
    <ProductAttribute ID="10">
        <ProductAttributeValue>
            <Value>Иван Иванов</Value>
        </ProductAttributeValue>
    </ProductAttribute>
    <ProductAttribute ID="11">
        <ProductAttributeValue>
            <Value>ivan@example.com</Value>
        </ProductAttributeValue>
    </ProductAttribute>
    <ProductAttribute ID="12">
        <ProductAttributeValue>
            <Value>С Днём Рождения!</Value>
        </ProductAttributeValue>
    </ProductAttribute>
    <ProductAttribute ID="13">
        <ProductAttributeValue>
            <Value>15.02.2026</Value>
        </ProductAttributeValue>
    </ProductAttribute>
</Attributes>
```

## Важные сервисы и интерфейсы

### ProductAttributeService

**Файл**: `Libraries/Nop.Services/Catalog/ProductAttributeService.cs`

**Основные методы**:

```csharp
// Получить все маппинги атрибутов для товара
Task<IList<ProductAttributeMapping>> GetProductAttributeMappingsByProductIdAsync(int productId);

// Получить значения атрибута
Task<IList<ProductAttributeValue>> GetProductAttributeValuesAsync(int productAttributeMappingId);

// Получить комбинации атрибутов
Task<IList<ProductAttributeCombination>> GetAllProductAttributeCombinationsAsync(int productId);

// Найти комбинацию по AttributesXml
Task<ProductAttributeCombination> FindProductAttributeCombinationAsync(
    Product product, 
    string attributesXml);
```

### ProductAttributeParser

**Файл**: `Libraries/Nop.Services/Catalog/ProductAttributeParser.cs`

**Основные методы**:

```csharp
// Парсинг атрибутов из формы
Task<string> ParseProductAttributesAsync(
    Product product, 
    IFormCollection form, 
    List<string> errors);

// Получить выбранные маппинги
Task<IList<ProductAttributeMapping>> ParseProductAttributeMappingsAsync(string attributesXml);

// Получить выбранные значения
Task<IList<ProductAttributeValue>> ParseProductAttributeValuesAsync(
    string attributesXml, 
    int productAttributeMappingId = 0);

// Добавить атрибут в XML
string AddProductAttribute(
    string attributesXml, 
    ProductAttributeMapping productAttributeMapping, 
    string value, 
    int? quantity = null);

// Удалить атрибут из XML
string RemoveProductAttribute(
    string attributesXml, 
    ProductAttributeMapping productAttributeMapping);

// Сравнить атрибуты
Task<bool> AreProductAttributesEqualAsync(
    string attributesXml1, 
    string attributesXml2, 
    bool ignoreNonCombinableAttributes, 
    bool ignoreQuantity = true);
```

### ProductAttributeFormatter

**Файл**: `Libraries/Nop.Services/Catalog/ProductAttributeFormatter.cs`

**Основные методы**:

```csharp
// Форматирование атрибутов для отображения (HTML)
Task<string> FormatAttributesAsync(
    Product product, 
    string attributesXml,
    Customer customer = null,
    string separator = "<br />",
    bool htmlEncode = true,
    bool renderPrices = true,
    bool renderProductAttributes = true,
    bool renderGiftCardAttributes = true,
    bool allowHyperlinks = true);
```

## Лучшие практики

### 1. Использование глобальных атрибутов

✅ **Правильно**: Создать глобальный атрибут "Размер" и использовать его во всех товарах

❌ **Неправильно**: Создавать отдельный атрибут "Размер футболки", "Размер брюк" и т.д.

### 2. Именование атрибутов

✅ **Правильно**: 
- "Цвет"
- "Размер"
- "Материал"

❌ **Неправильно**:
- "Выберите цвет товара"
- "Size" (если сайт на русском)
- "color_attribute_1"

### 3. Порядок атрибутов

Используйте `DisplayOrder` для логичной последовательности:
1. Основные характеристики (размер, цвет)
2. Дополнительные опции (гравировка, упаковка)
3. Персонализация (текстовые поля)

### 4. Производительность

- **Кэшируйте** атрибуты товаров при отображении списка
- **Индексируйте** AttributesXml для быстрого поиска
- Используйте **комбинации атрибутов** для часто покупаемых вариантов

### 5. Валидация

Всегда устанавливайте валидацию для текстовых полей:
- Минимальная и максимальная длина
- Регулярные выражения (для email, телефона)
- Допустимые расширения файлов

## Связанные файлы

### Основные модели

- `Libraries/Nop.Core/Domain/Catalog/ProductAttribute.cs`
- `Libraries/Nop.Core/Domain/Catalog/ProductAttributeMapping.cs`
- `Libraries/Nop.Core/Domain/Catalog/ProductAttributeValue.cs`
- `Libraries/Nop.Core/Domain/Catalog/ProductAttributeCombination.cs`
- `Libraries/Nop.Core/Domain/Catalog/AttributeControlType.cs`

### Сервисы

- `Libraries/Nop.Services/Catalog/ProductAttributeService.cs`
- `Libraries/Nop.Services/Catalog/IProductAttributeService.cs`
- `Libraries/Nop.Services/Catalog/ProductAttributeParser.cs`
- `Libraries/Nop.Services/Catalog/IProductAttributeParser.cs`
- `Libraries/Nop.Services/Catalog/ProductAttributeFormatter.cs`
- `Libraries/Nop.Services/Catalog/IProductAttributeFormatter.cs`

### Контроллеры

- `Presentation/Nop.Web/Controllers/ProductController.cs`
- `Presentation/Nop.Web/Controllers/ShoppingCartController.cs`
- `Presentation/Nop.Web/Areas/Admin/Controllers/ProductController.cs`

### Фабрики

- `Presentation/Nop.Web/Factories/ProductModelFactory.cs`
- `Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs`
- `Presentation/Nop.Web/Areas/Admin/Factories/ProductModelFactory.cs`

### Модели представлений

- `Presentation/Nop.Web/Models/Catalog/ProductDetailsModel.cs`
- `Presentation/Nop.Web/Models/ShoppingCart/ShoppingCartModel.cs`
- `Presentation/Nop.Web/Areas/Admin/Models/Catalog/ProductAttributeModel.cs`
- `Presentation/Nop.Web/Areas/Admin/Models/Catalog/ProductAttributeValueModel.cs`

### Представления

- `Presentation/Nop.Web/Views/Product/ProductTemplate.Simple.cshtml`
- `Presentation/Nop.Web/Views/ShoppingCart/Cart.cshtml`
- `Presentation/Nop.Web/Areas/Admin/Views/Product/ProductAttributeMappings.cshtml`
