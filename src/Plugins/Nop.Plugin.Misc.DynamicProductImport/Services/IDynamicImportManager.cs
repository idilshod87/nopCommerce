using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Nop.Plugin.Misc.DynamicProductImport.Models;

namespace Nop.Plugin.Misc.DynamicProductImport.Services;

public interface IDynamicImportManager
{
    Task ImportProductsFromXlsxAsync(Stream stream, IEnumerable<ImportProductMapping> mappings, int? vendorId = null);
}

