using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using Celestite.Network.Models;
using Celestite.Pages.Default;
using Celestite.Utils;
using Cysharp.Threading.Tasks;
using ZeroLog;

namespace Celestite.Network
{
    public class DmmOpenApiResult
    {
        private static readonly Log Logger = LogManager.GetLogger("DmmOpenApi");
        public bool Success { get; }
        public bool Failed => !Success;
        public DmmOpenApiErrorBody? Error { get; }

        protected DmmOpenApiResult(bool success, DmmOpenApiErrorBody? error = null)
        {
            switch (success)
            {
                case true when error != null:
                    throw new InvalidOperationException();
                case false when error == null:
                    throw new InvalidOperationException();
            }

            Success = success;
            Error = error;

            if (!success)
            {
                Logger.Warn(error!.Reason);
                NotificationHelper.Error(error!.Reason);
            }
        }

        protected DmmOpenApiResult(Exception error)
        {
            Success = false;
            Error = new DmmOpenApiErrorBody
            {
                Code = "E999001",
                Reason = error!.Message
            };
            /*
            Logger.Error(string.Empty, error);
            NotificationHelper.Error(error!.Message);*/
        }

        public static DmmOpenApiResult<T> Ok<T>(T value) where T : class
        {
            return new DmmOpenApiResult<T>(value, true, null);
        }

        public static DmmOpenApiResult<T> Fail<T>(DmmOpenApiErrorBody error) where T : class
        {
            return new DmmOpenApiResult<T>(null, false, error);
        }
        public static DmmOpenApiResult<T> Fail<T>(Exception error) where T : class
        {
            return new DmmOpenApiResult<T>(error);
        }
        public static DmmOpenApiResult<T> Fail<T>(string errorMessage) where T : class
        {
            return new DmmOpenApiResult<T>(false, new DmmOpenApiErrorBody
            {
                Code = "E999002",
                Reason = errorMessage
            });
        }
        public static DmmOpenApiResult Fail(DmmOpenApiErrorBody error)
        {
            return new DmmOpenApiResult(false, error);
        }
        public static DmmOpenApiResult Fail(Exception error)
        {
            return new DmmOpenApiResult(error);
        }
        public static DmmOpenApiResult Fail(string errorMessage)
        {
            return new DmmOpenApiResult(false, new DmmOpenApiErrorBody
            {
                Code = "E999002",
                Reason = errorMessage
            });
        }
    }

    public class DmmOpenApiResult<T> : DmmOpenApiResult where T : class
    {
        private readonly T? _value;
        public T Value => _value!;

        protected internal DmmOpenApiResult(T? value, bool success, DmmOpenApiErrorBody? error = null) : base(success, error)
        {
            _value = value;
        }
        protected internal DmmOpenApiResult(bool success, DmmOpenApiErrorBody? error = null) : base(success, error)
        {
        }

        protected internal DmmOpenApiResult(Exception error) : base(error)
        {
        }
    }

    public static class DmmOpenApiHelper
    {
        private const string ApiBaseUrl = "https://accounts.dmm.com";

        public static async UniTask<bool> CheckValidity(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return false;
            }
            var issueResponse = await DmmGamePlayerApiHelper.CheckAccessToken(accessToken);
            if (issueResponse.Failed || !issueResponse.Value.Result)
            {
                return false;
            }
            return true;
        }

        public static async UniTask<bool> CheckValidity(string loginSecureId, string loginSessionId)
        {
            if (string.IsNullOrEmpty(loginSecureId) || string.IsNullOrEmpty(loginSessionId))
            {
                return false;
            }
            var request = new HttpRequestMessage(HttpMethod.Post, "https://apidgp-gameplayer.games.dmm.com/v5/userinfo");
            request.Headers.Add("Cookie", $"login_secure_id={loginSecureId}; login_session_id={loginSessionId}");
            var userInfoResponse = await HttpHelper.SendRawAsync(request, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseUserInfoResponse);
            if (userInfoResponse.Failed || userInfoResponse.Value.ResultCode != DmmGamePlayerApiErrorCode.SUCCESS)
            {
                return false;
            }
            return true;
        }

