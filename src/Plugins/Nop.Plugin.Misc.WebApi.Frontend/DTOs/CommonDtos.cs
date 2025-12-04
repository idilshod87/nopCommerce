#nullable enable
using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

public class OperationResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// DTO for completing initial profile setup
/// </summary>
public class CompleteProfileRequestDto
{
    /// <summary>
    /// Username (required, max 256 characters)
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [MaxLength(256, ErrorMessage = "Username must not exceed 256 characters")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Company/Store name (required)
    /// </summary>
    [Required(ErrorMessage = "Company name is required")]
    public string Company { get; set; } = string.Empty;

    /// <summary>
    /// VAT Number (INN) - required, must be 14 digits
    /// </summary>
    [Required(ErrorMessage = "VAT Number (INN) is required")]
    [RegularExpression(@"^\d{14}$", ErrorMessage = "VAT Number (INN) must be 14 digits")]
    public string VatNumber { get; set; } = string.Empty;
}


