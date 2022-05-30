namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QmSettings
    {
        public const string DefaultBaseUrl = "https://ladder.cncnet.org";
        public const string DefaultLoginUrl = "/api/v1/auth/login";
        public const string DefaultRefreshUrl = "/api/v1/auth/refresh";
        public const string DefaultServerStatusUrl = "/api/v1/ping";
        public const string DefaultGetUserAccountsUrl = "/api/v1/user/account";
        public const string DefaultGetLaddersUrl = "/api/v1/ladder";
        public const string DefaultGetLadderMapsUrl = "/api/v1/qm/ladder/{0}/maps";

        public string BaseUrl { get; set; } = DefaultBaseUrl;
        public string LoginUrl { get; set; } = DefaultLoginUrl;
        public string RefreshUrl { get; set; } = DefaultRefreshUrl;
        public string ServerStatusUrl { get; set; } = DefaultServerStatusUrl;
        public string GetUserAccountsUrl { get; set; } = DefaultGetUserAccountsUrl;
        public string GetLaddersUrl { get; set; } = DefaultGetLaddersUrl;
        public string GetLadderMapsUrl { get; set; } = DefaultGetLadderMapsUrl;
    }
}
