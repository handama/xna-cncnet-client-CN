﻿using ClientCore;
using ClientCore.Statistics;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;


namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A generic base for all game lobbies (Skirmish, LAN and CnCNet).
    /// Contains the common logic for parsing game options and handling player info.
    /// </summary>
    public abstract class GameLobbyBase : XNAWindow
    {
        protected const int MAX_PLAYER_COUNT = 8;
        protected const int PLAYER_OPTION_VERTICAL_MARGIN = 12;
        protected const int PLAYER_OPTION_HORIZONTAL_MARGIN = 3;
        protected const int PLAYER_OPTION_CAPTION_Y = 6;
        private const int DROP_DOWN_HEIGHT = 21;

        private const int RANK_NONE = 0;
        private const int RANK_EASY = 1;
        private const int RANK_MEDIUM = 2;
        private const int RANK_HARD = 3;

        public string ra2ModePath = ProgramConstants.GetBaseResourcePath() + "ra2mode\\";
        public List<string> ra2ModefilesFullName = new List<string>();
        public List<string> ra2ModefilesName = new List<string>();


        public void CreateRa2FileList()
        {
            if (Directory.Exists(ra2ModePath))
            {
                DirectoryInfo folder = new DirectoryInfo(ra2ModePath);
                foreach (FileInfo file in folder.GetFiles("*"))
                {
                    ra2ModefilesFullName.Add(file.FullName);
                    ra2ModefilesName.Add(file.Name);
                }
            }
        }
        public void CopyRa2Files()
        {
            int i = 0;
            for (; i < ra2ModefilesName.Count;)
            {
                if (File.Exists(ra2ModefilesFullName[i]))
                {
                    File.Copy(ra2ModefilesFullName[i], ProgramConstants.GamePath + ra2ModefilesName[i], true);
                }
                i++;
            }
        }
        public void DeleteRa2Files()
        {
            int i = 0;
            for (; i < ra2ModefilesName.Count;)
            {
                if (File.Exists(ProgramConstants.GamePath + ra2ModefilesName[i]))
                {
                    File.Delete(ProgramConstants.GamePath + ra2ModefilesName[i]);
                }
                i++;
            }
        }
        /// <summary>
        /// Creates a new instance of the game lobby base.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="iniName">The name of the lobby in GameOptions.ini.</param>
        public GameLobbyBase(WindowManager windowManager, string iniName,
            List<GameMode> GameModes, bool isMultiplayer, DiscordHandler discordHandler) : base(windowManager)
        {
            _iniSectionName = iniName;
            this.GameModes = GameModes;
            this.isMultiplayer = isMultiplayer;
            this.discordHandler = discordHandler;
        }

        private string _iniSectionName;

        protected XNAPanel PlayerOptionsPanel;

        protected XNAPanel GameOptionsPanel;

        protected List<MultiplayerColor> MPColors;

        protected List<GameLobbyCheckBox> CheckBoxes = new List<GameLobbyCheckBox>();
        protected List<GameLobbyDropDown> DropDowns = new List<GameLobbyDropDown>();

        

        protected DiscordHandler discordHandler;

        /// <summary>
        /// The list of multiplayer game modes.
        /// </summary>
        protected List<GameMode> GameModes;

        private GameMode gameMode;

        /// <summary>
        /// The currently selected game mode.
        /// </summary>
        protected GameMode GameMode
        {
            get => gameMode;
            set
            {
                var oldGameMode = gameMode;
                gameMode = value;
                if (value != null && oldGameMode != value)
                    UpdateDiscordPresence();
            }
        }

        private Map map;

        /// <summary>
        /// The currently selected map.
        /// </summary>
        protected Map Map
        {
            get => map;
            set
            {
                var oldMap = map;
                map = value;
                if (value != null && oldMap != value)
                    UpdateDiscordPresence();
            }
        }

        protected XNAClientDropDown[] ddPlayerNames;
        protected XNAClientDropDown[] ddPlayerSides;
        protected XNAClientDropDown[] ddPlayerColors;
        protected XNAClientDropDown[] ddPlayerStarts;
        protected XNAClientDropDown[] ddPlayerTeams;

        protected XNALabel lblName;
        protected XNALabel lblSide;
        protected XNALabel lblColor;
        protected XNALabel lblStart;
        protected XNALabel lblTeam;

        protected XNAClientButton btnLeaveGame;
        protected GameLaunchButton btnLaunchGame;
        protected XNAClientButton btnPickRandomMap;
        protected XNALabel lblMapName;
        protected XNALabel lblMapAuthor;
        protected XNALabel lblGameMode;
        protected XNALabel lblMapSize;

        protected MapPreviewBox MapPreviewBox;

        protected XNAMultiColumnListBox lbMapList;
        protected XNAClientDropDown ddGameMode;
        protected XNALabel lblGameModeSelect;
        protected XNAContextMenu mapContextMenu;

        protected XNASuggestionTextBox tbMapSearch;

        protected XNAClientDropDown ddplayerNumbers;
        protected XNAClientDropDown ddAuthor;

        protected List<PlayerInfo> Players = new List<PlayerInfo>();
        protected List<PlayerInfo> AIPlayers = new List<PlayerInfo>();

        protected XNAClientButton btnCreateRandomMap;
        protected RandomMapWindow randomMapWindow;


        protected virtual PlayerInfo FindLocalPlayer() => Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
        protected bool PlayerUpdatingInProgress { get; set; }

        protected Texture2D[] RankTextures;

        /// <summary>
        /// The seed used for randomizing player options.
        /// </summary>
        protected int RandomSeed { get; set; }

        /// <summary>
        /// An unique identifier for this game.
        /// </summary>
        protected int UniqueGameID { get; set; }
        protected int SideCount { get; private set; }
        protected int RandomSelectorCount { get; private set; } = 1;

        protected List<int[]> RandomSelectors = new List<int[]>();

        protected string RandomMapName;


#if YR
        /// <summary>
        /// Controls whether Red Alert 2 mode is enabled for CnCNet YR. 
        /// </summary>
        protected bool RA2Mode = false;

        protected bool LadderMode = false;
#endif

        private readonly bool isMultiplayer = false;

        private MatchStatistics matchStatistics;

        private bool disableGameOptionUpdateBroadcast = false;
        protected XNALabel lblPlayerNumbers;
        protected XNALabel lblAuthor;

        /// <summary>
        /// If set, the client will remove all starting waypoints from the map
        /// before launching it.
        /// </summary>
        protected bool RemoveStartingLocations { get; set; } = false;
        protected IniFile GameOptionsIni { get; private set; }

        public override void Initialize()
        {
            Name = _iniSectionName;
            //if (WindowManager.RenderResolutionY < 800)
            //    ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY);
            //else
            ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX - 60, WindowManager.RenderResolutionY - 32);
            WindowManager.CenterControlOnScreen(this);
            BackgroundTexture = AssetLoader.LoadTexture("gamelobbybg.png");

            RankTextures = new Texture2D[4]
            {
                AssetLoader.LoadTexture("rankNone.png"),
                AssetLoader.LoadTexture("rankEasy.png"),
                AssetLoader.LoadTexture("rankNormal.png"),
                AssetLoader.LoadTexture("rankHard.png")
            };

            MPColors = MultiplayerColor.LoadColors();

            GameOptionsIni = new IniFile(ProgramConstants.GetBaseResourcePath() + "GameOptions.ini");

            GameOptionsPanel = new XNAPanel(WindowManager);
            GameOptionsPanel.Name = "GameOptionsPanel";
            GameOptionsPanel.ClientRectangle = new Rectangle(Width - 411, 12, 399, 289);
            GameOptionsPanel.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 192), 1, 1);
            GameOptionsPanel.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            PlayerOptionsPanel = new XNAPanel(WindowManager);
            PlayerOptionsPanel.Name = "PlayerOptionsPanel";
            PlayerOptionsPanel.ClientRectangle = new Rectangle(GameOptionsPanel.X - 401, 12, 395, GameOptionsPanel.Height);
            PlayerOptionsPanel.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 192), 1, 1);
            PlayerOptionsPanel.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            btnLeaveGame = new XNAClientButton(WindowManager);
            btnLeaveGame.Name = "btnLeaveGame";
            btnLeaveGame.ClientRectangle = new Rectangle(Width - 143, Height - 28, 133, 23);
            btnLeaveGame.Text = "离开游戏";
            btnLeaveGame.LeftClick += BtnLeaveGame_LeftClick;

            btnLaunchGame = new GameLaunchButton(WindowManager, RankTextures);
            btnLaunchGame.Name = "btnLaunchGame";
            btnLaunchGame.ClientRectangle = new Rectangle(12, btnLeaveGame.Y, 133, 23);
            btnLaunchGame.Text = "进入游戏";
            btnLaunchGame.LeftClick += BtnLaunchGame_LeftClick;

            MapPreviewBox = new MapPreviewBox(WindowManager, Players, AIPlayers, MPColors,
                GameOptionsIni.GetStringValue("General", "Sides", String.Empty).Split(','),
                GameOptionsIni);
            MapPreviewBox.Name = "MapPreviewBox";
            MapPreviewBox.ClientRectangle = new Rectangle(PlayerOptionsPanel.X,
                PlayerOptionsPanel.Bottom + 6,
                GameOptionsPanel.Right - PlayerOptionsPanel.X,
                Height - PlayerOptionsPanel.Bottom - 65);
            MapPreviewBox.FontIndex = 1;
            MapPreviewBox.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            MapPreviewBox.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);

            lblMapName = new XNALabel(WindowManager);
            lblMapName.Name = "lblMapName";
            lblMapName.ClientRectangle = new Rectangle(MapPreviewBox.X,
                MapPreviewBox.Bottom + 4, 0, 0);
            lblMapName.FontIndex = 1;
            lblMapName.Text = "地图：";

            lblMapAuthor = new XNALabel(WindowManager);
            lblMapAuthor.Name = "lblMapAuthor";
            lblMapAuthor.ClientRectangle = new Rectangle(MapPreviewBox.Right,
                lblMapName.Y, 0, 0);
            lblMapAuthor.FontIndex = 1;
            lblMapAuthor.Text = "作者：";

            lblGameMode = new XNALabel(WindowManager);
            lblGameMode.Name = "lblGameMode";
            lblGameMode.ClientRectangle = new Rectangle(lblMapName.X,
                lblMapName.Bottom + 3, 0, 0);
            lblGameMode.FontIndex = 1;
            lblGameMode.Text = "游戏模式：";

            lblMapSize = new XNALabel(WindowManager);
            lblMapSize.Name = "lblMapSize";
            lblMapSize.ClientRectangle = new Rectangle(lblMapName.X,
                lblGameMode.Bottom + 3, 0, 0);
            lblMapSize.FontIndex = 1;
            lblMapSize.Text = "地图大小：";
            lblMapSize.Visible = false;

            lbMapList = new XNAMultiColumnListBox(WindowManager);
            lbMapList.Name = "lbMapList";
            lbMapList.ClientRectangle = new Rectangle(btnLaunchGame.X, GameOptionsPanel.Y + 23,
                MapPreviewBox.X - btnLaunchGame.X - 6,
                MapPreviewBox.Bottom - 23 - GameOptionsPanel.Y);
            lbMapList.SelectedIndexChanged += LbMapList_SelectedIndexChanged;
            lbMapList.RightClick += LbMapList_RightClick;
            lbMapList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbMapList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 192), 1, 1);
            lbMapList.LineHeight = 16;
            lbMapList.DrawListBoxBorders = true;
            lbMapList.AllowKeyboardInput = true;
            lbMapList.AllowRightClickUnselect = false;

            mapContextMenu = new XNAContextMenu(WindowManager);
            mapContextMenu.Name = nameof(mapContextMenu);
            mapContextMenu.Width = 100;
            mapContextMenu.AddItem("删除地图", DeleteMapConfirmation, () => Map != null && !Map.Official);
            AddChild(mapContextMenu);

            XNAPanel rankHeader = new XNAPanel(WindowManager);
            rankHeader.BackgroundTexture = AssetLoader.LoadTexture("rank.png");
            rankHeader.ClientRectangle = new Rectangle(0, 0, rankHeader.BackgroundTexture.Width,
                19);

            XNAListBox rankListBox = new XNAListBox(WindowManager);
            rankListBox.TextBorderDistance = 2;

            lbMapList.AddColumn(rankHeader, rankListBox);

            lbMapList.AddColumn("地图名", lbMapList.Width - RankTextures[1].Width - 3);

            ddGameMode = new XNAClientDropDown(WindowManager);
            ddGameMode.Name = "ddGameMode";
            ddGameMode.ClientRectangle = new Rectangle(lbMapList.Right - 150, GameOptionsPanel.Y, 150, 21);
            ddGameMode.SelectedIndexChanged += DdGameMode_SelectedIndexChanged;

            foreach (GameMode gm in GameModes)
                ddGameMode.AddItem(gm.UIName);

            lblGameModeSelect = new XNALabel(WindowManager);
            lblGameModeSelect.Name = "lblGameModeSelect";
            lblGameModeSelect.ClientRectangle = new Rectangle(lbMapList.X, ddGameMode.Y + 2, 0, 0);
            lblGameModeSelect.FontIndex = 1;
            lblGameModeSelect.Text = "游戏模式";

            tbMapSearch = new XNASuggestionTextBox(WindowManager);
            tbMapSearch.Name = "tbMapSearch";
            tbMapSearch.ClientRectangle = new Rectangle(lbMapList.X,
                lbMapList.Bottom + 3, lbMapList.Width /2 -1, 21);
            tbMapSearch.Suggestion = "搜索地图...";
            tbMapSearch.MaximumTextLength = 64;
            tbMapSearch.InputReceived += TbMapSearch_InputReceived;

            btnPickRandomMap = new XNAClientButton(WindowManager);
            btnPickRandomMap.Name = "btnPickRandomMap";
            btnPickRandomMap.ClientRectangle = new Rectangle(btnLaunchGame.Right + 157 , btnLaunchGame.Y, 133, 23);
            btnPickRandomMap.Text = "随机选择地图";
            btnPickRandomMap.LeftClick += BtnPickRandomMap_LeftClick;
            btnPickRandomMap.Disable();

            lblPlayerNumbers = new XNALabel(WindowManager);
            lblPlayerNumbers.Name = "lblPlayerNumbers";
            lblPlayerNumbers.ClientRectangle = new Rectangle(lbMapList.X, ddGameMode.Y + 2, 0, 0);
            lblPlayerNumbers.FontIndex = 1;
            lblPlayerNumbers.Text = "玩家数量";

            lblAuthor = new XNALabel(WindowManager);
            lblAuthor.Name = "lblAuthor";
            lblAuthor.ClientRectangle = new Rectangle(lbMapList.X, ddGameMode.Y + 2, 0, 0);
            lblAuthor.FontIndex = 1;
            lblAuthor.Text = "作者";

            ddplayerNumbers = new XNAClientDropDown(WindowManager);
            ddplayerNumbers.Name = "ddplayerNumbers";
            ddplayerNumbers.ClientRectangle = new Rectangle(lbMapList.X + lbMapList.Width / 2 + 1,
                lbMapList.Bottom + 3, lbMapList.Width / 2 - 1, 21);
            ddplayerNumbers.AddItem("-");
            ddplayerNumbers.AddItem("2");
            ddplayerNumbers.AddItem("3");
            ddplayerNumbers.AddItem("4");
            ddplayerNumbers.AddItem("5");
            ddplayerNumbers.AddItem("6");
            ddplayerNumbers.AddItem("7");
            ddplayerNumbers.AddItem("8");
            ddplayerNumbers.AllowDropDown = true;
            ddplayerNumbers.SelectedIndex = 0;
            ddplayerNumbers.SelectedIndexChanged += MapScreenActived;
            ddplayerNumbers.Tag = true;


            btnCreateRandomMap = new XNAClientButton(WindowManager);
            btnCreateRandomMap.Name = "btnCreateRandomMap";
            btnCreateRandomMap.ClientRectangle = new Rectangle(btnLeaveGame.Right - btnLeaveGame.Width - 145,
                btnLeaveGame.Y, 133, 23);
            btnCreateRandomMap.Text = "创建随机地图";
            btnCreateRandomMap.LeftClick += BtnCreateRandomMap_LeftClick;
            AddChild(btnCreateRandomMap);
            btnCreateRandomMap.Visible = false;

            randomMapWindow = new RandomMapWindow(WindowManager);
            randomMapWindow.Initialize();
            randomMapWindow.DrawOrder = 1;
            randomMapWindow.UpdateOrder = 1;
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, randomMapWindow);
            randomMapWindow.CenterOnParent();
            randomMapWindow.Disable();
           

            AddChild(ddplayerNumbers);
            

            AddChild(lblMapName);
            AddChild(lblPlayerNumbers);
            AddChild(lblAuthor);
            AddChild(lblMapAuthor);
            AddChild(lblGameMode);
            AddChild(lblMapSize);
            AddChild(MapPreviewBox);

            AddChild(lbMapList);
            AddChild(tbMapSearch);
            AddChild(lblGameModeSelect);
            AddChild(ddGameMode);

            AddChild(GameOptionsPanel);

            string[] checkBoxes = GameOptionsIni.GetStringValue(_iniSectionName, "CheckBoxes", String.Empty).Split(',');

            foreach (string chkName in checkBoxes)
            {
                GameLobbyCheckBox chkBox = new GameLobbyCheckBox(WindowManager);
                chkBox.Name = chkName;
                AddChild(chkBox);
                chkBox.GetAttributes(GameOptionsIni);
                CheckBoxes.Add(chkBox);
                chkBox.CheckedChanged += ChkBox_CheckedChanged;
            }

            string[] labels = GameOptionsIni.GetStringValue(_iniSectionName, "Labels", String.Empty).Split(',');

            foreach (string labelName in labels)
            {
                XNALabel label = new XNALabel(WindowManager);
                label.Name = labelName;
                AddChild(label);
                label.GetAttributes(GameOptionsIni);
            }

            string[] dropDowns = GameOptionsIni.GetStringValue(_iniSectionName, "DropDowns", String.Empty).Split(',');

            foreach (string ddName in dropDowns)
            {
                GameLobbyDropDown dropdown = new GameLobbyDropDown(WindowManager);
                dropdown.Name = ddName;
                AddChild(dropdown);
                dropdown.GetAttributes(GameOptionsIni);
                DropDowns.Add(dropdown);
                dropdown.SelectedIndexChanged += Dropdown_SelectedIndexChanged;
            }

            ddAuthor = new XNAClientDropDown(WindowManager);
            ddAuthor.Name = "ddAuthor";
            ddAuthor.ClientRectangle = new Rectangle(lbMapList.X + lbMapList.Width / 2 + 1,
                lbMapList.Bottom + 3, lbMapList.Width / 2 - 1, 21);
           
            var mpMapsIni = new IniFile(ProgramConstants.GamePath + ClientConfiguration.Instance.MPMapsIniPath);
            List<string> authorListIndex = mpMapsIni.GetSectionKeys("AuthorList");
            List<string> authorList = new List<string>();
            foreach (string index in authorListIndex)
            {
                authorList.Add(mpMapsIni.GetStringValue("AuthorList", index, ""));
            }

            ddAuthor.AddItem("-");
            foreach (string author in authorList)
            {
                ddAuthor.AddItem(author);
            }
            ddAuthor.SelectedIndex = 0;
            ddAuthor.AllowDropDown = true;
            ddAuthor.SelectedIndexChanged += MapScreenActived;
            ddAuthor.Tag = true;

            AddChild(ddAuthor);
            AddChild(PlayerOptionsPanel);
            AddChild(btnLaunchGame);
            AddChild(btnLeaveGame);
            AddChild(btnPickRandomMap);
        }

        private void BtnCreateRandomMap_LeftClick(object sender, EventArgs e) => ShowRandomMapWindow();

        private void ShowRandomMapWindow()
        {
            randomMapWindow.Open();
        }

        private void BtnPickRandomMap_LeftClick(object sender, EventArgs e)
        {
            PickRandomMap();
        }

        private void TbMapSearch_InputReceived(object sender, EventArgs e)
        {
            ListMaps();
        }

        private void MapScreenActived(object sender, EventArgs e)
        {
            ListMaps();
        }

        private void Dropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (disableGameOptionUpdateBroadcast)
                return;

            var dd = (GameLobbyDropDown)sender;
            dd.HostSelectedIndex = dd.SelectedIndex;
            OnGameOptionChanged();
        }

        private void ChkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (disableGameOptionUpdateBroadcast)
                return;

            var checkBox = (GameLobbyCheckBox)sender;
            checkBox.HostChecked = checkBox.Checked;
            OnGameOptionChanged();
        }

        /// <summary>
        /// Initializes the underlying window class.
        /// </summary>
        protected void InitializeWindow()
        {
            base.Initialize();
        }

        protected virtual void OnGameOptionChanged()
        {
            CheckDisallowedSides();

            btnLaunchGame.SetRank(GetRank());
        }

        protected void DdGameMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            GameMode = GameModes[ddGameMode.SelectedIndex];

            tbMapSearch.Text = string.Empty;
            tbMapSearch.OnSelectedChanged();
            ddplayerNumbers.OnSelectedChanged();

            ListMaps();

            if (lbMapList.SelectedIndex == -1)
                lbMapList.SelectedIndex = 0; // Select default map
            else
                ChangeMap(GameMode, Map);
        }

        protected void ListMaps()
        {
            lbMapList.SelectedIndexChanged -= LbMapList_SelectedIndexChanged;

            lbMapList.ClearItems();
            lbMapList.SetTopIndex(0);

            lbMapList.SelectedIndex = -1;

            int mapIndex = -1;
            int skippedMapsCount = 0;

            for (int i = 0; i < GameMode.Maps.Count; i++)
            {
                if (tbMapSearch.Text != tbMapSearch.Suggestion)
                {
                    if (!GameMode.Maps[i].Name.ToUpper().Contains(tbMapSearch.Text.ToUpper()))
                    {
                        skippedMapsCount++;
                        continue;
                    }
                }

                if (!ddplayerNumbers.SelectedItem.Text.Contains("-"))
                {
                    if (GameMode.Maps[i].MaxPlayers != int.Parse(ddplayerNumbers.SelectedItem.Text))
                    {
                        skippedMapsCount++;
                        continue;
                    }
                }
                if (!ddAuthor.SelectedItem.Text.Contains("-"))
                {
                    if (!GameMode.Maps[i].Author.Contains(ddAuthor.SelectedItem.Text))
                    {
                        skippedMapsCount++;
                        continue;
                    }
                }

                XNAListBoxItem rankItem = new XNAListBoxItem();
                if (GameMode.Maps[i].IsCoop)
                {
                    if (StatisticsManager.Instance.HasBeatCoOpMap(GameMode.Maps[i].Name, GameMode.UIName))
                        rankItem.Texture = RankTextures[Math.Abs(2 - GameMode.CoopDifficultyLevel) + 1];
                    else
                        rankItem.Texture = RankTextures[0];
                }
                else
                    rankItem.Texture = RankTextures[GetDefaultMapRankIndex(GameMode.Maps[i]) + 1];

                XNAListBoxItem mapNameItem = new XNAListBoxItem();
                mapNameItem.Text = Renderer.GetSafeString(GameMode.Maps[i].Name, lbMapList.FontIndex);
                if ((GameMode.Maps[i].MultiplayerOnly || GameMode.MultiplayerOnly) && !isMultiplayer)
                    mapNameItem.TextColor = UISettings.ActiveSettings.DisabledItemColor;
                mapNameItem.Tag = GameMode.Maps[i];

                XNAListBoxItem[] mapInfoArray = new XNAListBoxItem[]
                {
                    rankItem,
                    mapNameItem,
                };

                lbMapList.AddItem(mapInfoArray);

                if (GameMode.Maps[i] == Map)
                    mapIndex = i - skippedMapsCount;
            }

            if (mapIndex > -1)
            {
                lbMapList.SelectedIndex = mapIndex;
                while (mapIndex > lbMapList.LastIndex)
                    lbMapList.TopIndex++;
            }

            lbMapList.SelectedIndexChanged += LbMapList_SelectedIndexChanged;
        }

        protected void ListMapsGuest()
        {

            lbMapList.ClearItems();
            lbMapList.SetTopIndex(0);

            lbMapList.SelectedIndex = -1;

            int mapIndex = -1;
            int skippedMapsCount = 0;

            for (int i = 0; i < GameMode.Maps.Count; i++)
            {

                XNAListBoxItem rankItem = new XNAListBoxItem();
                if (GameMode.Maps[i].IsCoop)
                {
                    if (StatisticsManager.Instance.HasBeatCoOpMap(GameMode.Maps[i].Name, GameMode.UIName))
                        rankItem.Texture = RankTextures[Math.Abs(2 - GameMode.CoopDifficultyLevel) + 1];
                    else
                        rankItem.Texture = RankTextures[0];
                }
                else
                    rankItem.Texture = RankTextures[GetDefaultMapRankIndex(GameMode.Maps[i]) + 1];

                XNAListBoxItem mapNameItem = new XNAListBoxItem();
                mapNameItem.Text = Renderer.GetSafeString(GameMode.Maps[i].Name, lbMapList.FontIndex);
                if ((GameMode.Maps[i].MultiplayerOnly || GameMode.MultiplayerOnly) && !isMultiplayer)
                    mapNameItem.TextColor = UISettings.ActiveSettings.DisabledItemColor;
                mapNameItem.Tag = GameMode.Maps[i];

                XNAListBoxItem[] mapInfoArray = new XNAListBoxItem[]
                {
                    rankItem,
                    mapNameItem,
                };

                lbMapList.AddItem(mapInfoArray);

                if (GameMode.Maps[i] == Map)
                    mapIndex = i - skippedMapsCount;
            }

            if (mapIndex > -1)
            {
                lbMapList.SelectedIndex = mapIndex;
                while (mapIndex > lbMapList.LastIndex)
                    lbMapList.TopIndex++;
            }
        }

        protected abstract int GetDefaultMapRankIndex(Map map);

        private void LbMapList_RightClick(object sender, EventArgs e)
        {
            if (isMultiplayer || lbMapList.SelectedIndex < 0 || lbMapList.SelectedIndex >= lbMapList.ItemCount)
                return;

            mapContextMenu.Open(GetCursorPoint());
        }

        private void DeleteMapConfirmation()
        {
            if (Map == null)
                return;

            var messageBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "删除确认",
                "你确认要删除非官方地图： \"" + Map.Name + "\"?");
            messageBox.YesClickedAction = DeleteSelectedMap;
        }

        private void DeleteSelectedMap(XNAMessageBox messageBox)
        {
            try
            {
                Logger.Log("Deleting map " + Map.BaseFilePath);
                File.Delete(Map.CompleteFilePath);
                foreach (GameMode gameMode in GameModes)
                {
                    gameMode.Maps.Remove(Map);
                }

                tbMapSearch.Text = string.Empty;
                GameMode newGameMode = GameMode;
                if (newGameMode.Maps.Count == 0)
                    newGameMode = GameModes.Find(gm => gm.Maps.Count > 0);

                Map = newGameMode?.Maps[0];

                ListMaps();
                ChangeMap(newGameMode, Map);
            }
            catch (IOException ex)
            {
                Logger.Log($"Deleting map {Map.BaseFilePath} failed! Message: {ex.Message}");
                XNAMessageBox.Show(WindowManager, "地图删除失败", "地图删除失败！原因：" + ex.Message);
            }
        }

        private void LbMapList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbMapList.SelectedIndex < 0 || lbMapList.SelectedIndex >= lbMapList.ItemCount)
                return;

            XNAListBoxItem item = lbMapList.GetItem(1, lbMapList.SelectedIndex);

            Map map = (Map)item.Tag;

            ChangeMap(GameMode, map);
        }

        private void PickRandomMap()
        {
            int totalPlayerCount = Players.Count(p => p.SideId < ddPlayerSides[0].Items.Count - 1)
                   + AIPlayers.Count;
            List<Map> maps = GetMapList(totalPlayerCount);
            if (maps.Count < 1)
                return;

            int random = new Random().Next(0, maps.Count);
            Map = maps[random];

            Logger.Log("PickRandomMap: Rolled " + random + " out of " + maps.Count + ". Picked map: " + Map.Name);

            ChangeMap(GameMode, Map);
            //tbMapSearch.Text = string.Empty;
            //tbMapSearch.OnSelectedChanged();
            
            ListMaps();
        }

        public virtual void BtnGenerateMap_LeftClick(object sender, EventArgs e)
        {
            var random = new Random();
            RandomMapName = "Random Map 01";
            int i = 1;
            while (File.Exists($"Maps\\{ClientConfiguration.Instance.CustomMapFolderName}\\{RandomMapName}.map"))
            {
                RandomMapName = "Random Map" + string.Format(" {0:D2}", i);
                i++;
            }

            int width = 0;
            int height = 0;
            int totalPlayer = 0;

            foreach (var player in randomMapWindow.ddPlayers)
            {
                totalPlayer += player.SelectedIndex;
            }

            string mapType = (string)randomMapWindow.ddMapMode.SelectedItem.Tag;

            var settings1 = new IniFile(RandomMapWindow.GeneratorPath + "settings.ini");
            var workFolder = settings1.GetStringValue("settings", "WorkingFolder", "MapUnits");
            workFolder = workFolder.EndsWith("\\") ? workFolder : workFolder + "\\";
            var mapModeFolder = RandomMapWindow.GeneratorPath + workFolder + mapType + "\\";

            var settings = new IniFile(mapModeFolder + "settings.ini"); 
            int GiganticMapSideLengthMin = int.Parse(settings.GetStringValue("settings", "GiganticMapSideLength", "95,110").Split(',')[0]);
            int GiganticMapSideLengthMax = int.Parse(settings.GetStringValue("settings", "GiganticMapSideLength", "95,110").Split(',')[1]);
            int BigMapSideLengthMin = int.Parse(settings.GetStringValue("settings", "BigMapSideLength", "80,100").Split(',')[0]);
            int BigMapSideLengthMax = int.Parse(settings.GetStringValue("settings", "BigMapSideLength", "80,100").Split(',')[1]);
            int MediumMapSideLengthMin = int.Parse(settings.GetStringValue("settings", "MediumMapSideLength", "65,80").Split(',')[0]);
            int MediumMapSideLengthMax = int.Parse(settings.GetStringValue("settings", "MediumMapSideLength", "65,80").Split(',')[1]);
            int SmallMapSideLengthMin = int.Parse(settings.GetStringValue("settings", "SmallMapSideLength", "50,65").Split(',')[0]);
            int SmallMapSideLengthMax = int.Parse(settings.GetStringValue("settings", "SmallMapSideLength", "50,65").Split(',')[1]);
            int playerAddition = settings.GetIntValue("settings", totalPlayer.ToString()+"PlayerAddition", 0);
            string gamemode = GameMode.Name.ToLower();
            string gamemodeLine = "";
            if (gamemode == "standard")
                gamemodeLine = "standard";
            else
                gamemodeLine = "standard," + gamemode;

            if (randomMapWindow.ddMapSize.SelectedIndex == 0)
            {
                width = random.Next(GiganticMapSideLengthMin + playerAddition, GiganticMapSideLengthMax + playerAddition);
                height = random.Next(GiganticMapSideLengthMin + playerAddition, GiganticMapSideLengthMax + playerAddition);
            }
            else if (randomMapWindow.ddMapSize.SelectedIndex == 1)
            {
                width = random.Next(BigMapSideLengthMin + playerAddition, BigMapSideLengthMax + playerAddition);
                height = random.Next(BigMapSideLengthMin + playerAddition, BigMapSideLengthMax + playerAddition);
            }
            else if (randomMapWindow.ddMapSize.SelectedIndex == 2)
            {
                width = random.Next(MediumMapSideLengthMin + playerAddition, MediumMapSideLengthMax + playerAddition);
                height = random.Next(MediumMapSideLengthMin + playerAddition, MediumMapSideLengthMax + playerAddition);
            }
            else if (randomMapWindow.ddMapSize.SelectedIndex == 3)
            {
                width = random.Next(SmallMapSideLengthMin + playerAddition, SmallMapSideLengthMax + playerAddition);
                height = random.Next(SmallMapSideLengthMin + playerAddition, SmallMapSideLengthMax + playerAddition);
            }
            string size = $"-w {width} -h {height}";
            string playerLocation = $" --nwp {randomMapWindow.ddPlayerNW.SelectedIndex} --np {randomMapWindow.ddPlayerN.SelectedIndex} --nep {randomMapWindow.ddPlayerNE.SelectedIndex} --ep {randomMapWindow.ddPlayerE.SelectedIndex} --sep {randomMapWindow.ddPlayerSE.SelectedIndex} --sp {randomMapWindow.ddPlayerS.SelectedIndex} --swp {randomMapWindow.ddPlayerSW.SelectedIndex} --wp {randomMapWindow.ddPlayerW.SelectedIndex}";
            string damaged = "";
            if (randomMapWindow.chkDamagedBuilding.Checked)
                damaged = " -d -s 0.02";
            string thumbnail = "";
/*            if (!randomMapWindow.chkNoThumbnail.Checked)
                thumbnail = " --no-thumbnail-output";
            else*/
                thumbnail = " --no-thumbnail";
            
            Process RandomMapGenerator = new Process();
            RandomMapGenerator.StartInfo.FileName = RandomMapWindow.GeneratorPath + "RandomMapGenerator.exe";
            RandomMapGenerator.StartInfo.WorkingDirectory = RandomMapWindow.GeneratorPath;
            RandomMapGenerator.StartInfo.UseShellExecute = false;
            RandomMapGenerator.StartInfo.CreateNoWindow = true;
            RandomMapGenerator.StartInfo.Arguments = $" {size} {playerLocation} {damaged} -n \"{RandomMapName}\" {thumbnail} -g {gamemodeLine} --type {mapType}";
            RandomMapGenerator.Start();
            while (!RandomMapGenerator.HasExited) { }
            randomMapWindow.Disable();
        }

        private List<Map> GetMapList(int playerCount)
        {
            List<Map> mapList = new List<Map>();
            for (int i = 0; i < GameMode.Maps.Count; i++)
            {
                if (tbMapSearch.Text != tbMapSearch.Suggestion)
                {
                    if (!GameMode.Maps[i].Name.ToUpper().Contains(tbMapSearch.Text.ToUpper()))
                    {
                        continue;
                    }
                }

                if ((GameMode.Name == "RA2 Ladder" || GameMode.Name == "YR Ladder" || GameMode.Name == "StealingHotPot") && ddplayerNumbers.SelectedItem.Text.Contains("-"))
                {
                    if (GameMode.Maps[i].MaxPlayers < playerCount)
                        continue;

                    mapList.Add(GameMode.Maps[i]);
                    continue;
                }


                if (!ddplayerNumbers.SelectedItem.Text.Contains("-"))
                {
                    if (GameMode.Maps[i].MaxPlayers != int.Parse(ddplayerNumbers.SelectedItem.Text))
                    {
                        continue;
                    }
                }
                else
                {
                    if (GameMode.Maps[i].MaxPlayers != playerCount)
                    {
                        continue;
                    }
                }
                if (!ddAuthor.SelectedItem.Text.Contains("-"))
                {
                    if (!GameMode.Maps[i].Author.Contains(ddAuthor.SelectedItem.Text))
                    {
                        continue;
                    }
                }
                mapList.Add(GameMode.Maps[i]);
            }

            if (mapList.Count < 1 && playerCount <= MAX_PLAYER_COUNT && ddplayerNumbers.SelectedItem.Text.Contains("-"))
                return GetMapList(playerCount + 1);
            else
                return mapList;
        }

        /// <summary>
        /// Refreshes the map selection UI to match the currently selected map
        /// and game mode.
        /// </summary>
        protected void RefreshMapSelectionUI()
        {
            if (GameMode == null)
                return;

            int gameModeIndex = ddGameMode.Items.FindIndex(i => i.Text == GameMode.UIName);

            if (gameModeIndex == -1)
                return;

            if (ddGameMode.SelectedIndex == gameModeIndex)
                DdGameMode_SelectedIndexChanged(this, EventArgs.Empty);

            ddGameMode.SelectedIndex = gameModeIndex;
        }

        /// <summary>
        /// Initializes the player option drop-down controls.
        /// </summary>
        protected void InitPlayerOptionDropdowns()
        {
            ddPlayerNames = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerSides = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerColors = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerStarts = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerTeams = new XNAClientDropDown[MAX_PLAYER_COUNT];

            int playerOptionVecticalMargin = GameOptionsIni.GetIntValue(Name, "PlayerOptionVerticalMargin", PLAYER_OPTION_VERTICAL_MARGIN);
            int playerOptionHorizontalMargin = GameOptionsIni.GetIntValue(Name, "PlayerOptionHorizontalMargin", PLAYER_OPTION_HORIZONTAL_MARGIN);
            int playerOptionCaptionLocationY = GameOptionsIni.GetIntValue(Name, "PlayerOptionCaptionLocationY", PLAYER_OPTION_CAPTION_Y);
            int playerNameWidth = GameOptionsIni.GetIntValue(Name, "PlayerNameWidth", 136);
            int sideWidth = GameOptionsIni.GetIntValue(Name, "SideWidth", 91);
            int colorWidth = GameOptionsIni.GetIntValue(Name, "ColorWidth", 79);
            int startWidth = GameOptionsIni.GetIntValue(Name, "StartWidth", 49);
            int teamWidth = GameOptionsIni.GetIntValue(Name, "TeamWidth", 46);
            int locationX = GameOptionsIni.GetIntValue(Name, "PlayerOptionLocationX", 25);
            int locationY = GameOptionsIni.GetIntValue(Name, "PlayerOptionLocationY", 24);

            // InitPlayerOptionDropdowns(136, 91, 79, 49, 46, new Point(25, 24));

            string[] sides = ClientConfiguration.Instance.Sides.Split(',');
            SideCount = sides.Length;

            List<string> selectorNames = new List<string>();
            GetRandomSelectors(selectorNames, RandomSelectors);
            RandomSelectorCount = RandomSelectors.Count + 1;
            MapPreviewBox.RandomSelectorCount = RandomSelectorCount;

            string randomColor = GameOptionsIni.GetStringValue("General", "RandomColor", "255,255,255");

            for (int i = MAX_PLAYER_COUNT - 1; i > -1; i--)
            {
                var ddPlayerName = new XNAClientDropDown(WindowManager);
                ddPlayerName.Name = "ddPlayerName" + i;
                ddPlayerName.ClientRectangle = new Rectangle(locationX,
                    locationY + (DROP_DOWN_HEIGHT + playerOptionVecticalMargin) * i,
                    playerNameWidth, DROP_DOWN_HEIGHT);
                ddPlayerName.AddItem(String.Empty);
                ddPlayerName.AddItem("简单的电脑");
                ddPlayerName.AddItem("普通的电脑");
                ddPlayerName.AddItem("冷酷的电脑");
                ddPlayerName.AllowDropDown = true;
                ddPlayerName.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerName.Tag = true;

                var ddPlayerSide = new XNAClientDropDown(WindowManager);
                ddPlayerSide.Name = "ddPlayerSide" + i;
                ddPlayerSide.ClientRectangle = new Rectangle(
                    ddPlayerName.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y, sideWidth, DROP_DOWN_HEIGHT);
                ddPlayerSide.AddItem("随机", AssetLoader.LoadTexture("randomicon.png"));
                foreach (string randomSelector in selectorNames)
                    ddPlayerSide.AddItem(randomSelector, AssetLoader.LoadTexture(randomSelector + "icon.png"));
                foreach (string sideName in sides)
                    ddPlayerSide.AddItem(sideName, AssetLoader.LoadTexture(sideName + "icon.png"));
                ddPlayerSide.AllowDropDown = false;
                ddPlayerSide.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerSide.Tag = true;

                var ddPlayerColor = new XNAClientDropDown(WindowManager);
                ddPlayerColor.Name = "ddPlayerColor" + i;
                ddPlayerColor.ClientRectangle = new Rectangle(
                    ddPlayerSide.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y, colorWidth, DROP_DOWN_HEIGHT);
                ddPlayerColor.AddItem("随机", AssetLoader.GetColorFromString(randomColor));
                foreach (MultiplayerColor mpColor in MPColors)
                    ddPlayerColor.AddItem(mpColor.Name, mpColor.XnaColor);
                ddPlayerColor.AllowDropDown = false;
                ddPlayerColor.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerColor.Tag = false;

                var ddPlayerTeam = new XNAClientDropDown(WindowManager);
                ddPlayerTeam.Name = "ddPlayerTeam" + i;
                ddPlayerTeam.ClientRectangle = new Rectangle(
                    ddPlayerColor.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y, teamWidth, DROP_DOWN_HEIGHT);
                ddPlayerTeam.AddItem("-");
                ddPlayerTeam.AddItem("A");
                ddPlayerTeam.AddItem("B");
                ddPlayerTeam.AddItem("C");
                ddPlayerTeam.AddItem("D");
                ddPlayerTeam.AllowDropDown = false;
                ddPlayerTeam.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerTeam.Tag = true;

                var ddPlayerStart = new XNAClientDropDown(WindowManager);
                ddPlayerStart.Name = "ddPlayerStart" + i;
                ddPlayerStart.ClientRectangle = new Rectangle(
                    ddPlayerTeam.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y, startWidth, DROP_DOWN_HEIGHT);
                for (int j = 1; j < 9; j++)
                    ddPlayerStart.AddItem(j.ToString());
                ddPlayerStart.AllowDropDown = false;
                ddPlayerStart.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerStart.Visible = false;
                ddPlayerStart.Enabled = false;
                ddPlayerStart.Tag = true;

                ddPlayerNames[i] = ddPlayerName;
                ddPlayerSides[i] = ddPlayerSide;
                ddPlayerColors[i] = ddPlayerColor;
                ddPlayerStarts[i] = ddPlayerStart;
                ddPlayerTeams[i] = ddPlayerTeam;

                PlayerOptionsPanel.AddChild(ddPlayerName);
                PlayerOptionsPanel.AddChild(ddPlayerSide);
                PlayerOptionsPanel.AddChild(ddPlayerColor);
                PlayerOptionsPanel.AddChild(ddPlayerStart);
                PlayerOptionsPanel.AddChild(ddPlayerTeam);
            }

            lblName = new XNALabel(WindowManager);
            lblName.Name = "lblName";
            lblName.Text = "玩家";
            lblName.FontIndex = 1;
            lblName.ClientRectangle = new Rectangle(ddPlayerNames[0].X, playerOptionCaptionLocationY, 0, 0);

            lblSide = new XNALabel(WindowManager);
            lblSide.Name = "lblSide";
            lblSide.Text = "阵营";
            lblSide.FontIndex = 1;
            lblSide.ClientRectangle = new Rectangle(ddPlayerSides[0].X, playerOptionCaptionLocationY, 0, 0);

            lblColor = new XNALabel(WindowManager);
            lblColor.Name = "lblColor";
            lblColor.Text = "颜色";
            lblColor.FontIndex = 1;
            lblColor.ClientRectangle = new Rectangle(ddPlayerColors[0].X, playerOptionCaptionLocationY, 0, 0);

            lblStart = new XNALabel(WindowManager);
            lblStart.Name = "lblStart";
            lblStart.Text = "开始";
            lblStart.FontIndex = 1;
            lblStart.ClientRectangle = new Rectangle(ddPlayerStarts[0].X, playerOptionCaptionLocationY, 0, 0);
            lblStart.Visible = false;

            lblTeam = new XNALabel(WindowManager);
            lblTeam.Name = "lblTeam";
            lblTeam.Text = "小队";
            lblTeam.FontIndex = 1;
            lblTeam.ClientRectangle = new Rectangle(ddPlayerTeams[0].X, playerOptionCaptionLocationY, 0, 0);

            PlayerOptionsPanel.AddChild(lblName);
            PlayerOptionsPanel.AddChild(lblSide);
            PlayerOptionsPanel.AddChild(lblColor);
            PlayerOptionsPanel.AddChild(lblStart);
            PlayerOptionsPanel.AddChild(lblTeam);

            CheckDisallowedSides();
            
        }

        /// <summary>
        /// Loads random side selectors from GameOptions.ini
        /// </summary>
        /// <param name="selectorNames">TODO comment</param>
        /// <param name="selectorSides">TODO comment</param>
        private void GetRandomSelectors(List<string> selectorNames, List<int[]> selectorSides)
        {
            List<string> keys = GameOptionsIni.GetSectionKeys("RandomSelectors");

            if (keys == null)
                return;

            foreach (string randomSelector in keys)
            {
                List<int> randomSides = new List<int>();
                try
                {
                    string[] tmp = GameOptionsIni.GetStringValue("RandomSelectors", randomSelector, string.Empty).Split(',');
                    randomSides = Array.ConvertAll<string, int>(tmp, int.Parse).Distinct().ToList();
                    randomSides.RemoveAll(x => (x >= SideCount || x < 0));
                }
                catch (FormatException) { }

                if (randomSides.Count > 1)
                {
                    selectorNames.Add(randomSelector);
                    selectorSides.Add(randomSides.ToArray());
                }
            }
        }

        protected abstract void BtnLaunchGame_LeftClick(object sender, EventArgs e);

        protected abstract void BtnLeaveGame_LeftClick(object sender, EventArgs e);

        /// <summary>
        /// Updates Discord Rich Presence with actual information.
        /// </summary>
        /// <param name="resetTimer">Whether to restart the "Elapsed" timer or not</param>
        protected abstract void UpdateDiscordPresence(bool resetTimer = false);

        /// <summary>
        /// Resets Discord Rich Presence to default state.
        /// </summary>
        protected void ResetDiscordPresence()
        {
            discordHandler?.UpdatePresence();
        }

        protected void LoadDefaultMap()
        {
            if (ddGameMode.Items.Count > 0)
            {
                ddGameMode.SelectedIndex = 0;

                lbMapList.SelectedIndex = 0;
            }
        }

