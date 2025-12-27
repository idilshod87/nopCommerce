namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

/// <summary>
/// Generic API response wrapper matching NopStation Cart API style (top-level Data property).
/// </summary>
/// <typeparam name="T">Payload type.</typeparam>
public class ApiResponse<T>
{
    public T Data { get; set; } = default!;
}