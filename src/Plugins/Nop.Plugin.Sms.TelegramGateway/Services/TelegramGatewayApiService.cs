namespace Nop.Plugin.Sms.TelegramGateway.Services;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Nop.Plugin.Sms.TelegramGateway.Domain;


public class TelegramGatewayApiService : ITelegramGatewayApiService
{
    private readonly HttpClient _httpClient;

    public TelegramGatewayApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://gatewayapi.telegram.org/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "AAG6KwAA0B-5jQbWWfc8c48nee8YT0EfzRU0PIL75a-wQg");
    }

    public async Task<TelegramGatewayResult<SendVerificationResult>> SendVerificationCodeAsync(
        string phoneNumber, int codeLength = 4)
    {
        try
        {
            var request = new { phone_number = phoneNumber, code_length = codeLength };
            var response = await _httpClient.PostAsJsonAsync("sendVerificationMessage", request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return Fail<SendVerificationResult>(TelegramGatewayResultCode.Unauthorized, "Invalid bearer token");

            var result = await response.Content.ReadFromJsonAsync<TelegramGatewayResponse<SendVerificationResult>>();
            if (result == null)
                return Fail<SendVerificationResult>(TelegramGatewayResultCode.UnknownError, "Empty response");

            if (!result.Ok)
                return Fail<SendVerificationResult>(MapError(result.Error), result.Error);

            return Success(result.Result!);
        }
        catch (Exception ex)
        {
            return Fail<SendVerificationResult>(TelegramGatewayResultCode.NetworkError, ex.Message);
        }
    }

    public async Task<TelegramGatewayResult<CheckVerificationResult>> CheckVerificationStatusAsync(
        string requestId, string code)
    {
        try
        {
            var request = new { request_id = requestId, code };
            var response = await _httpClient.PostAsJsonAsync("checkVerificationStatus", request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return Fail<CheckVerificationResult>(TelegramGatewayResultCode.Unauthorized, "Invalid bearer token");

            var result = await response.Content.ReadFromJsonAsync<TelegramGatewayResponse<CheckVerificationResult>>();
            if (result == null)
                return Fail<CheckVerificationResult>(TelegramGatewayResultCode.UnknownError, "Empty response");

            if (!result.Ok)
                return Fail<CheckVerificationResult>(MapError(result.Error), result.Error);

            // Проверяем статус в verification_status
            var status = result.Result?.VerificationStatus?.Status;

            return status switch
            {
                "code_valid" =>
                    Success(result.Result!),
                "code_invalid" =>
                    Fail<CheckVerificationResult>(TelegramGatewayResultCode.CodeInvalid, "Incorrect verification code"),
                "code_max_attempts_exceeded" =>
                    Fail<CheckVerificationResult>(TelegramGatewayResultCode.MaxAttemptsExceeded, "Maximum verification attempts exceeded"),
                "expired" =>
                    Fail<CheckVerificationResult>(TelegramGatewayResultCode.CodeExpired, "Verification code has expired"),
                null =>
                    Fail<CheckVerificationResult>(TelegramGatewayResultCode.UnknownError, "Verification status missing"),
                _ =>
                    Fail<CheckVerificationResult>(TelegramGatewayResultCode.UnknownError, $"Unknown verification status: {status}")
            };
        }
        catch (Exception ex)
        {
            return Fail<CheckVerificationResult>(TelegramGatewayResultCode.NetworkError, ex.Message);
        }
    }

    private static TelegramGatewayResult<T> Success<T>(T data) =>
        new() { Code = TelegramGatewayResultCode.Success, Data = data };

    private static TelegramGatewayResult<T> Fail<T>(TelegramGatewayResultCode code, string message) =>
        new() { Code = code, ErrorMessage = message };

    private static TelegramGatewayResultCode MapError(string error) => error switch
    {
        "CODE_INVALID" => TelegramGatewayResultCode.CodeInvalid,
        "REQUEST_ID_INVALID" => TelegramGatewayResultCode.RequestIdInvalid,
        "TOO_MANY_REQUESTS" => TelegramGatewayResultCode.RateLimited,
        "BALANCE_NOT_ENOUGH" => TelegramGatewayResultCode.BalanceNotEnough,
        "UNAUTHORIZED" => TelegramGatewayResultCode.Unauthorized,
        _ => TelegramGatewayResultCode.UnknownError
    };
}
