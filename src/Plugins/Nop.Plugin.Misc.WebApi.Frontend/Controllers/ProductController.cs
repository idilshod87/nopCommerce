using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.WebApi.Frontend.DTOs;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Seo;
using Nop.Services.Catalog;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Simplified public API for product details, compatible by route with NopStation Cart API.
/// </summary>
[ApiController]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
[Route("public-api/product")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IPictureService _pictureService;
    private readonly IUrlRecordService _urlRecordService;
    private readonly IProductModelFactory _productModelFactory;
    private readonly IOrderReportService _orderReportService;
    private readonly IStoreContext _storeContext;
    private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
    private readonly IWorkContext _workContext;
    private readonly IProductReviewService _productReviewService;
    private readonly ICustomerService _customerService;
    private readonly CatalogSettings _catalogSettings;
    private readonly ILocalizationService _localizationService;

    public ProductController(
        IProductService productService,
        IPictureService pictureService,
        IUrlRecordService urlRecordService,
        IProductModelFactory productModelFactory,
        IOrderReportService orderReportService,
        IStoreContext storeContext,
        IShoppingCartModelFactory shoppingCartModelFactory,
        IWorkContext workContext,
        IProductReviewService productReviewService,
        ICustomerService customerService,
        CatalogSettings catalogSettings,
        ILocalizationService localizationService)
    {
        _productService = productService;
        _pictureService = pictureService;
        _urlRecordService = urlRecordService;
        _productModelFactory = productModelFactory;
        _orderReportService = orderReportService;
        _storeContext = storeContext;
        _shoppingCartModelFactory = shoppingCartModelFactory;
        _workContext = workContext;
        _productReviewService = productReviewService;
        _customerService = customerService;
        _catalogSettings = catalogSettings;
        _localizationService = localizationService;
    }

    public class EstimateShippingRequest
    {
        public int ProductId { get; set; }
        public int? CountryId { get; set; }
        public int? StateProvinceId { get; set; }
        public string? ZipPostalCode { get; set; }
        public string? City { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class AddProductReviewRequest
    {
        public string? Title { get; set; }
        public string? ReviewText { get; set; }
        public int Rating { get; set; }
    }

    /// <summary>
    /// GET /product/productdetails/{id}
    /// Returns basic product information for product details page (simplified DTO).
    /// </summary>
    [HttpGet("productdetails/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailsModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductDetails(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { Message = "Product not found" });

        // Use standard nopCommerce ProductDetailsModel so JSON shape is rich and consistent
        var model = await _productModelFactory.PrepareProductDetailsModelAsync(product);

        return Ok(new ApiResponse<ProductDetailsModel> { Data = model });
    }

    /// <summary>
    /// GET /product/relatedproducts/{id}
    /// Returns list of related products (overview models) for given product.
    /// </summary>
    [HttpGet("relatedproducts/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductOverviewModel>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRelatedProducts(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { Message = "Product not found" });

        var related = await _productService.GetRelatedProductsByProductId1Async(product.Id);
        if (!related.Any())
            return Ok(new { Data = Array.Empty<object>() });

        var relatedProducts = await _productService.GetProductsByIdsAsync(related.Select(rp => rp.ProductId2).ToArray());
        var overviewModels = await _productModelFactory.PrepareProductOverviewModelsAsync(relatedProducts);

        return Ok(new ApiResponse<IEnumerable<ProductOverviewModel>> { Data = overviewModels });
    }

    /// <summary>
    /// GET /product/productsalsopurchased/{id}
    /// Returns products that customers also purchased with the given product.
    /// </summary>
    [HttpGet("productsalsopurchased/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductOverviewModel>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductsAlsoPurchased(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { Message = "Product not found" });

        var store = await _storeContext.GetCurrentStoreAsync();

        var productIds = await _orderReportService.GetAlsoPurchasedProductsIdsAsync(store.Id, product.Id);
        if (productIds == null || productIds.Length == 0)
            return Ok(new { Data = Array.Empty<object>() });

        var products = await _productService.GetProductsByIdsAsync(productIds);
        var overviewModels = await _productModelFactory.PrepareProductOverviewModelsAsync(products);

        return Ok(new ApiResponse<IEnumerable<ProductOverviewModel>> { Data = overviewModels });
    }

    /// <summary>
    /// POST /product/estimateshipping
    /// Uses same logic as nopCommerce estimate shipping, but for a single product and quantity.
    /// </summary>
    [HttpPost("estimateshipping")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<EstimateShippingResultModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProductEstimateShipping([FromBody] EstimateShippingRequest request)
    {
        if (request == null || request.ProductId <= 0)
            return BadRequest(new { Message = "Invalid request" });

        var product = await _productService.GetProductByIdAsync(request.ProductId);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { Message = "Product not found" });

        if (request.Quantity <= 0)
            request.Quantity = 1;

        var store = await _storeContext.GetCurrentStoreAsync();

        // wrap product in a temporary cart item (same approach as in ProductModelFactory)
        var wrappedItem = new ShoppingCartItem
        {
            StoreId = store.Id,
            ShoppingCartTypeId = (int)ShoppingCartType.ShoppingCart,
            CustomerId = 0,
            ProductId = product.Id,
            Quantity = request.Quantity,
            CreatedOnUtc = DateTime.UtcNow
        };

        var estimateModel = new EstimateShippingModel
        {
            CountryId = request.CountryId,
            StateProvinceId = request.StateProvinceId,
            ZipPostalCode = request.ZipPostalCode,
            City = request.City
        };

        var result = await _shoppingCartModelFactory.PrepareEstimateShippingResultModelAsync(
            new[] { wrappedItem }, estimateModel, true);

        return Ok(new ApiResponse<EstimateShippingResultModel> { Data = result });
    }

    /// <summary>
    /// GET /product/productreviews
    /// Returns all product reviews of current customer (CustomerProductReviewsModel).
    /// </summary>
    [HttpGet("productreviews")]
    [ProducesResponseType(typeof(ApiResponse<CustomerProductReviewsModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCustomerProductReviews([FromQuery] int? pageNumber = null)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return Unauthorized();

        var model = await _productModelFactory.PrepareCustomerProductReviewsModelAsync(pageNumber);
        return Ok(new ApiResponse<CustomerProductReviewsModel> { Data = model });
    }

    /// <summary>
    /// GET /product/productreviews/{id}
    /// Returns reviews for specific product (ProductReviewsModel).
    /// </summary>
    [HttpGet("productreviews/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewsModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductReviews(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { Message = "Product not found" });

        var model = await _productModelFactory.PrepareProductReviewsModelAsync(product);
        return Ok(new ApiResponse<ProductReviewsModel> { Data = model });
    }

    /// <summary>
    /// POST /product/productreviewsadd/{id}
    /// Adds a new review for specific product.
    /// </summary>
    [HttpPost("productreviewsadd/{id:int}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewsModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddProductReview(int id, [FromBody] AddProductReviewRequest request)
    {
        var product = await _productService.GetProductByIdAsync(id);
        var currentStore = await _storeContext.GetCurrentStoreAsync();

        if (product == null || product.Deleted || !product.Published || !product.AllowCustomerReviews ||
            !await _productReviewService.CanAddReviewAsync(product.Id, _catalogSettings.ShowProductReviewsPerStore ? currentStore.Id : 0))
            return NotFound(new { Message = "Product not found or reviews disabled" });

        // validate basic fields
        if (request == null || string.IsNullOrWhiteSpace(request.ReviewText))
            return BadRequest(new { Message = "ReviewText is required" });

        foreach (var error in await _productReviewService.ValidateProductReviewAvailabilityAsync(product))
        {
            return BadRequest(new { Message = error });
        }

        var rating = request.Rating;
        if (rating is < 1 or > 5)
            rating = _catalogSettings.DefaultProductRatingValue;

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isApproved = !_catalogSettings.ProductReviewsMustBeApproved;

        var productReview = new ProductReview
        {
            ProductId = product.Id,
            CustomerId = customer.Id,
            Title = request.Title,
            ReviewText = request.ReviewText,
            Rating = rating,
            HelpfulYesTotal = 0,
            HelpfulNoTotal = 0,
            IsApproved = isApproved,
            CreatedOnUtc = DateTime.UtcNow,
            StoreId = currentStore.Id,
        };

        await _productReviewService.InsertProductReviewAsync(productReview, new List<ProductReviewReviewTypeMapping>());

        var model = await _productModelFactory.PrepareProductReviewsModelAsync(product);
        return Ok(new ApiResponse<ProductReviewsModel> { Data = model });
    }

    /// <summary>
    /// POST /product/setproductreviewhelpfulness/{reviewId}
    /// Vote for product review helpfulness.
    /// </summary>
    [HttpPost("setproductreviewhelpfulness/{reviewId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewHelpfulnessModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetProductReviewHelpfulness(int reviewId, [FromQuery] bool wasHelpful)
    {
        var productReview = await _productReviewService.GetProductReviewByIdAsync(reviewId);
        if (productReview == null)
            return BadRequest(new { Message = "Review not found" });

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer) && !_catalogSettings.AllowAnonymousUsersToReviewProduct)
        {
            return BadRequest(new
            {
                Message = await _localizationService.GetResourceAsync("Reviews.Helpfulness.OnlyRegistered"),
                TotalYes = productReview.HelpfulYesTotal,
                TotalNo = productReview.HelpfulNoTotal
            });
        }

        if (productReview.CustomerId == customer.Id)
        {
            return BadRequest(new
            {
                Message = await _localizationService.GetResourceAsync("Reviews.Helpfulness.YourOwnReview"),
                TotalYes = productReview.HelpfulYesTotal,
                TotalNo = productReview.HelpfulNoTotal
            });
        }

        await _productReviewService.SetProductReviewHelpfulnessAsync(productReview, wasHelpful);
        await _productReviewService.UpdateProductReviewHelpfulnessTotalsAsync(productReview);

        var model = new ProductReviewHelpfulnessModel
        {
            ProductReviewId = productReview.Id,
            HelpfulYesTotal = productReview.HelpfulYesTotal,
            HelpfulNoTotal = productReview.HelpfulNoTotal
        };

        return Ok(new ApiResponse<ProductReviewHelpfulnessModel> { Data = model });
    }
}


