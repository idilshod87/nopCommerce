#nullable enable
using Nop.Web.Models.Customer;

namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

/// <summary>
/// Simplified address DTO for API responses
/// </summary>
public class AddressDto
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public int? CountryId { get; set; }
    public string? CountryName { get; set; }
    public int? StateProvinceId { get; set; }
    public string? StateProvinceName { get; set; }
    public string? County { get; set; }
    public string? City { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? ZipPostalCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FaxNumber { get; set; }
    public string? AddressLine { get; set; }
}

/// <summary>
/// Extended customer info response that includes address information
/// </summary>
public class CustomerInfoResponseDto
{
    /// <summary>
    /// Customer information
    /// </summary>
    public CustomerInfoModel? CustomerInfo { get; set; }

    /// <summary>
    /// All customer addresses
    /// </summary>
    public IList<AddressDto> Addresses { get; set; } = new List<AddressDto>();
}

/// <summary>
/// Customer DTO with addresses included
/// </summary>
public class CustomerDto
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Company { get; set; }
    public string? StreetAddress { get; set; }
    public string? StreetAddress2 { get; set; }
    public string? ZipPostalCode { get; set; }
    public string? City { get; set; }
    public string? County { get; set; }
    public int? CountryId { get; set; }
    public string? CountryName { get; set; }
    public int? StateProvinceId { get; set; }
    public string? StateProvinceName { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? VatNumber { get; set; }
    
    /// <summary>
    /// Customer addresses
    /// </summary>
    public IList<AddressDto> Addresses { get; set; } = new List<AddressDto>();
}
