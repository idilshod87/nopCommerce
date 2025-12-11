using System.Reflection;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.ExportImport;
using Nop.Services.ExportImport.Help;
using Nop.Services.FilterLevels;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Date;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using Nop.Plugin.Misc.DynamicProductImport.Models;

namespace Nop.Plugin.Misc.DynamicProductImport.Services;

/// <summary>
/// Import manager override that hosts dynamic mapping helpers in the plugin
/// </summary>
public class DynamicImportManager : ImportManager, IDynamicImportManager
{
    public DynamicImportManager(
        CatalogSettings catalogSettings,
        IAddressService addressService,
        IBackInStockSubscriptionService backInStockSubscriptionService,
        ICategoryService categoryService,
        ICountryService countryService,
        ICustomerActivityService customerActivityService,
        ICustomerService customerService,
        ICustomNumberFormatter customNumberFormatter,
        INopDataProvider dataProvider,
        IDateRangeService dateRangeService,
        IFilterLevelValueService filterLevelValueService,
        IGenericAttributeService genericAttributeService,
        IHttpClientFactory httpClientFactory,
        ILanguageService languageService,
        ILocalizationService localizationService,
        ILocalizedEntityService localizedEntityService,
        ILogger logger,
        IManufacturerService manufacturerService,
        IMeasureService measureService,
        INewsLetterSubscriptionService newsLetterSubscriptionService,
        INewsLetterSubscriptionTypeService newsLetterSubscriptionTypeService,
        INopFileProvider fileProvider,
        IOrderService orderService,
        IPictureService pictureService,
        IProductAttributeService productAttributeService,
        IProductService productService,
        IProductTagService productTagService,
        IProductTemplateService productTemplateService,
        IServiceScopeFactory serviceScopeFactory,
        ISpecificationAttributeService specificationAttributeService,
        IStateProvinceService stateProvinceService,
        IStoreContext storeContext,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        ITaxCategoryService taxCategoryService,
        IUrlRecordService urlRecordService,
        IVendorService vendorService,
        IWarehouseService warehouseService,
        IWorkContext workContext,
        MediaSettings mediaSettings,
        SecuritySettings securitySettings,
        TaxSettings taxSettings,
        VendorSettings vendorSettings)
        : base(
            catalogSettings,
            addressService,
            backInStockSubscriptionService,
            categoryService,
            countryService,
            customerActivityService,
            customerService,
            customNumberFormatter,
            dataProvider,
            dateRangeService,
            filterLevelValueService,
            genericAttributeService,
            httpClientFactory,
            languageService,
            localizationService,
            localizedEntityService,
            logger,
            manufacturerService,
            measureService,
            newsLetterSubscriptionService,
            newsLetterSubscriptionTypeService,
            fileProvider,
            orderService,
            pictureService,
            productAttributeService,
            productService,
            productTagService,
            productTemplateService,
            serviceScopeFactory,
            specificationAttributeService,
            stateProvinceService,
            storeContext,
            storeMappingService,
            storeService,
            taxCategoryService,
            urlRecordService,
            vendorService,
            warehouseService,
            workContext,
            mediaSettings,
            securitySettings,
            taxSettings,
            vendorSettings)
    {
    }

