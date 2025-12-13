using Nop.Web.Framework.Models;
using Nop.Web.Models.Media;

namespace Nop.Web.Models.Catalog;

public partial record VendorNavigationModel : BaseNopModel
{
    public VendorNavigationModel()
    {
        Vendors = new List<VendorBriefInfoModel>();
    }

    public IList<VendorBriefInfoModel> Vendors { get; set; }

    public int TotalVendors { get; set; }
}

public partial record VendorBriefInfoModel : BaseNopEntityModel
{
    public VendorBriefInfoModel()
    {
        PictureModel = new PictureModel();
    }

    public string Name { get; set; }

    public string SeName { get; set; }

    public PictureModel PictureModel { get; set; }
}