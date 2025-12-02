using Nop.Plugin.ExternalAuth.Telegram.Domain;

namespace Nop.Plugin.ExternalAuth.Telegram.Services;

public interface ITelegramGatewayApiService
{
    Task<TelegramGatewayResult<SendVerificationResult>> SendVerificationCodeAsync(string phoneNumber, int codeLength = 4);
    Task<TelegramGatewayResult<CheckVerificationResult>> CheckVerificationStatusAsync(string requestId, string code);
}
