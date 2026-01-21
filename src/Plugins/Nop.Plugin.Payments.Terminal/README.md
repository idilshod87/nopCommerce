# Terminal payment plugin (Payments.Terminal)

Плагин **Оплата по терминалу** (`Payments.Terminal`) добавляет офлайн‑способ оплаты, когда клиент оплачивает заказ банковской картой через POS‑терминал (в точке выдачи или при доставке курьером). Интеграций с платёжными шлюзами нет — плагин используется только для информирования клиента.

## Возможности

- Добавляет способ оплаты **«Оплата по терминалу»** на шаге оформления заказа.
- Показывает на шаге оплаты HTML‑инструкцию (поле **DescriptionText**) — вы можете описать условия оплаты, ограничения, контакты и т.п.
- Позволяет задать дополнительную комиссию (фиксированную или в процентах).
- Может отображаться только для заказов с доставляемыми товарами (настраивается флагом).

## Структура проекта

```
Plugins/Nop.Plugin.Payments.Terminal/
 ├── Components/
 │   └── TerminalPaymentInfoViewComponent.cs   # Отдаёт PaymentInfo для checkout
 ├── Controllers/
 │   └── PaymentTerminalController.cs         # Страница настройки в админке
 ├── Localization/
 │   ├── resources.ru-RU.xml                  # Русская локализация
 │   └── resources.uz-UZ.xml                  # Узбекская локализация
 ├── Models/
 │   ├── ConfigurationModel.cs                # Модель настроек плагина
 │   └── PaymentInfoModel.cs                  # Модель для PaymentInfo.cshtml
 ├── Views/
 │   ├── Configure.cshtml                     # Настройки в админке
 │   ├── PaymentInfo.cshtml                   # Текст на шаге оплаты
 │   └── _ViewImports.cshtml
 ├── TerminalPaymentProcessor.cs              # Реализация IPaymentMethod
 ├── TerminalPaymentSettings.cs               # Настройки (DescriptionText, комиссии и т.п.)
 ├── Nop.Plugin.Payments.Terminal.csproj
 ├── plugin.json
 └── README.md                                # Текущий файл
```

## Установка

1. Откройте solution **NopCommerce.sln** из папки `src/`.
2. Убедитесь, что проект `Nop.Plugin.Payments.Terminal` включён в решение (он уже добавлен в репозиторий).
3. Соберите решение:

   ```powershell
   cd src
   dotnet build NopCommerce.sln
   ```

4. После сборки файлы плагина появятся в папке:

   `Presentation/Nop.Web/Plugins/Payments.Terminal`

5. Зайдите в админку nopCommerce → **Configuration → Local plugins**.
6. Найдите плагин **«Оплата по терминалу»** (`Payments.Terminal`) и нажмите **Install**.
7. При необходимости выполните **Restart application**.

## Настройка

1. В админке перейдите в **Configuration → Local plugins** и нажмите **Configure** напротив «Оплата по терминалу».
2. Настройте параметры:
   - **Описание (DescriptionText)** — HTML‑текст, который увидит клиент на шаге оплаты (например, условия оплаты через терминал, напоминание иметь при себе карту и т.п.).
   - **Дополнительная комиссия** и **Дополнительная комиссия, %** — при необходимости задайте наценку за использование метода.
   - **Требуется доставляемый товар** — если включено, метод будет отображаться только для корзин, где есть товары с доставкой.
3. Сохраните настройки.
4. В разделе **Configuration → Payment methods** убедитесь, что метод **Payments.Terminal** включён и имеет нужный порядок отображения.

## Использование

- При оформлении заказа клиент увидит способ оплаты **«Оплата по терминалу»** и текст, заданный в поле **DescriptionText**.
- После подтверждения заказа вы обрабатываете оплату офлайн на терминале (в магазине или при доставке). Система nopCommerce не выполняет никаких онлайн‑списаний, только фиксирует выбранный способ оплаты.

## Сборка из командной строки

```powershell
cd src
dotnet build NopCommerce.sln
```

После успешной сборки плагин готов к установке через **Configuration → Local plugins**.
