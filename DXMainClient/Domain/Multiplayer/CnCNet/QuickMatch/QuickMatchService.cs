using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientCore;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;
using JWT.Serializers;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QuickMatchService
    {
        private readonly QuickMatchUserSettingsService userSettingsService;
        private readonly QuickMatchApiService apiService;
        private readonly QmUserSettings qmUserSettings;
        private readonly QmData qmData;

        private static QuickMatchService Instance;

        public event EventHandler<QmStatusMessageEventArgs> StatusMessageEvent;
        public event EventHandler<QmLoginEventArgs> LoginEvent;
        public event EventHandler<QmLadderMapsEventArgs> LadderMapsEvent;

        private QuickMatchService()
        {
            userSettingsService = new QuickMatchUserSettingsService();
            apiService = new QuickMatchApiService();
            qmUserSettings = userSettingsService.LoadSettings();
            qmData = new QmData();
        }

        public static QuickMatchService GetInstance() => Instance ?? (Instance = new QuickMatchService());

        /// <summary>
        /// Login process to cncnet
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        public async Task LoginAsync(string email, string password)
        {
            try
            {
                StatusMessageEvent?.Invoke(this, new QmStatusMessageEventArgs("Logging in..."));
                var authData = await apiService.LoginAsync(email, password);
                await FinishLogin(authData, false, email);
            }
            catch (ClientException e)
            {
                Logger.Log(e.Message);
                LoginEvent?.Invoke(this, new QmLoginEventArgs(QmLoginEventStatusEnum.Unauthorized));
            }
            catch (Exception e)
            {
                Logger.Log(e.StackTrace);
                LoginEvent?.Invoke(this, new QmLoginEventArgs(QmLoginEventStatusEnum.Unknown));
            }

            StatusMessageEvent?.Invoke(this, null);
        }

        /// <summary>
        /// Simply clear all auth data from our settings
        /// </summary>
        public void Logout()
        {
            ClearAuthData();
            LoginEvent?.Invoke(this, new QmLoginEventArgs(QmLoginEventStatusEnum.Logout));
        }

        private void ClearAuthData()
        {
            userSettingsService.ClearAuthData();
            userSettingsService.SaveSettings();
        }

        /// <summary>
        /// Attempts to refresh an existing auth token
        /// </summary>
        public async Task RefreshAsync()
        {
            try
            {
                StatusMessageEvent?.Invoke(this, new QmStatusMessageEventArgs("Refreshing login..."));
                var authData = await apiService.RefreshAsync();
                await FinishLogin(authData, false);
            }
            catch (ClientException e)
            {
                Logger.Log(e.Message);
                ClearAuthData();
                LoginEvent?.Invoke(this, new QmLoginEventArgs(QmLoginEventStatusEnum.FailedRefresh));
            }
            catch (Exception e)
            {
                Logger.Log(e.StackTrace);
                ClearAuthData();
                LoginEvent?.Invoke(this, new QmLoginEventArgs(QmLoginEventStatusEnum.Unknown));
            }
            StatusMessageEvent?.Invoke(this, null);
        }

        public IEnumerable<QmUserAccount> GetUserAccounts() => qmData?.UserAccounts;

        public QmLadder GetLadderForId(int ladderId) => qmData.Ladders.FirstOrDefault(l => l.Id == ladderId);

        public string GetCachedEmail() => qmUserSettings.Email;

        public string GetCachedLadder() => qmUserSettings.Ladder;

        public bool IsServerAvailable() => apiService.IsServerAvailable();

        public bool IsLoggedIn()
        {
            if (qmUserSettings.AuthData == null)
                return false;

            try
            {
                DecodeToken(qmUserSettings.AuthData.Token);
            }
            catch (TokenExpiredException)
            {
                Logger.Log("QuickMatch token is expired");
                return false;
            }
            catch (Exception e)
            {
                Logger.Log(e.StackTrace);
                return false;
            }

            apiService.SetToken(qmUserSettings.AuthData.Token);

            return true;
        }

        public void SetLadder(string ladder)
        {
            qmUserSettings.Ladder = ladder;
            userSettingsService.SaveSettings();

            FetchLadderMapsForAbbrAsync(ladder);
        }

        private async Task FetchDataAsync()
        {
            StatusMessageEvent?.Invoke(this, new QmStatusMessageEventArgs("Fetching data..."));
            var fetchLaddersTask = apiService.FetchLaddersAsync();
            var fetchUserAccountsTask = apiService.FetchUserAccountsAsync();

            await Task.WhenAll(fetchLaddersTask, fetchUserAccountsTask);
            qmData.Ladders = fetchLaddersTask.Result;
            qmData.UserAccounts = fetchUserAccountsTask.Result;

            LoginEvent?.Invoke(this, new QmLoginEventArgs(QmLoginEventStatusEnum.Success));
        }

        private async Task FetchLadderMapsForAbbrAsync(string ladderAbbr)
        {
            StatusMessageEvent?.Invoke(this, new QmStatusMessageEventArgs("Fetching ladder maps..."));
            var ladderMaps = await apiService.FetchLadderMapsForAbbrAsync(ladderAbbr);

            LadderMapsEvent?.Invoke(this, new QmLadderMapsEventArgs(ladderMaps));
            StatusMessageEvent?.Invoke(this, null);
        }

        private async Task FinishLogin(QmAuthData authData, bool refresh, string email = null)
        {
            if (authData == null)
            {
                userSettingsService.ClearAuthData();
                LoginEvent?.Invoke(this, new QmLoginEventArgs(refresh ? QmLoginEventStatusEnum.FailedRefresh : QmLoginEventStatusEnum.Unauthorized));
                return;
            }

            qmUserSettings.AuthData = authData;
            qmUserSettings.Email = email ?? qmUserSettings.Email;
            userSettingsService.SaveSettings();
            await FetchDataAsync();
        }

        /// <summary>
        /// We only need to verify the expiration date of the token so that we can refresh or request a new one if it is expired.
        /// We do not need to worry about the signature. The API will handle that validation when the token is used.
        /// </summary>
        /// <param name="token"></param>
        private static void DecodeToken(string token)
        {
            IJsonSerializer serializer = new JsonNetSerializer();
            IDateTimeProvider provider = new UtcDateTimeProvider();
            ValidationParameters validationParameters = ValidationParameters.Default;
            validationParameters.ValidateSignature = false;
            IJwtValidator validator = new JwtValidator(serializer, provider, validationParameters);
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtAlgorithm algorithm = new HMACSHA256Algorithm(); // symmetric
            IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder, algorithm);

            decoder.Decode(token, "nosecret", verify: true);
        }
    }
}
