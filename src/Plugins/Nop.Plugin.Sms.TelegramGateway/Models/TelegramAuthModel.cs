namespace Nop.Plugin.Sms.TelegramGateway.Models;

public class TelegramAuthModel
{
    public string TelegramUserId { get; set; } = default!;
    public string Username { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string AvatarUrl { get; set; }
    public string Hash { get; set; } = default!;

    // Unix timestamp (seconds) when the authorization was completed on Telegram side
    public long AuthDate { get; set; }

    // Bot token used to validate login data integrity. It's optional if bot token is configured server-side.
    public string BotToken { get; set; } = default!;
}