        public static async UniTask<DmmOpenApiResult<SessionIdResponse>> LegacyLogin(string email, string password)
        {
            var loginUrlResponse = await DmmGamePlayerApiHelper.GetLoginUrl();
            if (loginUrlResponse.Failed) return DmmOpenApiResult.Fail<SessionIdResponse>(loginUrlResponse.Error!);

            var loginResponse = await HttpHelper.GetStringAsync(loginUrlResponse.Value.Url);
            if (loginResponse.Failed) return DmmOpenApiResult.Fail<SessionIdResponse>(loginResponse.Exception);

            Match tokenMatch = Regex.Match(loginResponse.Value, "<input type=\"hidden\" name=\"token\" value=\"([^\"]+)\"/>");
            if (!tokenMatch.Success)
                return DmmOpenApiResult.Fail<SessionIdResponse>(new Exception("Failed to get login token from html string"));
            Match pathMatch = Regex.Match(loginResponse.Value, "<input type=\"hidden\" name=\"path\" value=\"([^\"]+)\"/>");
            if (!pathMatch.Success)
                return DmmOpenApiResult.Fail<SessionIdResponse>(new Exception("Failed to get login path from html string"));

            var authPayload = new Dictionary<string, string>
            {
                { "token", tokenMatch.Groups[1].Value },
                { "login_id", email },
                { "password", password },
                { "path", pathMatch.Groups[1].Value },
                { "device", "games-player" }
            };
            var authResponse = await HttpHelper.PostFormDataAsync(ApiBaseUrl, "/service/login/password/authenticate", authPayload);
            if (authResponse.Failed) return DmmOpenApiResult.Fail<SessionIdResponse>(authResponse.Exception);
            if (!authResponse.Value.IsSuccessStatusCode)
                return DmmOpenApiResult.Fail<SessionIdResponse>(new Exception($"Error auth response code: {authResponse.Value.StatusCode}"));

            var cookies = HttpHelper.GlobalCookieContainer.GetAllCookies().Where(c => !c.Expired);
            try
            {
                string loginSecureId = cookies.Single(x => x.Name == "login_secure_id").Value;
                string loginSessionId = cookies.Single(x => x.Name == "login_session_id").Value;
                return DmmOpenApiResult.Ok(new SessionIdResponse
                {
                    SecureId = loginSecureId,
                    UniqueId = loginSessionId,
                });
            }
            catch (Exception e)
            {
                return DmmOpenApiResult.Fail<SessionIdResponse>(new Exception($"Failed to get loginSecureId or loginSessionId, {e.Message}"));
            }
        }

        //public static async UniTask<DmmOpenApiResult<TokenResponse>> Login(string email, string password)
        //{
        //    var json = await HttpHelper.PostJsonWithAuthorizationAsync(ApiBaseUrl, "/connect/v1/token",
        //        new TokenRequestPassword
        //        {
        //            Email = email,
        //            Password = password
        //        }, DmmOpenApiRequestBaseContext.Default.TokenRequestPassword, DmmOpenApiResponseBaseContext.Default.DmmOpenApiResponse, DefaultAuthValue);
        //    if (json.Failed) return DmmOpenApiResult.Fail<TokenResponse>(json.Exception);
        //    if (json.Value.Header.ResultCode.Equals("0"))
        //    {
        //        var tokenResponse = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.TokenResponse)!;
        //        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
        //            UpdateRefreshToken(tokenResponse.RefreshToken);
        //        return DmmOpenApiResult.Ok(tokenResponse);
        //    }
        //    var error = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.DmmOpenApiErrorBody)!;
        //    return DmmOpenApiResult.Fail<TokenResponse>(error);
        //}

        //public static async UniTask<DmmOpenApiResult<TokenResponse>> ExchangeAccessToken(string accessToken)
        //{
        //    var json = await HttpHelper.PostJsonWithAuthorizationAsync(ApiBaseUrl, "/connect/v1/token",
        //        new TokenRequestAccessToken
        //        {
        //            AccessToken = accessToken
        //        }, DmmOpenApiRequestBaseContext.Default.TokenRequestAccessToken, DmmOpenApiResponseBaseContext.Default.DmmOpenApiResponse, DefaultAuthValue);
        //    if (json.Failed) return DmmOpenApiResult.Fail<TokenResponse>(json.Exception);
        //    if (json.Value.Header.ResultCode.Equals("0"))
        //    {
        //        var tokenResponse = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.TokenResponse)!;
        //        if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
        //            UpdateAccessToken(tokenResponse.AccessToken);
        //        return DmmOpenApiResult.Ok(tokenResponse);
        //    }
        //    var error = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.DmmOpenApiErrorBody)!;
        //    return DmmOpenApiResult.Fail<TokenResponse>(error);
        //}

