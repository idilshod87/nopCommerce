# Copilot Instructions for nopCommerce

## Architecture Overview

- **Solution Structure**: The main solution file is `NopCommerce.sln`. Core logic is in `Libraries/`, plugins in `Plugins/`, web UI in `Presentation/`, and tests in `Tests/`.
- **Plugins**: Each plugin is isolated in its own folder under `Plugins/`. Plugins extend nopCommerce via custom logic, UI, and integration points. Example: `Nop.Plugin.Misc.Metrx` customizes admin features, while `Nop.Plugin.Misc.TelegramNotifications` integrates Telegram notifications.
- **Data Flow**: Core entities and helpers are in `Nop.Core/`. Services in `Nop.Services/` handle business logic. Plugins interact with these via dependency injection and event hooks.
- **Plugin System**: Plugins are loaded via `ApplicationPartManagerExtensions.InitializePlugins`. Each plugin folder is self-contained and may register dependencies via `INopStartup`.
- **Admin UI**: Customizations use ViewComponents (see `VendorDeliveryDateAdminViewComponent.cs` in `Nop.Plugin.Misc.Metrx`).
- **Integration**: External services (e.g., Telegram, Facebook, Google) are integrated via dedicated plugins and event hooks.

## Developer Workflows

- **Build**: Use `dotnet build NopCommerce.sln` from the `src` folder. For plugin development, build the plugin project and restart the app.
- **Run/Debug**: Use `dotnet watch run --project NopCommerce.sln` for hot-reload. Debug via Visual Studio or VS Code launch configurations.
- **Publish**: Use `dotnet publish NopCommerce.sln` for deployment artifacts.
- **Testing**: Tests are in `Tests/Nop.Tests/`. Run with `dotnet test`.
- **Plugin Build**: When developing plugins, set all `.cshtml` and `web.config` files to `Content` and `Copy if newer`. Update the `.csproj` to use:
	```xml
	<Project Sdk="Microsoft.NET.Sdk">
		<PropertyGroup>
			<TargetFramework>net9.0</TargetFramework>
			<OutputPath>$(SolutionDir)\Presentation\Nop.Web\Plugins\PLUGIN_OUTPUT_DIRECTORY</OutputPath>
			<OutDir>$(OutputPath)</OutDir>
		</PropertyGroup>
		<!-- For NuGet packages, set CopyLocalLockFileAssemblies to true -->
	</Project>
	```
- **Plugin Output**: Replace `PLUGIN_OUTPUT_DIRECTORY` with your plugin's output folder. This ensures correct DLL placement and reference resolution.
- **ClearPluginAssemblies**: Some plugins use a post-build target to clean up unnecessary libraries (see `ClearPluginAssemblies.proj`).

## Project-Specific Conventions

- **Plugin Registration**: Plugins must be copied to `Plugins/` and installed via Admin Panel → Configuration → Local Plugins.
- **Plugin Dependencies**: Some plugins require others (e.g., `Nop.Plugin.Misc.TelegramNotifications` requires `Nop.Plugin.ExternalAuth.Telegram`).
- **Plugin Build Actions**: All views (`.cshtml`) and `web.config` files in plugins should have `Build action` set to `Content` and `Copy to output directory` set to `Copy if newer`.
- **Plugin OutputPath**: Always set the plugin's output path to `Presentation/Nop.Web/Plugins/PLUGIN_OUTPUT_DIRECTORY` in the `.csproj`.
- **UI Customization**: Admin UI customizations use ViewComponents (see `VendorDeliveryDateAdminViewComponent.cs` in `Nop.Plugin.Misc.Metrx`).
- **Data Persistence**: Plugins may create their own tables (e.g., `TelegramAuthSession`).
- **Logging**: Application logs are in `App_Data/Logs`.
- **Plugin Metadata**: Each plugin should have a `plugin.json` for metadata.

## Integration Points

- **External Auth**: Plugins like `Nop.Plugin.ExternalAuth.Telegram` enable third-party authentication.
- **Notifications**: Plugins can send notifications via external services (e.g., Telegram bots).
- **Widget Zones**: Plugins can inject UI into specific zones using `AdminWidgetZones`.
- **Event Hooks**: Plugins may subscribe to events for cross-component communication (see `Services/` in plugins).
- **Custom Formatters**: Some plugins (e.g., Telegram) register custom JSON formatters for specific API routes via `INopStartup`.

## Key Files & Directories
- `NopCommerce.sln` – Solution entry point
- `Libraries/Nop.Core/` – Core entities, helpers
- `Libraries/Nop.Services/` – Business logic
- `Plugins/` – All plugin code
- `Presentation/Nop.Web/` – Main web application
- `Tests/Nop.Tests/` – Unit tests

- `Build/ClearPluginAssemblies.proj` – Used by some plugins for post-build cleanup
- `plugin.json` – Plugin metadata

## Examples
- **Adding a Delivery Date to Vendor**: See `VendorDeliveryDateAdminViewComponent.cs` in `Nop.Plugin.Misc.Metrx`.
- **Telegram Notification Flow**: See `README.md` in `Nop.Plugin.Misc.TelegramNotifications` for required plugin dependencies and setup steps.

- **Plugin Project Structure**: See `README.md` in `Nop.Plugin.Misc.Metrx` for a typical plugin layout (Components, Infrastructure, Services, Views, etc).

---
For more details, review plugin-specific `README.md` files and core service implementations. If conventions or workflows are unclear, ask for clarification or examples from maintainers.

---
For more details, review plugin-specific `README.md` files and core service implementations. If conventions or workflows are unclear, ask for clarification or examples from maintainers.
