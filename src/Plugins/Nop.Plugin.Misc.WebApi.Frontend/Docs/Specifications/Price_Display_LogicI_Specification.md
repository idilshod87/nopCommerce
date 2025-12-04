# Логика отображения цены на карточке товара

## Структура данных (ProductPriceModel)

```json
{
  "productPrice": {
    "oldPrice": "string или null",           // Старая цена (если есть)
    "oldPriceValue": "decimal или null",     // Числовое значение старой цены
    "price": "string",                       // Текущая цена (форматированная, например "$2.80")
    "priceValue": "decimal",                 // Числовое значение текущей цены
    "basePricePAngV": "string или null",     // Базовая цена с единицей измерения (например "8000 сум / кг")
    "basePricePAngVValue": "decimal или null", // Числовое значение базовой цены
    "hidePrices": "boolean",                 // Скрывать ли цены
    "callForPrice": "boolean",               // Показывать ли "Call for price"
    "customerEntersPrice": "boolean",        // Вводит ли клиент цену самостоятельно
    "currencyCode": "string"                 // Код валюты (например "USD")
  }
}
```

## Алгоритм отображения

### Шаг 1: Проверка флагов

```javascript
// Псевдокод
if (productPrice.hidePrices === true) {
  // Не показывать цену вообще
  return;
}

if (productPrice.callForPrice === true) {
  // Показать текст "Call for price" или "Узнать цену"
  return;
}

if (productPrice.customerEntersPrice === true) {
  // Цена вводится клиентом - показать соответствующее поле ввода
  return;
}
```

### Шаг 2: Отображение цены

```javascript
// Основная логика отображения

// 1. Старая цена (зачеркнутая) - если есть
if (productPrice.oldPrice && productPrice.oldPrice.trim() !== '') {
  displayOldPrice(productPrice.oldPrice); // Зачеркнутый текст
}

// 2. Текущая цена
displayCurrentPrice(productPrice.price); // Обычный текст, жирный

// 3. Базовая цена с единицей измерения (если есть)
if (productPrice.basePricePAngV && productPrice.basePricePAngV.trim() !== '') {
  displayBasePrice(productPrice.basePricePAngV); // Например: "8000 сум / кг"
}
```

## Примеры UI отображения

### Пример 1: Товар со старой ценой и базовой ценой

```
┌─────────────────────────────┐
│  ~~$5.00~~  $2.80          │  ← Старая цена зачеркнута, новая цена
│  8000 сум / кг              │  ← Базовая цена с единицей измерения
└─────────────────────────────┘
```

### Пример 2: Товар без старой цены

```
┌─────────────────────────────┐
│  $2.80                      │  ← Текущая цена
│  8000 сум / кг              │  ← Базовая цена
└─────────────────────────────┘
```

### Пример 3: Товар без базовой цены

```
┌─────────────────────────────┐
│  ~~$5.00~~  $2.80          │  ← Старая и новая цена
└─────────────────────────────┘
```

## Реализация (пример на JavaScript)

```javascript
function renderProductPrice(productPrice) {
  // Проверка флагов
  if (productPrice.hidePrices) {
    return null;
  }
  
  if (productPrice.callForPrice) {
    return <Text>Узнать цену</Text>;
  }
  
  if (productPrice.customerEntersPrice) {
    return <PriceInputField />;
  }
  
  return (
    <View>
      {/* Старая цена */}
      {productPrice.oldPrice && (
        <Text style={{ textDecorationLine: 'line-through', color: 'gray' }}>
          {productPrice.oldPrice}
        </Text>
      )}
      
      {/* Текущая цена */}
      <Text style={{ fontWeight: 'bold', fontSize: 18 }}>
        {productPrice.price}
      </Text>
      
      {/* Базовая цена с единицей измерения */}
      {productPrice.basePricePAngV && (
        <Text style={{ fontSize: 12, color: 'gray' }}>
          {productPrice.basePricePAngV}
        </Text>
      )}
    </View>
  );
}
```

## Важные замечания

1. **Старая цена (`oldPrice`)**: Показывается только если она не пустая и не null
2. **Текущая цена (`price`)**: Всегда отображается (если не скрыта флагом)
3. **Базовая цена (`basePricePAngV`)**: Содержит уже отформатированную строку с единицей измерения, например "8000 сум / кг" - показывать как есть
4. **Валюта**: Уже включена в форматированную цену (`price`), дополнительно можно использовать `currencyCode` для отображения символа валюты
5. **Цвета**: Рекомендуется использовать серый цвет для старой цены, основной цвет для текущей цены

## Дополнительные поля (опционально)

- `priceWithDiscount` - цена со скидкой (если отличается от основной)
- `priceValue` / `oldPriceValue` - числовые значения для сортировки/сравнения
- `currencyCode` - код валюты для дополнительного форматирования

