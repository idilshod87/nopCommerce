# Metrx Customizations plugin

Metrx Customizations (`Misc.Metrx`) расширяет административную часть nopCommerce и служит точкой входа для дальнейших кастомизаций.

## Возможности

- Добавляет карточку "Vendor delivery date" на страницу редактирования поставщика.
- Позволяет назначить поставщику стандартный срок доставки из стандартного справочника **Catalog → Attributes → Delivery dates**.
- Сохраняет выбор для уже существующих поставщиков сразу после сохранения формы, а для новых — сразу после создания записи.
- В будущем может расширяться дополнительными зонами виджетов внутри `AdminWidgetZones`.

## Структура проекта

```
Plugins/Nop.Plugin.Misc.Metrx/
 ├── Components/                     # ViewComponents, выводящие UI в админке
 │   └── VendorDeliveryDateAdminViewComponent.cs
 ├── Infrastructure/                 # Регистрация зависимостей через INopStartup
 │   └── PluginNopStartup.cs
 ├── Models/Admin/                   # Модели для Razor-представлений
 ├── Services/                       # Сервисы + подписчики на события
 ├── Views/                          # Razor-представления карточки
 ├── MetrxDefaults.cs                # Общие константы
 ├── MetrxPlugin.cs                  # Главный класс плагина
 ├── plugin.json                     # Метаданные
 └── README.md                       # Текущий файл
```

## Установка

1. Откройте корневой solution `NopCommerce.sln`.
2. Убедитесь, что проект `Nop.Plugin.Misc.Metrx` включён в сборку (уже добавлен в `.sln`).
3. Соберите решение (`dotnet build NopCommerce.sln`).
4. Зайдите в админку → **Configuration → Local plugins** и найдите «Metrx Customizations».
5. Нажмите **Install**, затем (при необходимости) **Restart application**.

## Использование

1. Перейдите в **Customers → Vendors** и откройте нужного поставщика.
2. В карточке "Vendor delivery date" выберите значение из выпадающего списка и сохраните форму.
3. Значение хранится в generic-атрибуте поставщика `Metrx.Vendor.DeliveryDateId` и доступно для дальнейших кастомных сценариев (например, автозаполнение сроков доставки у товаров конкретного поставщика).

## Расширение

- Добавляйте новые виджеты, используя `AdminWidgetZones` и `IWidgetPlugin` из `MetrxPlugin.cs`.
- Подписывайтесь на дополнительные доменные события (через `Services/EventConsumer.cs`) для автоматизации бизнес-логики.
- Локализацию регистрируйте в `InstallAsync()` основного плагина.

## Сборка из командной строки

```powershell
dotnet build NopCommerce.sln
```

После успешной сборки файлы плагина появятся в `Presentation/Nop.Web/Plugins/Misc.Metrx`.
