# Изменения в плагине Telegram Notifications

## Версия 2.0 - Индивидуальные уведомления покупателям

### Основные изменения

#### 1. Удалено поле ChatId из настроек
- **Было:** Администратор указывал единый Chat ID для всех уведомлений
- **Стало:** Уведомления отправляются индивидуально каждому покупателю

#### 2. Интеграция с Nop.Plugin.ExternalAuth.Telegram
- Добавлена зависимость от проекта `Nop.Plugin.ExternalAuth.Telegram`
- Используется таблица `TelegramAuthSession` для получения `TelegramChatId` покупателя
- Bot Token должен быть тот же, что используется для аутентификации

#### 3. Изменения в TelegramNotificationService

**Добавлен метод:**
```csharp
private async Task<long?> GetCustomerTelegramChatIdAsync(int customerId)
```

Этот метод:
- Ищет запись в `TelegramAuthSession` по `CustomerId`
- Фильтрует по статусу `Verified` (StatusId = 50)
- Проверяет наличие `TelegramChatId`
- Возвращает последний подтвержденный `TelegramChatId`

**Изменен метод:**
```csharp
public async Task SendOrderStatusNotificationAsync(Order order, OrderStatus? previousStatus = null)
```

Теперь:
- Получает `TelegramChatId` покупателя через `GetCustomerTelegramChatIdAsync`
- Если Chat ID не найден - логирует предупреждение и пропускает уведомление
- Отправляет уведомление в личный чат покупателя

**Изменен метод:**
```csharp
private async Task SendTelegramMessageAsync(long chatId, string message)
```

Теперь принимает `chatId` как параметр вместо использования из настроек.

#### 4. Изменения в моделях

**TelegramNotificationsSettings.cs:**
- Удалено свойство `ChatId`
- Оставлен только `BotToken`

**ConfigurationModel.cs:**
- Удалено свойство `ChatId`

#### 5. Изменения в контроллере

**TelegramNotificationsController.cs:**
- Удалена работа с `ChatId` в методах `Configure` и `Configure [POST]`

#### 6. Изменения во View

**Configure.cshtml:**
- Удалено поле ввода Chat ID
- Добавлено информационное сообщение о необходимости использовать тот же Bot Token

#### 7. Документация

Обновлены файлы:
- `README.md` - описание плагина и требований
- Создан `USAGE_GUIDE.md` - подробное руководство по использованию

### Технические детали

#### Зависимости

Добавлена ссылка в `.csproj`:
```xml
<ProjectReference Include="..\Nop.Plugin.ExternalAuth.Telegram\Nop.Plugin.ExternalAuth.Telegram.csproj" />
```

#### Типы данных

Используются типы из `Nop.Plugin.ExternalAuth.Telegram`:
- `TelegramAuthSession` - сущность с информацией о Telegram-сессии покупателя
- `TelegramAuthStatus` - enum со статусами сессии

#### Логика работы

1. Событие заказа (OrderPlaced/StatusChanged/Paid)
2. EventConsumer получает Order
3. TelegramNotificationService вызывается с Order
4. Получается CustomerId из Order
5. Запрос в TelegramAuthSession по CustomerId и StatusId = 50
6. Если найден TelegramChatId - отправка уведомления
7. Если не найден - логирование предупреждения

### Миграция с версии 1.0

Если у вас была установлена предыдущая версия плагина:

1. Удалите старую версию через Admin Panel
2. Установите плагин `Nop.Plugin.ExternalAuth.Telegram` (если еще не установлен)
3. Настройте аутентификацию через Telegram
4. Установите новую версию плагина уведомлений
5. Используйте тот же Bot Token что и для аутентификации

### Обратная совместимость

**НЕТ обратной совместимости:**
- Удалено поле ChatId из настроек
- Требуется установка плагина аутентификации
- Покупатели должны быть зарегистрированы через Telegram

### Преимущества новой версии

✅ **Персонализация:** Каждый покупатель получает уведомления в свой личный чат

✅ **Безопасность:** Уведомления видит только владелец заказа

✅ **Единый бот:** Один бот для аутентификации и уведомлений

✅ **Автоматика:** Не нужно вручную собирать Chat ID покупателей

✅ **Масштабируемость:** Поддержка неограниченного количества покупателей

### Известные ограничения

⚠️ **Требуется регистрация через Telegram:** Покупатели, не зарегистрированные через Telegram, не будут получать уведомления

⚠️ **Зависимость от плагина аутентификации:** Требуется установка Nop.Plugin.ExternalAuth.Telegram

⚠️ **Один бот:** Bot Token должен быть один и тот же для обоих плагинов

### Дальнейшее развитие

Планируемые возможности:
- Шаблоны уведомлений в базе данных
- Мультиязычность шаблонов
- Настройка форматирования сообщений через админ-панель
- Возможность отключения уведомлений покупателем
- Статистика отправленных уведомлений