#if YR
        protected bool CheckRa2Mode()
        {
            // TODO obsolete, remove when it's certain that this is not needed anywhere

            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            {
                if (checkBox.Name == "chkRA2Mode" && checkBox.Checked)
                {
                    RA2Mode = true;
                }
                else if (checkBox.Name == "chkRA2Mode" && !checkBox.Checked)
                {
                    RA2Mode = false;
                }
            }
            if (RA2Mode == true)
                return true;
            else
                return false;
        }

        protected bool CheckLadderMode()
        {
            // TODO obsolete, remove when it's certain that this is not needed anywhere
            if (GameMode.Name == "RA2 Ladder" || GameMode.Name == "YR Ladder")
                LadderMode = true;
            else
                LadderMode = false;

            if (LadderMode == true)
                return true;
            else
                return false;
        }
#endif

        private int GetSpectatorSideIndex() => SideCount + RandomSelectorCount;

        /// <summary>
        /// Applies disallowed side indexes to the side option drop-downs
        /// and player options.
        /// </summary>
        protected void CheckDisallowedSides()
        {
            var disallowedSideArray = GetDisallowedSides();
            int defaultSide = 0;
            int allowedSideCount = disallowedSideArray.Count(b => b == false);

            if (allowedSideCount == 1)
            {
                // Disallow Random

                for (int i = 0; i < disallowedSideArray.Length; i++)
                {
                    if (!disallowedSideArray[i])
                        defaultSide = i + RandomSelectorCount;
                }

                foreach (XNADropDown dd in ddPlayerSides)
                {
                    //dd.Items[0].Selectable = false;
                    for (int i = 0; i < RandomSelectorCount; i++)
                    {
                        dd.Items[i].Selectable = false;
                    }
                }
            }
            else
            {
                foreach (XNADropDown dd in ddPlayerSides)
                {
                    //dd.Items[0].Selectable = true;
                    for (int i = 0; i < RandomSelectorCount; i++)
                    {
                        dd.Items[i].Selectable = true;
                    }
                }
            }

            var concatPlayerList = Players.Concat(AIPlayers);

            // Disable custom random groups if all or all except one of included sides are unavailable.
            int c = 0;
            foreach (int[] randomsides in RandomSelectors)
            {
                int disablecount = 0;
                foreach (int side in randomsides)
                {
                    if (disallowedSideArray[side]) disablecount++;
                }
                bool disabled = false;
                if (disablecount >= randomsides.Length - 1) disabled = true;
                foreach (XNADropDown dd in ddPlayerSides)
                {
                    dd.Items[1 + c].Selectable = !disabled;
                }
                foreach (PlayerInfo pInfo in concatPlayerList)
                {
                    if (pInfo.SideId == 1 + c && disabled)
                        pInfo.SideId = defaultSide;
                }
                c++;
            }

            // Go over the side array and either disable or enable the side
            // dropdown options depending on whether the side is available
            for (int i = 0; i < disallowedSideArray.Length; i++)
            {
                bool disabled = disallowedSideArray[i];

                if (disabled)
                {
                    foreach (XNADropDown dd in ddPlayerSides)
                    {
                        dd.Items[i + RandomSelectorCount].Selectable = false;
                    }

                    // Change the sides of players that use the disabled 
                    // side to the default side
                    foreach (PlayerInfo pInfo in concatPlayerList)
                    {
                        if (pInfo.SideId == i + RandomSelectorCount)
                            pInfo.SideId = defaultSide;
                    }
                }
                else
                {
                    foreach (XNADropDown dd in ddPlayerSides)
                    {
                        dd.Items[i + RandomSelectorCount].Selectable = true;
                    }
                }
            }

            // If only 1 side is allowed, change all players' sides to that
            if (allowedSideCount == 1)
            {
                foreach (PlayerInfo pInfo in concatPlayerList)
                {
                    if (pInfo.SideId == 0)
                        pInfo.SideId = defaultSide;
                }
            }

            if (Map != null && Map.CoopInfo != null)
            {
                // Disallow spectator

                foreach (PlayerInfo pInfo in concatPlayerList)
                {
                    if (pInfo.SideId == GetSpectatorSideIndex())
                        pInfo.SideId = defaultSide;
                }

                foreach (XNADropDown dd in ddPlayerSides)
                {
                    if (dd.Items.Count > GetSpectatorSideIndex())
                        dd.Items[SideCount + RandomSelectorCount].Selectable = false;
                }
            }
            else
            {
                foreach (XNADropDown dd in ddPlayerSides)
                {
                    if (dd.Items.Count > SideCount + RandomSelectorCount)
                        dd.Items[SideCount + RandomSelectorCount].Selectable = true;
                }
            }
        }

        /// <summary>
        /// Gets a list of side indexes that are disallowed.
        /// </summary>
        /// <returns>A list of disallowed side indexes.</returns>
        protected bool[] GetDisallowedSides()
        {
            var returnValue = new bool[SideCount];

            if (Map != null && Map.CoopInfo != null)
            {
                // Co-Op map disallowed side logic

                foreach (int disallowedSideIndex in Map.CoopInfo.DisallowedPlayerSides)
                {
                    returnValue[disallowedSideIndex] = true;
                }
            }

            foreach (var checkBox in CheckBoxes)
                checkBox.ApplyDisallowedSideIndex(returnValue);

            return returnValue;
        }

        /// <summary>
        /// Randomizes options of both human and AI players
        /// and returns the options as an array of PlayerHouseInfos.
        /// </summary>
        /// <returns>An array of PlayerHouseInfos.</returns>
        protected virtual PlayerHouseInfo[] Randomize()
        {
            int totalPlayerCount = Players.Count + AIPlayers.Count;
            PlayerHouseInfo[] houseInfos = new PlayerHouseInfo[totalPlayerCount];

            for (int i = 0; i < totalPlayerCount; i++)
                houseInfos[i] = new PlayerHouseInfo();

            // Gather list of spectators
            for (int i = 0; i < Players.Count; i++)
            {
                houseInfos[i].IsSpectator = Players[i].SideId == GetSpectatorSideIndex();
            }

            // Gather list of available colors

            List<int> freeColors = new List<int>();

            for (int cId = 0; cId < MPColors.Count; cId++)
                freeColors.Add(cId);

            if (Map.CoopInfo != null)
            {
                foreach (int colorIndex in Map.CoopInfo.DisallowedPlayerColors)
                    freeColors.Remove(colorIndex);
            }

            foreach (PlayerInfo player in Players)
                freeColors.Remove(player.ColorId - 1); // The first color is Random

            foreach (PlayerInfo aiPlayer in AIPlayers)
                freeColors.Remove(aiPlayer.ColorId - 1);

            // Gather list of available starting locations

            List<int> freeStartingLocations = new List<int>();
            List<int> takenStartingLocations = new List<int>();

            for (int i = 0; i < Map.MaxPlayers; i++)
                freeStartingLocations.Add(i);

            for (int i = 0; i < Players.Count; i++)
            {
                if (!houseInfos[i].IsSpectator)
                {
                    freeStartingLocations.Remove(Players[i].StartingLocation - 1);
                    //takenStartingLocations.Add(Players[i].StartingLocation - 1);
                    // ^ Gives everyone with a selected location a completely random
                    // location in-game, because PlayerHouseInfo.RandomizeStart already
                    // fills the list itself
                }
            }

            for (int i = 0; i < AIPlayers.Count; i++)
            {
                freeStartingLocations.Remove(AIPlayers[i].StartingLocation - 1);
            }

            // Randomize options

            Random random = new Random(RandomSeed);

            for (int i = 0; i < totalPlayerCount; i++)
            {
                PlayerInfo pInfo;
                PlayerHouseInfo pHouseInfo = houseInfos[i];

                if (i < Players.Count)
                {
                    pInfo = Players[i];
                }
                else
                    pInfo = AIPlayers[i - Players.Count];

                pHouseInfo.RandomizeSide(pInfo, Map, SideCount, random, GetDisallowedSides(), RandomSelectors, RandomSelectorCount);

                pHouseInfo.RandomizeColor(pInfo, freeColors, MPColors, random);
                pHouseInfo.RandomizeStart(pInfo, Map,
                    freeStartingLocations, random, takenStartingLocations);
            }

            return houseInfos;
        }

        /// <summary>
        /// Writes spawn.ini. Returns the player house info returned from the randomizer.
        /// </summary>
        private PlayerHouseInfo[] WriteSpawnIni()
        {
            Logger.Log("Writing spawn.ini");

            File.Delete(ProgramConstants.GamePath + ProgramConstants.SPAWNER_SETTINGS);

            if (Map.IsCoop)
            {
                foreach (PlayerInfo pInfo in Players)
                    pInfo.TeamId = 1;

                foreach (PlayerInfo pInfo in AIPlayers)
                    pInfo.TeamId = 1;
            }

            PlayerHouseInfo[] houseInfos = Randomize();

            IniFile spawnIni = new IniFile(ProgramConstants.GamePath + ProgramConstants.SPAWNER_SETTINGS);

            IniSection settings = new IniSection("Settings");

            settings.SetStringValue("Name", ProgramConstants.PLAYERNAME);
            settings.SetStringValue("Scenario", ProgramConstants.SPAWNMAP_INI);
            settings.SetStringValue("UIGameMode", GameMode.UIName);
            settings.SetStringValue("UIMapName", Map.Name);
            settings.SetIntValue("PlayerCount", Players.Count);
            int myIndex = Players.FindIndex(c => c.Name == ProgramConstants.PLAYERNAME);
            settings.SetIntValue("Side", houseInfos[myIndex].InternalSideIndex);
            settings.SetBooleanValue("IsSpectator", houseInfos[myIndex].IsSpectator);
            settings.SetIntValue("Color", houseInfos[myIndex].ColorIndex);
            settings.SetStringValue("CustomLoadScreen", LoadingScreenController.GetLoadScreenName(houseInfos[myIndex].InternalSideIndex));
            settings.SetIntValue("AIPlayers", AIPlayers.Count);
            settings.SetIntValue("Seed", RandomSeed);
            if (GetPvPTeamCount() > 1)
                settings.SetBooleanValue("CoachMode", true);
            if (GetGameType() == GameType.Coop)
                settings.SetBooleanValue("AutoSurrender", false);
            spawnIni.AddSection(settings);
            WriteSpawnIniAdditions(spawnIni);

            foreach (GameLobbyCheckBox chkBox in CheckBoxes)
            {
                chkBox.ApplySpawnINICode(spawnIni);
            }

            foreach (GameLobbyDropDown dd in DropDowns)
            {
                dd.ApplySpawnIniCode(spawnIni);
            }

            // Apply forced options from GameOptions.ini

            List<string> forcedKeys = GameOptionsIni.GetSectionKeys("ForcedSpawnIniOptions");

            if (forcedKeys != null)
            {
                foreach (string key in forcedKeys)
                {
                    spawnIni.SetStringValue("Settings", key,
                        GameOptionsIni.GetStringValue("ForcedSpawnIniOptions", key, String.Empty));
                }
            }

            GameMode.ApplySpawnIniCode(spawnIni); // Forced options from the game mode
            Map.ApplySpawnIniCode(spawnIni, Players.Count + AIPlayers.Count,
                AIPlayers.Count, GameMode.CoopDifficultyLevel); // Forced options from the map

            // Player options

            int otherId = 1;

            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];
                PlayerHouseInfo pHouseInfo = houseInfos[pId];

                if (pInfo.Name == ProgramConstants.PLAYERNAME)
                    continue;

                string sectionName = "Other" + otherId;

                spawnIni.SetStringValue(sectionName, "Name", pInfo.Name);
                spawnIni.SetIntValue(sectionName, "Side", pHouseInfo.InternalSideIndex);
                spawnIni.SetBooleanValue(sectionName, "IsSpectator", pHouseInfo.IsSpectator);
                spawnIni.SetIntValue(sectionName, "Color", pHouseInfo.ColorIndex);
                spawnIni.SetStringValue(sectionName, "Ip", GetIPAddressForPlayer(pInfo));
                spawnIni.SetIntValue(sectionName, "Port", pInfo.Port);

                otherId++;
            }

            List<int> multiCmbIndexes = new List<int>();

            for (int cId = 0; cId < MPColors.Count; cId++)
            {
                for (int pId = 0; pId < Players.Count; pId++)
                {
                    if (houseInfos[pId].ColorIndex == MPColors[cId].GameColorIndex)
                        multiCmbIndexes.Add(pId);
                }
            }

            if (AIPlayers.Count > 0)
            {
                for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
                {
                    int multiId = multiCmbIndexes.Count + aiId + 1;

                    string keyName = "Multi" + multiId;

                    spawnIni.SetIntValue("HouseHandicaps", keyName, AIPlayers[aiId].AILevel);
                    spawnIni.SetIntValue("HouseCountries", keyName, houseInfos[Players.Count + aiId].InternalSideIndex);
                    spawnIni.SetIntValue("HouseColors", keyName, houseInfos[Players.Count + aiId].ColorIndex);
                }
            }

            for (int multiId = 0; multiId < multiCmbIndexes.Count; multiId++)
            {
                int pIndex = multiCmbIndexes[multiId];
                if (houseInfos[pIndex].IsSpectator)
                    spawnIni.SetBooleanValue("IsSpectator", "Multi" + (multiId + 1), true);
            }

            // Write alliances, the code is pretty big so let's take it to another class
            AllianceHolder.WriteInfoToSpawnIni(Players, AIPlayers, multiCmbIndexes, spawnIni);

            for (int pId = 0; pId < Players.Count; pId++)
            {
                int startingWaypoint = houseInfos[multiCmbIndexes[pId]].StartingWaypoint;

                // -1 means no starting location at all - let the game itself pick the starting location
                // using its own logic
                if (startingWaypoint > -1)
                {
                    int multiIndex = pId + 1;
                    spawnIni.SetIntValue("SpawnLocations", "Multi" + multiIndex,
                        startingWaypoint);
                }
            }

            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                int startingWaypoint = houseInfos[Players.Count + aiId].StartingWaypoint;

                if (startingWaypoint > -1)
                {
                    int multiIndex = Players.Count + aiId + 1;
                    spawnIni.SetIntValue("SpawnLocations", "Multi" + multiIndex,
                        startingWaypoint);
                }
            }

            spawnIni.WriteIniFile();

            return houseInfos;
        }

        /// <summary>
        /// Returns the number of teams with human players in them.
        /// Does not count spectators and human players that don't have a team set.
        /// </summary>
        /// <returns>The number of human player teams in the game.</returns>
        private int GetPvPTeamCount()
        {
            int[] teamPlayerCounts = new int[4];
            int playerTeamCount = 0;

            foreach (PlayerInfo pInfo in Players)
            {
                if (pInfo.IsAI || IsPlayerSpectator(pInfo))
                    continue;

                if (pInfo.TeamId > 0)
                {
                    teamPlayerCounts[pInfo.TeamId - 1]++;
                    if (teamPlayerCounts[pInfo.TeamId - 1] == 2)
                        playerTeamCount++;
                }
            }

            return playerTeamCount;
        }

        /// <summary>
        /// Checks whether the specified player has selected Spectator as their side.
        /// </summary>
        /// <param name="pInfo">The player.</param>
        /// <returns>True if the player is a spectator, otherwise false.</returns>
        private bool IsPlayerSpectator(PlayerInfo pInfo)
        {
            if (pInfo.SideId == GetSpectatorSideIndex())
                return true;

            return false;
        }

        protected virtual string GetIPAddressForPlayer(PlayerInfo player)
        {
            return "0.0.0.0";
        }

        /// <summary>
        /// Override this in a derived class to write game lobby specific code to
        /// spawn.ini. For example, CnCNet game lobbies should write tunnel info
        /// in this method.
        /// </summary>
        /// <param name="iniFile">The spawn INI file.</param>
        protected virtual void WriteSpawnIniAdditions(IniFile iniFile)
        {
            // Do nothing by default
        }

        private void InitializeMatchStatistics(PlayerHouseInfo[] houseInfos)
        {
            matchStatistics = new MatchStatistics(ProgramConstants.GAME_VERSION, UniqueGameID,
                Map.Name, GameMode.UIName, Players.Count, Map.IsCoop);

            bool isValidForStar = true;
            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            {
                if ((checkBox.MapScoringMode == CheckBoxMapScoringMode.DenyWhenChecked && checkBox.Checked) ||
                    (checkBox.MapScoringMode == CheckBoxMapScoringMode.DenyWhenUnchecked && !checkBox.Checked))
                {
                    isValidForStar = false;
                    break;
                }
            }

            matchStatistics.IsValidForStar = isValidForStar;

            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];
                matchStatistics.AddPlayer(pInfo.Name, pInfo.Name == ProgramConstants.PLAYERNAME,
                    false, pInfo.SideId == SideCount + RandomSelectorCount, houseInfos[pId].SideIndex + 1, pInfo.TeamId,
                    MPColors.FindIndex(c => c.GameColorIndex == houseInfos[pId].ColorIndex), 10);
            }

            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                var pHouseInfo = houseInfos[Players.Count + aiId];
                PlayerInfo aiInfo = AIPlayers[aiId];
                matchStatistics.AddPlayer("Computer", false, true, false,
                    pHouseInfo.SideIndex + 1, aiInfo.TeamId,
                    MPColors.FindIndex(c => c.GameColorIndex == pHouseInfo.ColorIndex),
                    aiInfo.ReversedAILevel);
            }
        }

        /// <summary>
        /// Writes spawnmap.ini.
        /// </summary>
        private void WriteMap(PlayerHouseInfo[] houseInfos)
        {
            File.Delete(ProgramConstants.GamePath + ProgramConstants.SPAWNMAP_INI);

            Logger.Log("Writing map.");

            Logger.Log("Loading map INI from " + Map.CompleteFilePath);

            IniFile mapIni = Map.GetMapIni();

            IniFile globalCodeIni = new IniFile(ProgramConstants.GamePath + "INI\\Map Code\\GlobalCode.ini");

            MapCodeHelper.ApplyMapCode(mapIni, GameMode.GetMapRulesIniFile());
            MapCodeHelper.ApplyMapCode(mapIni, globalCodeIni);

            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                checkBox.ApplyMapCode(mapIni, GameMode);

            foreach (GameLobbyDropDown dropDown in DropDowns)
                dropDown.ApplyMapCode(mapIni, GameMode);

            mapIni.MoveSectionToFirst("MultiplayerDialogSettings"); // Required by YR

            ManipulateStartingLocations(mapIni, houseInfos);

            mapIni.WriteIniFile(ProgramConstants.GamePath + ProgramConstants.SPAWNMAP_INI);
        }

        private void ManipulateStartingLocations(IniFile mapIni, PlayerHouseInfo[] houseInfos)
        {
            if (RemoveStartingLocations)
            {
                if (Map.EnforceMaxPlayers)
                    return;

                // All random starting locations given by the game
                IniSection waypointSection = mapIni.GetSection("Waypoints");
                if (waypointSection == null)
                    return;

                // TODO implement IniSection.RemoveKey in Rampastring.Tools, then
                // remove implementation that depends on internal implementation
                // of IniSection
                for (int i = 0; i <= 7; i++)
                {
                    int index = waypointSection.Keys.FindIndex(k => !string.IsNullOrEmpty(k.Key) && k.Key == i.ToString());
                    if (index > -1)
                        waypointSection.Keys.RemoveAt(index);
                }
            }

            // Multiple players cannot properly share the same starting location
            // without breaking the SpawnX house logic that pre-placed objects depend on

            // To work around this, we add new starting locations that just point
            // to the same cell coordinates as existing stacked starting locations
            // and make additional players in the same start loc start from the new
            // starting locations instead.

            // As an additional restriction, players can only start from waypoints 0 to 7.
            // That means that if the map already has too many starting waypoints,
            // we need to move existing (but un-occupied) starting waypoints to point 
            // to the stacked locations so we can spawn the players there.


            // Check for stacked starting locations (locations with more than 1 player on it)
            bool[] startingLocationUsed = new bool[MAX_PLAYER_COUNT];
            bool stackedStartingLocations = false;
            foreach (PlayerHouseInfo houseInfo in houseInfos)
            {
                if (houseInfo.RealStartingWaypoint > -1)
                {
                    startingLocationUsed[houseInfo.RealStartingWaypoint] = true;

                    // If assigned starting waypoint is unknown while the real 
                    // starting location is known, it means that
                    // the location is shared with another player
                    if (houseInfo.StartingWaypoint == -1)
                    {
                        stackedStartingLocations = true;
                    }
                }
            }

            // If any starting location is stacked, re-arrange all starting locations
            // so that unused starting locations are removed and made to point at used
            // starting locations
            if (!stackedStartingLocations)
                return;

            // We also need to modify spawn.ini because WriteSpawnIni
            // doesn't handle stacked positions.
            // We could move this code there, but then we'd have to process
            // the stacked locations in two places (here and in WriteSpawnIni)
            // because we'd need to modify the map anyway.
            // Not sure whether having it like this or in WriteSpawnIni
            // is better, but this implementation is quicker to write for now.
            IniFile spawnIni = new IniFile(ProgramConstants.GamePath + ProgramConstants.SPAWNER_SETTINGS);

            // For each player, check if they're sharing the starting location
            // with someone else
            // If they are, find an unused waypoint and assign their 
            // starting location to match that
            for (int pId = 0; pId < houseInfos.Length; pId++)
            {
                PlayerHouseInfo houseInfo = houseInfos[pId];

                if (houseInfo.RealStartingWaypoint > -1 &&
                    houseInfo.StartingWaypoint == -1)
                {
                    // Find first unused starting location index
                    int unusedLocation = -1;
                    for (int i = 0; i < startingLocationUsed.Length; i++)
                    {
                        if (!startingLocationUsed[i])
                        {
                            unusedLocation = i;
                            startingLocationUsed[i] = true;
                            break;
                        }
                    }

                    houseInfo.StartingWaypoint = unusedLocation;
                    mapIni.SetIntValue("Waypoints", unusedLocation.ToString(),
                        mapIni.GetIntValue("Waypoints", houseInfo.RealStartingWaypoint.ToString(), 0));
                    spawnIni.SetIntValue("SpawnLocations", $"Multi{pId + 1}", unusedLocation);
                }
            }

            spawnIni.WriteIniFile();
        }

        /// <summary>
        /// Writes spawn.ini, writes the map file, initializes statistics and
        /// starts the game process.
        /// </summary>
        protected virtual void StartGame()
        {
            PlayerHouseInfo[] houseInfos = WriteSpawnIni();
            InitializeMatchStatistics(houseInfos);
            WriteMap(houseInfos);

            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;
            CreateRa2FileList();
            CheckRa2Mode();
            CheckLadderMode();
            if (RA2Mode && !LadderMode)
                CopyRa2Files();
            else
                DeleteRa2Files();

            var settings = new IniFile(ClientConfiguration.Instance.SettingsIniName);
            if (LadderMode)
                settings.SetBooleanValue("Options", "LadderMode", true);
            else
                settings.SetBooleanValue("Options", "LadderMode", false);
            settings.WriteIniFile();

            GameProcessLogic.StartGameProcess();
            UpdateDiscordPresence(true);
        }

        private void GameProcessExited_Callback()
        {
            AddCallback(new Action(GameProcessExited), null);
        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;

            Logger.Log("GameProcessExited: Parsing statistics.");

            matchStatistics.ParseStatistics(ProgramConstants.GamePath, ClientConfiguration.Instance.LocalGame, false);

            Logger.Log("GameProcessExited: Adding match to statistics.");

            StatisticsManager.Instance.AddMatchAndSaveDatabase(true, matchStatistics);

            ClearReadyStatuses();

            CopyPlayerDataToUI();

            DeleteRa2Files();

            var settings = new IniFile(ClientConfiguration.Instance.SettingsIniName);
            settings.SetBooleanValue("Options", "LadderMode", false);
            settings.WriteIniFile();

            if (ClientConfiguration.Instance.ProcessScreenshots)
            {
                Logger.Log("GameProcessExited: Processing screenshots.");
                Thread thread = new Thread(ProcessScreenshots);
                thread.Start();
            }

            UpdateDiscordPresence(true);
        }

        private void ProcessScreenshots()
        {
            string[] filenames = Directory.GetFiles(ProgramConstants.GamePath, "SCRN*.bmp");
            string screenshotsDirectory = ProgramConstants.GamePath + "Screenshots";
            foreach (string filename in filenames)
            {
                Directory.CreateDirectory(screenshotsDirectory);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(filename);
                bitmap.Save(screenshotsDirectory + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(filename) + 
                    ".png", System.Drawing.Imaging.ImageFormat.Png);
                bitmap.Dispose();
                File.Delete(filename);
            }
        }

        /// <summary>
        /// "Copies" player information from the UI to internal memory,
        /// applying users' player options changes.
        /// </summary>
        protected virtual void CopyPlayerDataFromUI(object sender, EventArgs e)
        {
            if (PlayerUpdatingInProgress)
                return;

            var senderDropDown = (XNADropDown)sender;
            if ((bool)senderDropDown.Tag)
                ClearReadyStatuses();

            var oldSideId = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME)?.SideId;

            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];

                pInfo.ColorId = ddPlayerColors[pId].SelectedIndex;
                pInfo.SideId = ddPlayerSides[pId].SelectedIndex;
                pInfo.StartingLocation = ddPlayerStarts[pId].SelectedIndex;
                pInfo.TeamId = ddPlayerTeams[pId].SelectedIndex;

                if (pInfo.SideId == SideCount + RandomSelectorCount)
                    pInfo.StartingLocation = 0;

                XNADropDown ddName = ddPlayerNames[pId];

                switch (ddName.SelectedIndex)
                {
                    case 0:
                        break;
                    case 1:
                        ddName.SelectedIndex = 0;
                        break;
                    case 2:
                        KickPlayer(pId);
                        break;
                    case 3:
                        BanPlayer(pId);
                        break;
                }
            }

            AIPlayers.Clear();
            for (int cmbId = Players.Count; cmbId < 8; cmbId++)
            {
                XNADropDown dd = ddPlayerNames[cmbId];
                dd.Items[0].Text = "-";

                if (dd.SelectedIndex < 1)
                    continue;

                PlayerInfo aiPlayer = new PlayerInfo
                {
                    Name = dd.Items[dd.SelectedIndex].Text,
                    AILevel = 2 - (dd.SelectedIndex - 1),
                    SideId = Math.Max(ddPlayerSides[cmbId].SelectedIndex, 0),
                    ColorId = Math.Max(ddPlayerColors[cmbId].SelectedIndex, 0),
                    StartingLocation = Math.Max(ddPlayerStarts[cmbId].SelectedIndex, 0),
                    TeamId = Map != null && Map.IsCoop ? 1 : Math.Max(ddPlayerTeams[cmbId].SelectedIndex, 0),
                    IsAI = true
                };

                AIPlayers.Add(aiPlayer);
            }

            CopyPlayerDataToUI();
            btnLaunchGame.SetRank(GetRank());

            if (oldSideId != Players.Find(p => p.Name == ProgramConstants.PLAYERNAME)?.SideId)
                UpdateDiscordPresence();
        }

        /// <summary>
        /// Sets the ready status of all non-host human players to false.
        /// </summary>
        protected void ClearReadyStatuses()
        {
            for (int i = 1; i < Players.Count; i++)
            {
                if (!Players[i].AutoReady)
                    Players[i].Ready = false;
            }
        }

        /// <summary>
        /// Applies player information changes done in memory to the UI.
        /// </summary>
        protected virtual void CopyPlayerDataToUI()
        {
            PlayerUpdatingInProgress = true;
            

            bool allowOptionsChange = AllowPlayerOptionsChange();

            // Human players
            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];

                pInfo.Index = pId;

                XNADropDown ddPlayerName = ddPlayerNames[pId];
                ddPlayerName.Items[0].Text = pInfo.Name;
                ddPlayerName.Items[1].Text = string.Empty;
                ddPlayerName.Items[2].Text = "踢";
                ddPlayerName.Items[3].Text = "封";
                ddPlayerName.SelectedIndex = 0;
                ddPlayerName.AllowDropDown = false;

                bool allowPlayerOptionsChange = allowOptionsChange || pInfo.Name == ProgramConstants.PLAYERNAME;

                ddPlayerSides[pId].SelectedIndex = pInfo.SideId;
                ddPlayerSides[pId].AllowDropDown = allowPlayerOptionsChange;

                ddPlayerColors[pId].SelectedIndex = pInfo.ColorId;
                ddPlayerColors[pId].AllowDropDown = allowPlayerOptionsChange;

                ddPlayerStarts[pId].SelectedIndex = pInfo.StartingLocation;
                //ddPlayerStarts[pId].AllowDropDown = allowPlayerOptionsChange;

                ddPlayerTeams[pId].SelectedIndex = pInfo.TeamId;
                if (Map != null && GameMode != null)
                {
                    ddPlayerTeams[pId].AllowDropDown = allowPlayerOptionsChange && !Map.IsCoop && !Map.ForceNoTeams && !GameMode.ForceNoTeams;
                    ddPlayerStarts[pId].AllowDropDown = allowPlayerOptionsChange && (Map.IsCoop || !Map.ForceRandomStartLocations && !GameMode.ForceRandomStartLocations);
                }
            }

            // AI players
            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                PlayerInfo aiInfo = AIPlayers[aiId];

                int index = Players.Count + aiId;

                aiInfo.Index = index;

                XNADropDown ddPlayerName = ddPlayerNames[index];
                ddPlayerName.Items[0].Text = "-";
                ddPlayerName.Items[1].Text = "简单的电脑";
                ddPlayerName.Items[2].Text = "普通的电脑";
                ddPlayerName.Items[3].Text = "冷酷的电脑";
                ddPlayerName.SelectedIndex = 3 - aiInfo.AILevel;
                ddPlayerName.AllowDropDown = allowOptionsChange;

                ddPlayerSides[index].SelectedIndex = aiInfo.SideId;
                ddPlayerSides[index].AllowDropDown = allowOptionsChange;

                ddPlayerColors[index].SelectedIndex = aiInfo.ColorId;
                ddPlayerColors[index].AllowDropDown = allowOptionsChange;

                ddPlayerStarts[index].SelectedIndex = aiInfo.StartingLocation;
                //ddPlayerStarts[index].AllowDropDown = allowOptionsChange;

                ddPlayerTeams[index].SelectedIndex = aiInfo.TeamId;

                if (Map != null && GameMode != null)
                {
                    ddPlayerTeams[index].AllowDropDown = allowOptionsChange && !Map.IsCoop && !Map.ForceNoTeams && !GameMode.ForceNoTeams;
                    ddPlayerStarts[index].AllowDropDown = allowOptionsChange && (Map.IsCoop || !Map.ForceRandomStartLocations && !GameMode.ForceRandomStartLocations);
                }
            }

            // Unused player slots
            for (int ddIndex = Players.Count + AIPlayers.Count; ddIndex < MAX_PLAYER_COUNT; ddIndex++)
            {
                XNADropDown ddPlayerName = ddPlayerNames[ddIndex];
                ddPlayerName.AllowDropDown = false;
                ddPlayerName.Items[0].Text = string.Empty;
                ddPlayerName.Items[1].Text = "简单的电脑";
                ddPlayerName.Items[2].Text = "普通的电脑";
                ddPlayerName.Items[3].Text = "冷酷的电脑";
                ddPlayerName.SelectedIndex = 0;

                ddPlayerSides[ddIndex].SelectedIndex = -1;
                ddPlayerSides[ddIndex].AllowDropDown = false;

                ddPlayerColors[ddIndex].SelectedIndex = -1;
                ddPlayerColors[ddIndex].AllowDropDown = false;

                ddPlayerStarts[ddIndex].SelectedIndex = -1;
                ddPlayerStarts[ddIndex].AllowDropDown = false;

                ddPlayerTeams[ddIndex].SelectedIndex = -1;
                ddPlayerTeams[ddIndex].AllowDropDown = false;
            }

            if (allowOptionsChange && Players.Count + AIPlayers.Count < MAX_PLAYER_COUNT)
                ddPlayerNames[Players.Count + AIPlayers.Count].AllowDropDown = true;

            MapPreviewBox.UpdateStartingLocationTexts();
            UpdateMapPreviewBoxEnabledStatus();

            PlayerUpdatingInProgress = false;
        }

        /// <summary>
        /// Updates the enabled status of starting location selectors
        /// in the map preview box.
        /// </summary>
        protected abstract void UpdateMapPreviewBoxEnabledStatus();

        /// <summary>
        /// Override this in a derived class to kick players.
        /// </summary>
        /// <param name="playerIndex">The index of the player that should be kicked.</param>
        protected virtual void KickPlayer(int playerIndex)
        {
            // Do nothing by default
        }

        /// <summary>
        /// Override this in a derived class to ban players.
        /// </summary>
        /// <param name="playerIndex">The index of the player that should be banned.</param>
        protected virtual void BanPlayer(int playerIndex)
        {
            // Do nothing by default
        }

        /// <summary>
        /// Changes the current map and game mode.
        /// </summary>
        /// <param name="gameMode">The new game mode.</param>
        /// <param name="map">The new map.</param>
        protected virtual void ChangeMap(GameMode gameMode, Map map)
        {
            var oldGameMode = GameMode;
            GameMode = gameMode;

            Map = map;

            if (GameMode == null || Map == null)
            {
                lblMapName.Text = "地图：未知";
                lblMapAuthor.Text = "作者：未知";
                lblGameMode.Text = "游戏模式：未知";
                lblMapSize.Text = "地图大小：未知";

                lblMapAuthor.X = MapPreviewBox.Right - lblMapAuthor.Width;

                MapPreviewBox.Map = null;

                return;
            }

            lblMapName.Text = "地图：" + Renderer.GetSafeString(map.Name, lblMapName.FontIndex);
            lblMapAuthor.Text = "作者：" + Renderer.GetSafeString(map.Author, lblMapAuthor.FontIndex);
            lblGameMode.Text = "游戏模式：" + gameMode.UIName;
            lblMapSize.Text = "地图大小：" + map.GetSizeString();

            lblMapAuthor.X = MapPreviewBox.Right - lblMapAuthor.Width;

            disableGameOptionUpdateBroadcast = true;

            // Clear forced options
            foreach (var ddGameOption in DropDowns)
                ddGameOption.AllowDropDown = true;

            foreach (var checkBox in CheckBoxes)
                checkBox.AllowChecking = true;

            // Apply default options if we should

            //if (GameMode.LoadDefaultSettingsOnMapChange ||
            //    (oldGameMode != null && oldGameMode.LoadDefaultSettingsOnMapChange))
            //{
            //    foreach (var ddGameOption in DropDowns)
            //        ddGameOption.SetDefaultValue();

            //    foreach (var checkBox in CheckBoxes)
            //        checkBox.SetDefaultValue();
            //}

            // We could either pass the CheckBoxes and DropDowns of this class
            // to the Map and GameMode instances and let them apply their forced
            // options, or we could do it in this class with helper functions.
            // The second approach is probably clearer.

            // We use these temp lists to determine which options WERE NOT forced
            // by the map. We then return these to user-defined settings.
            // This prevents forced options from one map getting carried
            // to other maps.

            var checkBoxListClone = new List<GameLobbyCheckBox>(CheckBoxes);
            var dropDownListClone = new List<GameLobbyDropDown>(DropDowns);

            ApplyForcedCheckBoxOptions(checkBoxListClone, gameMode.ForcedCheckBoxValues);
            ApplyForcedCheckBoxOptions(checkBoxListClone, map.ForcedCheckBoxValues);

            ApplyForcedDropDownOptions(dropDownListClone, gameMode.ForcedDropDownValues);
            ApplyForcedDropDownOptions(dropDownListClone, map.ForcedDropDownValues);

            foreach (var chkBox in checkBoxListClone)
                chkBox.Checked = chkBox.HostChecked;

            foreach (var dd in dropDownListClone)
                dd.SelectedIndex = dd.HostSelectedIndex;

            // Enable all sides by default
            foreach (var ddSide in ddPlayerSides)
            {
                ddSide.Items.ForEach(item => item.Selectable = true);
            }

            // Enable all colors by default
            foreach (var ddColor in ddPlayerColors)
            {
                ddColor.Items.ForEach(item => item.Selectable = true);
            }

            // Apply starting locations
            foreach (var ddStart in ddPlayerStarts)
            {
                ddStart.Items.Clear();

                ddStart.AddItem("???");

                for (int i = 1; i <= Map.MaxPlayers; i++)
                    ddStart.AddItem(i.ToString());
            }


            // Check if AI players allowed
            bool AIAllowed = !(Map.MultiplayerOnly || GameMode.MultiplayerOnly) & !(Map.HumanPlayersOnly || GameMode.HumanPlayersOnly);
            foreach (var ddName in ddPlayerNames)
            {
                if (ddName.Items.Count > 3)
                {
                    ddName.Items[1].Selectable = AIAllowed;
                    ddName.Items[2].Selectable = AIAllowed;
                    ddName.Items[3].Selectable = AIAllowed;
                }
            }

            if (!AIAllowed) AIPlayers.Clear();
            IEnumerable<PlayerInfo> concatPlayerList = Players.Concat(AIPlayers);

            foreach (PlayerInfo pInfo in concatPlayerList)
            {
                if (pInfo.StartingLocation > Map.MaxPlayers || (!Map.IsCoop && (Map.ForceRandomStartLocations || GameMode.ForceRandomStartLocations)))
                    pInfo.StartingLocation = 0;
                if (!Map.IsCoop && (Map.ForceNoTeams || GameMode.ForceNoTeams))
                    pInfo.TeamId = 0;
            }

            CheckDisallowedSides();


            if (map.CoopInfo != null)
            {
                // Co-Op map disallowed color logic
                foreach (int disallowedColorIndex in map.CoopInfo.DisallowedPlayerColors)
                {
                    if (disallowedColorIndex >= MPColors.Count)
                        continue;

                    foreach (XNADropDown ddColor in ddPlayerColors)
                        ddColor.Items[disallowedColorIndex + 1].Selectable = false;

                    foreach (PlayerInfo pInfo in concatPlayerList)
                    {
                        if (pInfo.ColorId == disallowedColorIndex + 1)
                            pInfo.ColorId = 0;
                    }
                }

                // Force teams
                foreach (PlayerInfo pInfo in concatPlayerList)
                    pInfo.TeamId = 1;
            }

            OnGameOptionChanged();

            MapPreviewBox.Map = map;
            CopyPlayerDataToUI();

            disableGameOptionUpdateBroadcast = false;
        }

        private void ApplyForcedCheckBoxOptions(List<GameLobbyCheckBox> optionList,
            List<KeyValuePair<string, bool>> forcedOptions)
        {
            foreach (KeyValuePair<string, bool> option in forcedOptions)
            {
                GameLobbyCheckBox checkBox = CheckBoxes.Find(chk => chk.Name == option.Key);
                if (checkBox != null)
                {
                    checkBox.Checked = option.Value;
                    checkBox.AllowChecking = false;
                    optionList.Remove(checkBox);
                }
            }
        }

        private void ApplyForcedDropDownOptions(List<GameLobbyDropDown> optionList,
            List<KeyValuePair<string, int>> forcedOptions)
        {
            foreach (KeyValuePair<string, int> option in forcedOptions)
            {
                GameLobbyDropDown dropDown = DropDowns.Find(dd => dd.Name == option.Key);
                if (dropDown != null)
                {
                    dropDown.SelectedIndex = option.Value;
                    dropDown.AllowDropDown = false;
                    optionList.Remove(dropDown);
                }
            }
        }

        protected string AILevelToName(int aiLevel)
        {
            switch (aiLevel)
            {
                case 0:
                    return "冷酷的电脑";
                case 1:
                    return "普通的电脑";
                case 2:
                    return "简单的电脑";
            }

            return string.Empty;
        }

        protected GameType GetGameType()
        {
            int teamCount = GetPvPTeamCount();

            if (teamCount == 0)
                return GameType.FFA;

            if (teamCount == 1)
                return GameType.Coop;

            return GameType.TeamGame;
        }

        protected int GetRank()
        {
            if (GameMode == null || Map == null)
                return RANK_NONE;

            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            {
                if ((checkBox.MapScoringMode == CheckBoxMapScoringMode.DenyWhenChecked && checkBox.Checked) ||
                    (checkBox.MapScoringMode == CheckBoxMapScoringMode.DenyWhenUnchecked && !checkBox.Checked))
                {
                    return RANK_NONE;
                }
            }

            PlayerInfo localPlayer = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

            if (localPlayer == null)
                return RANK_NONE;

            if (IsPlayerSpectator(localPlayer))
                return RANK_NONE;

            // These variables are used by both the skirmish and multiplayer code paths
            int[] teamMemberCounts = new int[5];
            int lowestEnemyAILevel = 2;
            int highestAllyAILevel = 0;

            foreach (PlayerInfo aiPlayer in AIPlayers)
            {
                teamMemberCounts[aiPlayer.TeamId]++;

                if (aiPlayer.TeamId > 0 && aiPlayer.TeamId == localPlayer.TeamId)
                {
                    if (aiPlayer.ReversedAILevel > highestAllyAILevel)
                        highestAllyAILevel = aiPlayer.ReversedAILevel;
                }
                else
                {
                    if (aiPlayer.ReversedAILevel < lowestEnemyAILevel)
                        lowestEnemyAILevel = aiPlayer.ReversedAILevel;
                }
            }

            if (isMultiplayer)
            {
                if (Players.Count == 1)
                    return RANK_NONE;

                // PvP stars for 2-player and 3-player maps
                if (Map.MaxPlayers <= 3)
                {
                    List<PlayerInfo> filteredPlayers = Players.Where(p => !IsPlayerSpectator(p)).ToList();

                    if (AIPlayers.Count > 0)
                        return RANK_NONE;

                    if (filteredPlayers.Count != Map.MaxPlayers)
                        return RANK_NONE;

                    int localTeamIndex = localPlayer.TeamId;
                    if (localTeamIndex > 0 && filteredPlayers.Count(p => p.TeamId == localTeamIndex) > 1)
                        return RANK_NONE;

                    return RANK_HARD;
                }

                // Coop stars for maps with 4 or more players
                // See the code in StatisticsManager.GetRankForCoopMatch for the conditions

                if (Players.Find(p => IsPlayerSpectator(p)) != null)
                    return RANK_NONE;

                if (AIPlayers.Count == 0)
                    return RANK_NONE;

                if (Players.Find(p => p.TeamId != localPlayer.TeamId) != null)
                    return RANK_NONE;

                if (Players.Find(p => p.TeamId == 0) != null)
                    return RANK_NONE;

                if (AIPlayers.Find(p => p.TeamId == 0) != null)
                    return RANK_NONE;

                teamMemberCounts[localPlayer.TeamId] += Players.Count;

                if (lowestEnemyAILevel < highestAllyAILevel)
                {
                    // Check that the player's AI allies aren't stronger 
                    return RANK_NONE;
                }

                // Check that all teams have at least as many players
                // as the human players' team
                int allyCount = teamMemberCounts[localPlayer.TeamId];

                for (int i = 1; i < 5; i++)
                {
                    if (i == localPlayer.TeamId)
                        continue;

                    if (teamMemberCounts[i] > 0)
                    {
                        if (teamMemberCounts[i] < allyCount)
                            return RANK_NONE;
                    }
                }

                return lowestEnemyAILevel + 1;
            }

            // *********
            // Skirmish!
            // *********

            if (AIPlayers.Count != Map.MaxPlayers - 1)
                return RANK_NONE;

            teamMemberCounts[localPlayer.TeamId]++;

            if (lowestEnemyAILevel < highestAllyAILevel)
            {
                // Check that the player's AI allies aren't stronger 
                return RANK_NONE;
            }

            if (localPlayer.TeamId > 0)
            {
                // Check that all teams have at least as many players
                // as the local player's team
                int allyCount = teamMemberCounts[localPlayer.TeamId];

                for (int i = 1; i < 5; i++)
                {
                    if (i == localPlayer.TeamId)
                        continue;

                    if (teamMemberCounts[i] > 0)
                    {
                        if (teamMemberCounts[i] < allyCount)
                            return RANK_NONE;
                    }
                }

                // Check that there is a team other than the players' team that is at least as large
                bool pass = false;
                for (int i = 1; i < 5; i++)
                {
                    if (i == localPlayer.TeamId)
                        continue;

                    if (teamMemberCounts[i] >= allyCount)
                    {
                        pass = true;
                        break;
                    }
                }

                if (!pass)
                    return RANK_NONE;
            }

            return lowestEnemyAILevel + 1;
        }

        protected string AddGameOptionPreset(string name)
        {
            string error = GameOptionPreset.IsNameValid(name);
            if (!string.IsNullOrEmpty(error))
                return error;

            GameOptionPreset preset = new GameOptionPreset(name);
            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            {
                preset.AddCheckBoxValue(checkBox.Name, checkBox.Checked);
            }

            foreach (GameLobbyDropDown dropDown in DropDowns)
            {
                preset.AddDropDownValue(dropDown.Name, dropDown.SelectedIndex);
            }

            GameOptionPresets.Instance.AddPreset(preset);
            return null;
        }

        public bool LoadGameOptionPreset(string name)
        {
            GameOptionPreset preset = GameOptionPresets.Instance.GetPreset(name);
            if (preset == null)
                return false;

            disableGameOptionUpdateBroadcast = true;

            var checkBoxValues = preset.GetCheckBoxValues();
            foreach (var kvp in checkBoxValues)
            {
                GameLobbyCheckBox checkBox = CheckBoxes.Find(c => c.Name == kvp.Key);
                if (checkBox != null && checkBox.AllowChanges && checkBox.AllowChecking)
                    checkBox.Checked = kvp.Value;
            }

            var dropDownValues = preset.GetDropDownValues();
            foreach (var kvp in dropDownValues)
            {
                GameLobbyDropDown dropDown = DropDowns.Find(d => d.Name == kvp.Key);
                if (dropDown != null && dropDown.AllowDropDown)
                    dropDown.SelectedIndex = kvp.Value;
            }

            disableGameOptionUpdateBroadcast = false;
            OnGameOptionChanged();
            return true;
        }

        protected abstract bool AllowPlayerOptionsChange();
    }
}
