using Nop.Services.Events;
using Nop.Web.Framework.Events;

namespace Nop.Plugin.Misc.Metrx.Services;

/// <summary>
/// Removes the default "Help" menu node from the admin navigation.
/// </summary>
public class HelpMenuOverrideConsumer : IConsumer<AdminMenuCreatedEvent>
{
    public Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
    {
        if (eventMessage?.RootMenuItem?.ChildNodes == null)
            return Task.CompletedTask;

        var helpNode = eventMessage.RootMenuItem.ChildNodes
            .FirstOrDefault(node => string.Equals(node.SystemName, "Help", StringComparison.InvariantCultureIgnoreCase));

        if (helpNode != null)
            eventMessage.RootMenuItem.ChildNodes.Remove(helpNode);

        return Task.CompletedTask;
    }
}
