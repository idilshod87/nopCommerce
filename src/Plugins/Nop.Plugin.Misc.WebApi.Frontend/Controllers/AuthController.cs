using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.WebApi.Frontend.Infrastructure;
using Nop.Plugin.Misc.WebApi.Frontend.Models.Authentication;
using Nop.Metrx.Core.Services;
using Nop.Services.Authentication;
using Nop.Services.Customers;
using Nop.Services.Logging;
using Nop.Services.Orders;
using System.Net;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Authentication API for JWT token management (login, refresh, validation).
/// </summary>
[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
[Route("public-api/auth")]
public class AuthController : ControllerBase
{
    private readonly ICustomerActivityService _customerActivityService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICustomerRegistrationService _customerRegistrationService;
    private readonly ICustomerService _customerService;
    private readonly CustomerSettings _customerSettings;

    public AuthController(
        ICustomerService customerService,
        ICustomerRegistrationService customerRegistrationService,
        ICustomerActivityService customerActivityService,
        IShoppingCartService shoppingCartService,
        IAuthenticationService authenticationService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        CustomerSettings customerSettings)
    {
        _customerService = customerService;
        _customerRegistrationService = customerRegistrationService;
        _customerActivityService = customerActivityService;
        _shoppingCartService = shoppingCartService;
        _authenticationService = authenticationService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _customerSettings = customerSettings;
    }

    /// <summary>
    /// Request authentication token (login or guest).
    /// </summary>
    /// <param name="model">Token request containing username/password or guest flag</param>
    /// <returns>Access token, refresh token, and user information</returns>
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> RequestToken([FromBody] TokenRequest model)
    {
        if (model == null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Request body is required");
        }

        // For refresh token requests, use the /token/refresh endpoint instead
        if (!model.Guest && string.IsNullOrEmpty(model.Username) && string.IsNullOrEmpty(model.Password))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Endpoint",
                detail: "For refresh token, use POST /public-api/auth/token/refresh endpoint");
        }

        Customer oldCustomer = await _authenticationService.GetAuthenticatedCustomerAsync();
        Customer newCustomer;

        if (model.Guest)
        {
            newCustomer = await _customerService.InsertGuestCustomerAsync();

            if (!await _customerService.IsInCustomerRoleAsync(newCustomer, Constants.Roles.ApiRoleSystemName))
            {
                //add to 'ApiUserRole' role if not yet present
                var apiRole = await _customerService.GetCustomerRoleBySystemNameAsync(Constants.Roles.ApiRoleSystemName);
                if (apiRole == null)
                    throw new InvalidOperationException($"'{Constants.Roles.ApiRoleSystemName}' role could not be loaded");
                await _customerService.AddCustomerRoleMappingAsync(new CustomerCustomerRoleMapping { CustomerId = newCustomer.Id, CustomerRoleId = apiRole.Id });
            }
        }
        else
        {
            if (string.IsNullOrEmpty(model.Username))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Validation Error",
                    detail: "Username is required");
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Validation Error",
                    detail: "Password is required");
            }

            newCustomer = await LoginAsync(model.Username, model.Password, model.RememberMe);

            if (newCustomer is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Authentication Failed",
                    detail: "Wrong username or password");
            }
        }

        // migrate shopping cart, if the user is different
        if (oldCustomer is not null && oldCustomer.Id != newCustomer.Id)
        {
            await _shoppingCartService.MigrateShoppingCartAsync(oldCustomer, newCustomer, true); // migrate shopping cart items to newly logged in customer
        }

        var jwt = _jwtTokenService.GenerateToken(newCustomer);

        // Generate refresh token
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(newCustomer, ipAddress);

        var expiresIn = (int)(jwt.ExpiresAtUtc - jwt.CreatedAtUtc).TotalSeconds;

        var tokenResponse = new TokenResponse(jwt.AccessToken, jwt.CreatedAtUtc, jwt.ExpiresAtUtc)
        {
            CustomerId = jwt.CustomerId,
            CustomerGuid = jwt.CustomerGuid,
            Username = jwt.Username,
            TokenType = "Bearer",
            RefreshToken = refreshToken.Token,
            ExpiresIn = expiresIn
        };

        await _authenticationService.SignInAsync(newCustomer, model.RememberMe); // update cookie-based authentication - not needed for api, avoids automatic generation of guest customer with each request to api

        // activity log
        await _customerActivityService.InsertActivityAsync(newCustomer, "Api.TokenRequest", "API token request", newCustomer);

        return Ok(tokenResponse);
    }

    /// <summary>
    /// Validate the current JWT token.
    /// </summary>
    /// <returns>OK if token is valid, NotFound if customer not found, Unauthorized if token invalid</returns>
    [HttpGet("token/check")]
    [Authorize(Policy = JwtBearerDefaults.AuthenticationScheme, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> ValidateToken()
    {
        Customer currentCustomer = await _authenticationService.GetAuthenticatedCustomerAsync(); // this gets customer entity from db if it exists
        if (currentCustomer is null)
            return NotFound();
        return Ok();
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    /// <param name="model">Refresh token request</param>
    /// <returns>New access token and refresh token</returns>
    [HttpPost("token/refresh")]
    [ProducesResponseType(typeof(TokenResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest model)
    {
        if (string.IsNullOrEmpty(model?.RefreshToken))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error",
                detail: "Refresh token is required");
        }

        // Validate refresh token and get customer
        var customer = await _refreshTokenService.ValidateRefreshTokenAsync(model.RefreshToken);

        if (customer == null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid Token",
                detail: "Invalid or expired refresh token");
        }

        // Revoke old refresh token
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _refreshTokenService.RevokeRefreshTokenAsync(model.RefreshToken, ipAddress);

        // Generate new access token
        var jwt = _jwtTokenService.GenerateToken(customer);

        // Generate new refresh token
        var newRefreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(customer, ipAddress);

        var expiresIn = (int)(jwt.ExpiresAtUtc - jwt.CreatedAtUtc).TotalSeconds;

        var tokenResponse = new TokenResponse(jwt.AccessToken, jwt.CreatedAtUtc, jwt.ExpiresAtUtc)
        {
            CustomerId = jwt.CustomerId,
            CustomerGuid = jwt.CustomerGuid,
            Username = jwt.Username,
            TokenType = "Bearer",
            RefreshToken = newRefreshToken.Token,
            ExpiresIn = expiresIn
        };

        // activity log
        await _customerActivityService.InsertActivityAsync(customer, "Api.TokenRefresh", "API token refresh", customer);

        return Ok(tokenResponse);
    }

    #region Private methods

    private async Task<Customer> LoginAsync(string username, string password, bool rememberMe)
    {
        var result = await _customerRegistrationService.ValidateCustomerAsync(username, password);

        if (result == CustomerLoginResults.Successful)
        {
            var customer = await (_customerSettings.UsernamesEnabled
                       ? _customerService.GetCustomerByUsernameAsync(username)
                       : _customerService.GetCustomerByEmailAsync(username));
            return customer;
        }

        return null;
    }

    #endregion
}
