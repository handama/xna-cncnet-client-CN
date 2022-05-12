using System;
using System.IO;
using ClientCore;
using Newtonsoft.Json;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QuickMatchSettingsService
    {
        private static readonly string SettingsFile = $"{ProgramConstants.ClientUserFilesPath}QuickMatchSettings.ini";

        private const string BasicSectionKey = "Basic";
        private const string AuthDataKey = "AuthData";
        private const string EmailKey = "Email";
        private const string LadderKey = "Ladder";

        private QmSettings qmSettings;

        public QmSettings LoadSettings()
        {
            if (qmSettings != null)
                return qmSettings;

            qmSettings = new QmSettings();
            if (!File.Exists(SettingsFile))
                return qmSettings;

            var iniFile = new IniFile(SettingsFile);
            var basicSection = iniFile.GetSection(BasicSectionKey);
            if (basicSection == null)
                return qmSettings;

            qmSettings.AuthData = GetAuthData(basicSection);
            qmSettings.Email = basicSection.GetStringValue(EmailKey, null);
            qmSettings.Ladder = basicSection.GetStringValue(LadderKey, null);

            return qmSettings;
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

        public void ClearAuthData() => qmSettings.AuthData = null;

        public void SaveSettings()
        {
            var iniFile = new IniFile();
            var basicSection = new IniSection(BasicSectionKey);
            basicSection.AddKey(EmailKey, qmSettings.Email ?? string.Empty);
            basicSection.AddKey(LadderKey, qmSettings.Ladder ?? string.Empty);
            basicSection.AddKey(AuthDataKey, JsonConvert.SerializeObject(qmSettings.AuthData) ?? string.Empty);

            iniFile.AddSection(basicSection);
            iniFile.WriteIniFile(SettingsFile);
        }
    }
}
