# API: Добавление товара в корзину

## Описание
Эндпоинт для добавления товара в корзину авторизованного пользователя. Поддерживает добавление товара с указанием количества.

## Endpoint
```
POST /public-api/shoppingCart/add
```

## Аутентификация
Требуется авторизованный пользователь (Bearer токен).

## Заголовки запроса
```
Content-Type: application/json
Authorization: Bearer {token}
```

## Тело запроса (Request Body)

### Структура JSON
```json
{
  "productId": 123,
  "quantity": 1
}
```

### Параметры

| Поле | Тип | Обязательное | Описание | Ограничения |
|------|-----|--------------|----------|-------------|
| `productId` | int | Да | ID товара | Должен существовать и быть доступным |
| `quantity` | int | Нет | Количество товара | По умолчанию: 1. Должно быть > 0 |

## Примеры использования (cURL)

> **Примечание:** Мобильное приложение разрабатывается на Flutter. Используйте примеры cURL для тестирования и понимания структуры запросов.

---

### Пример 1: Минимум - только ID товара

Добавление товара в корзину без указания количества. В корзину будет добавлена **1 штука** товара по умолчанию.

```bash
curl -X POST "https://api.example.com/public-api/shoppingCart/add" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "productId": 123
  }'
```

**Ответ на успешный запрос (200 OK):**
```json
{
  "data": {
    "success": true,
    "message": "Product added to cart successfully",
    "cartSummary": {
      "itemsCount": 1,
      "totalQuantity": 1,
      "subtotal": "29.99",
      "subtotalValue": 29.99,
      "currencyCode": "USD"
    }
  }
}
```

---

### Пример 2: ID товара + количество

Добавление товара с заранее выбранным количеством. Пользователь в UI выбирает количество (например, 3 штуки), затем нажимает "Добавить в корзину".

```bash
curl -X POST "https://api.example.com/public-api/shoppingCart/add" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "productId": 123,
    "quantity": 3
  }'
```

**Ответ на успешный запрос (200 OK):**
```json
{
  "data": {
    "success": true,
    "message": "Product added to cart successfully",
    "cartSummary": {
      "itemsCount": 1,
      "totalQuantity": 3,
      "subtotal": "89.97",
      "subtotalValue": 89.97,
      "currencyCode": "USD"
    }
  }
}
```

---

## Ошибки

### 400 Bad Request - Некорректные данные

#### Ошибка: ProductId не указан
```bash
curl -X POST "https://api.example.com/public-api/shoppingCart/add" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{}'
```

**Ответ (400 Bad Request):**
```json
{
  "message": "ProductId is required"
}
```

#### Ошибка: Некорректное количество
```bash
curl -X POST "https://api.example.com/public-api/shoppingCart/add" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "productId": 123,
    "quantity": 0
  }'
```

**Ответ (400 Bad Request):**
```json
{
  "message": "Quantity must be greater than 0"
}
```

#### Ошибка: Товар недоступен
```bash
curl -X POST "https://api.example.com/public-api/shoppingCart/add" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "productId": 99999
  }'
```

**Ответ (400 Bad Request):**
```json
{
  "message": "Product is not available"
}
```

### 401 Unauthorized

Пользователь не авторизован или сессия истекла.

```bash
curl -X POST "https://api.example.com/public-api/shoppingCart/add" \
  -H "Content-Type: application/json" \
  -d '{
    "productId": 123
  }'
```

**Ответ (401 Unauthorized):**
```json
{
  "message": "Unauthorized"
}
```

---

## Валидация на стороне клиента

Перед отправкой запроса рекомендуется выполнить валидацию:

1. **ProductId:**
   - Обязательное поле
   - Должно быть положительным числом

2. **Quantity:**
   - Если указано, должно быть > 0
   - Если не указано, будет использовано значение по умолчанию: 1
   - Рекомендуется ограничить максимальное значение (например, 999)

## UI/UX Рекомендации

1. **Экран карточки товара:**
   - Кнопка "Добавить в корзину" - использует **Пример 1** (минимум)
   - Опционально: степпер количества перед кнопкой - использует **Пример 2**

2. **Обработка успешного ответа:**
   - Показать уведомление об успехе (например, "Товар добавлен в корзину")
   - Обновить счетчик товаров в корзине (использовать `cartSummary.itemsCount` или `cartSummary.totalQuantity`)
   - Опционально: показать краткую информацию о корзине из `cartSummary`

3. **Обработка ошибок:**
   - Показывать понятные сообщения об ошибках
   - При 401 - перенаправлять на экран входа
   - При 400 - показывать конкретное сообщение об ошибке валидации

4. **Опциональный сценарий с количеством:**
   - Если пользователь может заранее выбрать количество:
     - Показать UI элемент для выбора количества (степпер, поле ввода)
     - При нажатии "Добавить в корзину" передавать выбранное количество в поле `quantity`
   - Если количество не выбирается:
     - Использовать минимальный запрос (только `productId`)
     - В корзину будет добавлена 1 штука по умолчанию

## Примечания

- После успешного добавления товара в корзину, данные сохраняются в `ShoppingCart` для текущего авторизованного пользователя
- Если товар с такими же параметрами уже есть в корзине, количество может быть увеличено (зависит от бизнес-логики)
- Для получения актуального состояния корзины после добавления товара, рекомендуется использовать эндпоинт `GET /public-api/cart`