    public async Task ImportProductsFromXlsxAsync(Stream stream, IEnumerable<ImportProductMapping> mappings, int? vendorId = null)
    {
        var languages = await _languageService.GetAllLanguagesAsync(showHidden: true);

        using var workbook = new XLWorkbook(stream);
        var downloadedFiles = new List<string>();

        var metadata = await PrepareImportProductDataWithMappingsAsync(workbook, languages, mappings);
        var defaultWorksheet = metadata.DefaultWorksheet;

        var currentVendor = await _workContext.GetCurrentVendorAsync();
        var importVendor = vendorId.HasValue ? await _vendorService.GetVendorByIdAsync(vendorId.Value) : currentVendor;

        if (currentVendor != null && importVendor != null && currentVendor.Id != importVendor.Id)
            throw new ArgumentException(await _localizationService.GetResourceAsync("Admin.Catalog.Products.Import.VendorNotAllowed"));

        var actingVendor = importVendor ?? currentVendor;

        if (_catalogSettings.ExportImportSplitProductsFile && metadata.CountProductsInFile > _catalogSettings.ExportImportProductsCountInOneFile)
        {
            await ImportProductsFromSplitedXlsxAsync(defaultWorksheet, metadata, mappings, actingVendor?.Id);
            return;
        }

        //performance optimization, load all products by SKU in one SQL request
        var allProductsBySku = await _productService.GetProductsBySkuAsync(metadata.AllSku.ToArray(), actingVendor?.Id ?? 0);

        //validate maximum number of products per vendor
        if (_vendorSettings.MaximumProductNumber > 0 &&
            actingVendor != null)
        {
            var newProductsCount = metadata.CountProductsInFile - allProductsBySku.Count;
            if (await _productService.GetNumberOfProductsByVendorIdAsync(actingVendor.Id) + newProductsCount > _vendorSettings.MaximumProductNumber)
                throw new ArgumentException(string.Format(await _localizationService.GetResourceAsync("Admin.Catalog.Products.ExceededMaximumNumber"), _vendorSettings.MaximumProductNumber));
        }

        //performance optimization, load all categories IDs for products in one SQL request
        var allProductsCategoryIds = await _categoryService.GetProductCategoryIdsAsync(allProductsBySku.Select(p => p.Id).ToArray());

        //performance optimization, load all categories in one SQL request
        Dictionary<CategoryKey, Category> allCategories = new();
        try
        {
            var allCategoryList = await _categoryService.GetAllCategoriesAsync(showHidden: true);

            allCategories = await allCategoryList
                .WhereAwait(async c => await _categoryService.CanVendorAddProductsAsync(c, allCategoryList))
                .ToDictionaryAwaitAsync(async c =>
                {
                    var keyName = await _categoryService.GetFormattedBreadCrumbAsync(c, allCategoryList);
                    return new CategoryKey(keyName, c, c.LimitedToStores ? (await _storeMappingService.GetStoresIdsWithAccessAsync(c)).ToList() : new List<int>());
                });
        }
        catch (ArgumentException)
        {
            //categories with the same name are not supported in the same category level
            throw new ArgumentException(await _localizationService.GetResourceAsync("Admin.Catalog.Products.Import.CategoriesWithSameNameNotSupported"));
        }

        //performance optimization, load all manufacturers IDs for products in one SQL request
        var allProductsManufacturerIds = await _manufacturerService.GetProductManufacturerIdsAsync(allProductsBySku.Select(p => p.Id).ToArray());

        //performance optimization, load all manufacturers in one SQL request
        var allManufacturers = await _manufacturerService.GetAllManufacturersAsync(showHidden: true);

        //performance optimization, load all stores in one SQL request
        var allStores = await _storeService.GetAllStoresAsync();

        //product to import images
        var productPictureMetadata = new List<ProductPictureMetadata>();

        Product lastLoadedProduct = null;
        var typeOfExportedAttribute = ExportedAdditionalProductInfoType.NotSpecified;

        for (var iRow = 2; iRow < metadata.EndRow; iRow++)
        {
            if (defaultWorksheet.Row(iRow).OutlineLevel != 0)
            {
                if (lastLoadedProduct == null)
                    continue;

                var newTypeOfExportedAttribute = GetTypeOfExportedAdditionalProductInfo(defaultWorksheet, metadata.LocalizedWorksheets, metadata.ProductAttributeManager, metadata.SpecificationAttributeManager, metadata.TierPriceManager, iRow);

                //skip caption row
                if (newTypeOfExportedAttribute != ExportedAdditionalProductInfoType.NotSpecified &&
                    newTypeOfExportedAttribute != typeOfExportedAttribute)
                {
                    typeOfExportedAttribute = newTypeOfExportedAttribute;
                    continue;
                }

                switch (typeOfExportedAttribute)
                {
                    case ExportedAdditionalProductInfoType.ProductAttribute:
                        await ImportProductAttributeAsync(metadata, lastLoadedProduct, languages, iRow);
                        break;
                    case ExportedAdditionalProductInfoType.SpecificationAttribute:
                        await ImportSpecificationAttributeAsync(metadata, lastLoadedProduct, languages, iRow);
                        break;
                    case ExportedAdditionalProductInfoType.TierPrices:
                        await ImportTierPriceAsync(metadata, lastLoadedProduct, languages, iRow);
                        break;
                    case ExportedAdditionalProductInfoType.NotSpecified:
                    default:
                        continue;
                }

                continue;
            }

            metadata.Manager.ReadDefaultFromXlsx(defaultWorksheet, iRow);

            var product = metadata.SkuCellNum > 0 ? allProductsBySku.FirstOrDefault(p => p.Sku == metadata.Manager.GetDefaultProperty("SKU").StringValue) : null;

            var isNew = product == null;

            product ??= new Product();

            //some of previous values
            var previousStockQuantity = product.StockQuantity;
            var previousWarehouseId = product.WarehouseId;
            var prevTotalStockQuantity = await _productService.GetTotalStockQuantityAsync(product);

            if (isNew)
                product.CreatedOnUtc = DateTime.UtcNow;

            foreach (var property in metadata.Manager.GetDefaultProperties)
            {
                switch (property.PropertyName)
                {
                    case "ProductType":
                        product.ProductTypeId = property.IntValue;
                        break;
                    case "ParentGroupedProductId":
                        product.ParentGroupedProductId = property.IntValue;
                        break;
                    case "VisibleIndividually":
                        product.VisibleIndividually = property.BooleanValue;
                        break;
                    case "Name":
                        product.Name = property.StringValue;
                        break;
                    case "ShortDescription":
                        product.ShortDescription = property.StringValue;
                        break;
                    case "FullDescription":
                        product.FullDescription = property.StringValue;
                        break;
                    case "Vendor":
                        //vendor can't change this field
                        if (actingVendor == null)
                            product.VendorId = property.IntValue;
                        break;
                    case "ProductTemplate":
                        product.ProductTemplateId = property.IntValue;
                        break;
                    case "ShowOnHomepage":
                        //vendor can't change this field
                        if (actingVendor == null)
                            product.ShowOnHomepage = property.BooleanValue;
                        break;
                    case "DisplayOrder":
                        //vendor can't change this field
                        if (actingVendor == null)
                            product.DisplayOrder = property.IntValue;
                        break;
                    case "MetaKeywords":
                        product.MetaKeywords = property.StringValue;
                        break;
                    case "MetaDescription":
                        product.MetaDescription = property.StringValue;
                        break;
                    case "MetaTitle":
                        product.MetaTitle = property.StringValue;
                        break;
                    case "AllowCustomerReviews":
                        product.AllowCustomerReviews = property.BooleanValue;
                        break;
                    case "Published":
                        product.Published = property.BooleanValue;
                        break;
                    case "SKU":
                        product.Sku = property.StringValue;
                        break;
                    case "ManufacturerPartNumber":
                        product.ManufacturerPartNumber = property.StringValue;
                        break;
                    case "Gtin":
                        product.Gtin = property.StringValue;
                        break;
                    case "IsGiftCard":
                        product.IsGiftCard = property.BooleanValue;
                        break;
                    case "GiftCardType":
                        product.GiftCardTypeId = property.IntValue;
                        break;
                    case "OverriddenGiftCardAmount":
                        product.OverriddenGiftCardAmount = property.DecimalValue;
                        break;
                    case "RequireOtherProducts":
                        product.RequireOtherProducts = property.BooleanValue;
                        break;
                    case "RequiredProductIds":
                        product.RequiredProductIds = property.StringValue;
                        break;
                    case "AutomaticallyAddRequiredProducts":
                        product.AutomaticallyAddRequiredProducts = property.BooleanValue;
                        break;
                    case "IsDownload":
                        product.IsDownload = property.BooleanValue;
                        break;
                    case "DownloadId":
                        product.DownloadId = property.IntValue;
                        break;
                    case "UnlimitedDownloads":
                        product.UnlimitedDownloads = property.BooleanValue;
                        break;
                    case "MaxNumberOfDownloads":
                        product.MaxNumberOfDownloads = property.IntValue;
                        break;
                    case "DownloadActivationType":
                        product.DownloadActivationTypeId = property.IntValue;
                        break;
                    case "HasSampleDownload":
                        product.HasSampleDownload = property.BooleanValue;
                        break;
                    case "SampleDownloadId":
                        product.SampleDownloadId = property.IntValue;
                        break;
                    case "HasUserAgreement":
                        product.HasUserAgreement = property.BooleanValue;
                        break;
                    case "UserAgreementText":
                        product.UserAgreementText = property.StringValue;
                        break;
                    case "IsRecurring":
                        product.IsRecurring = property.BooleanValue;
                        break;
                    case "RecurringCycleLength":
                        product.RecurringCycleLength = property.IntValue;
                        break;
                    case "RecurringCyclePeriod":
                        product.RecurringCyclePeriodId = property.IntValue;
                        break;
                    case "RecurringTotalCycles":
                        product.RecurringTotalCycles = property.IntValue;
                        break;
                    case "IsRental":
                        product.IsRental = property.BooleanValue;
                        break;
                    case "RentalPriceLength":
                        product.RentalPriceLength = property.IntValue;
                        break;
                    case "RentalPricePeriod":
                        product.RentalPricePeriodId = property.IntValue;
                        break;
                    case "IsShipEnabled":
                        product.IsShipEnabled = property.BooleanValue;
                        break;
                    case "IsFreeShipping":
                        product.IsFreeShipping = property.BooleanValue;
                        break;
                    case "ShipSeparately":
                        product.ShipSeparately = property.BooleanValue;
                        break;
                    case "AdditionalShippingCharge":
                        product.AdditionalShippingCharge = property.DecimalValue;
                        break;
                    case "DeliveryDate":
                        product.DeliveryDateId = property.IntValue;
                        break;
                    case "IsTaxExempt":
                        product.IsTaxExempt = property.BooleanValue;
                        break;
                    case "TaxCategory":
                        product.TaxCategoryId = property.IntValue;
                        break;
                    case "ManageInventoryMethod":
                        product.ManageInventoryMethodId = property.IntValue;
                        break;
                    case "ProductAvailabilityRange":
                        product.ProductAvailabilityRangeId = property.IntValue;
                        break;
                    case "UseMultipleWarehouses":
                        product.UseMultipleWarehouses = property.BooleanValue;
                        break;
                    case "WarehouseId":
                        product.WarehouseId = property.IntValue;
                        break;
                    case "StockQuantity":
                        product.StockQuantity = property.IntValue;
                        break;
                    case "DisplayStockAvailability":
                        product.DisplayStockAvailability = property.BooleanValue;
                        break;
                    case "DisplayStockQuantity":
                        product.DisplayStockQuantity = property.BooleanValue;
                        break;
                    case "MinStockQuantity":
                        product.MinStockQuantity = property.IntValue;
                        break;
                    case "LowStockActivity":
                        product.LowStockActivityId = property.IntValue;
                        break;
                    case "NotifyAdminForQuantityBelow":
                        product.NotifyAdminForQuantityBelow = property.IntValue;
                        break;
                    case "BackorderMode":
                        product.BackorderModeId = property.IntValue;
                        break;
                    case "AllowBackInStockSubscriptions":
                        product.AllowBackInStockSubscriptions = property.BooleanValue;
                        break;
                    case "OrderMinimumQuantity":
                        product.OrderMinimumQuantity = property.IntValue;
                        break;
                    case "OrderMaximumQuantity":
                        product.OrderMaximumQuantity = property.IntValue;
                        break;
                    case "AllowedQuantities":
                        product.AllowedQuantities = property.StringValue;
                        break;
                    case "AllowAddingOnlyExistingAttributeCombinations":
                        product.AllowAddingOnlyExistingAttributeCombinations = property.BooleanValue;
                        break;
                    case "DisableBuyButton":
                        product.DisableBuyButton = property.BooleanValue;
                        break;
                    case "DisableWishlistButton":
                        product.DisableWishlistButton = property.BooleanValue;
                        break;
                    case "AvailableForPreOrder":
                        product.AvailableForPreOrder = property.BooleanValue;
                        break;
                    case "PreOrderAvailabilityStartDateTimeUtc":
                        product.PreOrderAvailabilityStartDateTimeUtc = property.DateTimeNullable;
                        break;
                    case "CallForPrice":
                        product.CallForPrice = property.BooleanValue;
                        break;
                    case "Price":
                        product.Price = property.DecimalValue;
                        break;
                    case "OldPrice":
                        product.OldPrice = property.DecimalValue;
                        break;
                    case "ProductCost":
                        product.ProductCost = property.DecimalValue;
                        break;
                    case "CustomerEntersPrice":
                        product.CustomerEntersPrice = property.BooleanValue;
                        break;
                    case "MinimumCustomerEnteredPrice":
                        product.MinimumCustomerEnteredPrice = property.DecimalValue;
                        break;
                    case "MaximumCustomerEnteredPrice":
                        product.MaximumCustomerEnteredPrice = property.DecimalValue;
                        break;
                    case "BasepriceEnabled":
                        product.BasepriceEnabled = property.BooleanValue;
                        break;
                    case "BasepriceAmount":
                        product.BasepriceAmount = property.DecimalValue;
                        break;
                    case "BasepriceUnit":
                        product.BasepriceUnitId = property.IntValue;
                        break;
                    case "BasepriceBaseAmount":
                        product.BasepriceBaseAmount = property.DecimalValue;
                        break;
                    case "BasepriceBaseUnit":
                        product.BasepriceBaseUnitId = property.IntValue;
                        break;
                    case "MarkAsNew":
                        product.MarkAsNew = property.BooleanValue;
                        break;
                    case "MarkAsNewStartDateTimeUtc":
                        product.MarkAsNewStartDateTimeUtc = property.DateTimeNullable;
                        break;
                    case "MarkAsNewEndDateTimeUtc":
                        product.MarkAsNewEndDateTimeUtc = property.DateTimeNullable;
                        break;
                    case "Weight":
                        product.Weight = property.DecimalValue;
                        break;
                    case "Length":
                        product.Length = property.DecimalValue;
                        break;
                    case "Width":
                        product.Width = property.DecimalValue;
                        break;
                    case "Height":
                        product.Height = property.DecimalValue;
                        break;
                    case "IsLimitedToStores":
                        product.LimitedToStores = property.BooleanValue;
                        break;
                    case "DisplayAttributeCombinationImagesOnly":
                        product.DisplayAttributeCombinationImagesOnly = property.BooleanValue;
                        break;
                    case "AgeVerification":
                        product.AgeVerification = property.BooleanValue;
                        break;
                    case "MinimumAgeToPurchase":
                        product.MinimumAgeToPurchase = property.IntValue;
                        break;
                    case "AvailableStartDateTimeUtc":
                        product.AvailableStartDateTimeUtc = property.DateTimeNullable;
                        break;
                    case "AvailableEndDateTimeUtc":
                        product.AvailableEndDateTimeUtc = property.DateTimeNullable;
                        break;
                    case "Id":
                        product.Id = property.IntValue;
                        break;
                }
            }

            //set some default values if not specified
            if (isNew && metadata.Properties.All(p => p.PropertyName != "ProductType"))
                product.ProductType = ProductType.SimpleProduct;
            if (isNew && metadata.Properties.All(p => p.PropertyName != "VisibleIndividually"))
                product.VisibleIndividually = true;
            if (isNew && metadata.Properties.All(p => p.PropertyName != "Published"))
                product.Published = true;

            if (isNew && actingVendor != null)
                product.VendorId = actingVendor.Id;

            var originalVendorId = product.VendorId;
            product.VendorId = actingVendor?.Id ?? product.VendorId;

            //set Some common properties
            var nowUtc = DateTime.UtcNow;
            if (isNew)
            {
                product.CreatedOnUtc = nowUtc;
                product.UpdatedOnUtc = nowUtc;
                await _productService.InsertProductAsync(product);
            }
            else
            {
                product.UpdatedOnUtc = nowUtc;
                await _productService.UpdateProductAsync(product);
            }

            if (originalVendorId > 0 && product.VendorId != originalVendorId)
            {
                //vendor is no longer assign to this product
                //set IsShipEnabled to "false" in order to exclude this product from shipping
                product.IsShipEnabled = false;
                await _productService.UpdateProductAsync(product);
            }

            //warehouse
            if (!product.UseMultipleWarehouses && product.ManageInventoryMethod == ManageInventoryMethod.ManageStock)
            {
                var sku = product.Sku;
                var warehouse = await _warehouseService.GetWarehouseByIdAsync(product.WarehouseId);

                if (warehouse != null && warehouse.AdminComment != sku)
                {
                    warehouse.AdminComment = sku;
                    await _warehouseService.UpdateWarehouseAsync(warehouse);
                }
            }

            //back in stock subscriptions
            if (!isNew &&
                product.ManageInventoryMethod == ManageInventoryMethod.ManageStock &&
                product.BackorderMode == BackorderMode.NoBackorders &&
                product.AllowBackInStockSubscriptions &&
                await _productService.GetTotalStockQuantityAsync(product) > 0 &&
                prevTotalStockQuantity <= 0 &&
                product.Published &&
                !product.Deleted)
            {
                await _backInStockSubscriptionService.SendNotificationsToSubscribersAsync(product);
            }

            var tempProperty = metadata.Manager.GetDefaultProperty("SeName");

            //search engine name
            var seName = tempProperty?.StringValue ?? (isNew ? string.Empty : await _urlRecordService.GetSeNameAsync(product, 0));
            await _urlRecordService.SaveSlugAsync(product, await _urlRecordService.ValidateSeNameAsync(product, seName, product.Name, true), 0);

            //save product localized data
            await ImportProductLocalizedAsync(product, metadata, iRow, languages);

            tempProperty = metadata.Manager.GetDefaultProperty("Categories");

            if (tempProperty != null)
            {
                var categoryList = tempProperty.StringValue;

                //category mappings
                var categories = isNew || !allProductsCategoryIds.ContainsKey(product.Id) ? Array.Empty<int>() : allProductsCategoryIds[product.Id];

                var storesIds = product.LimitedToStores
                    ? (await _storeMappingService.GetStoresIdsWithAccessAsync(product)).ToList()
                    : new List<int>();

                var importedCategories = await categoryList.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(categoryName => new CategoryKey(categoryName, storesIds: storesIds))
                    .SelectAwait(async categoryKey =>
                    {
                        var rez = (allCategories.TryGetValue(categoryKey, out var value) ? value.Id : allCategories.Values.FirstOrDefault(c => c.Name == categoryKey.Key)?.Id) ??
                                  allCategories.FirstOrDefault(p =>
                                          p.Key.Key.Equals(categoryKey.Key, StringComparison.InvariantCultureIgnoreCase))
                                      .Value?.Id;

                        if (!rez.HasValue && int.TryParse(categoryKey.Key, out var id))
                            rez = id;

                        if (!rez.HasValue)
                            //database doesn't contain the imported category
                            //this can happen if the category was deleted during the import process
                            await _logger.WarningAsync(string.Format(await _localizationService.GetResourceAsync("Admin.Catalog.Products.Import.DatabaseNotContainCategory"), product.Name, categoryKey.Key));

                        return rez;
                    }).Where(id => id != null).ToListAsync();

                foreach (var categoryId in importedCategories)
                {
                    if (categories.Any(c => c == categoryId))
                        continue;

                    var productCategory = new ProductCategory
                    {
                        ProductId = product.Id,
                        CategoryId = categoryId.Value,
                        IsFeaturedProduct = false,
                        DisplayOrder = 1
                    };
                    await _categoryService.InsertProductCategoryAsync(productCategory);
                }

                //delete product categories
                var deletedProductCategories = await categories.Where(categoryId => !importedCategories.Contains(categoryId))
                    .SelectAwait(async categoryId => (await _categoryService.GetProductCategoriesByProductIdAsync(product.Id, true)).FirstOrDefault(pc => pc.CategoryId == categoryId)).Where(pc => pc != null).ToListAsync();

                await _categoryService.DeleteProductCategoriesAsync(deletedProductCategories);
            }

            tempProperty = metadata.Manager.GetDefaultProperty("Manufacturers");
            if (tempProperty != null)
            {
                var manufacturerList = tempProperty.StringValue;

                //manufacturer mappings
                var manufacturers = isNew || !allProductsManufacturerIds.ContainsKey(product.Id) ? Array.Empty<int>() : allProductsManufacturerIds[product.Id];

                var importedManufacturers = await manufacturerList
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .SelectAwait(async x =>
                    {
                        var id = allManufacturers.FirstOrDefault(m => m.Name == x.Trim())?.Id;

                        if (id != null)
                            return id;

                        id = int.TryParse(x, out var parsedId) ? parsedId : null;

                        if (!id.HasValue)
                            //database doesn't contain the imported manufacturer
                            //this can happen if the manufacturer was deleted during the import process
                            await _logger.WarningAsync(string.Format(await _localizationService.GetResourceAsync("Admin.Catalog.Products.Import.DatabaseNotContainManufacturer"), product.Name, x));

                        return id;
                    }).Where(id => id.HasValue).ToListAsync();

                foreach (var manufacturerId in importedManufacturers)
                {
                    if (manufacturers.Any(c => c == manufacturerId))
                        continue;

                    var productManufacturer = new ProductManufacturer
                    {
                        ProductId = product.Id,
                        ManufacturerId = manufacturerId.Value,
                        IsFeaturedProduct = false,
                        DisplayOrder = 1
                    };
                    await _manufacturerService.InsertProductManufacturerAsync(productManufacturer);
                }

                //delete product manufacturers
                var deletedProductsManufacturers = await manufacturers.Where(manufacturerId => !importedManufacturers.Contains(manufacturerId))
                    .SelectAwait(async manufacturerId => (await _manufacturerService.GetProductManufacturersByProductIdAsync(product.Id)).FirstOrDefault(pc => pc.ManufacturerId == manufacturerId)).ToListAsync();

                deletedProductsManufacturers = deletedProductsManufacturers.Where(m => m != null).ToList();
                await _manufacturerService.DeleteProductManufacturersAsync(deletedProductsManufacturers);
            }

            tempProperty = metadata.Manager.GetDefaultProperty("ProductTags");
            if (tempProperty != null)
            {
                var productTags = tempProperty.StringValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

                //searching existing product tags by their id
                var productTagIds = productTags.Where(pt => int.TryParse(pt, out var _)).Select(int.Parse);

                var productTagsByIds = (await _productTagService.GetAllProductTagsByProductIdAsync(product.Id)).Where(pt => productTagIds.Contains(pt.Id)).ToList();

                productTags.AddRange(productTagsByIds.Select(pt => pt.Name));
                var filter = productTagsByIds.Select(pt => pt.Id.ToString()).ToList();

                //product tag mappings
                await _productTagService.UpdateProductTagsAsync(product, productTags.Where(pt => !filter.Contains(pt)).ToArray());
            }

            tempProperty = metadata.Manager.GetDefaultProperty("LimitedToStores");
            if (tempProperty != null)
            {
                var limitedToStoresList = tempProperty.StringValue;

                var importedStores = product.LimitedToStores ? limitedToStoresList.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => allStores.FirstOrDefault(store => store.Name == x.Trim())?.Id ?? int.Parse(x.Trim())).ToList() : new List<int>();

                await _storeMappingService.SaveStoreMappingsAsync(product, importedStores);
            }

            var pictureMetaData = new ProductPictureMetadata
            {
                ProductItem = product,
                IsNew = isNew
            };

            var pictureProperties = new List<string> { "Picture1", "Picture2", "Picture3" };

            if (actingVendor != null)
                pictureProperties = pictureProperties.Take(_vendorSettings.MaximumProductPicturesNumber).ToList();

            foreach (var propertyName in pictureProperties)
                pictureMetaData.PicturePaths.Add(await DownloadFileAsync(metadata.Manager.GetDefaultProperty(propertyName)?.StringValue, downloadedFiles));

            productPictureMetadata.Add(pictureMetaData);

            lastLoadedProduct = product;
        }

