using Kinde.Api.Client;
using Kinde.Api.Models.Configuration;
using Kinde.Api;
using Microsoft.AspNetCore.Mvc;

namespace AuthWithKinde.Controllers
{
    public class AuthController : Controller
    {

        private readonly IAuthorizationConfigurationProvider _authConfigurationProvider;
        private readonly IApplicationConfigurationProvider _appConfigurationProvider;

        public AuthController(IAuthorizationConfigurationProvider authConfigurationProvider, IApplicationConfigurationProvider appConfigurationProvider)
        {
            _authConfigurationProvider = authConfigurationProvider;
            _appConfigurationProvider = appConfigurationProvider;
        }

        public async Task<IActionResult> Login()
        {
            // We need some artificial id to correlate user session to client instance

            // NOTE: Session.Id will be always random, we need to add something to session to make it persistent.
            var correlationId = HttpContext.Session?.GetString("KindeCorrelationId");

            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("KindeCorrelationId", correlationId);
            }

            // Get client's instance...
            var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());

            // ...and authorize it
            await client.Authorize(_authConfigurationProvider.Get());

            // if auth flow is not ClientCredentials flow, we need to redirect user to another page
            if (client.AuthorizationState == Kinde.Api.Enums.AuthorizationStates.UserActionsNeeded)
            {
                // redirect user to login page
                return Redirect(await client.GetRedirectionUrl(correlationId));
            }
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> SignUp()
        {
            var correlationId = HttpContext.Session?.GetString("KindeCorrelationId");

            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("KindeCorrelationId", correlationId);
            }

            var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());

            await client.Register(_authConfigurationProvider.Get());

            if (client.AuthorizationState == Kinde.Api.Enums.AuthorizationStates.UserActionsNeeded)
            {
                return Redirect(await client.GetRedirectionUrl(correlationId));
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            var correlationId = HttpContext.Session?.GetString("KindeCorrelationId");
            var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());
            var url = await client.Logout();
            return Redirect(url);
        }

        public IActionResult Callback(string code, string state)
        {
            KindeClient.OnCodeReceived(code, state);
            var correlationId = HttpContext.Session?.GetString("KindeCorrelationId");
            var client = KindeClientFactory.Instance.Get(correlationId); // already authorized instance

            if (client.AuthorizationState == Kinde.Api.Enums.AuthorizationStates.Authorized)
            {
                var organizationId = client.GetOrganization(); // use the client to retrieve details about the user
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
