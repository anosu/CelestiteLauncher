using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Celestite.Network.Models;
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
        private const string ApiBaseUrl = "https://evo.dmmapis.com";
        private static readonly string DefaultAuthKey = Convert.ToBase64String("9p69sajOH9pdsHatYIDGebf81AgR:yaVBs60OAp4vR4XqR0S1DpotRCYiJkft"u8);
        private static readonly AuthenticationHeaderValue DefaultAuthValue = new("Basic", DefaultAuthKey);
        private static string _currentAccessToken = string.Empty;
        private static string _currentRefreshToken = string.Empty;

        private const string LoginBaseUrl = "https://accounts.dmm.com/service/login";
        private static readonly string LoginPathBase = "DRVESRUMTh1PEkYWV1sLGQIKWxldQEsUTQNCEloKRVkfBB8GFFMSQlcLQl1sQh9HBFhVWVRcQloOC1IIRjpeVFhQX1ELdBMBUSllTFsDE3YxBEEGfwgnXFAWXUBBEVZEAFxRY";
        private static readonly string[] LoginPathSuffixes = { "V5dVgZBPQN1", "T5cV0t8N1pC", "S1yKWoONnIA" };


        public static void UpdateAccessToken(string accessToken)
        {
            _currentAccessToken = accessToken;
        }
        public static void UpdateRefreshToken(string refreshToken)
        {
            _currentRefreshToken = refreshToken;
        }

        public static async UniTask<DmmOpenApiResult<SessionIdResponse>> Login(string email, string password)
        {
            var random = new Random();
            string loginPath = LoginPathBase + LoginPathSuffixes[random.Next(LoginPathSuffixes.Length)];
            string loginUrl = $"{LoginBaseUrl}/password/=/path={loginPath}?device=games-player";
            var loginResponse = await HttpHelper.GetStringAsync(loginUrl);
            if (loginResponse.Failed) return DmmOpenApiResult.Fail<SessionIdResponse>(loginResponse.Exception);
            string tokenPattern = "\"token\":\"(.*?)\"";
            Match tokenMatch = Regex.Match(loginResponse.Value, tokenPattern);
            if (!tokenMatch.Success)
                return DmmOpenApiResult.Fail<SessionIdResponse>(new Exception("Failed to get login token from html string"));
            string token = tokenMatch.Groups[1].Value;
            string authUrl = $"{LoginBaseUrl}/password/authenticate";
            var authPayload = new Dictionary<string, string>
            {
                { "token", token },
                { "login_id", email },
                { "password", password },
                { "path", loginPath },
                { "device", "games-player" }
            };
            var authResponse = await HttpHelper.PostFormDataAsync(LoginBaseUrl, "/password/authenticate", authPayload);
            if (authResponse.Failed) return DmmOpenApiResult.Fail<SessionIdResponse>(authResponse.Exception);
            if (!authResponse.Value.IsSuccessStatusCode)
                return DmmOpenApiResult.Fail<SessionIdResponse>(new Exception($"Error auth response code: {authResponse.Value.StatusCode}"));
            var cookies = HttpHelper.GlobalCookieContainer.GetCookies(new Uri("https://dmm.com"));
            string loginSecureId = null;
            string loginSessionId = null;
            foreach (Cookie cookie in cookies)
            {
                if (cookie.Name == "login_secure_id") loginSecureId = cookie.Value;
                if (cookie.Name == "login_session_id") loginSessionId = cookie.Value;
            }
            if (loginSecureId == null || loginSessionId == null)
                return DmmOpenApiResult.Fail<SessionIdResponse>(new Exception($"Error auth response code: {authResponse.Value.StatusCode}"));
            return DmmOpenApiResult.Ok(new SessionIdResponse
            {
                SecureId = loginSecureId,
                UniqueId = loginSessionId,
            });
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

        public static async UniTask<DmmOpenApiResult<TokenResponse>> ExchangeAccessToken(string accessToken)
        {
            var json = await HttpHelper.PostJsonWithAuthorizationAsync(ApiBaseUrl, "/connect/v1/token",
                new TokenRequestAccessToken
                {
                    AccessToken = accessToken
                }, DmmOpenApiRequestBaseContext.Default.TokenRequestAccessToken, DmmOpenApiResponseBaseContext.Default.DmmOpenApiResponse, DefaultAuthValue);
            if (json.Failed) return DmmOpenApiResult.Fail<TokenResponse>(json.Exception);
            if (json.Value.Header.ResultCode.Equals("0"))
            {
                var tokenResponse = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.TokenResponse)!;
                if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
                    UpdateAccessToken(tokenResponse.AccessToken);
                return DmmOpenApiResult.Ok(tokenResponse);
            }
            var error = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.DmmOpenApiErrorBody)!;
            return DmmOpenApiResult.Fail<TokenResponse>(error);
        }

        public static async UniTask<DmmOpenApiResult<SessionIdResponse>> IssueSessionId(string userId)
        {
            if (string.IsNullOrEmpty(_currentAccessToken))
                throw new InvalidOperationException();
            var json = await HttpHelper.PostJsonWithAuthorizationAsync(ApiBaseUrl, "/connect/v1/issueSessionId",
                new IssueSessionIdRequest
                {
                    UserId = userId
                }, DmmOpenApiRequestBaseContext.Default.IssueSessionIdRequest, DmmOpenApiResponseBaseContext.Default.DmmOpenApiResponse, new AuthenticationHeaderValue("Bearer", _currentAccessToken));
            if (json.Failed) return DmmOpenApiResult.Fail<SessionIdResponse>(json.Exception);
            if (json.Value.Header.ResultCode.Equals("0"))
            {
                return DmmOpenApiResult.Ok(json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.SessionIdResponse)!);
            }
            var error = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.DmmOpenApiErrorBody)!;
            return DmmOpenApiResult.Fail<SessionIdResponse>(error);
        }

        public static async UniTask<DmmOpenApiResult<TokenResponse>> RefreshToken()
        {
            var json = await HttpHelper.PostJsonWithAuthorizationAsync(ApiBaseUrl, "/connect/v1/token",
                new RefreshTokenRequest()
                {
                    RefreshToken = _currentRefreshToken
                }, DmmOpenApiRequestBaseContext.Default.RefreshTokenRequest, DmmOpenApiResponseBaseContext.Default.DmmOpenApiResponse, DefaultAuthValue);
            if (json.Failed) return DmmOpenApiResult.Fail<TokenResponse>(json.Exception);
            if (json.Value.Header.ResultCode.Equals("0"))
            {
                var tokenResponse = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.TokenResponse)!;
                if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                    UpdateRefreshToken(tokenResponse.RefreshToken);
                if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
                    UpdateAccessToken(tokenResponse.AccessToken);
                return DmmOpenApiResult.Ok(tokenResponse);
            }
            var error = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.DmmOpenApiErrorBody)!;
            return DmmOpenApiResult.Fail<TokenResponse>(error);
        }
    }
}
