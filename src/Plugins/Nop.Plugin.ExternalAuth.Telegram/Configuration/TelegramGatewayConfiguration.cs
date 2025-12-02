using Nop.Core.Configuration;

namespace Nop.Plugin.ExternalAuth.Telegram.Configuration;

/// <summary>
/// Represents Telegram Gateway settings
/// </summary>
public class TelegramGatewayConfiguration : ISettings
{
    public int AllowedClockSkewInMinutes { get; set; } = 5;
    public string SecurityKey { get; set; } = "NowIsTheTimeForAllGoodMenToComeToTheAideOfTheirCountry";

    public string BotToken { get; set; }

    public string BotUsername { get; set; }

    public string WebhookSecretToken { get; set; } = "telegram-auth";

    public int SessionTtlMinutes { get; set; } = 10;

    public int VerificationCodeLength { get; set; } = 4;

    public int VerificationCodeTtlMinutes { get; set; } = 5;
}
