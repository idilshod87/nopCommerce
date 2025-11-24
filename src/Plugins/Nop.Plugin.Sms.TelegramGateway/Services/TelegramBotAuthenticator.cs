using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Nop.Plugin.Sms.TelegramGateway.Domain;

namespace Nop.Plugin.Sms.TelegramGateway.Services
{
    public interface ITelegramBotAuthenticator
    {
        Task<TelegramGatewayResult<BotInfo>> ValidateBotTokenAsync(string botToken, CancellationToken cancellationToken = default);
    }

    public class TelegramBotAuthenticator : ITelegramBotAuthenticator
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TelegramBotAuthenticator> _logger;
        private const string TelegramApiBase = "https://api.telegram.org";

        public TelegramBotAuthenticator(IHttpClientFactory httpClientFactory, ILogger<TelegramBotAuthenticator> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TelegramGatewayResult<BotInfo>> ValidateBotTokenAsync(string botToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(botToken))
            {
                return new TelegramGatewayResult<BotInfo>
                {
                    Code = TelegramGatewayResultCode.Unauthorized,
                    ErrorMessage = "Bot token is null or empty."
                };
            }

            var client = _httpClientFactory.CreateClient("telegram_gateway_client");
            var requestUri = $"{TelegramApiBase}/bot{WebUtility.UrlEncode(botToken)}/getMe";

            try
            {
                using var response = await client.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Telegram getMe responded with {StatusCode}", response.StatusCode);

                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        return new TelegramGatewayResult<BotInfo>
                        {
                            Code = TelegramGatewayResultCode.Unauthorized,
                            ErrorMessage = $"HTTP {(int)response.StatusCode} - Unauthorized"
                        };
                    }

                    return new TelegramGatewayResult<BotInfo>
                    {
                        Code = TelegramGatewayResultCode.NetworkError,
                        ErrorMessage = $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}"
                    };
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<TelegramGatewayResponse<BotInfo>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                if (apiResponse == null)
                {
                    return new TelegramGatewayResult<BotInfo>
                    {
                        Code = TelegramGatewayResultCode.UnknownError,
                        ErrorMessage = "Empty response from Telegram API."
                    };
                }

                if (apiResponse.Ok && apiResponse.Result is not null)
                {
                    return new TelegramGatewayResult<BotInfo>
                    {
                        Code = TelegramGatewayResultCode.Success,
                        Data = apiResponse.Result
                    };
                }

                var error = apiResponse.Error ?? "Unknown error from Telegram API.";

                if (error.Contains("Unauthorized") || error.Contains("bot is not found", StringComparison.OrdinalIgnoreCase))
                {
                    return new TelegramGatewayResult<BotInfo>
                    {
                        Code = TelegramGatewayResultCode.Unauthorized,
                        ErrorMessage = error
                    };
                }

                return new TelegramGatewayResult<BotInfo>
                {
                    Code = TelegramGatewayResultCode.UnknownError,
                    ErrorMessage = error
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling Telegram API");
                return new TelegramGatewayResult<BotInfo>
                {
                    Code = TelegramGatewayResultCode.NetworkError,
                    ErrorMessage = ex.Message
                };
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new TelegramGatewayResult<BotInfo>
                {
                    Code = TelegramGatewayResultCode.NetworkError,
                    ErrorMessage = "Request cancelled."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error validating bot token");
                return new TelegramGatewayResult<BotInfo>
                {
                    Code = TelegramGatewayResultCode.UnknownError,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
