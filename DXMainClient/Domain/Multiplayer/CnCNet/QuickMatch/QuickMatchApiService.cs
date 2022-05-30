using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ClientCore;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QuickMatchApiService
    {
        private readonly QmSettings qmSettings;
        private string _token;

        public QuickMatchApiService()
        {
            var settingsService = new QuickMatchSettingsService();
            qmSettings = settingsService.LoadSettings();
        }

        public void SetToken(string token)
        {
            _token = token;
        }

        public async Task<IEnumerable<QmLadderMap>> FetchLadderMapsForAbbrAsync(string ladderAbbreviation)
        {
            var httpClient = CreateAuthenticatedClient();
            string url = string.Format(qmSettings.GetLadderMapsUrl, ladderAbbreviation);
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new ClientException($"Error fetching ladder maps: {response.ReasonPhrase}");

            return JsonConvert.DeserializeObject<IEnumerable<QmLadderMap>>(await response.Content.ReadAsStringAsync());
        }

        public async Task<IEnumerable<QmUserAccount>> FetchUserAccountsAsync()
        {
            var httpClient = CreateAuthenticatedClient();
            var response = await httpClient.GetAsync(qmSettings.GetUserAccountsUrl);
            if (!response.IsSuccessStatusCode)
                throw new ClientException($"Error fetching user accounts: {response.ReasonPhrase}");

            return JsonConvert.DeserializeObject<IEnumerable<QmUserAccount>>(await response.Content.ReadAsStringAsync());
        }

        public async Task<IEnumerable<QmLadder>> FetchLaddersAsync()
        {
            var httpClient = CreateAuthenticatedClient();
            var response = await httpClient.GetAsync(qmSettings.GetLaddersUrl);
            if (!response.IsSuccessStatusCode)
                throw new ClientException($"Error fetching ladders: {response.ReasonPhrase}");

            return JsonConvert.DeserializeObject<IEnumerable<QmLadder>>(await response.Content.ReadAsStringAsync());
        }

        public async Task<QmAuthData> LoginAsync(string email, string password)
        {
            var httpClient = CreateHttpClient();
            var postBodyContent = CreatePostBody(new QMLoginRequest()
            {
                Email = email,
                Password = password
            });
            var response = await httpClient.PostAsync(qmSettings.LoginUrl, postBodyContent);
            if (!response.IsSuccessStatusCode)
                throw new ClientException($"Error logging in: {response.ReasonPhrase}");

            var authData = JsonConvert.DeserializeObject<QmAuthData>(await response.Content.ReadAsStringAsync());
            _token = authData.Token;
            return authData;
        }

        public async Task<QmAuthData> RefreshAsync()
        {
            var httpClient = CreateAuthenticatedClient();
            var response = await httpClient.GetAsync(qmSettings.RefreshUrl);
            if (!response.IsSuccessStatusCode)
                throw new ClientException($"Error refreshing token: {response.ReasonPhrase}");

            var authData = JsonConvert.DeserializeObject<QmAuthData>(await response.Content.ReadAsStringAsync());
            _token = authData.Token;
            return authData;
        }

        public bool IsServerAvailable()
        {
            var httpClient = CreateAuthenticatedClient();
            var response = httpClient.GetAsync(qmSettings.ServerStatusUrl).Result;
            return response.IsSuccessStatusCode;
        }

        private HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(qmSettings.BaseUrl)
            };

            return httpClient;
        }

        private HttpClient CreateAuthenticatedClient()
        {
            var httpClient = CreateHttpClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_token}");
            return httpClient;
        }

        private static FormUrlEncodedContent CreatePostBody(IPostBodyModel model)
            => new FormUrlEncodedContent(model.ToDictionary());
    }
}
