using FluentMigrator;

using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Metrx.Core.Domain;

namespace Nop.Plugin.Misc.WebApi.Frontend.Data;

[NopMigration("2020/01/01 00:00:00", "WebApi.Frontend base schema", MigrationProcessType.Installation)]
public class RefreshTokenSchemaMigration : AutoReversingMigration
{
    #region Methods

    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        Create.TableFor<RefreshToken>();

        Create.Index("IX_RefreshToken_Token")
                .OnTable(nameof(RefreshToken))
                .OnColumn(nameof(RefreshToken.Token)).Ascending()
                .WithOptions().Unique();

        Create.Index("IX_RefreshToken_CustomerId")
                .OnTable(nameof(RefreshToken))
                .OnColumn(nameof(RefreshToken.CustomerId)).Ascending();

        Create.Index("IX_RefreshToken_ExpiresAtUtc")
                .OnTable(nameof(RefreshToken))
                .OnColumn(nameof(RefreshToken.ExpiresAtUtc)).Ascending();
    }

    #endregion
}
