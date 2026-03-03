# Nop.Plugin.ExternalAuth.Telegram

Плагин внешней аутентификации через Telegram Bot для nopCommerce 4.90.

## Описание

Позволяет покупателям входить в магазин через Telegram: пользователь открывает бота, делится номером телефона и вводит одноразовый код. При первом входе аккаунт создаётся автоматически. Плагин используется как базовая зависимость для `Nop.Plugin.Misc.TelegramNotifications`.

## Поток аутентификации

```
1. Клиент  →  POST /api/telegram-auth/session
             ← { sessionToken, deepLink }

2. Клиент открывает deepLink (https://t.me/<BotUsername>?start=<sessionToken>)

3. Telegram Bot  ←  пользователь нажимает /start и делится контактом
   Bot сохраняет TelegramUserId, PhoneNumber, высылает код подтверждения

4. Клиент  →  POST /api/telegram-auth/verify  { sessionToken, code }
             ← { accessToken, refreshToken }  (JWT)
```

## API-эндпоинты (`/api/telegram-auth`)

| Метод | Путь | Описание |
|-------|------|----------|
| `POST` | `/session` | Создать новую сессию, получить deepLink на бота |
| `POST` | `/verify` | Подтвердить код и получить JWT-токены |
| `POST` | `/webhook` | Входящие webhook-события от Telegram (только для бота) |

Конфигурация плагина доступна по адресу: `Admin/TelegramGateway/Configure`.

## Конфигурация

| Параметр | По умолчанию | Описание |
|----------|-------------|----------|
| `BotToken` | — | Токен бота из [@BotFather](https://t.me/BotFather) |
| `BotUsername` | — | Username бота без `@` |
| `WebhookSecretToken` | `telegram-auth` | Секрет для проверки подлинности webhook-запросов |
| `SecurityKey` | *(встроенный)* | Ключ подписи JWT-токенов |
| `SessionTtlMinutes` | `10` | Время жизни сессии аутентификации |
| `VerificationCodeLength` | `4` | Длина одноразового кода |
| `VerificationCodeTtlMinutes` | `5` | Время жизни одноразового кода |
| `AllowedClockSkewInMinutes` | `5` | Допустимое расхождение системных часов |

> **Важно:** замените значение `SecurityKey` на собственный секретный ключ в настройках плагина — значение по умолчанию предназначено только для разработки.

## Установка

1. Собрать проект — DLL автоматически копируется в `Presentation/Nop.Web/Plugins/ExternalAuth.Telegram/`.
2. Установить плагин: **Admin → Configuration → Local Plugins → Install**.
3. Перейти в настройки плагина и заполнить `BotToken` и `BotUsername`.
4. Настроить webhook в Telegram:
   ```
   https://api.telegram.org/bot<BotToken>/setWebhook
     ?url=https://<your-domain>/api/telegram-auth/webhook
     &secret_token=<WebhookSecretToken>
   ```

## Структура проекта

```
Configuration/
  TelegramGatewayConfiguration.cs   — настройки (ISettings)
Controllers/
  TelegramAuthController.cs         — REST API аутентификации
  TelegramGatewayController.cs      — страница конфигурации в Admin
Data/
  TelegramAuthSessionMapping.cs     — маппинг таблицы сессий
Domain/
  Authentication/
    TelegramAuthSession.cs          — сущность сессии в БД
  TelegramWebhook/                  — модели webhook-событий от Telegram
Services/
  ITelegramAuthService.cs / TelegramAuthService.cs   — управление сессиями и верификацией
  ITelegramBotMessenger.cs          — отправка сообщений через бота
  TelegramBotAuthenticator.cs       — обработка входящих сообщений бота
  TelegramGatewayApiService.cs      — взаимодействие с Telegram Gateway API
Models/                             — модели запросов/ответов API
Views/                              — Razor-страница конфигурации
TelegramGatewayPlugin.cs            — точка входа плагина
plugin.json                         — метаданные плагина
```

## Зависимости

- `Nop.Metrx.Core` — `IJwtTokenService`, `IRefreshTokenService`
- nopCommerce 4.90 (`Nop.Core`, `Nop.Services`, `Nop.Data`)
