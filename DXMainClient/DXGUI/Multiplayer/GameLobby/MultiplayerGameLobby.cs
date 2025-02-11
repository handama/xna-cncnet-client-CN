﻿using System;
using System.Collections.Generic;
using System.Linq;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;
using ClientCore;
using System.IO;
using Rampastring.Tools;
using ClientCore.Statistics;
using DTAClient.DXGUI.Generic;
using DTAClient.Domain.Multiplayer;
using ClientGUI;
using System.Text;
using DTAClient.Domain;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A generic base class for multiplayer game lobbies (CnCNet and LAN).
    /// </summary>
    public abstract class MultiplayerGameLobby : GameLobbyBase, ISwitchable
    {
        private const int MAX_DICE = 10;
        private const int MAX_DIE_SIDES = 100;

        public MultiplayerGameLobby(WindowManager windowManager, string iniName, 
            TopBar topBar, List<GameMode> GameModes, MapLoader mapLoader, DiscordHandler discordHandler)
            : base(windowManager, iniName, GameModes, true, discordHandler)
        {
            TopBar = topBar;
            MapLoader = mapLoader;

            chatBoxCommands = new List<ChatBoxCommand>
            {
                new ChatBoxCommand("HIDEMAPS", "隐藏地图列表(仅房主)", true,
                    s => HideMapList()),
                new ChatBoxCommand("SHOWMAPS", "展示地图列表(仅房主)", true,
                    s => ShowMapList()),
                new ChatBoxCommand("FRAMESENDRATE", "改变阶延迟/帧发送速率(默认2)(仅房主)", true,
                    s => SetFrameSendRate(s)),
                new ChatBoxCommand("MAXAHEAD", "改变最大提前(默认0)(仅房主)", true,
                    s => SetMaxAhead(s)),
                new ChatBoxCommand("PROTOCOLVERSION", "改变协议版本(默认2)(仅房主)", true,
                    s => SetProtocolVersion(s)),
                new ChatBoxCommand("LOADMAP", $"加载一个非官方地图，使用在 \\Maps\\{ClientConfiguration.Instance.CustomMapFolderName}\\ 文件夹里的文件名", true, LoadCustomMap),
                new ChatBoxCommand("RANDOMSTARTS", "启用完全随机的起始位置(只适用于基于TS的游戏)", true,
                    s => SetStartingLocationClearance(s)),
                new ChatBoxCommand("ROLL", "掷个骰子，比如 /roll 3d6 表示在1~6里面选择三个", false, RollDiceCommand),
                new ChatBoxCommand("SAVEOPTIONS [name]", "保存游戏设置预设(name为可选项)", false, HandleGameOptionPresetSaveCommand),
                new ChatBoxCommand("LOADOPTIONS [name]", "加载游戏设置预设(name为可选项)", true, HandleGameOptionPresetLoadCommand)
            };
        }

        protected XNACheckBox[] ReadyBoxes;

        protected ChatListBox lbChatMessages;
        protected XNAChatTextBox tbChatInput;
        protected XNAClientButton btnLockGame;
        protected XNAClientCheckBox chkAutoReady;


        protected bool IsHost = false;

        private bool locked = false;
        protected bool Locked
        {
            get => locked;
            set
            {
                bool oldLocked = locked;
                locked = value;
                if (oldLocked != value)
                    UpdateDiscordPresence();
            }
        }

        protected EnhancedSoundEffect sndJoinSound;
        protected EnhancedSoundEffect sndLeaveSound;
        protected EnhancedSoundEffect sndMessageSound;
        protected EnhancedSoundEffect sndGetReadySound;
        protected Texture2D[] PingTextures;
        protected TopBar TopBar;

        protected int FrameSendRate { get; set; } = 2;

        /// <summary>
        /// Controls the MaxAhead parameter. The default value of 0 means that 
        /// the value is not written to spawn.ini, which allows the spawner the
        /// calculate and assign the MaxAhead value.
        /// </summary>
        protected int MaxAhead { get; set; }

        protected int ProtocolVersion { get; set; } = 2;

        protected List<ChatBoxCommand> chatBoxCommands;

        private FileSystemWatcher fsw;

        private bool gameSaved = false;

        protected MapLoader MapLoader;

        /// <summary>
        /// Allows derived classes to add their own chat box commands.
        /// </summary>
        /// <param name="command">The command to add.</param>
        protected void AddChatBoxCommand(ChatBoxCommand command)
        {
            chatBoxCommands.Add(command);
        }

        public override void Initialize()
        {
            Name = "MultiplayerGameLobby";

            base.Initialize();
            PingTextures = new Texture2D[5]
            {
                AssetLoader.LoadTexture("ping0.png"),
                AssetLoader.LoadTexture("ping1.png"),
                AssetLoader.LoadTexture("ping2.png"),
                AssetLoader.LoadTexture("ping3.png"),
                AssetLoader.LoadTexture("ping4.png")
            };
            InitPlayerOptionDropdowns();

            ReadyBoxes = new XNACheckBox[MAX_PLAYER_COUNT];

            int readyBoxX = GameOptionsIni.GetIntValue(Name, "PlayerReadyBoxX", 7);
            int readyBoxY = GameOptionsIni.GetIntValue(Name, "PlayerReadyBoxY", 4);

            for (int i = 0; i < MAX_PLAYER_COUNT; i++)
            {
                XNACheckBox chkPlayerReady = new XNACheckBox(WindowManager);
                chkPlayerReady.Name = "chkPlayerReady" + i;
                chkPlayerReady.Checked = false;
                chkPlayerReady.AllowChecking = false;
                chkPlayerReady.ClientRectangle = new Rectangle(readyBoxX, ddPlayerTeams[i].Y + readyBoxY,
                    0, 0);

                PlayerOptionsPanel.AddChild(chkPlayerReady);

                chkPlayerReady.DisabledClearTexture = chkPlayerReady.ClearTexture;
                chkPlayerReady.DisabledCheckedTexture = chkPlayerReady.CheckedTexture;

                ReadyBoxes[i] = chkPlayerReady;
                ddPlayerSides[i].AddItem("观察者", AssetLoader.LoadTexture("spectatoricon.png"));
            }

            ddGameMode.ClientRectangle = new Rectangle(
                MapPreviewBox.X - 12 - ddGameMode.Width,
                MapPreviewBox.Y, ddGameMode.Width,
                ddGameMode.Height);

            lblGameModeSelect.ClientRectangle = new Rectangle(
                btnLaunchGame.X, ddGameMode.Y + 1,
                lblGameModeSelect.Width, lblGameModeSelect.Height);

            lbMapList.ClientRectangle = new Rectangle(btnLaunchGame.X, 
                MapPreviewBox.Y + 23,
                MapPreviewBox.X - btnLaunchGame.X - 12,
                MapPreviewBox.Height - 23);

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = "lbChatMessages";
            lbChatMessages.ClientRectangle = new Rectangle(lbMapList.X, 
                GameOptionsPanel.Y,
               lbMapList.Width, GameOptionsPanel.Height - 24);
            lbChatMessages.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.LineHeight = 16;
            lbChatMessages.DrawOrder = -1;
            lbChatMessages.UpdateOrder = -1;

            tbChatInput = new XNAChatTextBox(WindowManager);
            tbChatInput.Name = "tbChatInput";
            tbChatInput.Suggestion = "在这里聊天...";
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.X, 
                lbChatMessages.Bottom + 3,
                lbChatMessages.Width, 21);
            tbChatInput.MaximumTextLength = 150;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;
            tbChatInput.DrawOrder = 1;
            tbChatInput.UpdateOrder = 1;

            btnLockGame = new XNAClientButton(WindowManager);
            btnLockGame.Name = "btnLockGame";
            btnLockGame.ClientRectangle = new Rectangle(btnLaunchGame.Right + 12,
                btnLaunchGame.Y, 133, 23);
            btnLockGame.Text = "锁定游戏";
            btnLockGame.LeftClick += BtnLockGame_LeftClick;

            chkAutoReady = new XNAClientCheckBox(WindowManager);
            chkAutoReady.Name = "chkAutoReady";
            chkAutoReady.ClientRectangle = new Rectangle(btnLaunchGame.Right + 12,
                btnLaunchGame.Y + 2, 133, 23);
            chkAutoReady.Text = "自动准备";
            chkAutoReady.CheckedChanged += ChkAutoReady_CheckedChanged;
            chkAutoReady.Disable();

            AddChild(lbChatMessages);
            AddChild(tbChatInput);
            AddChild(btnLockGame);
            AddChild(chkAutoReady);

            MapPreviewBox.LocalStartingLocationSelected += MapPreviewBox_LocalStartingLocationSelected;
            MapPreviewBox.StartingLocationApplied += MapPreviewBox_StartingLocationApplied;

            randomMapWindow.btnGenerateMap.LeftClick += BtnGenerateMap_LeftClick;

            sndJoinSound = new EnhancedSoundEffect("joingame.wav");
            sndLeaveSound = new EnhancedSoundEffect("leavegame.wav");
            sndMessageSound = new EnhancedSoundEffect("message.wav");
            sndGetReadySound = new EnhancedSoundEffect("getready.wav", 0.0, 0.0, 5.0f);

            if (SavedGameManager.AreSavedGamesAvailable())
            {
                fsw = new FileSystemWatcher(ProgramConstants.GamePath + "Saved Games", "*.NET");
                fsw.Created += fsw_Created;
                fsw.Changed += fsw_Created;
                fsw.EnableRaisingEvents = false;
            }
            else
                Logger.Log("MultiplayerGameLobby: Saved games are not available!");
        }


        public override void BtnGenerateMap_LeftClick(object sender, EventArgs e)
        {
            AddNotice("正在生成随机地图...");
            base.BtnGenerateMap_LeftClick(sender, e);

            AddNotice($"随机地图{RandomMapName}已成功生成。");
            LoadCustomMap(RandomMapName + ".map");


        }
        /// <summary>
        /// Performs initialization that is necessary after derived 
        /// classes have performed their own initialization.
        /// </summary>
        protected void PostInitialize()
        {
            InitializeWindow();
            CenterOnParent();
            LoadDefaultMap();
        }

        private void fsw_Created(object sender, FileSystemEventArgs e)
        {
            AddCallback(new Action<FileSystemEventArgs>(FSWEvent), e);
        }

        private void FSWEvent(FileSystemEventArgs e)
        {
            Logger.Log("FSW Event: " + e.FullPath);

            if (Path.GetFileName(e.FullPath) == "SAVEGAME.NET")
            {
                if (!gameSaved)
                {
                    bool success = SavedGameManager.InitSavedGames();

                    if (!success)
                        return;
                }

                gameSaved = true;

                SavedGameManager.RenameSavedGame();
            }
        }

        protected override void StartGame()
        {
            if (fsw != null)
                fsw.EnableRaisingEvents = true;

            base.StartGame();
        }

        protected override void GameProcessExited()
        {
            gameSaved = false;

            if (fsw != null)
                fsw.EnableRaisingEvents = false;

            base.GameProcessExited();

            if (IsHost)
            {
                GenerateGameID();
                DdGameMode_SelectedIndexChanged(null, EventArgs.Empty); // Refresh ranks
            }
        }

        private void GenerateGameID()
        {
            int i = 0;

            while (i < 20)
            {
                string s = DateTime.Now.Day.ToString() +
                    DateTime.Now.Month.ToString() +
                    DateTime.Now.Hour.ToString() +
                    DateTime.Now.Minute.ToString();

                UniqueGameID = int.Parse(i.ToString() + s);

                if (StatisticsManager.Instance.GetMatchWithGameID(UniqueGameID) == null)
                    break;

                i++;
            }
        }

        private void BtnLockGame_LeftClick(object sender, EventArgs e)
        {
            HandleLockGameButtonClick();
        }

        protected virtual void HandleLockGameButtonClick()
        {
            if (Locked)
                UnlockGame(true);
            else
                LockGame();
        }

        protected abstract void LockGame();

        protected abstract void UnlockGame(bool manual);

        private void TbChatInput_EnterPressed(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbChatInput.Text))
                return;

            if (tbChatInput.Text.StartsWith("/"))
            {
                string text = tbChatInput.Text;
                string command;
                string parameters;

                int spaceIndex = text.IndexOf(' ');

                if (spaceIndex == -1)
                {
                    command = text.Substring(1).ToUpper();
                    parameters = string.Empty;
                }
                else
                {
                    command = text.Substring(1, spaceIndex - 1);
                    parameters = text.Substring(spaceIndex + 1);
                }
                
                tbChatInput.Text = string.Empty;

                foreach (var chatBoxCommand in chatBoxCommands)
                {
                    if (command.ToUpper() == chatBoxCommand.Command)
                    {
                        if (!IsHost && chatBoxCommand.HostOnly)
                        {
                            AddNotice(string.Format("/{0} 只能由房主使用。", chatBoxCommand.CommandRaw));
                            return;
                        }

                        chatBoxCommand.Action(parameters);
                        return;
                    }
                }

                StringBuilder sb = new StringBuilder("要使用命令，像这样在对话框中输入： /<命令> 。可用的对话框命令：");
                foreach (var chatBoxCommand in chatBoxCommands)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(Environment.NewLine);
                    sb.Append($"{chatBoxCommand.CommandRaw}: {chatBoxCommand.Description}");
                }
                XNAMessageBox.Show(WindowManager, "对话框命令帮助", sb.ToString());
                return;
            }

            SendChatMessage(tbChatInput.Text);
            tbChatInput.Text = string.Empty;
        }

        private void ChkAutoReady_CheckedChanged(object sender, EventArgs e)
        {
            btnLaunchGame.Enabled = !chkAutoReady.Checked;
            RequestReadyStatus();
        }

        protected void ResetAutoReadyCheckbox()
        {
            chkAutoReady.CheckedChanged -= ChkAutoReady_CheckedChanged;
            chkAutoReady.Checked = false;
            chkAutoReady.CheckedChanged += ChkAutoReady_CheckedChanged;
            btnLaunchGame.Enabled = true;
        }

        private void SetFrameSendRate(string value)
        {
            bool success = int.TryParse(value, out int intValue);

            if (!success)
            {
                AddNotice("命令语法：/FrameSendRate <数字>。");
                return;
            }

            FrameSendRate = intValue;
            AddNotice("帧发送速率被变更为：" + intValue + "。");

            OnGameOptionChanged();
            ClearReadyStatuses();
        }

        private void SetMaxAhead(string value)
        {
            bool success = int.TryParse(value, out int intValue);

            if (!success)
            {
                AddNotice("命令语法：/MaxAhead <数字>。");
                return;
            }

            MaxAhead = intValue;
            AddNotice("最大提前被更改为：" + intValue + "。");

            OnGameOptionChanged();
            ClearReadyStatuses();
        }

        private void SetProtocolVersion(string value)
        {
            bool success = int.TryParse(value, out int intValue);

            if (!success)
            {
                AddNotice("命令语法：/ProtocolVersion <数字>。");
                return;
            }

            if (!(intValue == 0 || intValue == 2))
            {
                AddNotice("协议版本可用值只有0和2。");
                return;
            }

            ProtocolVersion = intValue;
            AddNotice("协议版本被更改为：" + intValue + "。");

            OnGameOptionChanged();
            ClearReadyStatuses();
        }

        private void SetStartingLocationClearance(string value)
        {
            bool removeStartingLocations = Conversions.BooleanFromString(value, RemoveStartingLocations);

            SetRandomStartingLocations(removeStartingLocations);

            OnGameOptionChanged();
            ClearReadyStatuses();
        }

        /// <summary>
        /// Enables or disables completely random starting locations and informs
        /// the user accordingly.
        /// </summary>
        /// <param name="newValue">The new value of completely random starting locations.</param>
        protected void SetRandomStartingLocations(bool newValue)
        {
            if (newValue != RemoveStartingLocations)
            {
                RemoveStartingLocations = newValue;
                if (RemoveStartingLocations)
                    AddNotice("房主开起了完全随机起始位置(只对常规地图有效)。");
                else
                    AddNotice("房主禁止了完全随机起始位置。");
            }
        }

        /// <summary>
        /// Handles the dice rolling command.
        /// </summary>
        /// <param name="dieType">The parameters given for the command by the user.</param>
        private void RollDiceCommand(string dieType)
        {
            int dieSides = 6;
            int dieCount = 1;

            if (!string.IsNullOrEmpty(dieType))
            {
                string[] parts = dieType.Split('d');
                if (parts.Length == 2)
                {
                    if (!int.TryParse(parts[0], out dieCount) || !int.TryParse(parts[1], out dieSides))
                    {
                        AddNotice("无效的骰子。命令语法：/roll <骰子数>d<骰子面>。");
                        return;
                    }
                }
            }

            if (dieCount > MAX_DICE || dieCount < 1)
            {
                AddNotice("你一次只能掷1~10个骰子。");
                return;
            }
            
            if (dieSides > MAX_DIE_SIDES || dieSides < 2)
            {
                AddNotice("一个骰子只能有2~100个面。");
                return;
            }

            int[] results = new int[dieCount];
            Random random = new Random();
            for (int i = 0; i < dieCount; i++)
            {
                results[i] = random.Next(1, dieSides + 1);
            }

            BroadcastDiceRoll(dieSides, results);
        }

        /// <summary>
        /// Handles custom map load command.
        /// </summary>
        /// <param name="mapName">Name of the map given as a parameter, without file extension.</param>
        private void LoadCustomMap(string mapName)
        {
            Map map = MapLoader.LoadCustomMapManual($"Maps\\{ClientConfiguration.Instance.CustomMapFolderName}\\{mapName}", out string resultMessage);
            if (map != null)
                AddNotice(resultMessage);
            else
            {
                AddNotice(resultMessage, Color.Red);
                return;
            }
                

            foreach (var gm in GameModes)
            {
                foreach (var thisGm in map.GameModes)
                {
                    if (gm.Name == thisGm)
                    {
                        ChangeMap(gm, map);
                    }
                }
            }

            ListMaps();
        }

        /// <summary>
        /// Override in derived classes to broadcast the results of rolling dice to other players.
        /// </summary>
        /// <param name="dieSides">The number of sides in the dice.</param>
        /// <param name="results">The results of the dice roll.</param>
        protected abstract void BroadcastDiceRoll(int dieSides, int[] results);

        /// <summary>
        /// Parses and lists the results of rolling dice.
        /// </summary>
        /// <param name="senderName">The player that rolled the dice.</param>
        /// <param name="result">The results of rolling dice, with each die separated by a comma
        /// and the number of sides in the die included as the first number.</param>
        /// <example>
        /// HandleDiceRollResult("Rampastring", "6,3,5,1") would mean that
        /// Rampastring rolled three six-sided dice and got 3, 5 and 1.
        /// </example>
        protected void HandleDiceRollResult(string senderName, string result)
        {
            if (string.IsNullOrEmpty(result))
                return;

            string[] parts = result.Split(',');
            if (parts.Length < 2 || parts.Length > MAX_DICE + 1)
                return;

            int[] intArray = Array.ConvertAll(parts, (s) => { return Conversions.IntFromString(s, -1); });
            int dieSides = intArray[0];
            if (dieSides < 1 || dieSides > MAX_DIE_SIDES)
                return;
            int[] results = new int[intArray.Length - 1];
            Array.ConstrainedCopy(intArray, 1, results, 0, results.Length);

            for (int i = 1; i < intArray.Length; i++)
            {
                if (intArray[i] < 1 || intArray[i] > dieSides)
                    return;
            }

            PrintDiceRollResult(senderName, dieSides, results);
        }

        /// <summary>
        /// Prints the result of rolling dice.
        /// </summary>
        /// <param name="senderName">The player who rolled dice.</param>
        /// <param name="dieSides">The number of sides in the die.</param>
        /// <param name="results">The results of the roll.</param>
        protected void PrintDiceRollResult(string senderName, int dieSides, int[] results)
        {
            AddNotice($"{senderName} 掷了 {results.Length}d{dieSides} 得到了 {string.Join(", ", results)}。");
        }

        private void HandleGameOptionPresetSaveCommand(string presetName)
        {
            string error = AddGameOptionPreset(presetName);
            
            if (!string.IsNullOrEmpty(error))
                AddNotice(error);
            else
                AddNotice($"游戏设置预设 {presetName} 已成功保存。");
        }

        private void HandleGameOptionPresetLoadCommand(string presetName)
        {
            if (LoadGameOptionPreset(presetName))
                AddNotice($"游戏设置预设 {presetName} 已成功加载。");
            else
                AddNotice($"游戏设置预设 {presetName} 找不到！");
        }

        protected abstract void SendChatMessage(string message);

        /// <summary>
        /// Changes the game lobby's UI depending on whether the local player is the host.
        /// </summary>
        /// <param name="isHost">Determines whether the local player is the host of the game.</param>
        protected void Refresh(bool isHost)
        {
            IsHost = isHost;
            Locked = false;

            UpdateMapPreviewBoxEnabledStatus();
            //MapPreviewBox.EnableContextMenu = IsHost;

            btnLaunchGame.Text = IsHost ? "开始游戏" : "准备";

            if (IsHost)
            {
                ShowMapList();

                btnLockGame.Text = "锁定房间";
                btnLockGame.Enabled = true;
                btnLockGame.Visible = true;
                chkAutoReady.Disable();

                lblPlayerNumbers.Enable();
                lblAuthor.Enable();
                ddAuthor.Enable();
                ddplayerNumbers.Enable();
                btnCreateRandomMap.Enable();

                foreach (GameLobbyDropDown dd in DropDowns)
                {
                    dd.InputEnabled = true;
                    dd.SelectedIndex = dd.UserSelectedIndex;
                }

                foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                {
                    checkBox.AllowChanges = true;
                    checkBox.Checked = checkBox.UserChecked;
                }

                GenerateGameID();
            }
            else
            {
                HideMapList();

                lblPlayerNumbers.Disable();
                lblAuthor.Disable();
                ddAuthor.Disable();
                ddplayerNumbers.Disable();
                btnCreateRandomMap.Disable();

                btnLockGame.Enabled = false;
                btnLockGame.Visible = false;
                chkAutoReady.GetAttributes(ThemeIni);

                foreach (GameLobbyDropDown dd in DropDowns)
                    dd.InputEnabled = false;

                foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                    checkBox.AllowChanges = false;
            }

            LoadDefaultMap();

            lbChatMessages.Clear();
            lbChatMessages.TopIndex = 0;

            lbChatMessages.AddItem("输入 / 查看可用的对话框命令。", Color.Silver, true);

            if (SavedGameManager.GetSaveGameCount() > 0)
            {
                lbChatMessages.AddItem("检测到以前游戏中保存的多人游戏。" +
                    "如果你在游戏中创建新的存档，旧的存档会被删除。",
                    Color.Yellow, true);
            }
        }

        private void HideMapList()
        {
            lbChatMessages.ClientRectangle = new Rectangle(lbMapList.X,
                PlayerOptionsPanel.Y,
                lbMapList.Width,
                MapPreviewBox.Bottom - PlayerOptionsPanel.Y);
            lbChatMessages.Name = "lbChatMessages_Player";

            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.X,
                lbChatMessages.Bottom + 3,
                lbChatMessages.Width, 21);
            tbChatInput.Name = "tbChatInput_Player";

            ddGameMode.Disable();
            lblGameModeSelect.Disable();
            lbMapList.Disable();
            tbMapSearch.Disable();
            btnPickRandomMap.Disable();

            lbChatMessages.GetAttributes(ThemeIni);
            tbChatInput.GetAttributes(ThemeIni);
            lbMapList.GetAttributes(ThemeIni);
        }

        private void ShowMapList()
        {
            lbMapList.ClientRectangle = new Rectangle(btnLaunchGame.X,
                MapPreviewBox.Y + 23,
                MapPreviewBox.X - btnLaunchGame.X - 12,
                MapPreviewBox.Height - 23);

            lbChatMessages.ClientRectangle = new Rectangle(lbMapList.X,
                GameOptionsPanel.Y,
                lbMapList.Width, GameOptionsPanel.Height - 26);
            lbChatMessages.Name = "lbChatMessages_Host";

            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.X,
                lbChatMessages.Bottom + 3,
                lbChatMessages.Width, 21);
            tbChatInput.Name = "tbChatInput_Host";

            ddGameMode.Enable();
            lblGameModeSelect.Enable();
            lbMapList.Enable();
            tbMapSearch.Enable();
            btnPickRandomMap.GetAttributes(ThemeIni);

            lbChatMessages.GetAttributes(ThemeIni);
            tbChatInput.GetAttributes(ThemeIni);
            lbMapList.GetAttributes(ThemeIni);
        }

        private void MapPreviewBox_LocalStartingLocationSelected(object sender, LocalStartingLocationEventArgs e)
        {
            int mTopIndex = Players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);

            if (mTopIndex == -1 || Players[mTopIndex].SideId == ddPlayerSides[0].Items.Count - 1)
                return;

            ddPlayerStarts[mTopIndex].SelectedIndex = e.StartingLocationIndex;
        }

        private void MapPreviewBox_StartingLocationApplied(object sender, EventArgs e)
        {
            ClearReadyStatuses();
            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
        }

        /// <summary>
        /// Handles the user's click on the "Launch Game" / "I'm Ready" button.
        /// If the local player is the game host, checks if the game can be launched and then
        /// launches the game if it's allowed. If the local player isn't the game host,
        /// sends a ready request.
        /// </summary>
        protected override void BtnLaunchGame_LeftClick(object sender, EventArgs e)
        {
            if (!IsHost)
            {
                RequestReadyStatus();
                return;
            }

            if (!Locked)
            {
                LockGameNotification();
                return;
            }

            List<int> occupiedColorIds = new List<int>();
            foreach (PlayerInfo player in Players)
            {
                if (occupiedColorIds.Contains(player.ColorId) && player.ColorId > 0)
                {
                    SharedColorsNotification();
                    return;
                }

                occupiedColorIds.Add(player.ColorId);
            }

            if (AIPlayers.Count(pInfo => pInfo.SideId == ddPlayerSides[0].Items.Count - 1) > 0)
            {
                AISpectatorsNotification();
                return;
            }

            if (Map.EnforceMaxPlayers)
            {
                foreach (PlayerInfo pInfo in Players)
                {
                    if (pInfo.StartingLocation == 0)
                        continue;

                    if (Players.Concat(AIPlayers).ToList().Find(
                        p => p.StartingLocation == pInfo.StartingLocation && 
                        p.Name != pInfo.Name) != null)
                    {
                        SharedStartingLocationNotification();
                        return;
                    }
                }

                for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
                {
                    int startingLocation = AIPlayers[aiId].StartingLocation;

                    if (startingLocation == 0)
                        continue;

                    int index = AIPlayers.FindIndex(aip => aip.StartingLocation == startingLocation);

                    if (index > -1 && index != aiId)
                    {
                        SharedStartingLocationNotification();
                        return;
                    }
                }

                int totalPlayerCount = Players.Count(p => p.SideId < ddPlayerSides[0].Items.Count - 1)
                    + AIPlayers.Count;

                if (totalPlayerCount < Map.MinPlayers)
                {
                    InsufficientPlayersNotification();
                    return;
                }

                if (Map.EnforceMaxPlayers && totalPlayerCount > Map.MaxPlayers)
                {
                    TooManyPlayersNotification();
                    return;
                }
            }

            int iId = 0;
            foreach (PlayerInfo player in Players)
            {
                iId++;

                if (player.Name == ProgramConstants.PLAYERNAME)
                    continue;

                if (!player.Verified)
                {
                    NotVerifiedNotification(iId - 1);
                    return;
                }

                if (!player.Ready)
                {
                    if (player.IsInGame)
                    {
                        StillInGameNotification(iId - 1);
                    }
                    else
                    {
                        GetReadyNotification();
                    }

                    return;
                }
            }

            HostLaunchGame();
        }

        protected virtual void LockGameNotification()
        {
            AddNotice("开始游戏前需要锁定房间。");
        }

        protected virtual void SharedColorsNotification()
        {
            AddNotice("玩家不能使用同样的颜色。");
        }

        protected virtual void AISpectatorsNotification()
        {
            AddNotice("AI不想只是看着，他们想参与！");
        }

        protected virtual void SharedStartingLocationNotification()
        {
            AddNotice("玩家不能占据相同的起始位置。");
        }

        protected virtual void NotVerifiedNotification(int playerIndex)
        {
            if (playerIndex > -1 && playerIndex < Players.Count)
            {
                AddNotice(string.Format("无法开启游戏；玩家 {0} 无法被证实。", Players[playerIndex].Name));
            }
        }

        protected virtual void StillInGameNotification(int playerIndex)
        {
            if (playerIndex > -1 && playerIndex < Players.Count)
            {
                AddNotice("无法开启游戏；玩家" + Players[playerIndex].Name + "仍在你之前的游戏中游玩。");
            }
        }

        protected virtual void GetReadyNotification()
        {
            AddNotice("房主想要开始游戏，但是部分玩家没有就绪。");
            sndGetReadySound.Play();
        }

        protected virtual void InsufficientPlayersNotification()
        {
            if (Map != null)
                AddNotice("无法开启游戏：这张图最低要求有" + Map.MinPlayers + "位玩家。");
        }

        protected virtual void TooManyPlayersNotification()
        {
            if (Map != null)
                AddNotice("无法开启游戏：这张图最多只能有" + Map.MaxPlayers + "位玩家。");
        }

        public virtual void Clear()
        {
            if (!IsHost)
                AIPlayers.Clear();

            Players.Clear();
        }

        protected override void OnGameOptionChanged()
        {
            base.OnGameOptionChanged();

            ClearReadyStatuses();
            CopyPlayerDataToUI();
        }

        protected abstract void HostLaunchGame();



        protected override void CopyPlayerDataFromUI(object sender, EventArgs e)
        {
            if (PlayerUpdatingInProgress)
                return;

            if (IsHost)
            {
                base.CopyPlayerDataFromUI(sender, e);
                BroadcastPlayerOptions();
                return;
            }

            int mTopIndex = Players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);

            if (mTopIndex == -1)
                return;

            int requestedSide = ddPlayerSides[mTopIndex].SelectedIndex;
            int requestedColor = ddPlayerColors[mTopIndex].SelectedIndex;
            int requestedStart = ddPlayerStarts[mTopIndex].SelectedIndex;
            int requestedTeam = ddPlayerTeams[mTopIndex].SelectedIndex;

            RequestPlayerOptions(requestedSide, requestedColor, requestedStart, requestedTeam);
        }

        protected override void CopyPlayerDataToUI()
        {
            if (Players.Count + AIPlayers.Count > MAX_PLAYER_COUNT)
                return;

            base.CopyPlayerDataToUI();
            ClearPingIndicators();
            if (IsHost)
            {
                for (int pId = 1; pId < Players.Count; pId++)
                {
                    ddPlayerNames[pId].AllowDropDown = true;
                }
            }

            for (int pId = 0; pId < Players.Count; pId++)
            {
                ReadyBoxes[pId].Checked = Players[pId].Ready;
                UpdatePlayerPingIndicator(Players[pId]);
            }

            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                ReadyBoxes[aiId + Players.Count].Checked = true;
            }

            for (int i = AIPlayers.Count + Players.Count; i < MAX_PLAYER_COUNT; i++)
            {
                ReadyBoxes[i].Checked = false;
            }
        }

        protected virtual void ClearPingIndicators()
        {
            foreach (XNAClientDropDown dd in ddPlayerNames)
            {
                dd.Items[0].Texture = null;
                dd.ToolTip.Text = string.Empty;
            }
        }

        protected virtual void UpdatePlayerPingIndicator(PlayerInfo pInfo)
        {
            XNAClientDropDown ddPlayerName = ddPlayerNames[pInfo.Index];
            ddPlayerName.Items[0].Texture = GetTextureForPing(pInfo.Ping);
            if (pInfo.Ping < 0)
                ddPlayerName.ToolTip.Text = "延迟：? ms";
            else
                ddPlayerName.ToolTip.Text = $"延迟：{pInfo.Ping} ms";
        }

        private Texture2D GetTextureForPing(int ping)
        {
            switch (ping)
            {
                case int p when (p > 350):
                    return PingTextures[4];
                case int p when (p > 250):
                    return PingTextures[3];
                case int p when (p > 100):
                    return PingTextures[2];
                case int p when (p >= 0):
                    return PingTextures[1];
                default:
                    return PingTextures[0];
            }
        }

        protected abstract void BroadcastPlayerOptions();

        protected abstract void RequestPlayerOptions(int side, int color, int start, int team);

        protected abstract void RequestReadyStatus();

        protected void AddNotice(string message)
        {
            AddNotice(message, Color.White);
        }

        protected abstract void AddNotice(string message, Color color);

        protected override bool AllowPlayerOptionsChange()
        {
            return IsHost;
        }

        protected override void ChangeMap(GameMode gameMode, Map map)
        {
            base.ChangeMap(gameMode, map);

            ClearReadyStatuses();

            //if (IsHost)
            //    OnGameOptionChanged();
        }

        protected override void WriteSpawnIniAdditions(IniFile iniFile)
        {
            base.WriteSpawnIniAdditions(iniFile);
            iniFile.SetIntValue("Settings", "FrameSendRate", FrameSendRate);
            if (MaxAhead > 0)
                iniFile.SetIntValue("Settings", "MaxAhead", MaxAhead);
            iniFile.SetIntValue("Settings", "Protocol", ProtocolVersion);
        }

        protected override int GetDefaultMapRankIndex(Map map)
        {
            if (map.MaxPlayers > 3)
                return StatisticsManager.Instance.GetCoopRankForDefaultMap(map.Name, map.MaxPlayers);

            if (StatisticsManager.Instance.HasWonMapInPvP(map.Name, GameMode.UIName, map.MaxPlayers))
                return 2;

            return -1;
        }

        public void SwitchOn()
        {
            Enabled = true;
            Visible = true;
        }

        public void SwitchOff()
        {
            Enabled = false;
            Visible = false;
        }

        public abstract string GetSwitchName();

        protected override void UpdateMapPreviewBoxEnabledStatus()
        {
            if (Map != null && GameMode != null)
            {
                bool disablestartlocs = (Map.ForceRandomStartLocations || GameMode.ForceRandomStartLocations);
                MapPreviewBox.EnableContextMenu = disablestartlocs ? false : IsHost;
                MapPreviewBox.EnableStartLocationSelection = !disablestartlocs;
            }
            else
            {
                MapPreviewBox.EnableContextMenu = IsHost;
                MapPreviewBox.EnableStartLocationSelection = true;
            }
        }
    }
}