        if (_mediaSettings.ImportProductImagesUsingHash && await _pictureService.IsStoreInDbAsync())
        await ImportProductImagesUsingHashAsync(productPictureMetadata, allProductsBySku);
        else
            await ImportProductImagesUsingServicesAsync(productPictureMetadata);

        foreach (var downloadedFile in downloadedFiles)
        {
            if (!_fileProvider.FileExists(downloadedFile))
                continue;

            try
            {
                _fileProvider.DeleteFile(downloadedFile);
            }
            catch
            {
                // ignored
            }
        }

        //activity log
        await _customerActivityService.InsertActivityAsync("ImportProducts", string.Format(await _localizationService.GetResourceAsync("ActivityLog.ImportProducts"), metadata.CountProductsInFile));
    }

    protected override async Task<ImportProductMetadata> PrepareImportProductDataAsync(IXLWorkbook workbook, IList<Language> languages)
    {
        return await PrepareImportProductDataWithMappingsAsync(workbook, languages, null);
    }

    protected virtual async Task<ImportProductMetadata> PrepareImportProductDataWithMappingsAsync(IXLWorkbook workbook, IList<Language> languages, IEnumerable<ImportProductMapping> mappings)
    {
        var workbookMetadata = GetWorkbookMetadata<Product>(workbook, languages);
        var defaultWorksheet = workbookMetadata.DefaultWorksheet;
        var defaultProperties = workbookMetadata.DefaultProperties;
        var localizedProperties = workbookMetadata.LocalizedProperties;

        var mappingList = mappings?.Where(m => m != null && !string.IsNullOrWhiteSpace(m.Property)).ToList() ?? new List<ImportProductMapping>();
        if (mappingList.Any())
        {
            var propertyLookup = defaultProperties
                .GroupBy(p => p.PropertyName, StringComparer.InvariantCultureIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.InvariantCultureIgnoreCase);

            foreach (var mapping in mappingList.Where(m => m.ColumnIndex > 0))
            {
                if (!propertyLookup.TryGetValue(mapping.Property, out var property))
                {
                    property = new PropertyByName<Product>(mapping.Property);
                    defaultProperties.Add(property);
                    propertyLookup.Add(mapping.Property, property);
                }

                property.PropertyOrderPosition = mapping.ColumnIndex;
            }
        }

        var manager = new PropertyManager<Product>(defaultProperties, _catalogSettings, localizedProperties, languages);

        if (mappingList.Any())
        {
            foreach (var mapping in mappingList.Where(m => m.ColumnIndex > 0))
            {
                var property = manager.GetDefaultProperty(mapping.Property);
                if (property != null)
                    property.PropertyOrderPosition = mapping.ColumnIndex;
            }
        }

        return await PrepareImportProductDataCommonAsync(workbookMetadata, defaultWorksheet, defaultProperties, localizedProperties, manager, languages);
    }

    protected virtual async Task<ImportProductMetadata> PrepareImportProductDataBaseAsync(IXLWorkbook workbook, IList<Language> languages)
    {
        var workbookMetadata = GetWorkbookMetadata<Product>(workbook, languages);
        var defaultWorksheet = workbookMetadata.DefaultWorksheet;
        var defaultProperties = workbookMetadata.DefaultProperties;
        var localizedProperties = workbookMetadata.LocalizedProperties;

        var manager = new PropertyManager<Product>(defaultProperties, _catalogSettings, localizedProperties, languages);

        return await PrepareImportProductDataCommonAsync(workbookMetadata, defaultWorksheet, defaultProperties, localizedProperties, manager, languages);
    }

    protected virtual async Task<ImportProductMetadata> PrepareImportProductDataCommonAsync(
        WorkbookMetadata<Product> workbookMetadata,
        IXLWorksheet defaultWorksheet,
        IList<PropertyByName<Product>> defaultProperties,
        IList<PropertyByName<Product>> localizedProperties,
        PropertyManager<Product> manager,
        IList<Language> languages)
    {
        var productAttributeProperties = new[]
        {
            new PropertyByName<ExportProductAttribute>("AttributeId"),
            new PropertyByName<ExportProductAttribute>("AttributeName"),
            new PropertyByName<ExportProductAttribute>("DefaultValue"),
            new PropertyByName<ExportProductAttribute>("ValidationMinLength"),
            new PropertyByName<ExportProductAttribute>("ValidationMaxLength"),
            new PropertyByName<ExportProductAttribute>("ValidationFileAllowedExtensions"),
            new PropertyByName<ExportProductAttribute>("ValidationFileMaximumSize"),
            new PropertyByName<ExportProductAttribute>("AttributeTextPrompt"),
            new PropertyByName<ExportProductAttribute>("AttributeIsRequired"),
            new PropertyByName<ExportProductAttribute>("AttributeControlType"),
            new PropertyByName<ExportProductAttribute>("AttributeDisplayOrder"),
            new PropertyByName<ExportProductAttribute>("ProductAttributeValueId"),
            new PropertyByName<ExportProductAttribute>("ValueName"),
            new PropertyByName<ExportProductAttribute>("AttributeValueType"),
            new PropertyByName<ExportProductAttribute>("AssociatedProductId"),
            new PropertyByName<ExportProductAttribute>("ColorSquaresRgb"),
            new PropertyByName<ExportProductAttribute>("ImageSquaresPictureId"),
            new PropertyByName<ExportProductAttribute>("PriceAdjustment"),
            new PropertyByName<ExportProductAttribute>("PriceAdjustmentUsePercentage"),
            new PropertyByName<ExportProductAttribute>("WeightAdjustment"),
            new PropertyByName<ExportProductAttribute>("Cost"),
            new PropertyByName<ExportProductAttribute>("CustomerEntersQty"),
            new PropertyByName<ExportProductAttribute>("Quantity"),
            new PropertyByName<ExportProductAttribute>("IsPreSelected"),
            new PropertyByName<ExportProductAttribute>("DisplayOrder"),
            new PropertyByName<ExportProductAttribute>("PictureIds")
        };

        var productAttributeLocalizedProperties = new[]
        {
            new PropertyByName<ExportProductAttribute>("DefaultValue"),
            new PropertyByName<ExportProductAttribute>("AttributeTextPrompt"),
            new PropertyByName<ExportProductAttribute>("ValueName")
        };

        var productAttributeManager = new PropertyManager<ExportProductAttribute>(productAttributeProperties, _catalogSettings, productAttributeLocalizedProperties, languages);

        var specificationAttributeProperties = new[]
        {
            new PropertyByName<ExportSpecificationAttribute>("AttributeType", (p, l) => p.AttributeTypeId),
            new PropertyByName<ExportSpecificationAttribute>("SpecificationAttribute", (p, l) => p.SpecificationAttributeId),
            new PropertyByName<ExportSpecificationAttribute>("CustomValue", (p, l) => p.CustomValue),
            new PropertyByName<ExportSpecificationAttribute>("SpecificationAttributeOptionId", (p, l) => p.SpecificationAttributeOptionId),
            new PropertyByName<ExportSpecificationAttribute>("AllowFiltering", (p, l) => p.AllowFiltering),
            new PropertyByName<ExportSpecificationAttribute>("ShowOnProductPage", (p, l) => p.ShowOnProductPage),
            new PropertyByName<ExportSpecificationAttribute>("DisplayOrder", (p, l) => p.DisplayOrder)
        };

        var specificationAttributeLocalizedProperties = new[]
        {
            new PropertyByName<ExportSpecificationAttribute>("CustomValue")
        };

        var specificationAttributeManager = new PropertyManager<ExportSpecificationAttribute>(specificationAttributeProperties, _catalogSettings, specificationAttributeLocalizedProperties, languages);

        var tierPriceProperties = new[]
        {
            new PropertyByName<ExportTierPrice>("TierPriceId"),
            new PropertyByName<ExportTierPrice>("Store"),
            new PropertyByName<ExportTierPrice>("CustomerRole"),
            new PropertyByName<ExportTierPrice>("Quantity"),
            new PropertyByName<ExportTierPrice>("Price"),
            new PropertyByName<ExportTierPrice>("StartDateTimeUtc"),
            new PropertyByName<ExportTierPrice>("EndDateTimeUtc")
        };

        var tierPriceManager = new PropertyManager<ExportTierPrice>(tierPriceProperties, _catalogSettings, languages: languages);

        var endRow = 2;
        var allCategories = new List<string>();
        var allSku = new List<string>();

        var tempProperty = manager.GetDefaultProperty("Categories");
        var categoryCellNum = tempProperty?.PropertyOrderPosition ?? -1;

        tempProperty = manager.GetDefaultProperty("SKU");
        var skuCellNum = tempProperty?.PropertyOrderPosition ?? -1;

        var allManufacturers = new List<string>();
        tempProperty = manager.GetDefaultProperty("Manufacturers");
        var manufacturerCellNum = tempProperty?.PropertyOrderPosition ?? -1;

        var allStores = new List<string>();
        tempProperty = manager.GetDefaultProperty("LimitedToStores");
        var limitedToStoresCellNum = tempProperty?.PropertyOrderPosition ?? -1;

        if (_catalogSettings.ExportImportUseDropdownlistsForAssociatedEntities)
        {
            tierPriceManager.SetSelectList("Store", (await _storeService.GetAllStoresAsync()).ToSelectList(p => (p as Store)?.Name ?? string.Empty));
            tierPriceManager.SetSelectList("CustomerRole", (await _customerService.GetAllCustomerRolesAsync()).ToSelectList(p => (p as CustomerRole)?.Name ?? string.Empty));

            productAttributeManager.SetSelectList("AttributeControlType", await AttributeControlType.TextBox.ToSelectListAsync(useLocalization: false));
            productAttributeManager.SetSelectList("AttributeValueType", await AttributeValueType.Simple.ToSelectListAsync(useLocalization: false));

            specificationAttributeManager.SetSelectList("AttributeType", await SpecificationAttributeType.Option.ToSelectListAsync(useLocalization: false));
            specificationAttributeManager.SetSelectList("SpecificationAttribute", (await _specificationAttributeService
                    .GetAllSpecificationAttributesAsync())
                .Select(sa => sa as BaseEntity)
                .ToSelectList(p => (p as SpecificationAttribute)?.Name ?? string.Empty));

            manager.SetSelectList("ProductType", await ProductType.SimpleProduct.ToSelectListAsync(useLocalization: false));
            manager.SetSelectList("GiftCardType", await GiftCardType.Virtual.ToSelectListAsync(useLocalization: false));
            manager.SetSelectList("DownloadActivationType",
                await DownloadActivationType.Manually.ToSelectListAsync(useLocalization: false));
            manager.SetSelectList("ManageInventoryMethod",
                await ManageInventoryMethod.DontManageStock.ToSelectListAsync(useLocalization: false));
            manager.SetSelectList("LowStockActivity",
                await LowStockActivity.Nothing.ToSelectListAsync(useLocalization: false));
            manager.SetSelectList("BackorderMode", await BackorderMode.NoBackorders.ToSelectListAsync(useLocalization: false));
            manager.SetSelectList("RecurringCyclePeriod",
                await RecurringProductCyclePeriod.Days.ToSelectListAsync(useLocalization: false));
            manager.SetSelectList("RentalPricePeriod", await RentalPricePeriod.Days.ToSelectListAsync(useLocalization: false));

            manager.SetSelectList("Vendor",
                (await _vendorService.GetAllVendorsAsync(showHidden: true)).Select(v => v as BaseEntity)
                .ToSelectList(p => (p as Vendor)?.Name ?? string.Empty));
            manager.SetSelectList("ProductTemplate",
                (await _productTemplateService.GetAllProductTemplatesAsync()).Select(pt => pt as BaseEntity)
                .ToSelectList(p => (p as ProductTemplate)?.Name ?? string.Empty));
            manager.SetSelectList("DeliveryDate",
                (await _dateRangeService.GetAllDeliveryDatesAsync()).Select(dd => dd as BaseEntity)
                .ToSelectList(p => (p as DeliveryDate)?.Name ?? string.Empty));
            manager.SetSelectList("ProductAvailabilityRange",
                (await _dateRangeService.GetAllProductAvailabilityRangesAsync()).Select(range => range as BaseEntity)
                .ToSelectList(p => (p as ProductAvailabilityRange)?.Name ?? string.Empty));
            manager.SetSelectList("TaxCategory",
                (await _taxCategoryService.GetAllTaxCategoriesAsync()).Select(tc => tc as BaseEntity)
                .ToSelectList(p => (p as TaxCategory)?.Name ?? string.Empty));
            manager.SetSelectList("BasepriceUnit",
                (await _measureService.GetAllMeasureWeightsAsync()).Select(mw => mw as BaseEntity)
                .ToSelectList(p => (p as MeasureWeight)?.Name ?? string.Empty));
            manager.SetSelectList("BasepriceBaseUnit",
                (await _measureService.GetAllMeasureWeightsAsync()).Select(mw => mw as BaseEntity)
                .ToSelectList(p => (p as MeasureWeight)?.Name ?? string.Empty));
        }

        var allAttributeIds = new List<int>();
        var allSpecificationAttributeOptionIds = new List<int>();

        var attributeIdCellNum = 1 + ExportImportDefaults.ProductAdditionalInfoCellOffset;
        var specificationAttributeOptionIdCellNum =
            specificationAttributeManager.GetIndex("SpecificationAttributeOptionId") +
            ExportImportDefaults.ProductAdditionalInfoCellOffset;

        var productsInFile = new List<int>();

        var typeOfExportedAttribute = ExportedAdditionalProductInfoType.NotSpecified;
        while (true)
        {
            var allColumnsAreEmpty = manager.GetDefaultProperties
                .Select(property => defaultWorksheet.Row(endRow).Cell(property.PropertyOrderPosition))
                .All(cell => string.IsNullOrEmpty(cell?.Value.ToString()));

            if (allColumnsAreEmpty)
                break;

            if (new[] { 1, 2 }.Select(cellNum => defaultWorksheet.Row(endRow).Cell(cellNum))
                    .All(cell => string.IsNullOrEmpty(cell?.Value.ToString())) &&
                defaultWorksheet.Row(endRow).OutlineLevel == 0)
            {
                var cellValue = defaultWorksheet.Row(endRow).Cell(attributeIdCellNum).Value;
                await SetOutLineForProductAttributeRowAsync(cellValue, defaultWorksheet, endRow);
                await SetOutLineForSpecificationAttributeRowAsync(cellValue, defaultWorksheet, endRow);
            }

            if (defaultWorksheet.Row(endRow).OutlineLevel != 0)
            {
                var newTypeOfExportedAttribute = GetTypeOfExportedAdditionalProductInfo(defaultWorksheet, workbookMetadata.LocalizedWorksheets, productAttributeManager, specificationAttributeManager, tierPriceManager, endRow);

                if (newTypeOfExportedAttribute != ExportedAdditionalProductInfoType.NotSpecified && newTypeOfExportedAttribute != typeOfExportedAttribute)
                {
                    typeOfExportedAttribute = newTypeOfExportedAttribute;
                    endRow++;
                    continue;
                }

                switch (typeOfExportedAttribute)
                {
                    case ExportedAdditionalProductInfoType.ProductAttribute:
                        productAttributeManager.ReadDefaultFromXlsx(defaultWorksheet, endRow,
                            ExportImportDefaults.ProductAdditionalInfoCellOffset);
                        if (int.TryParse(defaultWorksheet.Row(endRow).Cell(attributeIdCellNum).Value.ToString(), out var aid))
                            allAttributeIds.Add(aid);
                        break;
                    case ExportedAdditionalProductInfoType.SpecificationAttribute:
                        specificationAttributeManager.ReadDefaultFromXlsx(defaultWorksheet, endRow, ExportImportDefaults.ProductAdditionalInfoCellOffset);

                        if (int.TryParse(defaultWorksheet.Row(endRow).Cell(specificationAttributeOptionIdCellNum).Value.ToString(), out var saoid))
                            allSpecificationAttributeOptionIds.Add(saoid);
                        break;
                }

                endRow++;
                continue;
            }

            if (categoryCellNum > 0)
            {
                var categoryIds = defaultWorksheet.Row(endRow).Cell(categoryCellNum).Value.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(categoryIds))
                    allCategories.AddRange(categoryIds
                        .Split(new[] { ";", ">>" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())
                        .Distinct());
            }

            if (skuCellNum > 0)
            {
                var sku = defaultWorksheet.Row(endRow).Cell(skuCellNum).Value.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(sku))
                    allSku.Add(sku);
            }

            if (manufacturerCellNum > 0)
            {
                var manufacturerIds = defaultWorksheet.Row(endRow).Cell(manufacturerCellNum).Value.ToString() ??
                                      string.Empty;
                if (!string.IsNullOrEmpty(manufacturerIds))
                    allManufacturers.AddRange(manufacturerIds
                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));
            }

            if (limitedToStoresCellNum > 0)
            {
                var storeIds = defaultWorksheet.Row(endRow).Cell(limitedToStoresCellNum).Value.ToString() ??
                               string.Empty;
                if (!string.IsNullOrEmpty(storeIds))
                    allStores.AddRange(storeIds
                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));
            }

            productsInFile.Add(endRow);

            endRow++;
        }

        var notExistingCategories = await _categoryService.GetNotExistingCategoriesAsync(allCategories.ToArray());
        if (notExistingCategories.Any())
        {
            throw new ArgumentException(string.Format(await _localizationService.GetResourceAsync("Admin.Catalog.Products.Import.CategoriesDontExist"), string.Join(", ", notExistingCategories)));
        }

        var notExistingManufacturers = await _manufacturerService.GetNotExistingManufacturersAsync(allManufacturers.ToArray());
        if (notExistingManufacturers.Any())
        {
            throw new ArgumentException(string.Format(await _localizationService.GetResourceAsync("Admin.Catalog.Products.Import.ManufacturersDontExist"), string.Join(", ", notExistingManufacturers)));
        }

        var notExistingProductAttributes = await _productAttributeService.GetNotExistingAttributesAsync(allAttributeIds.ToArray());
        if (notExistingProductAttributes.Any())
        {
            throw new ArgumentException(string.Format(await _localizationService.GetResourceAsync("Admin.Catalog.Products.Import.ProductAttributesDontExist"), string.Join(", ", notExistingProductAttributes)));
        }

        var notExistingSpecificationAttributeOptions = await _specificationAttributeService.GetNotExistingSpecificationAttributeOptionsAsync(allSpecificationAttributeOptionIds.Where(saoId => saoId != 0).ToArray());
        if (notExistingSpecificationAttributeOptions.Any())
        {
            throw new ArgumentException($"The following specification attribute option ID(s) don't exist - {string.Join(", ", notExistingSpecificationAttributeOptions)}");
        }

        var notExistingStores = await _storeService.GetNotExistingStoresAsync(allStores.ToArray());
        if (notExistingStores.Any())
            throw new ArgumentException(string.Format(await _localizationService.GetResourceAsync("Admin.Catalog.Products.Import.StoresDontExist"), string.Join(", ", notExistingStores)));

        var result = new ImportProductMetadata();
        SetMetadata(result, nameof(ImportProductMetadata.EndRow), endRow);
        SetMetadata(result, nameof(ImportProductMetadata.Manager), manager);
        SetMetadata(result, nameof(ImportProductMetadata.Properties), defaultProperties);
        SetMetadata(result, nameof(ImportProductMetadata.ProductsInFile), productsInFile);
        SetMetadata(result, nameof(ImportProductMetadata.ProductAttributeManager), productAttributeManager);
        SetMetadata(result, nameof(ImportProductMetadata.DefaultWorksheet), defaultWorksheet);
        SetMetadata(result, nameof(ImportProductMetadata.LocalizedWorksheets), workbookMetadata.LocalizedWorksheets);
        SetMetadata(result, nameof(ImportProductMetadata.SpecificationAttributeManager), specificationAttributeManager);
        SetMetadata(result, nameof(ImportProductMetadata.TierPriceManager), tierPriceManager);
        SetMetadata(result, nameof(ImportProductMetadata.SkuCellNum), skuCellNum);
        SetMetadata(result, nameof(ImportProductMetadata.AllSku), allSku);

        return result;
    }

    private async Task ImportProductsFromSplitedXlsxAsync(IXLWorksheet worksheet, ImportProductMetadata metadata, IEnumerable<ImportProductMapping> mappings, int? vendorId)
    {
        foreach (var path in SplitProductFile(worksheet, metadata))
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var importManager = EngineContext.Current.Resolve<IDynamicImportManager>(scope);

            using var sr = new StreamReader(path);
            await importManager.ImportProductsFromXlsxAsync(sr.BaseStream, mappings, vendorId);

            try
            {
                _fileProvider.DeleteFile(path);
            }
            catch
            {
            }
        }
    }

    private static void SetMetadata<TValue>(ImportProductMetadata metadata, string propertyName, TValue value)
    {
        var prop = typeof(ImportProductMetadata).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        prop?.SetValue(metadata, value);
    }
}

