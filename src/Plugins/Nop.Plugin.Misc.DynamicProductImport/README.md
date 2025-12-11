# Dynamic Product Import (plugin)

Плагин добавляет в админке кнопку **Dynamic Import** на списке товаров и позволяет:
- загрузить Excel-файл, прочитать заголовки и сопоставить их с полями товара;
- выбрать поставщика (Vendor) для импортируемых товаров;
- выполнить импорт с учётом пользовательских маппингов колонок.

## Требования
- nopCommerce 4.90 (совместимость по `plugin.json`);
- проект плагина подключён в `NopCommerce.sln`;
- в ядре ослаблены сеттеры `ImportProductMetadata` (public set), чтобы плагин мог заполнять метаданные.

## Сборка и установка
1) Соберите решение из корня репозитория:
   ```bash
   dotnet build NopCommerce.sln --no-incremental
   ```
2) Скопируйте артефакты в папку `Presentation/Nop.Web/Plugins/Misc.DynamicProductImport` (или используйте встроенный publish плагинов, если настроен).
3) Перезапустите приложение / сайт.
4) В админке перейдите **Configuration → Local plugins**, найдите **Dynamic Product Import** и нажмите **Install**.

## Использование
1) В админке откройте **Catalog → Products**.
2) Нажмите кнопку **Dynamic Import**.
3) Шаги в модальном окне:
   - загрузите Excel (.xlsx);
   - выберите поставщика;
   - сопоставьте обязательные поля (SKU, Name, Price, StockQuantity) и любые дополнительные;
   - запустите импорт.

## Архитектура
- **Контроллер**: `Controllers/DynamicProductImportController` — приём загрузки, чтение заголовков, выполнение импорта.
- **Сервис импорта**: `Services/DynamicImportManager` — расширяет базовый импорт, применяет маппинги и прокидывает Vendor.
- **Виджет**: `Components/DynamicImportButtonViewComponent` + `Views/Product/DynamicImportButton.cshtml` — кнопка и модальное окно.
- **Регистрация**: `Infrastructure/DynamicImportDependencyRegistrar` — регистрирует `IDynamicImportManager`.
- **Модели**: `Models/*` — запросы и маппинги (включая `ImportProductMapping`).

## Ограничения и заметки
- Поддерживается только формат `.xlsx`.
- Антифорджери: экшен импорта помечен `[IgnoreAntiforgeryToken]`, клиент отправляет токен в заголовке.
- Требуются права `Catalog.PRODUCTS_IMPORT_EXPORT`.
- При выборе поставщика колонка `Vendor` проставляется автоматически в загружаемой книге перед вызовом базового импорта.



