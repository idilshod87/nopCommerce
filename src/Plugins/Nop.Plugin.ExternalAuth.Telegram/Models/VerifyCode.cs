namespace Nop.Plugin.ExternalAuth.Telegram.Models;

public class VerifyCode
{
    public string RequestId { get; set; } = default!;
    public string Code { get; set; } = default!;
}