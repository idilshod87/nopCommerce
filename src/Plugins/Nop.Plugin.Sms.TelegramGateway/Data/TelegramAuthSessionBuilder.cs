using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Sms.TelegramGateway.Domain.Authentication;

namespace Nop.Plugin.Sms.TelegramGateway.Data;

public class TelegramAuthSessionBuilder : NopEntityBuilder<TelegramAuthSession>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(TelegramAuthSession.SessionToken)).AsGuid().NotNullable()
            .WithColumn(nameof(TelegramAuthSession.TelegramUserId)).AsInt64().Nullable()
            .WithColumn(nameof(TelegramAuthSession.TelegramChatId)).AsInt64().Nullable()
            .WithColumn(nameof(TelegramAuthSession.TelegramUsername)).AsString(256).Nullable()
            .WithColumn(nameof(TelegramAuthSession.PhoneNumber)).AsString(64).Nullable()
            .WithColumn(nameof(TelegramAuthSession.FirstName)).AsString(128).Nullable()
            .WithColumn(nameof(TelegramAuthSession.LastName)).AsString(128).Nullable()
            .WithColumn(nameof(TelegramAuthSession.VerificationCode)).AsString(64).Nullable()
            .WithColumn(nameof(TelegramAuthSession.CodeExpiresOnUtc)).AsDateTime2().Nullable()
            .WithColumn(nameof(TelegramAuthSession.CustomerId)).AsInt32().Nullable()
            .WithColumn(nameof(TelegramAuthSession.StatusId)).AsInt32().NotNullable()
            .WithColumn(nameof(TelegramAuthSession.CreatedOnUtc)).AsDateTime2().NotNullable()
            .WithColumn(nameof(TelegramAuthSession.UpdatedOnUtc)).AsDateTime2().NotNullable()
            .WithColumn(nameof(TelegramAuthSession.VerifiedOnUtc)).AsDateTime2().Nullable()
            .WithColumn(nameof(TelegramAuthSession.StartPayload)).AsString(128).Nullable()
            .WithColumn(nameof(TelegramAuthSession.ClientHint)).AsString(256).Nullable();
    }
}

