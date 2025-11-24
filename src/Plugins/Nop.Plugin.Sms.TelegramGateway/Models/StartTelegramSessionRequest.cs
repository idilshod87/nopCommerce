namespace Nop.Plugin.Sms.TelegramGateway.Models;

public class StartTelegramSessionRequest
{
    public string ClientHint { get; set; }
}

public class VerifyTelegramSessionRequest
{
    public string SessionToken { get; set; } = default!;
    public string Code { get; set; } = default!;
}

