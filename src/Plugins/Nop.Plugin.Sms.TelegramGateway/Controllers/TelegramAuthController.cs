namespace Nop.Plugin.Sms.TelegramGateway.Controllers;

using System.Net;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nop.Plugin.Sms.TelegramGateway.Configuration;
using Nop.Plugin.Sms.TelegramGateway.Models;
using Nop.Plugin.Sms.TelegramGateway.Services;
using Nop.Plugin.Sms.TelegramGateway.Domain.TelegramWebhook;

[Route("api/telegram-auth")]
public class TelegramAuthController : BaseApiController
{
    private readonly ITelegramAuthService _authService;
    private readonly ITelegramBotMessenger _botMessenger;
    private readonly TelegramGatewayConfiguration _config;
    private readonly ILogger<TelegramAuthController> _logger;

    public TelegramAuthController(
        ITelegramAuthService authService,
        ITelegramBotMessenger botMessenger,
        TelegramGatewayConfiguration config,
        ILogger<TelegramAuthController> logger)
    {
        _authService = authService;
        _botMessenger = botMessenger;
        _config = config;
        _logger = logger;
    }

    [HttpPost("session")]
    public async Task<IActionResult> StartSession([FromBody] StartTelegramSessionRequest request)
    {
        var session = await _authService.CreateSessionAsync(request?.ClientHint);

        if (string.IsNullOrWhiteSpace(_config.BotUsername))
            return Problem("Bot username is not configured.", statusCode: (int)HttpStatusCode.InternalServerError);

        var deepLink = $"https://t.me/{_config.BotUsername}?start={session.SessionToken:N}";
        return Ok(new
        {
            success = true,
            sessionToken = session.SessionToken,
            botUrl = deepLink,
            codeLength = _config.VerificationCodeLength,
            codeTtlMinutes = _config.VerificationCodeTtlMinutes
        });
    }

    [HttpGet("session/{token:guid}")]
    public async Task<IActionResult> GetSession(Guid token)
    {
        var session = await _authService.GetByTokenAsync(token);
        if (session == null)
            return NotFound(new { success = false, error = "Session not found" });

        return Ok(new
        {
            success = true,
            status = session.Status.ToString(),
            phoneNumber = session.PhoneNumber,
            expiresAtUtc = session.CodeExpiresOnUtc,
            verifiedAtUtc = session.VerifiedOnUtc
        });
    }