        //public static async UniTask<DmmOpenApiResult<SessionIdResponse>> IssueSessionId(string userId)
        //{
        //    if (string.IsNullOrEmpty(_currentAccessToken))
        //        throw new InvalidOperationException();
        //    var json = await HttpHelper.PostJsonWithAuthorizationAsync(ApiBaseUrl, "/connect/v1/issueSessionId",
        //        new IssueSessionIdRequest
        //        {
        //            UserId = userId
        //        }, DmmOpenApiRequestBaseContext.Default.IssueSessionIdRequest, DmmOpenApiResponseBaseContext.Default.DmmOpenApiResponse, new AuthenticationHeaderValue("Bearer", _currentAccessToken));
        //    if (json.Failed) return DmmOpenApiResult.Fail<SessionIdResponse>(json.Exception);
        //    if (json.Value.Header.ResultCode.Equals("0"))
        //    {
        //        return DmmOpenApiResult.Ok(json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.SessionIdResponse)!);
        //    }
        //    var error = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.DmmOpenApiErrorBody)!;
        //    return DmmOpenApiResult.Fail<SessionIdResponse>(error);
        //}

        //public static async UniTask<DmmOpenApiResult<TokenResponse>> RefreshToken()
        //{
        //    var json = await HttpHelper.PostJsonWithAuthorizationAsync(ApiBaseUrl, "/connect/v1/token",
        //        new RefreshTokenRequest()
        //        {
        //            RefreshToken = _currentRefreshToken
        //        }, DmmOpenApiRequestBaseContext.Default.RefreshTokenRequest, DmmOpenApiResponseBaseContext.Default.DmmOpenApiResponse, DefaultAuthValue);
        //    if (json.Failed) return DmmOpenApiResult.Fail<TokenResponse>(json.Exception);
        //    if (json.Value.Header.ResultCode.Equals("0"))
        //    {
        //        var tokenResponse = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.TokenResponse)!;
        //        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
        //            UpdateRefreshToken(tokenResponse.RefreshToken);
        //        if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
        //            UpdateAccessToken(tokenResponse.AccessToken);
        //        return DmmOpenApiResult.Ok(tokenResponse);
        //    }
        //    var error = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.DmmOpenApiErrorBody)!;
        //    return DmmOpenApiResult.Fail<TokenResponse>(error);
        //}

        public static async UniTask<DmmOpenApiResult<LoginSuccessResponse>> Login(string email, string password)
        {
            var loginUrlResponse = await DmmGamePlayerApiHelper.GetAuthLoginUrl();
            if (loginUrlResponse.Failed) return DmmOpenApiResult.Fail<LoginSuccessResponse>("Fail to get login url");

            Uri loginUrl = new Uri(loginUrlResponse.Value.Url);

            var loginRedirectResponse = await HttpHelper.GetResponseHeaderAsync(loginUrl);
            if (loginRedirectResponse.Failed) return DmmOpenApiResult.Fail<LoginSuccessResponse>("Fail to get redirected login url");

            var loginResponse = await HttpHelper.GetStringAsync($"{loginUrl.GetLeftPart(UriPartial.Authority)}{loginRedirectResponse.Value.Headers.Location}");
            if (loginResponse.Failed) return DmmOpenApiResult.Fail<LoginSuccessResponse>("Fail to get login html");

            string tokenPattern = "<input type=\"hidden\" name=\"token\" value=\"([^\"]+)\"/>";
            Match tokenMatch = Regex.Match(loginResponse.Value, tokenPattern);
            if (!tokenMatch.Success)
                return DmmOpenApiResult.Fail<LoginSuccessResponse>(new Exception("Failed to get login token from html string"));

            string loginToken = tokenMatch.Groups[1].Value;
            string authorizeUrl = loginUrlResponse.Value.Url.Split('=').Last();
            var authResponse = await HttpHelper.PostFormDataAsync(ApiBaseUrl, "/service/oauth/authenticate", new Dictionary<string, string>
            {
                { "token", loginToken },
                { "login_id", email },
                { "password", password },
                { "path", authorizeUrl },
                { "recaptchaToken", "" }
            });
            if (authResponse.Failed) return DmmOpenApiResult.Fail<LoginSuccessResponse>(authResponse.Exception);

            var codeResponse = await HttpHelper.GetResponseHeaderAsync(WebUtility.UrlDecode(authorizeUrl));
            if (codeResponse.Failed || codeResponse.Value.Headers.Location == null)
                return DmmOpenApiResult.Fail<LoginSuccessResponse>(new Exception("Failed to get login success code"));

            Uri loginSuccessUrl = codeResponse.Value.Headers.Location;
            string? code = HttpUtility.ParseQueryString(loginSuccessUrl.Query).Get("code");
            if (string.IsNullOrEmpty(code)) return DmmOpenApiResult.Fail<LoginSuccessResponse>(new Exception("Code not found in login success url"));

            return DmmOpenApiResult.Ok(new LoginSuccessResponse { Code = code });
        }

    }
}
