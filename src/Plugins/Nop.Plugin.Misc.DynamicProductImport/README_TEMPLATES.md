# Система шаблонов маппинга для динамического импорта продуктов

## Описание
Реализована функциональность сохранения и переиспользования шаблонов маппинга полей при импорте товаров из Excel.

## Основные возможности

### Типы шаблонов
1. **Системные шаблоны** (`IsSystemTemplate = true`)
   - Создаются администратором
   - Видны всем пользователям (включая поставщиков)
   - Могут использоваться как базовые настройки для всех

2. **Шаблоны поставщика** (`VendorId > 0`)
   - Создаются поставщиком для своих нужд
   - Видны только владельцу (конкретному поставщику)
   - Позволяют сохранить индивидуальные настройки маппинга

## Архитектура решения

### Доменная модель
**MappingTemplate** (`Domain/MappingTemplate.cs`):
- `Id` - идентификатор шаблона
- `Name` - название шаблона
- `VendorId` - ID поставщика (0 для системных шаблонов)
- `IsSystemTemplate` - флаг системного шаблона
- `MappingsJson` - JSON с массивом маппингов полей
- `CreatedOnUtc` - дата создания

### Сервисный слой
**IMappingTemplateService / MappingTemplateService** (`Services/`):
- `GetTemplateByIdAsync(int id)` - получение шаблона по ID
- `GetTemplatesAsync(int vendorId, bool includeSystemTemplates)` - получение списка шаблонов
  - Для администратора: все системные + свои
  - Для поставщика: все системные + свои шаблоны
- `InsertTemplateAsync(MappingTemplate)` - создание нового шаблона
- `UpdateTemplateAsync(MappingTemplate)` - обновление шаблона
- `DeleteTemplateAsync(MappingTemplate)` - удаление шаблона

### API методы контроллера
**DynamicProductImportController**:

1. **GET /Admin/DynamicProductImport/GetMappingTemplates**
   - Параметры: `vendorId` (опционально)
   - Возвращает список доступных шаблонов
   - Фильтрует по правам доступа

2. **POST /Admin/DynamicProductImport/SaveMappingTemplate**
   - Body: `{ name: string, isSystemTemplate: bool, mappings: ImportProductMapping[] }`
   - Создает новый шаблон
   - Системные шаблоны может создавать только администратор

3. **GET /Admin/DynamicProductImport/GetMappingTemplate**
   - Параметры: `id`
   - Возвращает шаблон с маппингами
   - Проверяет права доступа

4. **POST /Admin/DynamicProductImport/DeleteMappingTemplate**
   - Параметры: `id`
   - Удаляет шаблон
   - Системные могут удалять только администраторы
   - Свои шаблоны - только их владельцы

### База данных
**Таблица MappingTemplate** (создается через миграцию):
```sql
CREATE TABLE MappingTemplate (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(400) NOT NULL,
    VendorId INT NOT NULL,
    IsSystemTemplate BIT NOT NULL,
    MappingsJson NVARCHAR(MAX),
    CreatedOnUtc DATETIME2 NOT NULL
)
```

**Миграция**: `MappingTemplateSchemaMigration.cs`
- Версия: "2025/12/26 12:00:00"
- Тип: Installation

### UI компоненты

#### Добавлено в DynamicImportButton.cshtml:

1. **Селектор шаблонов** (появляется после загрузки файла):
   ```html
   <select id="dynamic-import-template-select">
     <option value="">Выберите шаблон...</option>
     <!-- Системные помечены (System) -->
     <option value="1">Базовый маппинг (System)</option>
     <option value="2">Мой шаблон Aliexpress</option>
   </select>
   <button id="dynamic-import-template-load">Загрузить</button>
   <button id="dynamic-import-template-delete">Удалить</button>
   ```

2. **Кнопка сохранения**:
   ```html
   <button id="dynamic-import-template-save">
     <i class="fas fa-save"></i> Сохранить как шаблон
   </button>
   ```

#### JavaScript функции:

