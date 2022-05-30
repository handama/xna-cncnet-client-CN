using System.IO;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QuickMatchSettingsService
    {
        private static readonly string SettingsFile = ClientConfiguration.Instance.QuickMatchPath;

        private const string BasicSectionKey = "Basic";

        private const string BaseUrlKey = "BaseUrl";
        private const string LoginUrlKey = "LoginUrl";
        private const string RefreshUrlKey = "RefreshUrl";
        private const string ServerStatusUrlKey = "ServerStatusUrl";
        private const string GetUserAccountsUrlKey = "GetUserAccountsUrl";
        private const string GetLaddersUrlKey = "GetLaddersUrl";
        private const string GetLadderMapsUrlKey = "GetLadderMapsUrl";


        private QmSettings qmSettings;

        public QmSettings LoadSettings()
        {
            if (qmSettings != null)
                return qmSettings;

            qmSettings = new QmSettings();
            if (!File.Exists(SettingsFile))
                SaveSettings(); // init the settings file

            var iniFile = new IniFile(SettingsFile);
            var basicSection = iniFile.GetSection(BasicSectionKey);
            if (basicSection == null)
                return qmSettings;

            qmSettings.BaseUrl = basicSection.GetStringValue(BaseUrlKey, QmSettings.DefaultBaseUrl);
            qmSettings.LoginUrl = basicSection.GetStringValue(LoginUrlKey, QmSettings.DefaultLoginUrl);
            qmSettings.RefreshUrl = basicSection.GetStringValue(RefreshUrlKey, QmSettings.DefaultRefreshUrl);
            qmSettings.ServerStatusUrl = basicSection.GetStringValue(ServerStatusUrlKey, QmSettings.DefaultServerStatusUrl);
            qmSettings.GetUserAccountsUrl = basicSection.GetStringValue(GetUserAccountsUrlKey, QmSettings.DefaultGetUserAccountsUrl);
            qmSettings.GetLaddersUrl = basicSection.GetStringValue(GetLaddersUrlKey, QmSettings.DefaultGetLaddersUrl);
            qmSettings.GetLadderMapsUrl = basicSection.GetStringValue(GetLadderMapsUrlKey, QmSettings.DefaultGetLadderMapsUrl);

            return qmSettings;
        }


        public void SaveSettings()
        {
            var iniFile = new IniFile();
            var basicSection = new IniSection(BasicSectionKey);
            basicSection.AddKey(BaseUrlKey, qmSettings.BaseUrl);
            basicSection.AddKey(LoginUrlKey, qmSettings.LoginUrl);
            basicSection.AddKey(RefreshUrlKey, qmSettings.RefreshUrl);
            basicSection.AddKey(ServerStatusUrlKey, qmSettings.ServerStatusUrl);
            basicSection.AddKey(GetUserAccountsUrlKey, qmSettings.GetUserAccountsUrl);
            basicSection.AddKey(GetLaddersUrlKey, qmSettings.GetLaddersUrl);
            basicSection.AddKey(GetLadderMapsUrlKey, qmSettings.GetLadderMapsUrl);

            iniFile.AddSection(basicSection);
            iniFile.WriteIniFile(SettingsFile);
        }
    }
}
