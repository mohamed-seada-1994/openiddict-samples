using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Matty.Client.ViewModels.Authorization;
using Matty.Client.ViewModels.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Matty.Client.Controllers
{
    public class AuthorizationController : Controller
    {
        private const string clientId = "device";
        private const string scopes = "openid offline_access profile email";
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthorizationController(IHttpClientFactory httpClientFactory)
            => _httpClientFactory = httpClientFactory;

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            using var client = _httpClientFactory.CreateClient();

            var result = await GetDeviceCodeAsync(client);

            HttpContext.Session.SetString(Parameters.DeviceCode, result.DeviceCode);
            HttpContext.Session.SetInt32(Parameters.ExpiresIn, (int)result.ExpiresIn);
            //Response.Cookies.Append(Parameters.DeviceCode, result.DeviceCode, new CookieOptions
            //{
            //    HttpOnly = true,
            //    Secure = true,
            //    IsEssential = true,
            //    MaxAge = TimeSpan.FromMinutes(5),
            //});
            return View("Index", result);
        }

        [HttpGet]
        public async Task<ActionResult> Authorize()
        {
            // 1- try exchenge refresh token // error - reset session
            // 2- try get device code // error - reset session
            // 3- exchange with the tokens / or redirect to index
            using var client = _httpClientFactory.CreateClient();

            var accessToekn = HttpContext.Session.GetString(Parameters.AccessToken);

            var refreshToken = HttpContext.Session.GetString(Parameters.RefreshToken);
            var tokens = await ExchangeDeviceCodeAsync(client, refreshToken);
            
            var deviceCode = HttpContext.Session.GetString(Parameters.DeviceCode);
            var tokens = await ExchangeDeviceCodeAsync(client, deviceCode);
            
            /*
            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme,
                "name",
                "user");

                    var authProperties = new AuthenticationProperties();

                    // save the tokens in the cookie
                    authProperties.StoreTokens(new List<AuthenticationToken>
            {
                new AuthenticationToken
                {
                    Name = "access_token",
                    Value = tokenresponse.AccessToken
                },
                new AuthenticationToken
                {
                    Name = "id_token",
                    Value = tokenresponse.IdentityToken
                }
            });

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
            */
            return RedirectToAction("Index","Home");
        }

        private static async Task<DeviceCodeViewModel> GetDeviceCodeAsync(HttpClient client)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:44321/connect/device");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [Parameters.ClientId] = clientId,
                [Parameters.Scope] = scopes
            });

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);

            var payload = await response.Content.ReadFromJsonAsync<OpenIddictResponse>();

            if (!string.IsNullOrEmpty(payload.Error))
            {
                throw new InvalidOperationException("An error occurred while retrieving an device code.");
            }

            return new DeviceCodeViewModel { 
                DeviceCode = payload.DeviceCode,
                UserCode = payload.UserCode,
                ExpiresIn = payload.ExpiresIn,
                VerificationUri = payload.GetParameter(Parameters.VerificationUri).ToString(),
                VerificationUriComplete = payload.GetParameter(Parameters.VerificationUriComplete).ToString()
            };
        }

        private static async Task<Result<TokensViewModel>> ExchangeDeviceCodeAsync(HttpClient client, string deviceCode)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:44321/connect/token");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [Parameters.GrantType] = GrantTypes.DeviceCode,
                [Parameters.ClientId] = clientId,
                [Parameters.DeviceCode] = deviceCode,
                [Parameters.Scope] = scopes
            });

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);

            var payload = await response.Content.ReadFromJsonAsync<OpenIddictResponse>();

            if (!string.IsNullOrEmpty(payload.Error))
            {
                return Result<TokensViewModel>.Failure("An error occurred while retrieving an access token.");
            }

            return Result<TokensViewModel>.Success(new TokensViewModel
            {
                IdToken = payload.IdToken,
                AccessToken = payload.AccessToken,
                RefreshToken = payload.RefreshToken,
                ExpiresIn = payload.ExpiresIn
            });
        }

        private static async Task<Result<TokensViewModel>> ExchangeRefreshTokenAsync(HttpClient client, string refreshToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:44321/connect/token");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [Parameters.GrantType] = GrantTypes.RefreshToken,
                [Parameters.ClientId] = clientId,
                [Parameters.RefreshToken] = refreshToken,
                [Parameters.Scope] = scopes
            });

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);

            var payload = await response.Content.ReadFromJsonAsync<OpenIddictResponse>();

            if (!string.IsNullOrEmpty(payload.Error))
            {
                return Result<TokensViewModel>.Failure("An error occurred while retrieving an access token.");
            }

            return Result<TokensViewModel>.Success(new TokensViewModel
            {
                IdToken = payload.IdToken,
                AccessToken = payload.AccessToken,
                RefreshToken = payload.RefreshToken,
                ExpiresIn = payload.ExpiresIn
            });
        }
    }
}