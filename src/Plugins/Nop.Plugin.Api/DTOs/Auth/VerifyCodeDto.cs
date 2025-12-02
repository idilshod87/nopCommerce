namespace Nop.Plugin.Api.DTOs.Auth;

public class VerifyCodeDto
{
    public string RequestId { get; set; } = default!;
    public string Code { get; set; } = default!;
}