    [HttpPost("session/verify")]
    public async Task<IActionResult> VerifySession([FromBody] VerifyTelegramSessionRequest request)
    {
        if (!Guid.TryParse(request.SessionToken, out var token))
            return BadRequest(new { success = false, error = "Invalid session token" });

        try
        {
            var tokenResponse = await _authService.VerifyCodeAsync(token, request.Code);
            return Ok(new { success = true, token = tokenResponse });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, error = "Session not found" });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: (int)HttpStatusCode.BadRequest);
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebhook()
    {
        // Log all headers for debugging
        var allHeaders = string.Join(", ", Request.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value.ToArray())}"));
        _logger.LogInformation("Webhook headers: {Headers}", allHeaders);

        Request.EnableBuffering();
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
        }
        _logger.LogInformation("Webhook request (raw body): {Body}", rawBody);

        TelegramUpdate update;
        try
        {
            update = JsonSerializer.Deserialize<TelegramUpdate>(rawBody) ?? throw new JsonException("Deserialized update is null");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Telegram webhook payload");
            return BadRequest(new { success = false, error = "Invalid Telegram payload" });
        }

        // Try to get secret token from header (case-insensitive)
        var secretToken = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault()
                         ?? Request.Headers["x-telegram-bot-api-secret-token"].FirstOrDefault()
                         ?? Request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString()
                         ?? Request.Headers.FirstOrDefault(h => h.Key.Equals("X-Telegram-Bot-Api-Secret-Token", StringComparison.OrdinalIgnoreCase)).Value.FirstOrDefault();

        _logger.LogInformation("Received webhook: UpdateId={UpdateId}, SecretTokenPresent={SecretTokenPresent}, SecretTokenLength={SecretTokenLength}",
            update?.UpdateId, !string.IsNullOrEmpty(secretToken), secretToken?.Length ?? 0);

        if (!IsWebhookAuthorized(secretToken))
        {
            _logger.LogWarning("Webhook unauthorized: SecretToken mismatch. Received={Received}, Configured={Configured}",
                secretToken ?? "(null)", _config.WebhookSecretToken ?? "(null)");
            return Unauthorized();
        }

        if (update.Message == null)
        {
            _logger.LogInformation("Webhook received but message is null");
            return Ok();
        }

        var message = update.Message;
        _logger.LogInformation("Processing message: ChatId={ChatId}, Text={Text}, HasContact={HasContact}",
            message.Chat?.Id, message.Text, message.Contact != null);

        if (!string.IsNullOrWhiteSpace(message.Text) && message.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            await HandleStartAsync(message);
        }
        else if (message.Contact != null)
        {
            await HandleContactAsync(message);
        }

        return Ok();
    }

    private async Task HandleStartAsync(TelegramMessage message)
    {
        var payload = ExtractStartPayload(message.Text);
        _logger.LogInformation("HandleStart: ChatId={ChatId}, Payload={Payload}", message.Chat.Id, payload);

        if (string.IsNullOrEmpty(payload))
        {
            _logger.LogWarning("Start command received without session token. ChatId={ChatId}", message.Chat.Id);
            // Try to find existing pending session by chatId
            var existingSession = await _authService.GetPendingSessionByChatIdAsync(message.Chat.Id);
            if (existingSession != null)
            {
                _logger.LogInformation("Found existing session: {SessionToken}", existingSession.SessionToken);
                await _botMessenger.SendContactRequestAsync(message.Chat.Id);
            }
            else
            {
                _logger.LogWarning("No active session found for ChatId={ChatId}", message.Chat.Id);
                await _botMessenger.SendTextAsync(message.Chat.Id, "Пожалуйста, используйте ссылку для входа, полученную в приложении.");
            }
            return;
        }

        if (!Guid.TryParse(payload, out var sessionToken))
        {
            _logger.LogWarning("Invalid session token in payload: {Payload}", payload);
            await _botMessenger.SendTextAsync(message.Chat.Id, "Неверная ссылка для входа. Пожалуйста, используйте ссылку из приложения.");
            return;
        }

        try
        {
            await _authService.HandleStartAsync(sessionToken, message.Chat.Id, message.From.Id, message.From.Username, payload);
            await _botMessenger.SendContactRequestAsync(message.Chat.Id);
            _logger.LogInformation("Successfully handled start command for session: {SessionToken}", sessionToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling start command for session: {SessionToken}", sessionToken);
            await _botMessenger.SendTextAsync(message.Chat.Id, "Произошла ошибка. Пожалуйста, попробуйте позже.");
        }
    }

    private async Task HandleContactAsync(TelegramMessage message)
    {
        var contact = message.Contact;
        if (contact.UserId.HasValue && contact.UserId.Value != message.From.Id)
            return;

        if (string.IsNullOrWhiteSpace(contact.PhoneNumber))
        {
            _logger.LogWarning("Contact message without phone number from ChatId={ChatId}", message.Chat.Id);
            await _botMessenger.SendTextAsync(message.Chat.Id, "Не удалось получить номер телефона. Пожалуйста, используйте кнопку \"Поделиться номером\".");
            await _botMessenger.SendContactRequestAsync(message.Chat.Id);
            return;
        }

        var session = await _authService.GetPendingSessionByChatIdAsync(message.Chat.Id);
        if (session == null)
            return;

        var updated = await _authService.HandleContactAsync(session.SessionToken, message.From.Id, contact.PhoneNumber, contact.FirstName, contact.LastName);
        await _botMessenger.SendVerificationCodeAsync(message.Chat.Id, updated.VerificationCode);
    }

    private bool IsWebhookAuthorized(string secretToken)
    {
        var configured = _config.WebhookSecretToken;

        if (string.IsNullOrWhiteSpace(configured))
        {
            _logger.LogWarning("WebhookSecretToken is not configured in settings");
            return false;
        }

        if (string.IsNullOrWhiteSpace(secretToken))
        {
            _logger.LogWarning("Secret token not provided in request header");
            return false;
        }

        var isAuthorized = string.Equals(configured, secretToken, StringComparison.Ordinal);
        if (!isAuthorized)
        {
            _logger.LogWarning("Secret token mismatch. Expected length={ExpectedLength}, Received length={ReceivedLength}",
                configured.Length, secretToken.Length);
        }

        return isAuthorized;
    }

    private static string ExtractStartPayload(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1] : string.Empty;
    }
}

