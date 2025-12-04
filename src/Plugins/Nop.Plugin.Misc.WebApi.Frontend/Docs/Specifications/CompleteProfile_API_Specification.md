# API: Завершение первичной настройки профиля

## Описание
Эндпоинт для завершения первичной настройки профиля пользователя с ролью "Retailers" после успешной регистрации. Позволяет заполнить имя пользователя и информацию о магазине (название компании и ИНН).

## Endpoint
```
POST /public-api/customer/completeprofile
```

## Аутентификация
Требуется авторизованный пользователь с ролью "Retailers".

## Заголовки запроса
```
Content-Type: application/json
Authorization: Bearer {token}
```

## Тело запроса (Request Body)

### Структура JSON
```json
{
  "username": "string",
  "company": "string",
  "vatNumber": "string"
}
```

### Параметры

| Поле | Тип | Обязательное | Описание | Ограничения |
|------|-----|--------------|----------|-------------|
| `username` | string | Да | Имя пользователя | Максимум 256 символов |
| `company` | string | Да | Название магазина/компании | Не пустое |
| `vatNumber` | string | Да | ИНН (налоговый номер) | Ровно 14 десятичных цифр |

### Пример запроса
```json
{
  "username": "retailer_user_123",
  "company": "ООО \"Магазин Электроники\"",
  "vatNumber": "12345678901234"
}
```

## Ответы

### Успешный ответ (200 OK)
```json
{
  "data": {
    "success": true,
    "message": "Profile completed successfully"
  }
}
```

### Ошибки

#### 400 Bad Request - Некорректные данные
```json
{
  "message": "Username is required"
}
```

Возможные сообщения об ошибках:
- `"Model is required"` - тело запроса отсутствует
- `"Username is required"` - имя пользователя не указано
- `"Username must not exceed 256 characters"` - имя пользователя слишком длинное
- `"Company name is required"` - название компании не указано
- `"VAT Number (INN) is required"` - ИНН не указан
- `"VAT Number (INN) must be 14 digits"` - ИНН должен содержать ровно 14 цифр
- `"Username already exists"` - имя пользователя уже занято (если включена проверка уникальности)

#### 401 Unauthorized
Пользователь не авторизован или сессия истекла.

```json
{
  "message": "Unauthorized"
}
```

#### 403 Forbidden
Пользователь не имеет роли "Retailers".

```json
{
  "message": "This endpoint is only available for customers with Retailers role"
}
```

## Валидация на стороне клиента

Перед отправкой запроса рекомендуется выполнить валидацию:

1. **Username:**
   - Не пустое
   - Максимум 256 символов
   - Рекомендуется: только буквы, цифры, подчеркивания, дефисы

2. **Company:**
   - Не пустое
   - Рекомендуется: обрезать пробелы в начале и конце

3. **VAT Number (ИНН):**
   - Не пустое
   - Ровно 14 символов
   - Только цифры (0-9)
   - Регулярное выражение: `^\d{14}$`

## Примеры использования (cURL)

> **Примечание:** Мобильное приложение разрабатывается на Flutter. Используйте примеры cURL для тестирования и понимания структуры запросов.

### Успешный запрос
```bash
curl -X POST "https://api.example.com/public-api/customer/completeprofile" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "username": "retailer_user_123",
    "company": "ООО \"Магазин Электроники\"",
    "vatNumber": "12345678901234"
  }'
```

### Ответ на успешный запрос
```json
{
  "data": {
    "success": true,
    "message": "Profile completed successfully"
  }
}
```

### Пример запроса с ошибкой валидации (некорректный ИНН)
```bash
curl -X POST "https://api.example.com/public-api/customer/completeprofile" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "username": "retailer_user_123",
    "company": "ООО \"Магазин Электроники\"",
    "vatNumber": "12345"
  }'
```

### Ответ при ошибке валидации (400 Bad Request)
```json
{
  "message": "VAT Number (INN) must be 14 digits"
}
```

### Пример запроса без авторизации
```bash
curl -X POST "https://api.example.com/public-api/customer/completeprofile" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "retailer_user_123",
    "company": "ООО \"Магазин Электроники\"",
    "vatNumber": "12345678901234"
  }'
```

### Ответ при отсутствии авторизации (401 Unauthorized)
```json
{
  "message": "Unauthorized"
}
```

### Пример запроса с недостаточными правами (403 Forbidden)
```bash
curl -X POST "https://api.example.com/public-api/customer/completeprofile" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer TOKEN_WITHOUT_RETAILERS_ROLE" \
  -d '{
    "username": "user_without_role",
    "company": "ООО \"Магазин\"",
    "vatNumber": "12345678901234"
  }'
```

### Ответ при недостаточных правах (403 Forbidden)
```json
{
  "message": "This endpoint is only available for customers with Retailers role"
}
```

### Пример запроса с пустым телом
```bash
curl -X POST "https://api.example.com/public-api/customer/completeprofile" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{}'
```

### Ответ при пустом теле запроса (400 Bad Request)
```json
{
  "message": "Username is required"
}
```

## Валидация ИНН

Для проверки ИНН на стороне клиента используйте регулярное выражение:
- **Паттерн:** `^\d{14}$`
- **Описание:** Ровно 14 десятичных цифр от начала до конца строки

Пример валидации в Dart (Flutter):
```dart
bool isValidINN(String inn) {
  return RegExp(r'^\d{14}$').hasMatch(inn);
}
```

## UI/UX Рекомендации

1. **Экран заполнения профиля:**
   - Показывать после успешной регистрации пользователя с ролью "Retailers"
   - Все поля обязательные
   - Показывать валидацию в реальном времени
   - Кнопка "Сохранить" активна только при валидных данных

2. **Валидация полей:**
   - **Username:** показывать счетчик символов (макс. 256)
   - **Company:** обычное текстовое поле
   - **VAT Number:** числовая клавиатура, форматирование (можно добавить пробелы для читаемости, но отправлять только цифры)

3. **Обработка ошибок:**
   - Показывать понятные сообщения об ошибках
   - При 401 - перенаправлять на экран входа
   - При 403 - показывать сообщение о недостаточных правах

4. **Успешное завершение:**
   - Показывать сообщение об успехе
   - Перенаправлять на главный экран приложения

## Примечания

- После успешного вызова эндпоинта данные сохраняются в:
  - `Customer.Username` - имя пользователя
  - `Customer.Company` - название компании
  - `Customer.VatNumber` - ИНН

- Если в настройках системы отключена возможность изменения username, поле все равно будет обновлено напрямую.

- Эндпоинт доступен только для пользователей с ролью "Retailers". Для других ролей вернется ошибка 403.

