using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Nop.Plugin.Sms.TelegramGateway.Configuration;

namespace Nop.Plugin.Sms.TelegramGateway.Services;

public interface ITelegramBotMessenger
{
    Task SendTextAsync(long chatId, string message, object? replyMarkup = null, CancellationToken cancellationToken = default);
    Task SendContactRequestAsync(long chatId, CancellationToken cancellationToken = default);
    Task SendVerificationCodeAsync(long chatId, string code, CancellationToken cancellationToken = default);
}

public class TelegramBotMessenger : ITelegramBotMessenger
{
    private const string TelegramApiBase = "https://api.telegram.org";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramBotMessenger> _logger;
    private readonly TelegramGatewayConfiguration _config;

    public TelegramBotMessenger(
        IHttpClientFactory httpClientFactory,
        ILogger<TelegramBotMessenger> logger,
        TelegramGatewayConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task SendTextAsync(long chatId, string message, object? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["chat_id"] = chatId,
            ["text"] = message,
            ["parse_mode"] = "HTML"
        };

        if (replyMarkup is not null)
            payload["reply_markup"] = replyMarkup;

        await SendAsync("sendMessage", payload, cancellationToken);
    }

    public Task SendContactRequestAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var keyboard = new
        {
            keyboard = new[]
            {
                new[]
                {
                    new
                    {
                        text = "Поделиться номером",
                        request_contact = true
                    }
                }
            },
            resize_keyboard = true,
            one_time_keyboard = true
        };

        const string prompt = "Необходимо привязать свой номер с помощью кнопки внизу: <b>поделиться номером</b>.";
        return SendTextAsync(chatId, prompt, keyboard, cancellationToken);
    }

    public Task SendVerificationCodeAsync(long chatId, string code, CancellationToken cancellationToken = default)
    {
        var message = $"Ваш код авторизации: <b>{code}</b>";
        return SendTextAsync(chatId, message, cancellationToken: cancellationToken);
    }

    private async Task SendAsync(string method, Dictionary<string, object> payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_config.BotToken))
            throw new InvalidOperationException("Telegram bot token is not configured.");

        var client = _httpClientFactory.CreateClient("telegram_bot_api");
        var requestUri = $"{TelegramApiBase}/bot{_config.BotToken}/{method}";

        using var response = await client.PostAsJsonAsync(requestUri, payload, cancellationToken);

        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("Telegram Bot API returned {StatusCode}: {Body}", response.StatusCode, body);
        response.EnsureSuccessStatusCode();
    }
}

