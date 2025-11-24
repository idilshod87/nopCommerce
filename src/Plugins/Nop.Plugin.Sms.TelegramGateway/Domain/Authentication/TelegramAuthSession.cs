using Nop.Core;

namespace Nop.Plugin.Sms.TelegramGateway.Domain.Authentication;

/// <summary>
/// Represents an authorization attempt performed through Telegram bot.
/// </summary>
public class TelegramAuthSession : BaseEntity
{
    /// <summary>
    /// Gets or sets the public token used by clients to reference this session.
    /// </summary>
    public Guid SessionToken { get; set; }

    /// <summary>
    /// Gets or sets the Telegram user identifier (user that owns the chat with the bot).
    /// </summary>
    public long? TelegramUserId { get; set; }

    /// <summary>
    /// Gets or sets the Telegram chat identifier.
    /// </summary>
    public long? TelegramChatId { get; set; }

    /// <summary>
    /// Gets or sets the Telegram username (without @).
    /// </summary>
    public string TelegramUsername { get; set; }

    /// <summary>
    /// Gets or sets the phone number shared by the user in Telegram (E.164).
    /// </summary>
    public string PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the first name from Telegram contact.
    /// </summary>
    public string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name from Telegram contact.
    /// </summary>
    public string LastName { get; set; }

    /// <summary>
    /// Gets or sets the verification code issued for this session.
    /// </summary>
    public string VerificationCode { get; set; }

    /// <summary>
    /// Gets or sets the expiration timestamp for the verification code.
    /// </summary>
    public DateTime? CodeExpiresOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the customer that has been created or linked to this session.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the current status identifier.
    /// </summary>
    public int StatusId { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime UpdatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the verification timestamp.
    /// </summary>
    public DateTime? VerifiedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the last start payload received from Telegram (/start payload).
    /// </summary>
    public string StartPayload { get; set; }

    /// <summary>
    /// Gets or sets the device or client hint provided when the session was created.
    /// </summary>
    public string ClientHint { get; set; }

    public TelegramAuthStatus Status
    {
        get => (TelegramAuthStatus)StatusId;
        set => StatusId = (int)value;
    }
}

public enum TelegramAuthStatus
{
    PendingStart = 10,
    AwaitingContact = 20,
    ContactReceived = 30,
    CodeIssued = 40,
    Verified = 50,
    Expired = 60
}

