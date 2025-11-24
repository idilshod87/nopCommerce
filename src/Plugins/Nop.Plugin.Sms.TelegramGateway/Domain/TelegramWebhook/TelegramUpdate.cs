using System.Text.Json.Serialization;

namespace Nop.Plugin.Sms.TelegramGateway.Domain.TelegramWebhook;

public class TelegramUpdate
{
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    [JsonPropertyName("message")]
    public TelegramMessage Message { get; set; }
}

public class TelegramMessage
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("chat")]
    public TelegramChat Chat { get; set; }

    [JsonPropertyName("from")]
    public TelegramUser From { get; set; }

    [JsonPropertyName("contact")]
    public TelegramContact Contact { get; set; }
}

public class TelegramChat
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
}

public class TelegramUser
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("is_bot")]
    public bool IsBot { get; set; }

    [JsonPropertyName("first_name")]
    public string FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string LastName { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; }
}

public class TelegramContact
{
    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; }

    [JsonPropertyName("first_name")]
    public string FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string LastName { get; set; }

    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }
}

