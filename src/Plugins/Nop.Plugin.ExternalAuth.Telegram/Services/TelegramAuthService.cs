using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Data;
using Nop.Plugin.ExternalAuth.Telegram.Configuration;
using Nop.Plugin.ExternalAuth.Telegram.Domain.Authentication;
using Nop.Services.Customers;

namespace Nop.Plugin.ExternalAuth.Telegram.Services;

public interface ITelegramAuthService
{
    Task<TelegramAuthSession> CreateSessionAsync(string? clientHint, CancellationToken cancellationToken = default);
    Task<TelegramAuthSession?> GetByTokenAsync(Guid sessionToken, CancellationToken cancellationToken = default);
    Task<TelegramAuthSession> RequireSessionAsync(Guid sessionToken, CancellationToken cancellationToken = default);
    Task<TelegramAuthSession?> GetPendingSessionByChatIdAsync(long chatId, CancellationToken cancellationToken = default);
    Task<TelegramAuthSession> HandleStartAsync(Guid sessionToken, long chatId, long userId, string? username, string? payload, CancellationToken cancellationToken = default);
    Task<TelegramAuthSession> HandleContactAsync(Guid sessionToken, long userId, string phoneNumber, string? firstName, string? lastName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Verifies the provided code for the given session and returns verification result.
    /// </summary>
    Task<TelegramVerificationResult> VerifyCodeAsync(Guid sessionToken, string code, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents result of successful Telegram session verification.
/// </summary>
public record TelegramVerificationResult(Customer Customer, bool IsNewCustomer);

public class TelegramAuthService : ITelegramAuthService
{
    private readonly IRepository<TelegramAuthSession> _sessionRepository;
    private readonly ICustomerService _customerService;
    private readonly ILogger<TelegramAuthService> _logger;
    private readonly TelegramGatewayConfiguration _config;

    public TelegramAuthService(
        IRepository<TelegramAuthSession> sessionRepository,
        ICustomerService customerService,
        ILogger<TelegramAuthService> logger,
        TelegramGatewayConfiguration config)
    {
        _sessionRepository = sessionRepository;
        _customerService = customerService;
        _logger = logger;
        _config = config;
    }

    public async Task<TelegramAuthSession> CreateSessionAsync(string? clientHint, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var session = new TelegramAuthSession
        {
            SessionToken = Guid.NewGuid(),
            Status = TelegramAuthStatus.PendingStart,
            CreatedOnUtc = now,
            UpdatedOnUtc = now,
            ClientHint = clientHint
        };

        await _sessionRepository.InsertAsync(session, false);
        return session;
    }

    public Task<TelegramAuthSession?> GetByTokenAsync(Guid sessionToken, CancellationToken cancellationToken = default) =>
        _sessionRepository.Table.FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

    public async Task<TelegramAuthSession> RequireSessionAsync(Guid sessionToken, CancellationToken cancellationToken = default)
    {
        var session = await GetByTokenAsync(sessionToken, cancellationToken);
        if (session == null)
            throw new KeyNotFoundException($"Session {sessionToken} was not found.");
        return session;
    }

    public Task<TelegramAuthSession?> GetPendingSessionByChatIdAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var activeStatuses = new[]
        {
            TelegramAuthStatus.PendingStart,
            TelegramAuthStatus.AwaitingContact,
            TelegramAuthStatus.ContactReceived,
            TelegramAuthStatus.CodeIssued
        }.Select(s => (int)s).ToArray();

        return _sessionRepository.Table
            .Where(s => s.TelegramChatId == chatId && activeStatuses.Contains(s.StatusId))
            .OrderByDescending(s => s.UpdatedOnUtc)
            .FirstOrDefaultAsync();
    }

    public async Task<TelegramAuthSession> HandleStartAsync(Guid sessionToken, long chatId, long userId, string? username, string? payload, CancellationToken cancellationToken = default)
    {
        var session = await RequireSessionAsync(sessionToken, cancellationToken);

        session.TelegramChatId = chatId;
        session.TelegramUserId = userId;
        session.TelegramUsername = username;
        session.StartPayload = payload;
        session.UpdatedOnUtc = DateTime.UtcNow;

        if (session.Status == TelegramAuthStatus.PendingStart)
            session.Status = TelegramAuthStatus.AwaitingContact;

        await _sessionRepository.UpdateAsync(session, false);
        return session;
    }

    public async Task<TelegramAuthSession> HandleContactAsync(Guid sessionToken, long userId, string phoneNumber, string? firstName, string? lastName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required", nameof(phoneNumber));

        var session = await RequireSessionAsync(sessionToken, cancellationToken);

        if (session.TelegramUserId.HasValue && session.TelegramUserId.Value != userId)
            throw new InvalidOperationException("Contact sender does not match the session owner.");

        session.TelegramUserId ??= userId;
        session.PhoneNumber = NormalizePhoneNumber(phoneNumber);
        session.FirstName = firstName;
        session.LastName = lastName;
        session.Status = TelegramAuthStatus.ContactReceived;
        session.UpdatedOnUtc = DateTime.UtcNow;

        var code = GenerateCode(_config.VerificationCodeLength);
        session.VerificationCode = code;
        session.CodeExpiresOnUtc = DateTime.UtcNow.AddMinutes(_config.VerificationCodeTtlMinutes);
        session.Status = TelegramAuthStatus.CodeIssued;

        await _sessionRepository.UpdateAsync(session, false);
        return session;
    }

    public async Task<TelegramVerificationResult> VerifyCodeAsync(Guid sessionToken, string code, CancellationToken cancellationToken = default)
    {
        var session = await RequireSessionAsync(sessionToken, cancellationToken);

        if (session.Status == TelegramAuthStatus.Expired)
            throw new InvalidOperationException("Session has expired.");

        if (session.CodeExpiresOnUtc.HasValue && session.CodeExpiresOnUtc.Value < DateTime.UtcNow)
        {
            session.Status = TelegramAuthStatus.Expired;
            await _sessionRepository.UpdateAsync(session, false);
            throw new InvalidOperationException("Verification code has expired.");
        }

        if (string.IsNullOrEmpty(session.VerificationCode))
            throw new InvalidOperationException("Verification code was not issued for this session.");

        if (!string.Equals(session.VerificationCode, code, StringComparison.Ordinal))
            throw new InvalidOperationException("Verification code is invalid.");

        Customer customer;
        var isNew = false;

        if (session.CustomerId.HasValue)
        {
            customer = await _customerService.GetCustomerByIdAsync(session.CustomerId.Value)
                       ?? throw new InvalidOperationException($"Customer {session.CustomerId.Value} not found.");
        }
        else
        {
            (customer, isNew) = await CreateOrLinkCustomerAsync(session, cancellationToken);
        }

        session.CustomerId = customer.Id;
        session.VerifiedOnUtc = DateTime.UtcNow;
        session.Status = TelegramAuthStatus.Verified;
        session.UpdatedOnUtc = DateTime.UtcNow;

        await _sessionRepository.UpdateAsync(session, false);

        // Return verification result; token is generated later by shared JwtTokenService
        return new TelegramVerificationResult(customer, isNew);
    }

    private async Task<(Customer customer, bool isNew)> CreateOrLinkCustomerAsync(TelegramAuthSession session, CancellationToken cancellationToken)
    {
        var phone = session.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var existing = await _customerService.GetAllCustomersAsync(phone: phone, pageSize: 1);
            if (existing.Any())
                return (existing.First(), false);
        }

        var customer = await _customerService.InsertGuestCustomerAsync();

        customer.Phone = phone;
        customer.Username = session.TelegramUsername ?? $"tg_{session.TelegramUserId ?? 0}";
        customer.Email = BuildSyntheticEmail(session);
        customer.FirstName = session.FirstName;
        customer.LastName = session.LastName;
        customer.Active = true;
        customer.LastActivityDateUtc = DateTime.UtcNow;

        await _customerService.UpdateCustomerAsync(customer);
        return (customer, true);
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits))
            return phoneNumber;

        digits = digits.TrimStart('0');
        if (digits.Length == 0)
            digits = "0";

        return "+" + digits;
    }

    private static string GenerateCode(int length)
    {
        var builder = new char[length];
        for (var i = 0; i < length; i++)
            builder[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        return new string(builder);
    }

    private static string BuildSyntheticEmail(TelegramAuthSession session)
    {
        var tokenPart = session.SessionToken.ToString("N")[..8];
        var userPart = session.TelegramUserId?.ToString() ?? "guest";
        return $"{userPart}.{tokenPart}@telegram.local";
    }
}

