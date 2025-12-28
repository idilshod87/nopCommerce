# Telegram Notifications Plugin for nopCommerce

## Описание

Плагин для отправки уведомлений о заказах покупателям в их личные Telegram чаты.

## Возможности

- ✅ Отправка уведомлений покупателям в их личные чаты Telegram
- ✅ Уведомления о новых заказах
- ✅ Уведомления об изменении статуса заказа
- ✅ Уведомления об оплате заказа
- ✅ Детальная информация о заказе в каждом уведомлении
- ✅ Настройка типов уведомлений
- ✅ Безопасная интеграция с существующим Telegram-ботом для аутентификации

## Требования

1. **Обязательно** должен быть установлен плагин **Nop.Plugin.ExternalAuth.Telegram**
2. Покупатели должны быть зарегистрированы/авторизованы через Telegram
3. Bot Token должен быть тот же, что используется в Nop.Plugin.ExternalAuth.Telegram

## Как работает

1. Покупатель регистрируется через Telegram (плагин Nop.Plugin.ExternalAuth.Telegram)
2. При регистрации создается связь между `CustomerId` и `TelegramChatId` в таблице `TelegramAuthSession`
3. При создании/изменении заказа, плагин уведомлений:
   - Получает `CustomerId` из заказа
   - Находит `TelegramChatId` покупателя в `TelegramAuthSession` (статус Verified)
   - Отправляет уведомление в личный чат покупателя

## Установка

1. Убедитесь что установлен плагин **Nop.Plugin.ExternalAuth.Telegram**
2. Скопируйте папку плагина в `Plugins/Nop.Plugin.Misc.TelegramNotifications`
3. Перезапустите приложение
4. Перейдите в Admin Panel -> Configuration -> Local Plugins
5. Найдите "Telegram Notifications" и нажмите "Install"
6. Нажмите "Configure" для настройки плагина

## Настройка

В настройках плагина укажите:
   - **Bot Token** - используйте тот же токен, что и в Nop.Plugin.ExternalAuth.Telegram
   - **Enabled** - включить/выключить отправку уведомлений
   - **Notify on Order Placed** - уведомлять о создании заказа
   - **Notify on Order Status Changed** - уведомлять об изменении статуса
   - **Notify on Order Paid** - уведомлять об оплате заказа

## Формат уведомлений

```
🛒 Новый заказ #12345

Покупатель: Иван Иванов
Сумма: 1,500.00 RUB

Статус: Pending
Дата: 15.01.2025 14:30
```

## Технические детали

- Использует Telegram Bot API
- Интегрируется с событиями nopCommerce: OrderPlacedEvent, OrderStatusChangedEvent, OrderPaidEvent
- Получает TelegramChatId из таблицы TelegramAuthSession
- Отправляет уведомления только тем покупателям, у которых есть Verified сессия в Telegram
- Если у покупателя нет Telegram сессии, уведомление не отправляется (логируется предупреждение)

## Устранение неполадок

**Уведомления не приходят:**
1. Проверьте, что покупатель зарегистрирован через Telegram
2. Убедитесь, что Bot Token правильный
3. Проверьте логи в `App_Data/Logs` для ошибок
4. Убедитесь, что в таблице TelegramAuthSession есть запись с StatusId = 50 (Verified) для данного CustomerId

**Ошибка "Customer does not have Telegram Chat ID":**
- Покупатель не зарегистрирован через Telegram или его сессия не имеет статус Verified

## Поддержка

При возникновении проблем проверьте:
- Правильность токена бота
- Доступность Telegram API
- Логи приложения на наличие ошибок
- Наличие плагина Nop.Plugin.ExternalAuth.Telegram
