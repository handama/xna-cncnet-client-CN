using ClientCore;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using Localization;

namespace DTAClient.Domain.Multiplayer
{
    public class MapLoader
    {
        public const string MAP_FILE_EXTENSION = ".map";
        private string CUSTOM_MAPS_DIRECTORY = $"Maps\\{ClientConfiguration.Instance.CustomMapFolderName}";

        /// <summary>
        /// List of game modes.
        /// </summary>
        public List<GameMode> GameModes = new List<GameMode>();

        /// <summary>
        /// An event that is fired when the maps have been loaded.
        /// </summary>
        public event EventHandler MapLoadingComplete;

        /// <summary>
        /// A list of game mode aliases.
        /// Every game mode entry that exists in this dictionary will get 
        /// replaced by the game mode entries of the value string array
        /// when map is added to game mode map lists.
        /// </summary>
        private Dictionary<string, string[]> GameModeAliases = new Dictionary<string, string[]>();

        /// <summary>
        /// List of gamemodes allowed to be used on custom maps in order for them to display in map list.
        /// </summary>
        private string[] AllowedGameModes = ClientConfiguration.Instance.AllowedCustomGameModes.Split(',');

        /// <summary>
        /// Loads multiplayer map info asynchonously.
        /// </summary>
        public void LoadMapsAsync()
        {
            Thread thread = new Thread(LoadMaps);
            thread.Start();
        }
        private string[] GetMaps(string dirPath ,bool isBaseFolder, params string[] searchPatterns)
        {
            string[] IgnoreMaps = {
                "Tower.mmx",
                "Tsunami.mmx",
                "Valley.mmx",
                "xmas.mmx",
                "YuriPlot.mmx",
                "amazon.mmx",
                "Arena.mmx",
                "Barrel.mmx",
                "BayOPigs.mmx",
                "Bermuda.mmx",
                "Break.mmx",
                "Carville.mmx",
                "Deadman.mmx",
                "Death.mmx",
                "Disaster.mmx",
                "Dustbowl.mmx",
                "EB1.mmx",
                "EB2.mmx",
                "EB3.mmx",
                "EB4.mmx",
                "EB5.mmx",
                "GoldSt.mmx",
                "Grinder.mmx",
                "HailMary.mmx",
                "Hills.mmx",
                "invasion.mmx",
                "Kaliforn.mmx",
                "Killer.mmx",
                "Lostlake.mmx",
                "NewHghts.mmx",
                "Oceansid.mmx",
                "Pacific.mmx",
                "Potomac.mmx",
                "PowdrKeg.mmx",
                "Rockets.mmx",
                "Roulette.mmx",
                "Round.mmx",
                "SeaofIso.mmx",
                "Shrapnel.mmx",
                "Tanyas.mmx",
                "DeepFrze.yro",
                "HighExpR.yro",
                "Ice_Age.yro",
                "IrvineCa.yro",
                "IsleLand.yro",
                "MojoSprt.yro",
                "MonsterM.yro",
                "MoonPatr.yro",
                "RiverRam.yro",
                "SinkSwim.yro",
                "Transylv.yro",
                "Unrepent.yro",
                "CrctBrd.yro"
            };
            if (searchPatterns.Length <= 0)
            {
                return null;
            }
            else
            {
                DirectoryInfo di = new DirectoryInfo(dirPath);
                FileInfo[][] fis = new FileInfo[searchPatterns.Length][];
                int count = 0;
                for (int i = 0; i < searchPatterns.Length; i++)
                {
                    FileInfo[] fileInfos = di.GetFiles(searchPatterns[i]);
                    fis[i] = fileInfos;
                    count += fileInfos.Length;
                }
                string[] files = new string[count];
                int n = 0;
                for (int i = 0; i <= fis.GetUpperBound(0); i++)
                {
                    for (int j = 0; j < fis[i].Length; j++)
                    {
                        string name = fis[i][j].Name;
                        bool hasIgnoreMap = IgnoreMaps.Any((id) =>
                        {
                            return name.Equals(id, StringComparison.OrdinalIgnoreCase);
                        });
                        if ( !(hasIgnoreMap && isBaseFolder) )
                        {
                            string temp = fis[i][j].FullName;
                            files[n] = temp;
                            n++;
                        }
                    }
                }
                return files;
            }
        }

