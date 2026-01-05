using Microsoft.AspNetCore.Mvc.Razor;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.Metrx.Infrastructure;

/// <summary>
/// Adds plugin-specific view locations so nopCommerce can resolve overrides from the Metrx plugin.
/// </summary>
public class MetrxViewLocationExpander : IViewLocationExpander
{
    private static readonly string[] AreaViewLocations =
    {
        $"/Plugins/{MetrxDefaults.SystemName}/Views/Areas/{{2}}/{{1}}/{{0}}.cshtml",
        $"/Plugins/{MetrxDefaults.SystemName}/Views/Areas/{{2}}/Shared/{{0}}.cshtml"
    };

    private static readonly string[] DefaultViewLocations =
    {
        $"/Plugins/{MetrxDefaults.SystemName}/Views/{{1}}/{{0}}.cshtml",
        $"/Plugins/{MetrxDefaults.SystemName}/Views/Shared/{{0}}.cshtml"
    };

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        // no-op
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        var pluginLocations = Enumerable.Empty<string>();

        if (!string.IsNullOrEmpty(context.AreaName))
            pluginLocations = pluginLocations.Concat(AreaViewLocations);

        pluginLocations = pluginLocations.Concat(DefaultViewLocations);

        return pluginLocations.Concat(viewLocations);
    }
}
