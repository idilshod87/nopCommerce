using Nop.Web.Framework.Models;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Misc.WebApi.Frontend.Models.Catalog;

/// <summary>
/// Represents the API search model with hierarchical categories
/// </summary>
public partial record ApiSearchModel : BaseNopModel
{
    public ApiSearchModel()
    {
        AvailableCategories = new List<CategoryModel>();
        AvailableManufacturers = new List<ManufacturerModel>();
        AvailableVendors = new List<VendorModel>();
        Vendors = new List<VendorBriefInfoModel>();
        CatalogProductsModel = new CatalogProductsModel();
    }

    /// <summary>
    /// Query string
    /// </summary>
    public string q { get; set; }

    /// <summary>
    /// Category ID
    /// </summary>
    public int cid { get; set; }

    /// <summary>
    /// Include subcategories
    /// </summary>
    public bool isc { get; set; }

    /// <summary>
    /// Manufacturer ID
    /// </summary>
    public int mid { get; set; }

    /// <summary>
    /// Vendor ID
    /// </summary>
    public int vid { get; set; }

    /// <summary>
    /// A value indicating whether to search in descriptions
    /// </summary>
    public bool sid { get; set; }

    /// <summary>
    /// A value indicating whether to search in product tags
    /// </summary>
    public bool sit { get; set; }

    /// <summary>
    /// A value indicating whether "advanced search" is enabled
    /// </summary>
    public bool advs { get; set; }

    /// <summary>
    /// A value indicating whether "allow search by vendor" is enabled
    /// </summary>
    public bool asv { get; set; }

    public CatalogProductsModel CatalogProductsModel { get; set; }

    /// <summary>
    /// Gets or sets the hierarchical categories
    /// </summary>
    public IList<CategoryModel> AvailableCategories { get; set; }

    /// <summary>
    /// Gets or sets available manufacturers
    /// </summary>
    public IList<ManufacturerModel> AvailableManufacturers { get; set; }

    /// <summary>
    /// Gets or sets available vendors
    /// </summary>
    public IList<VendorModel> AvailableVendors { get; set; }

    /// <summary>
    /// List of vendors found in search results
    /// </summary>
    public IList<VendorBriefInfoModel> Vendors { get; set; }

    #region Nested classes

    /// <summary>
    /// Represents a category model with hierarchical subcategories
    /// </summary>
    public partial record CategoryModel : BaseNopEntityModel
    {
        public CategoryModel()
        {
            SubCategories = new List<CategoryModel>();
        }

        /// <summary>
        /// Gets or sets the category name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the subcategories
        /// </summary>
        public IList<CategoryModel> SubCategories { get; set; }
    }

    /// <summary>
    /// Represents a manufacturer model
    /// </summary>
    public partial record ManufacturerModel : BaseNopEntityModel
    {
        /// <summary>
        /// Gets or sets the manufacturer name
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Represents a vendor model
    /// </summary>
    public partial record VendorModel : BaseNopEntityModel
    {
        /// <summary>
        /// Gets or sets the vendor name
        /// </summary>
        public string Name { get; set; }
    }

    #endregion
}