        /// <summary>
        /// Load maps based on INI info as well as those in the custom maps directory.
        /// </summary>
        public void LoadMaps()
        {
            Logger.Log("Loading maps.");

            IniFile mpMapsIni = new IniFile(ProgramConstants.GamePath + ClientConfiguration.Instance.MPMapsIniPath);

            var gameModes = mpMapsIni.GetSectionKeys("GameModes");

            if (gameModes != null)
            {
                foreach (string key in gameModes)
                {
                    string gameModeName = mpMapsIni.GetStringValue("GameModes", key, string.Empty);
                    if (!string.IsNullOrEmpty(gameModeName))
                    {
                        GameMode gm = new GameMode(gameModeName);
                        GameModes.Add(gm);
                    }
                }
            }

            var gmAliases = mpMapsIni.GetSectionKeys("GameModeAliases");

            if (gmAliases != null)
            {
                foreach (string key in gmAliases)
                {
                    GameModeAliases.Add(key, mpMapsIni.GetStringValue("GameModeAliases", key, string.Empty).Split(
                        new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }

            List<string> keys = mpMapsIni.GetSectionKeys("MultiMaps");

            if (keys == null)
            {
                Logger.Log("Loading multiplayer map list failed!!!");
                return;
            }

            List<Map> maps = new List<Map>();

            foreach (string key in keys)
            {
                string mapFilePath = mpMapsIni.GetStringValue("MultiMaps", key, string.Empty);

                if (!File.Exists(ProgramConstants.GamePath + mapFilePath + MAP_FILE_EXTENSION))
                {
                    Logger.Log("Map " + mapFilePath + " doesn't exist!");
                    continue;
                }

                Map map = new Map(mapFilePath, true);

                if (!map.SetInfoFromINI(mpMapsIni))
                    continue;

                maps.Add(map);
            }

            foreach (Map map in maps)
            {
                AddMapToGameModes(map, false);
            }

            List<Map> customMaps = new List<Map>();

            if (!Directory.Exists(ProgramConstants.GamePath + CUSTOM_MAPS_DIRECTORY))
            {
                Logger.Log("Custom maps directory does not exist!");
            }
            else
            {
                string[] mapsInCustomFolder = GetMaps(ProgramConstants.GamePath + CUSTOM_MAPS_DIRECTORY, false , "*.map", "*.yrm", "*.mpr", "*.mmx", "*.yro");
                string[] files = { };
                IniFile settingsIni = new IniFile(ProgramConstants.GamePath + ClientConfiguration.Instance.SettingsIniName);
                if (settingsIni.GetBooleanValue ("Options", "LoadRootFolderMaps", true))
                {
                    string[] mapsInRootFolder = GetMaps(ProgramConstants.GamePath, true, "*.map", "*.yrm", "*.mpr", "*.mmx", "*.yro");
                    files = mapsInCustomFolder.Concat(mapsInRootFolder).ToArray();
                }
                else files = mapsInCustomFolder;
                foreach (string file in files)
                {
                    if (file != null) 
                    { 
                        string baseFilePath = file.Substring(ProgramConstants.GamePath.Length);
                        string extension = baseFilePath.Substring(baseFilePath.Length - 4);
                        baseFilePath = baseFilePath.Substring(0, baseFilePath.Length - 4);
                        
                        Map map = new Map(baseFilePath, false);
                        map.MapExtension(extension);
                        if (map.SetInfoFromMap(file))
                            customMaps.Add(map);
                    }
                }
            }

            foreach (Map map in customMaps)
            {
                AddMapToGameModes(map, false);
            }

            GameModes.RemoveAll(g => g.Maps.Count < 1);

            MapLoadingComplete?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Attempts to load a custom map.
        /// </summary>
        /// <param name="mapPath">The path to the map file relative to the game directory.</param>
        /// <param name="resultMessage">When method returns, contains a message reporting whether or not loading the map failed and how.</param>
        /// <returns>The map if loading it was succesful, otherwise false.</returns>
        public Map LoadCustomMap(string mapPath, out string resultMessage)
        {
            if (!File.Exists(ProgramConstants.GamePath + mapPath + MAP_FILE_EXTENSION))
            {
                Logger.Log("LoadCustomMap: Map " + mapPath + MAP_FILE_EXTENSION +" not found!");
                resultMessage = $"地图文件 {mapPath}{MAP_FILE_EXTENSION}不存在！";

                return null;
            }

            Logger.Log("LoadCustomMap: Loading custom map " + mapPath);
            Map map = new Map(mapPath, false);

            if (map.SetInfoFromMap(ProgramConstants.GamePath + mapPath + MAP_FILE_EXTENSION))
            {
                foreach (GameMode gm in GameModes)
                {
                    if (gm.Maps.Find(m => m.SHA1 == map.SHA1) != null)
                    {
                        Logger.Log("LoadCustomMap: Custom map " + mapPath + " is already loaded!");
                        resultMessage = $"地图 {mapPath}{MAP_FILE_EXTENSION} 已经被加载。";

                        return null;
                    }
                }

                Logger.Log("LoadCustomMap: Map " + mapPath + " added succesfully.");

                AddMapToGameModes(map, true);

                resultMessage = $"地图 {mapPath}{MAP_FILE_EXTENSION} 成功加载。";

                return map;
            }

            Logger.Log("LoadCustomMap: Loading map " + mapPath + " failed!");
            resultMessage = $"加载地图 {mapPath}{MAP_FILE_EXTENSION} 失败！";

            return null;
        }
        public Map LoadCustomMapManual(string mapPath, out string resultMessage)
        {
            if (!File.Exists(ProgramConstants.GamePath + mapPath))
            {
                Logger.Log("LoadCustomMap: Map " + mapPath + " not found!");
                resultMessage = $"地图文件 {mapPath} 不存在！";

                return null;
            }

            Logger.Log("LoadCustomMap: Loading custom map " + mapPath);
            string mapPath2 = mapPath.Substring(0, mapPath.Length - 4);
            Map map = new Map(mapPath2, false);
            map.MapExtension(mapPath.Substring(mapPath.Length - 4));

            if (map.SetInfoFromMap(ProgramConstants.GamePath + mapPath))
            {
                foreach (GameMode gm in GameModes)
                {
                    if (gm.Maps.Find(m => m.SHA1 == map.SHA1) != null)
                    {
                        Logger.Log("LoadCustomMap: Custom map " + mapPath + " is already loaded!");
                        resultMessage = $"地图 {mapPath} 已经被加载。";

                        return null;
                    }
                }

                Logger.Log("LoadCustomMap: Map " + mapPath + " added succesfully.");

                AddMapToGameModes(map, true);

                resultMessage = $"地图 {mapPath} 成功加载。";

                return map;
            }

            Logger.Log("LoadCustomMap: Loading map " + mapPath + " failed!");
            resultMessage = $"加载地图 {mapPath} 失败！";

            return null;
        }
        /// <summary>
        /// Adds map to all eligible game modes.
        /// </summary>
        /// <param name="map">Map to add.</param>
        /// <param name="enableLogging">If set to true, a message for each game mode the map is added to is output to the log file.</param>
        private void AddMapToGameModes(Map map, bool enableLogging)
        {
            foreach (string gameMode in map.GameModes)
            {
                if (!GameModeAliases.TryGetValue(gameMode, out string[] gameModeAliases))
                    gameModeAliases = new string[] { gameMode };

                foreach (string gameModeAlias in gameModeAliases)
                {
                    if (!map.Official && !(AllowedGameModes.Contains(gameMode) || AllowedGameModes.Contains(gameModeAlias)))
                        continue;

                    GameMode gm = GameModes.Find(g => g.Name == gameModeAlias);
                    if (gm == null)
                    {
                        gm = new GameMode(gameModeAlias);
                        GameModes.Add(gm);
                    }

                    gm.Maps.Add(map);
                    if (enableLogging)
                        Logger.Log("AddMapToGameModes: Added map " + map.Name + " to game mode " + gm.Name);
                }
            }
        }

        public void WriteCustomMapCache()
        {

        }
    }
}
