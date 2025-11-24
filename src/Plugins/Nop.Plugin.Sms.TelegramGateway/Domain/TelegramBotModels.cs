using System.Text.Json.Serialization;

namespace Nop.Plugin.Sms.TelegramGateway.Domain
{
    /// <summary>
    /// Minimal subset of Telegram Bot API's User object for the bot returned by getMe.
    /// </summary>
    public class BotInfo
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("is_bot")]
        public bool IsBot { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("language_code")]
        public string? LanguageCode { get; set; }
    }
}