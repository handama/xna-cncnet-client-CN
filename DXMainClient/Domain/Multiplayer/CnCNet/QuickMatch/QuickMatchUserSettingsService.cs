using System;
using System.IO;
using ClientCore;
using Newtonsoft.Json;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QuickMatchUserSettingsService
    {
        private static readonly string SettingsFile = $"{ProgramConstants.ClientUserFilesPath}QuickMatchSettings.ini";

        private const string BasicSectionKey = "Basic";
        private const string AuthDataKey = "AuthData";
        private const string EmailKey = "Email";
        private const string LadderKey = "Ladder";

        private QmUserSettings qmUserSettings;

        public QmUserSettings LoadSettings()
        {
            if (qmUserSettings != null)
                return qmUserSettings;

            qmUserSettings = new QmUserSettings();
            if (!File.Exists(SettingsFile))
                return qmUserSettings;

            var iniFile = new IniFile(SettingsFile);
            var basicSection = iniFile.GetSection(BasicSectionKey);
            if (basicSection == null)
                return qmUserSettings;

            qmUserSettings.AuthData = GetAuthData(basicSection);
            qmUserSettings.Email = basicSection.GetStringValue(EmailKey, null);
            qmUserSettings.Ladder = basicSection.GetStringValue(LadderKey, null);

            return qmUserSettings;
        }

        private static QmAuthData GetAuthData(IniSection section)
        {
            if (!section.KeyExists(AuthDataKey))
                return null;

            string authDataValue = section.GetStringValue(AuthDataKey, null);
            if (string.IsNullOrEmpty(authDataValue))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<QmAuthData>(authDataValue);
            }
            catch (Exception e)
            {
                Logger.Log(e.StackTrace);
                return null;
            }
        }

        public void ClearAuthData() => qmUserSettings.AuthData = null;

        public void SaveSettings()
        {
            var iniFile = new IniFile();
            var basicSection = new IniSection(BasicSectionKey);
            basicSection.AddKey(EmailKey, qmUserSettings.Email ?? string.Empty);
            basicSection.AddKey(LadderKey, qmUserSettings.Ladder ?? string.Empty);
            basicSection.AddKey(AuthDataKey, JsonConvert.SerializeObject(qmUserSettings.AuthData) ?? string.Empty);

            iniFile.AddSection(basicSection);
            iniFile.WriteIniFile(SettingsFile);
        }
    }
}
