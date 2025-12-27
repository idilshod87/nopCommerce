using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Misc.WebApi.Frontend.Factories;

/// <summary>
/// Represents the interface of the API catalog model factory
/// </summary>
public interface IApiCatalogModelFactory
{
    /// <summary>
    /// Prepare the search model with support for multiple categories, manufacturers, and vendors
    /// </summary>
    /// <param name="searchModel">Search model</param>
    /// <param name="command">Catalog products command</param>
    /// <param name="categoryIds">Category IDs to search in (supports multiple)</param>
    /// <param name="manufacturerIds">Manufacturer IDs to filter by (supports multiple)</param>
    /// <param name="vendorIds">Vendor IDs to filter by (supports multiple)</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the search model
    /// </returns>
    Task<SearchModel> PrepareSearchModelAsync(
        SearchModel searchModel,
        CatalogProductsCommand command,
        IList<int> categoryIds = null,
        IList<int> manufacturerIds = null,
        IList<int> vendorIds = null);
}
