using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Nop.Core.Domain.Customers;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.Models.Authentication;
using Nop.Plugin.Api.Services;
using Nop.Services.Authentication;
using Nop.Services.Customers;
using Nop.Services.Logging;
using Nop.Services.Orders;

using System.Net;

namespace Nop.Plugin.Api.Controllers
{
    [AllowAnonymous]
    public class TokenController : Controller
    {
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ICustomerRegistrationService _customerRegistrationService;
        private readonly ICustomerService _customerService;
        private readonly CustomerSettings _customerSettings;

        public TokenController(
          ICustomerService customerService,
          ICustomerRegistrationService customerRegistrationService,
          ICustomerActivityService customerActivityService,
          IShoppingCartService shoppingCartService,
          IAuthenticationService authenticationService,
          IJwtTokenService jwtTokenService,
          CustomerSettings customerSettings)
        {
            _customerService = customerService;
            _customerRegistrationService = customerRegistrationService;
            _customerActivityService = customerActivityService;
            _shoppingCartService = shoppingCartService;
            _authenticationService = authenticationService;
            _jwtTokenService = jwtTokenService;
            _customerSettings = customerSettings;
        }

        [HttpPost]
        [Route("/token", Name = "RequestToken")]
        [ProducesResponseType(typeof(TokenResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> Create([FromBody] TokenRequest model)
        {
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
                    return BadRequest("Missing username");
                }

                if (string.IsNullOrEmpty(model.Password))
                {
                    return BadRequest("Missing password");
                }

                newCustomer = await LoginAsync(model.Username, model.Password, model.RememberMe);

                if (newCustomer is null)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden, "Wrong username or password");
                }
            }

            // migrate shopping cart, if the user is different
            if (oldCustomer is not null && oldCustomer.Id != newCustomer.Id)
            {
                await _shoppingCartService.MigrateShoppingCartAsync(oldCustomer, newCustomer, true); // migrate shopping cart items to newly logged in customer
            }

            var tokenResponse = _jwtTokenService.GenerateToken(newCustomer);

            await _authenticationService.SignInAsync(newCustomer, model.RememberMe); // update cookie-based authentication - not needed for api, avoids automatic generation of guest customer with each request to api

            // activity log
            await _customerActivityService.InsertActivityAsync(newCustomer, "Api.TokenRequest", "API token request", newCustomer);

            return Json(tokenResponse);
        }

        [HttpGet]
        [Authorize(Policy = JwtBearerDefaults.AuthenticationScheme, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // this validates token
        [Route("/token/check", Name = "ValidateToken")]
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
}
