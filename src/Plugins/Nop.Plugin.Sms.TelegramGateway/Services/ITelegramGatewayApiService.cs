using Nop.Plugin.Sms.TelegramGateway.Domain;

namespace Nop.Plugin.Sms.TelegramGateway.Services;

public interface ITelegramGatewayApiService
{
    Task<TelegramGatewayResult<SendVerificationResult>> SendVerificationCodeAsync(string phoneNumber, int codeLength = 4);
    Task<TelegramGatewayResult<CheckVerificationResult>> CheckVerificationStatusAsync(string requestId, string code);
}
