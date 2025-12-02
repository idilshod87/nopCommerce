using System.Data;

using FluentMigrator.Builders.Create.Table;

using Nop.Core.Domain.Customers;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Api.Domain;

namespace Nop.Plugin.Api.Data;

internal class CustomerLoginBuilder : NopEntityBuilder<CustomerLogin>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(CustomerLogin.Id)).AsInt32().PrimaryKey().Identity()
            .WithColumn(nameof(CustomerLogin.LoginProvider)).AsString(100).NotNullable()
            .WithColumn(nameof(CustomerLogin.ProviderKey)).AsString(100).NotNullable()
            .WithColumn(nameof(CustomerLogin.ProviderDisplayName)).AsString(200).Nullable()
            .WithColumn(nameof(CustomerLogin.CustomerId)).AsInt32().ForeignKey<Customer>(onDelete: Rule.Cascade).NotNullable();
    }
}