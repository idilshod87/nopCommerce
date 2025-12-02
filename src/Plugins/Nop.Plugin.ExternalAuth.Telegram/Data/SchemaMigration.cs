using FluentMigrator;
using Nop.Data.Migrations;
using Nop.Data.Extensions;
using Nop.Plugin.ExternalAuth.Telegram.Domain.Authentication;

namespace Nop.Plugin.ExternalAuth.Telegram.Data;

[NopMigration("2024-11-20 12:00:00", "Sms.TelegramGateway base schema", MigrationProcessType.Installation)]
public class SchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.TableFor<TelegramAuthSession>();

        Create.Index("IX_TelegramAuthSession_SessionToken")
            .OnTable(nameof(TelegramAuthSession))
            .OnColumn(nameof(TelegramAuthSession.SessionToken)).Ascending()
            .WithOptions().Unique();

        Create.Index("IX_TelegramAuthSession_Status")
            .OnTable(nameof(TelegramAuthSession))
            .OnColumn(nameof(TelegramAuthSession.StatusId)).Ascending();

        Create.Index("IX_TelegramAuthSession_TelegramUser")
            .OnTable(nameof(TelegramAuthSession))
            .OnColumn(nameof(TelegramAuthSession.TelegramUserId)).Ascending();
    }
}

