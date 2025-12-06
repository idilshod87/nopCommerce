using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Plugin.Misc.WebApi.Frontend.DTOs;
using Nop.Services.Catalog;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Public API for catalog browsing (categories, manufacturers, product tags, search), aligned with NopStation Cart API routes.
/// </summary>
[ApiController]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
[Route("public-api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogModelFactory _catalogModelFactory;
    private readonly ICategoryService _categoryService;
    private readonly IManufacturerService _manufacturerService;
    private readonly IProductTagService _productTagService;
    private readonly IProductService _productService;
    private readonly IProductModelFactory _productModelFactory;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly CatalogSettings _catalogSettings;
    private readonly MediaSettings _mediaSettings;

    public CatalogController(
        ICatalogModelFactory catalogModelFactory,
        ICategoryService categoryService,
        IManufacturerService manufacturerService,
        IProductTagService productTagService,
        IProductService productService,
        IProductModelFactory productModelFactory,
        IStoreContext storeContext,
        IWorkContext workContext,
        CatalogSettings catalogSettings,
        MediaSettings mediaSettings)
    {
        _catalogModelFactory = catalogModelFactory;
        _categoryService = categoryService;
        _manufacturerService = manufacturerService;
        _productTagService = productTagService;
        _productService = productService;
        _productModelFactory = productModelFactory;
        _storeContext = storeContext;
        _workContext = workContext;
        _catalogSettings = catalogSettings;
        _mediaSettings = mediaSettings;
    }

    /// <summary>
    /// GET /catalog/category/{id}
    /// Get products by category.
    /// </summary>
    [HttpGet("category/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategory(int id, [FromQuery] CatalogProductsCommand command)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null || category.Deleted || !category.Published)
            return NotFound(new { Message = "Category not found" });

        var model = await _catalogModelFactory.PrepareCategoryModelAsync(category, command);

        return Ok(new ApiResponse<CategoryModel> { Data = model });
    }

    /// <summary>
    /// GET /catalog/manufacturer/{id}
    /// Get products by manufacturer.
    /// </summary>
    [HttpGet("manufacturer/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ManufacturerModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManufacturer(int id, [FromQuery] CatalogProductsCommand command)
    {
        var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(id);
        if (manufacturer == null || manufacturer.Deleted || !manufacturer.Published)
            return NotFound(new { Message = "Manufacturer not found" });

        var model = await _catalogModelFactory.PrepareManufacturerModelAsync(manufacturer, command);

        return Ok(new ApiResponse<ManufacturerModel> { Data = model });
    }

    /// <summary>
    /// GET /catalog/manufacturer/all
    /// Get all manufacturers.
    /// </summary>
    [HttpGet("manufacturer/all")]
    [ProducesResponseType(typeof(ApiResponse<IList<ManufacturerModel>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllManufacturers()
    {
        var models = await _catalogModelFactory.PrepareManufacturerAllModelsAsync();
        return Ok(new ApiResponse<IList<ManufacturerModel>> { Data = models });
    }

    /// <summary>
    /// GET /catalog/producttag/{id}
    /// Get products by product tag.
    /// </summary>
    [HttpGet("producttag/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductsByTagModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductTag(int id, [FromQuery] CatalogProductsCommand command)
    {
        var productTag = await _productTagService.GetProductTagByIdAsync(id);
        if (productTag == null)
            return NotFound(new { Message = "Product tag not found" });

        var model = await _catalogModelFactory.PrepareProductsByTagModelAsync(productTag, command);

        return Ok(new ApiResponse<ProductsByTagModel> { Data = model });
    }

    /// <summary>
    /// GET /catalog/producttag/all
    /// Get all product tags.
    /// </summary>
    [HttpGet("producttag/all")]
    [ProducesResponseType(typeof(ApiResponse<PopularProductTagsModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProductTags()
    {
        var model = await _catalogModelFactory.PreparePopularProductTagsModelAsync(0);
        return Ok(new ApiResponse<PopularProductTagsModel> { Data = model });
    }

    /// <summary>
    /// GET /catalog/tag/productsbytag/{id}
    /// Get products by tag (alternative route).
    /// </summary>
    [HttpGet("tag/productsbytag/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductsByTagModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductsByTag(int id, [FromQuery] CatalogProductsCommand command)
    {
        return await GetProductTag(id, command);
    }

    /// <summary>
    /// GET /catalog/search
    /// Search products.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<SearchModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] CatalogProductsCommand command)
    {
        var searchModel = new SearchModel { q = q };
        var model = await _catalogModelFactory.PrepareSearchModelAsync(searchModel, command);

        return Ok(new ApiResponse<SearchModel> { Data = model });
    }

    /// <summary>
    /// GET /catalog/catalog/searchtermautocomplete
    /// Get autocomplete suggestions based on search query.
    /// </summary>
    [HttpGet("catalog/searchtermautocomplete")]
    [ProducesResponseType(typeof(ApiResponse<IList<SearchTermAutoCompleteDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTermAutoComplete([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return Ok(new ApiResponse<IList<SearchTermAutoCompleteDto>> { Data = new List<SearchTermAutoCompleteDto>() });

        term = term.Trim();

        if (term.Length < _catalogSettings.ProductSearchTermMinimumLength)
            return Ok(new ApiResponse<IList<SearchTermAutoCompleteDto>> { Data = new List<SearchTermAutoCompleteDto>() });

        var productNumber = _catalogSettings.ProductSearchAutoCompleteNumberOfProducts > 0
            ? _catalogSettings.ProductSearchAutoCompleteNumberOfProducts
            : 10;

        var store = await _storeContext.GetCurrentStoreAsync();
        var products = await _productService.SearchProductsAsync(0,
            storeId: store.Id,
            keywords: term,
            languageId: (await _workContext.GetWorkingLanguageAsync()).Id,
            visibleIndividuallyOnly: true,
            pageSize: productNumber);

        var showLinkToResultSearch = _catalogSettings.ShowLinkToAllResultInSearchAutoComplete && (products.TotalCount > productNumber);

        var models = (await _productModelFactory.PrepareProductOverviewModelsAsync(products, false, 
            _catalogSettings.ShowProductImagesInSearchAutoComplete, 
            _mediaSettings.AutoCompleteSearchThumbPictureSize)).ToList();

        var result = models.Select(p => new SearchTermAutoCompleteDto
        {
            Label = p.Name,
            ProductId = p.Id,
            ProductPictureUrl = p.PictureModels.FirstOrDefault()?.ImageUrl,
            ShowLinkToResultSearch = showLinkToResultSearch
        }).ToList();

        return Ok(new ApiResponse<IList<SearchTermAutoCompleteDto>> { Data = result });
    }
}

