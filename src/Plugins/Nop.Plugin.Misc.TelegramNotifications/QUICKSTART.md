# 🚀 Быстрый старт - Telegram Notifications для nopCommerce

## ✅ Что нужно перед установкой

1. ✅ Установлен **nopCommerce 4.90+**
2. ✅ Установлен плагин **Nop.Plugin.ExternalAuth.Telegram**
3. ✅ Настроен Telegram бот для аутентификации
4. ✅ Есть покупатели, зарегистрированные через Telegram

## 📦 Установка (3 минуты)

### Шаг 1: Установите плагин
```
Admin Panel → Configuration → Local Plugins → Telegram Notifications → Install
```

### Шаг 2: Настройте плагин
```
Telegram Notifications → Configure:
- Bot Token: [тот же токен что для аутентификации]
- Enabled: ✅
- Notify on Order Placed: ✅
- Notify on Order Status Changed: ✅
- Notify on Order Paid: ✅
```

### Шаг 3: Сохраните настройки
```
Нажмите Save
```

## ✅ Проверка работы

### Тест 1: Создайте заказ
1. Войдите как покупатель (зарегистрированный через Telegram)
2. Создайте заказ
3. Проверьте Telegram - должно прийти уведомление

### Тест 2: Проверьте логи
```
App_Data/Logs/nopCommerce-YYYY-MM-DD.txt
```

Должна быть запись:
```
Successfully sent Telegram notification to chat 123456789
```

## ❌ Проблемы?

### Уведомления не приходят?

**Проверка 1: Покупатель зарегистрирован через Telegram?**
```sql
SELECT CustomerId, TelegramChatId, StatusId 
FROM TelegramAuthSession 
WHERE CustomerId = [ID покупателя]
```
Должно быть: `StatusId = 50` и `TelegramChatId` не пустой

**Проверка 2: Bot Token правильный?**
```
Admin Panel → External Authentication Methods → Telegram Authentication → Configure
Сравните Bot Token с тем что указан в Telegram Notifications
```

**Проверка 3: Логи**
```
App_Data/Logs/nopCommerce-YYYY-MM-DD.txt
```

Ищите:
- `Customer X does not have Telegram Chat ID` - покупатель не зарегистрирован через Telegram
- `Telegram API returned 400/403` - неправильный Bot Token

## 📚 Подробная документация

- [README.md](README.md) - Полное описание плагина
- [USAGE_GUIDE.md](USAGE_GUIDE.md) - Подробное руководство
- [CHANGELOG.md](CHANGELOG.md) - История изменений
- [SETUP_GUIDE.md](SETUP_GUIDE.md) - Настройка с нуля

## 💡 Важно помнить

⚠️ **Один Bot Token для двух плагинов:**
- Nop.Plugin.ExternalAuth.Telegram
- Nop.Plugin.Misc.TelegramNotifications

⚠️ **Уведомления только для зарегистрированных:**
- Покупатели должны пройти регистрацию через Telegram
- Статус сессии должен быть Verified

⚠️ **Личные чаты:**
- Каждый покупатель получает уведомления в свой личный чат
- Администратор НЕ получает уведомления (это плагин для покупателей)

## ✅ Готово!

Теперь ваши покупатели будут получать уведомления о заказах в Telegram! 🎉
