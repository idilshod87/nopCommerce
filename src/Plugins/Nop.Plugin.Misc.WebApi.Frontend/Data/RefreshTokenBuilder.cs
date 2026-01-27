using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Metrx.Core.Domain;

namespace Nop.Plugin.Misc.WebApi.Frontend.Data;

public class RefreshTokenBuilder : NopEntityBuilder<RefreshToken>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(RefreshToken.Token)).AsString(256).NotNullable()
            .WithColumn(nameof(RefreshToken.CustomerId)).AsInt32().NotNullable()
            .WithColumn(nameof(RefreshToken.ExpiresAtUtc)).AsDateTime2().NotNullable()
            .WithColumn(nameof(RefreshToken.CreatedAtUtc)).AsDateTime2().NotNullable()
            .WithColumn(nameof(RefreshToken.CreatedByIp)).AsString(45).Nullable()
            .WithColumn(nameof(RefreshToken.IsRevoked)).AsBoolean().NotNullable()
            .WithColumn(nameof(RefreshToken.RevokedAtUtc)).AsDateTime2().Nullable();
    }
}
