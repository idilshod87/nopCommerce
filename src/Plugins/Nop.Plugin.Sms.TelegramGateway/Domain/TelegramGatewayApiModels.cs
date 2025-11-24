namespace Nop.Plugin.Sms.TelegramGateway.Domain;

using System.Text.Json.Serialization;

/// <summary>
/// Универсальный ответ от Telegram Gateway API.
/// </summary>
public class TelegramGatewayResponse<T>
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("result")]
    public T? Result { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; }
}

/// <summary>
/// Ответ на /sendVerificationMessage
/// </summary>
public class SendVerificationResult
{
    [JsonPropertyName("request_id")]
    public string RequestId { get; set; }

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; }

    [JsonPropertyName("request_cost")]
    public decimal RequestCost { get; set; }

    [JsonPropertyName("remaining_balance")]
    public decimal RemainingBalance { get; set; }

    [JsonPropertyName("delivery_status")]
    public DeliveryStatus DeliveryStatus { get; set; }
}

/// <summary>
/// Ответ на /checkVerificationStatus
/// </summary>
public class CheckVerificationResult
{
    [JsonPropertyName("request_id")]
    public string RequestId { get; set; }

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; }

    [JsonPropertyName("request_cost")]
    public decimal RequestCost { get; set; }

    [JsonPropertyName("delivery_status")]
    public DeliveryStatus DeliveryStatus { get; set; }

    [JsonPropertyName("verification_status")]
    public VerificationStatus VerificationStatus { get; set; }
}

/// <summary>
/// Детали статуса доставки сообщения.
/// </summary>
public class DeliveryStatus
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("updated_at")]
    public long UpdatedAt { get; set; }
}

/// <summary>
/// Детали проверки кода.
/// </summary>
public class VerificationStatus
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("updated_at")]
    public long UpdatedAt { get; set; }

    [JsonPropertyName("code_entered")]
    public string CodeEntered { get; set; }
}

public enum TelegramGatewayResultCode
{
    Success,
    CodeInvalid,
    MaxAttemptsExceeded,
    CodeExpired,
    RequestIdInvalid,
    RateLimited,
    BalanceNotEnough,
    Unauthorized,
    NetworkError,
    UnknownError
}

public class TelegramGatewayResult<T>
{
    public TelegramGatewayResultCode Code { get; set; }
    public string ErrorMessage { get; set; }
    public T? Data { get; set; }

    public bool IsSuccess => Code == TelegramGatewayResultCode.Success;
}