- `loadTemplates(vendorId)` - загрузка списка шаблонов
- `applyTemplate(template)` - применение шаблона к текущему маппингу
- Обработчики кнопок Load/Save/Delete

## Безопасность и права доступа

### Администратор:
- ✅ Видит все системные шаблоны
- ✅ Может создавать системные шаблоны
- ✅ Может удалять системные шаблоны
- ✅ Может создавать свои шаблоны

### Поставщик:
- ✅ Видит все системные шаблоны (только чтение)
- ✅ Видит только свои шаблоны
- ❌ НЕ может создавать системные шаблоны
- ❌ НЕ может удалять системные шаблоны
- ❌ НЕ может видеть шаблоны других поставщиков
- ✅ Может создавать свои шаблоны
- ✅ Может удалять свои шаблоны

## Локализация

Добавлены ресурсные строки:
- `Admin.Catalog.Products.DynamicImport.Template.NameRequired`
- `Admin.Catalog.Products.DynamicImport.Template.MappingsRequired`
- `Admin.Catalog.Products.DynamicImport.Template.Saved`
- `Admin.Catalog.Products.DynamicImport.Template.NotFound`
- `Admin.Catalog.Products.DynamicImport.Template.Deleted`
- `Admin.Catalog.Products.DynamicImport.Template.Load`
- `Admin.Catalog.Products.DynamicImport.Template.Save`
- `Admin.Catalog.Products.DynamicImport.Template.Name`
- `Admin.Catalog.Products.DynamicImport.Template.SystemTemplate`
- `Admin.Catalog.Products.DynamicImport.Template.Select`

## Использование

### Создание шаблона:
1. Загрузить Excel файл
2. Настроить маппинг полей (обязательные + опциональные)
3. Нажать "Сохранить как шаблон"
4. Ввести название
5. (Для администратора) Выбрать тип: системный или личный

### Применение шаблона:
1. Загрузить Excel файл
2. Выбрать шаблон из выпадающего списка
3. Нажать кнопку "Загрузить" (иконка download)
4. Маппинг автоматически применится
5. При необходимости - скорректировать
6. Выполнить импорт

### Удаление шаблона:
1. Выбрать шаблон из списка
2. Нажать кнопку "Удалить" (появляется только для разрешенных шаблонов)
3. Подтвердить удаление

## Формат хранения маппингов

Шаблоны хранят маппинги в JSON формате:
```json
[
  { "property": "SKU", "columnIndex": 1 },
  { "property": "Name", "columnIndex": 2 },
  { "property": "Price", "columnIndex": 5 },
  { "property": "StockQuantity", "columnIndex": 6 },
  { "property": "Categories", "columnIndex": 8 }
]
```

## Требования для компиляции

Для корректной работы необходимо:
1. Остановить запущенное приложение nopCommerce
2. Выполнить сборку: `dotnet build NopCommerce.sln -c Debug`
3. Запустить приложение
4. Миграция создаст таблицу автоматически при первом запуске

## Файлы изменений

### Созданные файлы:
- `Domain/MappingTemplate.cs` - доменная модель
- `Services/IMappingTemplateService.cs` - интерфейс сервиса
- `Services/MappingTemplateService.cs` - реализация сервиса
- `Models/SaveMappingTemplateModel.cs` - модель запроса
- `Infrastructure/MappingTemplateBuilder.cs` - маппинг для БД
- `Infrastructure/MappingTemplateSchemaMigration.cs` - миграция

### Модифицированные файлы:
- `Controllers/DynamicProductImportController.cs` - добавлены 4 метода API
- `Views/Product/DynamicImportButton.cshtml` - добавлен UI для шаблонов
- `Infrastructure/DynamicImportDependencyRegistrar.cs` - регистрация сервиса
- `DynamicProductImportPlugin.cs` - добавлены локализации

## Примечания

- Шаблоны не привязаны к конкретному Excel файлу
- Можно использовать один шаблон для разных файлов с похожей структурой
- При применении шаблона автоматически подбираются колонки по совпадению имен
- Несовпадающие поля можно настроить вручную
