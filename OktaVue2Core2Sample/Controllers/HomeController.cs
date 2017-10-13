using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Okta.Sdk;
using Okta.Sdk.Configuration;
using Microsoft.Extensions.Options;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Vue2Core2Sample.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private OktaClient client;

        public HomeController()
        {
            //TODO appsettings.json (inject config object)
            client = new OktaClient(new OktaClientConfiguration
            {
                OrgUrl = "https://your-okta-host-url", //e.g. https://your-hostname.oktapreview.com
                Token = "your-okta-api-key"
            });

        }

        public IActionResult Index()
        {
            var userId = User.FindFirst(x => x.Type == @"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            var oktaUser = client.Users.First(u => u.Id == userId).Result;
            var userViewData = OktaUserToAppUser(oktaUser);

            ViewData["UserFullName"] = userViewData.FullName;
            ViewData["UserEmailAddress"] = userViewData.EmailAddress;
            ViewData["UserCustomData"] = userViewData.CustomData;

            return View();
        }

        public IActionResult Error()
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }

        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            return SignOut(CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
        }

        #region Move Me
        //TODO: refactor
        private static dynamic OktaUserToAppUser(IUser OktaUser)
        {
            var customData = parseOktaCustomDataString((string)OktaUser.Profile["custom_data"]);

            return new
            {
                Id = OktaUser.Id,
                UserName = OktaUser.Profile.Login,
                EmailAddress = OktaUser.Profile.Email,
                TimeZone = OktaUser.Profile.GetProperty<string>("zoneInfo"),
                FullName = string.Format("{0} {1}", OktaUser.Profile.FirstName, OktaUser.Profile.LastName),
                CustomData = customData
            };
        }

        //Shouldn't be needed
        private static JObject parseOktaCustomDataString(string originalString)
        {
            var customDataString = originalString ?? "";
            if (customDataString.Length == 0)
                customDataString = "{}";

            //TODO: OKTA support needs to address their inconsistent escaping
            //this will break at some point
            var newString = customDataString.Replace("\\\"", "\"").Replace("\"[", "[").Replace("\"{", "{").Replace("]\"", "]").Replace("}\"", "}").Replace("'", "’");

            //now strip any html
            var rgx = new Regex("<.*?>");
            return JObject.Parse(rgx.Replace(newString, ""));

        }
        #endregion

    }
}
