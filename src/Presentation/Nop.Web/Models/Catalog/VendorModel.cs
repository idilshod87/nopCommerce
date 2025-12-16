using Nop.Web.Framework.Models;
using Nop.Web.Framework.Models.ArtificialIntelligence;
using Nop.Web.Models.Media;

namespace Nop.Web.Models.Catalog;

public partial record VendorModel : BaseNopEntityModel, IMetaTagsSupportedModel
{
    public VendorModel()
    {
        PictureModel = new PictureModel();
        CatalogProductsModel = new CatalogProductsModel();
        ProductReviews = new VendorProductReviewsListModel();
        ContactInfo = new VendorContactInfoModel();
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public string MetaKeywords { get; set; }
    public string MetaDescription { get; set; }
    public string MetaTitle { get; set; }
    public string SeName { get; set; }
    public bool AllowCustomersToContactVendors { get; set; }
    public int? PmCustomerId { get; set; }

    public PictureModel PictureModel { get; set; }

    public CatalogProductsModel CatalogProductsModel { get; set; }

    public VendorProductReviewsListModel ProductReviews { get; set; }

    public VendorContactInfoModel ContactInfo { get; set; }

    public partial record VendorContactInfoModel : BaseNopModel
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FaxNumber { get; set; }
        public string AddressLine { get; set; }
        public string Country { get; set; }
        public string StateProvince { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string ZipPostalCode { get; set; }
    }
}