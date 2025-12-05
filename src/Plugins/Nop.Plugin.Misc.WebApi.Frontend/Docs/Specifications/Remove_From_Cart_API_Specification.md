# API: Удаление товаров из корзины

## Описание
Эндпоинт для удаления одного или нескольких товаров из корзины авторизованного пользователя. Поддерживает удаление нескольких товаров за один запрос.

## Endpoint
```
POST /public-api/shoppingCart/removecart
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
  "itemIds": [123, 456, 789]
}
```

### Параметры

| Поле | Тип | Обязательное | Описание | Ограничения |
|------|-----|--------------|----------|-------------|
| `itemIds` | array[int] | Да | Массив ID товаров в корзине для удаления | Должен содержать хотя бы один элемент. Каждый ID должен быть положительным числом |

## Примеры использования (cURL)

> **Примечание:** Мобильное приложение разрабатывается на Flutter. Используйте примеры cURL для тестирования и понимания структуры запросов.

---

### Пример 1: Удаление одного товара

Удаление одного товара из корзины по его ID в корзине.

```bash
curl -X POST "https://api.example.com/public-api/shoppingCart/removecart" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "itemIds": [123]
  }'
```

**Ответ на успешный запрос (200 OK):**
```json
{
  "data": {
    "success": true,
    "message": "Successfully removed 1 item(s) from cart",
    "cartSummary": {
      "itemsCount": 2,
      "totalQuantity": 5,
      "subtotal": "",
      "subtotalValue": 0,
      "currencyCode": ""
    }
  }
}
```

---

### Пример 2: Удаление нескольких товаров

Удаление нескольких товаров из корзины за один запрос.

```bash
curl -X POST "https://api.example.com/public-api/shoppingCart/removecart" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "itemIds": [123, 456, 789]
  }'
```

**Ответ на успешный запрос (200 OK):**
```json
{
  "data": {
    "success": true,
    "message": "Successfully removed 3 item(s) from cart",
    "cartSummary": {
      "itemsCount": 1,
      "totalQuantity": 2,
      "subtotal": "",
      "subtotalValue": 0,
      "currencyCode": ""
    }
  }
}
```

---

### Пример 3: Частичное удаление (некоторые товары не найдены)

Если некоторые ID товаров не найдены в корзине, они будут пропущены, а в сообщении будет указано, какие ID не были найдены.

```bash
curl -X POST "https://api.example.com/public-api/shoppingCart/removecart" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "itemIds": [123, 99999, 456]
  }'
```

**Ответ (200 OK):**
```json
{
  "data": {
    "success": true,
    "message": "Successfully removed 2 item(s) from cart. Item IDs not found: 99999",
    "cartSummary": {
      "itemsCount": 1,
      "totalQuantity": 1,
      "subtotal": "",
      "subtotalValue": 0,
      "currencyCode": ""
    }
  }
}
```

---

## Ошибки

### 400 Bad Request - Некорректные данные

#### Ошибка: Тело запроса отсутствует
```bash
curl -X POST "https://api.example.com/public-api/shoppingCart/removecart" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d ''
```

**Ответ (400 Bad Request):**
```json
{
  "message": "Request body is required"
}
```

#### Ошибка: Массив itemIds пустой или отсутствует
```bash
curl -X POST "https://api.example.com/public-api/shoppingCart/removecart" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "itemIds": []
  }'
```

**Ответ (400 Bad Request):**
```json
{
  "message": "ItemIds is required and must contain at least one item ID"
}
```

### 401 Unauthorized

Пользователь не авторизован или сессия истекла.

```bash
curl -X POST "https://api.example.com/public-api/shoppingCart/removecart" \
  -H "Content-Type: application/json" \
  -d '{
    "itemIds": [123]
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

1. **ItemIds:**
   - Обязательное поле
   - Должен быть массивом целых чисел
   - Должен содержать хотя бы один элемент
   - Каждый ID должен быть положительным числом
   - Рекомендуется удалить дубликаты перед отправкой

2. **ID товаров в корзине:**
   - Это не ID товаров (ProductId), а ID элементов корзины (ShoppingCartItemId)
   - ID элементов корзины можно получить из ответа метода `GET /public-api/shoppingCart/cart`

## UI/UX Рекомендации

1. **Экран корзины:**
   - Кнопка удаления для каждого товара - использует **Пример 1** (один товар)
   - Кнопка "Удалить выбранные" - использует **Пример 2** (несколько товаров)
   - Поддержка множественного выбора товаров для массового удаления

2. **Обработка успешного ответа:**
   - Показать уведомление об успехе (например, "Товар(ы) удален(ы) из корзины")
   - Обновить список товаров в корзине (использовать `cartSummary.itemsCount` и `cartSummary.totalQuantity`)
   - Если некоторые товары не были найдены, показать предупреждение с их ID

3. **Обработка ошибок:**
   - Показывать понятные сообщения об ошибках
   - При 401 - перенаправлять на экран входа
   - При 400 - показывать конкретное сообщение об ошибке валидации

4. **Оптимизация:**
   - При удалении одного товара отправлять массив с одним элементом
   - При удалении нескольких товаров отправлять все ID одним запросом
   - После успешного удаления обновить локальное состояние корзины

## Примечания

- После успешного удаления товаров из корзины, данные обновляются в `ShoppingCart` для текущего авторизованного пользователя
- Если указанный ID товара в корзине не существует, он будет пропущен, но операция все равно вернет успешный результат (если был удален хотя бы один товар)
- Для получения актуального состояния корзины после удаления товаров, рекомендуется использовать эндпоинт `GET /public-api/shoppingCart/cart`
- `itemIds` - это ID элементов корзины (ShoppingCartItemId), а не ID товаров (ProductId). ID элементов корзины уникальны для каждого пользователя и каждой позиции в корзине

