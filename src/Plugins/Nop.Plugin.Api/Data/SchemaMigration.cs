using FluentMigrator;

using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Api.Domain;

namespace Nop.Plugin.Api.Data;

[NopMigration("2025-11-03 16:00:00", "TelegramGateway: Create CustomerLogin table", MigrationProcessType.Installation)]
public class SchemaMigration : AutoReversingMigration
{
    #region Methods

    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        Create.TableFor<CustomerLogin>();

        Create.Index("IX_CustomerLogin_LoginProvider_ProviderKey")
                .OnTable(nameof(CustomerLogin))
                .OnColumn(nameof(CustomerLogin.LoginProvider)).Ascending()
                .OnColumn(nameof(CustomerLogin.ProviderKey)).Ascending()
                .WithOptions().Unique();
    }

    #endregion
}