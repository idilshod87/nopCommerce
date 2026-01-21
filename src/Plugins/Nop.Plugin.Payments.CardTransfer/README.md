# Card transfer payment plugin (Payments.CardTransfer)

Плагин **Перевод на карту** (`Payments.CardTransfer`) добавляет офлайн‑способ оплаты, когда клиент самостоятельно переводит деньги на вашу банковскую карту. Интеграций с платёжными системами нет — плагин используется только для отображения реквизитов и инструкций.

## Возможности

- Добавляет способ оплаты **«Перевод на карту»** на шаге оформления заказа.
- Показывает HTML‑инструкцию (поле **DescriptionText**) с номером карты, ФИО получателя и дополнительными условиями.
- Позволяет задать дополнительную комиссию (фиксированную или в процентах).
- Может отображаться только для заказов с доставляемыми товарами.

## Структура проекта

```
Plugins/Nop.Plugin.Payments.CardTransfer/
 ├── Components/
 │   └── CardTransferPaymentInfoViewComponent.cs   # Отдаёт PaymentInfo для checkout
 ├── Controllers/
 │   └── PaymentCardTransferController.cs         # Страница настройки в админке
 ├── Localization/
 │   ├── resources.ru-RU.xml                      # Русская локализация
 │   └── resources.uz-UZ.xml                      # Узбекская локализация
 ├── Models/
 │   ├── ConfigurationModel.cs                    # Модель настроек плагина
 │   └── PaymentInfoModel.cs                      # Модель для PaymentInfo.cshtml
 ├── Views/
 │   ├── Configure.cshtml                         # Настройки в админке
 │   ├── PaymentInfo.cshtml                       # Текст на шаге оплаты
 │   └── _ViewImports.cshtml
 ├── CardTransferPaymentProcessor.cs              # Реализация IPaymentMethod
 ├── CardTransferPaymentSettings.cs               # Настройки (DescriptionText, комиссии и т.п.)
 ├── Nop.Plugin.Payments.CardTransfer.csproj
 ├── plugin.json
 └── README.md                                    # Текущий файл
```

## Установка

1. Откройте solution **NopCommerce.sln** из папки `src/`.
2. Убедитесь, что проект `Nop.Plugin.Payments.CardTransfer` включён в решение.
3. Соберите решение:

   ```powershell
   cd src
   dotnet build NopCommerce.sln
   ```

4. После сборки файлы плагина появятся в папке:

   `Presentation/Nop.Web/Plugins/Payments.CardTransfer`

5. Зайдите в админку nopCommerce → **Configuration → Local plugins**.
6. Найдите плагин **«Перевод на карту»** (`Payments.CardTransfer`) и нажмите **Install**.
7. При необходимости выполните **Restart application**.

## Настройка

1. В админке перейдите в **Configuration → Local plugins** и нажмите **Configure** напротив «Перевод на карту».
2. Настройте параметры:
   - **Описание (DescriptionText)** — HTML‑текст, который увидит клиент при выборе оплаты переводом на карту. Обычно здесь указывают номер карты, ФИО получателя и дополнительные инструкции (например, отправить чек в мессенджер).
   - **Дополнительная комиссия** и **Дополнительная комиссия, %** — при необходимости задайте наценку за использование этого метода.
   - **Требуется доставляемый товар** — если включено, метод будет отображаться только для заказов с доставкой.
3. Сохраните настройки.
4. В разделе **Configuration → Payment methods** включите метод **Payments.CardTransfer** и задайте нужный Display order.

## Использование

- При оформлении заказа клиент выбирает **«Перевод на карту»** и видит инструкцию из поля **DescriptionText**.
- Клиент выполняет перевод самостоятельно (через интернет‑банк / приложение банка).
- После получения оплаты вы меняете статус заказа вручную (nopCommerce не выполняет автоматических списаний).

## Сборка из командной строки

```powershell
cd src
dotnet build NopCommerce.sln
```

После успешной сборки плагин готов к установке через **Configuration → Local plugins**.
