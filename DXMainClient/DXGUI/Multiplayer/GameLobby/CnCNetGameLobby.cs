using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DTAClient.Domain.Multiplayer.CnCNet;
using System.Threading.Tasks;
using System.Threading;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public class CnCNetGameLobby : MultiplayerGameLobby
    {
        private const int HUMAN_PLAYER_OPTIONS_LENGTH = 3;
        private const int AI_PLAYER_OPTIONS_LENGTH = 2;

        private const double GAME_BROADCAST_INTERVAL = 30.0;
        private const double GAME_BROADCAST_ACCELERATION = 10.0;
        private const double INITIAL_GAME_BROADCAST_DELAY = 10.0;

        private static readonly Color ERROR_MESSAGE_COLOR = Color.Yellow;

        private const string MAP_SHARING_FAIL_MESSAGE = "MAPFAIL";
        private const string MAP_SHARING_DOWNLOAD_REQUEST = "MAPOK";
        private const string MAP_SHARING_UPLOAD_REQUEST = "MAPREQ";
        private const string MAP_SHARING_DISABLED_MESSAGE = "MAPSDISABLED";
        private const string CHEAT_DETECTED_MESSAGE = "CD";
        private const string DICE_ROLL_MESSAGE = "DR";
        private const string CHANGE_TUNNEL_SERVER_MESSAGE = "CHTNL";

        public CnCNetGameLobby(WindowManager windowManager, string iniName,
            TopBar topBar, List<GameMode> GameModes, CnCNetManager connectionManager,
            TunnelHandler tunnelHandler, GameCollection gameCollection, CnCNetUserData cncnetUserData, MapLoader mapLoader, DiscordHandler discordHandler) :
            base(windowManager, iniName, topBar, GameModes, mapLoader, discordHandler)
        {
            this.connectionManager = connectionManager;
            localGame = ClientConfiguration.Instance.LocalGame;
            LoadMapGame = ClientConfiguration.Instance.LoadMapGame;
            this.tunnelHandler = tunnelHandler;
            this.gameCollection = gameCollection;
            this.cncnetUserData = cncnetUserData;

            ctcpCommandHandlers = new CommandHandlerBase[]
            {
                new IntCommandHandler("OR", new Action<string, int>(HandleOptionsRequest)),
                new IntCommandHandler("R", new Action<string, int>(HandleReadyRequest)),
                new StringCommandHandler("PO", new Action<string, string>(ApplyPlayerOptions)),
                new StringCommandHandler("GO", new Action<string, string>(ApplyGameOptions)),
                new StringCommandHandler("START", new Action<string, string>(NonHostLaunchGame)),
                new NotificationHandler("AISPECS", HandleNotification, AISpectatorsNotification),
                new NotificationHandler("GETREADY", HandleNotification, GetReadyNotification),
                new NotificationHandler("INSFSPLRS", HandleNotification, InsufficientPlayersNotification),
                new NotificationHandler("TMPLRS", HandleNotification, TooManyPlayersNotification),
                new NotificationHandler("CLRS", HandleNotification, SharedColorsNotification),
                new NotificationHandler("SLOC", HandleNotification, SharedStartingLocationNotification),
                new NotificationHandler("LCKGME", HandleNotification, LockGameNotification),
                new IntNotificationHandler("NVRFY", HandleIntNotification, NotVerifiedNotification),
                new IntNotificationHandler("INGM", HandleIntNotification, StillInGameNotification),
                new StringCommandHandler(MAP_SHARING_UPLOAD_REQUEST, HandleMapUploadRequest),
                new StringCommandHandler(MAP_SHARING_FAIL_MESSAGE, HandleMapTransferFailMessage),
                new StringCommandHandler(MAP_SHARING_DOWNLOAD_REQUEST, HandleMapDownloadRequest),
                new NoParamCommandHandler(MAP_SHARING_DISABLED_MESSAGE, HandleMapSharingBlockedMessage),
                new NoParamCommandHandler("RETURN", ReturnNotification),
                new IntCommandHandler("TNLPNG", HandleTunnelPing),
                new StringCommandHandler("FHSH", FileHashNotification),
                new StringCommandHandler("MM", CheaterNotification),
                new StringCommandHandler(DICE_ROLL_MESSAGE, HandleDiceRollResult),
                new NoParamCommandHandler(CHEAT_DETECTED_MESSAGE, HandleCheatDetectedMessage),
                new StringCommandHandler(CHANGE_TUNNEL_SERVER_MESSAGE, HandleTunnelServerChangeMessage)
            };

            MapSharer.MapDownloadFailed += MapSharer_MapDownloadFailed;
            MapSharer.MapDownloadComplete += MapSharer_MapDownloadComplete;
            MapSharer.MapUploadFailed += MapSharer_MapUploadFailed;
            MapSharer.MapUploadComplete += MapSharer_MapUploadComplete;

            AddChatBoxCommand(new ChatBoxCommand("TUNNELINFO",
                "查看隧道服务器信息", false, PrintTunnelServerInformation));
            AddChatBoxCommand(new ChatBoxCommand("CHANGETUNNEL",
                "切换隧道服务器(仅房主)",
                true, (s) => ShowTunnelSelectionWindow("选择隧道服务器：")));
        }

        public event EventHandler GameLeft;

        private TunnelHandler tunnelHandler;
        private TunnelSelectionWindow tunnelSelectionWindow;
        private XNAClientButton btnChangeTunnel;

        private Channel channel;
        private CnCNetManager connectionManager;
        private string localGame;
        private string LoadMapGame;

        private GameCollection gameCollection;
        private CnCNetUserData cncnetUserData;

        private string hostName;

        private CommandHandlerBase[] ctcpCommandHandlers;

        private IRCColor chatColor;

        private XNATimerControl gameBroadcastTimer;

        private int playerLimit;

        private bool closed = false;

        private bool isCustomPassword = false;

        private string gameFilesHash;

        private List<string> hostUploadedMaps = new List<string>();

        private MapSharingConfirmationPanel mapSharingConfirmationPanel;

        /// <summary>
        /// The SHA1 of the latest selected map.
        /// Used for map sharing.
        /// </summary>
        private string lastMapSHA1;

        /// <summary>
        /// The game mode of the latest selected map.
        /// Used for map sharing.
        /// </summary>
        private string lastGameMode;

        public override void Initialize()
        {
            base.Initialize();

            btnChangeTunnel = new XNAClientButton(WindowManager);
            btnChangeTunnel.Name = "btnChangeTunnel";
            btnChangeTunnel.ClientRectangle = new Rectangle(btnLeaveGame.Right - btnLeaveGame.Width - 145,
                btnLeaveGame.Y, 133, 23);
            btnChangeTunnel.Text = "切换隧道";
            btnChangeTunnel.LeftClick += BtnChangeTunnel_LeftClick;
            AddChild(btnChangeTunnel);

            gameBroadcastTimer = new XNATimerControl(WindowManager);
            gameBroadcastTimer.AutoReset = true;
            gameBroadcastTimer.Interval = TimeSpan.FromSeconds(GAME_BROADCAST_INTERVAL);
            gameBroadcastTimer.Enabled = false;
            gameBroadcastTimer.TimeElapsed += GameBroadcastTimer_TimeElapsed;

            tunnelSelectionWindow = new TunnelSelectionWindow(WindowManager, tunnelHandler);
            tunnelSelectionWindow.Initialize();
            tunnelSelectionWindow.DrawOrder = 1;
            tunnelSelectionWindow.UpdateOrder = 1;
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, tunnelSelectionWindow);
            tunnelSelectionWindow.CenterOnParent();
            tunnelSelectionWindow.Disable();

            mapSharingConfirmationPanel = new MapSharingConfirmationPanel(WindowManager);
            MapPreviewBox.AddChild(mapSharingConfirmationPanel);
            mapSharingConfirmationPanel.MapDownloadConfirmed += MapSharingConfirmationPanel_MapDownloadConfirmed;

            WindowManager.AddAndInitializeControl(gameBroadcastTimer);

            PostInitialize();
        }

        private void BtnChangeTunnel_LeftClick(object sender, EventArgs e) => ShowTunnelSelectionWindow("选择隧道服务器：");

        private void GameBroadcastTimer_TimeElapsed(object sender, EventArgs e) => BroadcastGame();

        public void SetUp(Channel channel, bool isHost, int playerLimit, 
            CnCNetTunnel tunnel, string hostName, bool isCustomPassword)
        {
            this.channel = channel;
            channel.MessageAdded += Channel_MessageAdded;
            channel.CTCPReceived += Channel_CTCPReceived;
            channel.UserKicked += Channel_UserKicked;
            channel.UserQuitIRC += Channel_UserQuitIRC;
            channel.UserLeft += Channel_UserLeft;
            channel.UserAdded += Channel_UserAdded;
            channel.UserListReceived += Channel_UserListReceived;

            this.hostName = hostName;
            this.playerLimit = playerLimit;
            this.isCustomPassword = isCustomPassword;

            if (isHost)
            {
                RandomSeed = new Random().Next();
                RefreshMapSelectionUI();
                btnChangeTunnel.Enable();
            }
            else
            {
                channel.ChannelModesChanged += Channel_ChannelModesChanged;
                AIPlayers.Clear();
                btnChangeTunnel.Disable();
            }

            tunnelHandler.CurrentTunnel = tunnel;
            tunnelHandler.CurrentTunnelPinged += TunnelHandler_CurrentTunnelPinged;

            connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
            connectionManager.Disconnected += ConnectionManager_Disconnected;

            Refresh(isHost);
        }
        private void TunnelHandler_CurrentTunnelPinged(object sender, EventArgs e) => UpdatePing();
        public void OnJoined()
        {
            FileHashCalculator fhc = new FileHashCalculator();
            fhc.CalculateHashes(GameModes);

            gameFilesHash = fhc.GetCompleteHash();

            if (IsHost)
            {
                connectionManager.SendCustomMessage(new QueuedMessage(
                    string.Format("MODE {0} +klnNs {1} {2}", channel.ChannelName,
                    channel.Password, playerLimit),
                    QueuedMessageType.SYSTEM_MESSAGE, 50));

                connectionManager.SendCustomMessage(new QueuedMessage(
                    string.Format("TOPIC {0} :{1}", channel.ChannelName,
                    ProgramConstants.CNCNET_PROTOCOL_REVISION + ";" + localGame.ToLower()),
                    QueuedMessageType.SYSTEM_MESSAGE, 50));

                gameBroadcastTimer.Enabled = true;
                gameBroadcastTimer.Start();
                gameBroadcastTimer.SetTime(TimeSpan.FromSeconds(INITIAL_GAME_BROADCAST_DELAY));
            }
            else
            {
                channel.SendCTCPMessage("FHSH " + fhc.GetCompleteHash(), QueuedMessageType.SYSTEM_MESSAGE, 10);
                }

            TopBar.AddPrimarySwitchable(this);
            TopBar.SwitchToPrimary();
            WindowManager.SelectedControl = tbChatInput;
            ResetAutoReadyCheckbox();
            UpdatePing();
            UpdateDiscordPresence(true);
        }
        private void UpdatePing()
        {
            if (tunnelHandler.CurrentTunnel == null)
                return;

            channel.SendCTCPMessage("TNLPNG " + tunnelHandler.CurrentTunnel.PingInMs, QueuedMessageType.SYSTEM_MESSAGE, 10);

            PlayerInfo pInfo = Players.Find(p => p.Name.Equals(ProgramConstants.PLAYERNAME));
            if (pInfo != null)
            {
                pInfo.Ping = tunnelHandler.CurrentTunnel.PingInMs;
                UpdatePlayerPingIndicator(pInfo);
            }
        }

        private void PrintTunnelServerInformation(string s)
        {
            if (tunnelHandler.CurrentTunnel == null)
            {
                AddNotice("Tunnel server unavailable!");
            }
            else
            {
                AddNotice($"Current tunnel server: {tunnelHandler.CurrentTunnel.Name} ({tunnelHandler.CurrentTunnel.Country}) " +
                    $"(Players: {tunnelHandler.CurrentTunnel.Clients}/{tunnelHandler.CurrentTunnel.MaxClients}) (Official: {tunnelHandler.CurrentTunnel.Official})");
            }
        }

        private void ShowTunnelSelectionWindow(string description)
        {
            tunnelSelectionWindow.Open(description,
                tunnelHandler.CurrentTunnel?.Address);
            tunnelSelectionWindow.TunnelSelected += TunnelSelectionWindow_TunnelSelected;
        }

        private void TunnelSelectionWindow_TunnelSelected(object sender, TunnelEventArgs e)
        {
            tunnelSelectionWindow.TunnelSelected -= TunnelSelectionWindow_TunnelSelected;
            channel.SendCTCPMessage($"{CHANGE_TUNNEL_SERVER_MESSAGE} {e.Tunnel.Address}:{e.Tunnel.Port}", QueuedMessageType.SYSTEM_MESSAGE, 10);
            HandleTunnelServerChange(e.Tunnel);
        }

        public void ChangeChatColor(IRCColor chatColor)
        {
            this.chatColor = chatColor;
            tbChatInput.TextColor = chatColor.XnaColor;
        }

        public override void Clear()
        {
            base.Clear();

            if (channel != null)
            {
                channel.MessageAdded -= Channel_MessageAdded;
                channel.CTCPReceived -= Channel_CTCPReceived;
                channel.UserKicked -= Channel_UserKicked;
                channel.UserQuitIRC -= Channel_UserQuitIRC;
                channel.UserLeft -= Channel_UserLeft;
                channel.UserAdded -= Channel_UserAdded;
                channel.UserListReceived -= Channel_UserListReceived;

                if (!IsHost)
                {
                    channel.ChannelModesChanged -= Channel_ChannelModesChanged;
                }

                connectionManager.RemoveChannel(channel);
            }

            Disable();
            connectionManager.ConnectionLost -= ConnectionManager_ConnectionLost;
            connectionManager.Disconnected -= ConnectionManager_Disconnected;

            gameBroadcastTimer.Enabled = false;
            closed = false;

            tbChatInput.Text = string.Empty;

            tunnelHandler.CurrentTunnel = null;
            tunnelHandler.CurrentTunnelPinged -= TunnelHandler_CurrentTunnelPinged;
            GameLeft?.Invoke(this, EventArgs.Empty);

            TopBar.RemovePrimarySwitchable(this);
            ResetDiscordPresence();
        }

        private void ConnectionManager_Disconnected(object sender, EventArgs e) => HandleConnectionLoss();

        private void ConnectionManager_ConnectionLost(object sender, ConnectionLostEventArgs e) => HandleConnectionLoss();

        private void HandleConnectionLoss()
        {
            Clear();
            this.Visible = false;
            this.Enabled = false;
        }

        protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e)
        {
            if (IsHost)
            {
                closed = true;
                BroadcastGame();
            }

            Clear();
            channel.Leave();
        }

        protected override void UpdateDiscordPresence(bool resetTimer = false)
        {
            if (discordHandler == null)
                return;

            PlayerInfo player = FindLocalPlayer();
            if (player == null || Map == null || GameMode == null)
                return;
            string side = "";
            if (ddPlayerSides.Length > Players.IndexOf(player))
                side = ddPlayerSides[Players.IndexOf(player)].SelectedItem.Text;
            string currentState = ProgramConstants.IsInGame ? "在游戏中" : "在大厅中";

            discordHandler.UpdatePresence(
                Map.Name, GameMode.Name, "Multiplayer",
                currentState, Players.Count, playerLimit, side,
                channel.UIName, IsHost, isCustomPassword, Locked, resetTimer);
        }

        private void Channel_UserQuitIRC(object sender, UserNameIndexEventArgs e)
        {
            RemovePlayer(e.UserName);

            if (e.UserName == hostName)
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    ERROR_MESSAGE_COLOR, "房主关闭了房间。"));
                BtnLeaveGame_LeftClick(this, EventArgs.Empty);
            }
            else
                UpdateDiscordPresence();
        }

        private void Channel_UserLeft(object sender, UserNameIndexEventArgs e)
        {
            RemovePlayer(e.UserName);

            if (e.UserName == hostName)
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    ERROR_MESSAGE_COLOR, "房主关闭了房间。"));
                BtnLeaveGame_LeftClick(this, EventArgs.Empty);
            }
            else
                UpdateDiscordPresence();
        }

        private void Channel_UserKicked(object sender, UserNameIndexEventArgs e)
        {
            if (e.UserName == ProgramConstants.PLAYERNAME)
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    ERROR_MESSAGE_COLOR, "你被踢出了房间！"));
                Clear();
                this.Visible = false;
                this.Enabled = false;
                return;
            }

            int index = Players.FindIndex(p => p.Name == e.UserName);

            if (index > -1)
            {
                Players.RemoveAt(index);
                CopyPlayerDataToUI();
                UpdateDiscordPresence();
                ClearReadyStatuses();
            }
        }

        private void Channel_UserListReceived(object sender, EventArgs e)
        {
            if (!IsHost)
            {
                if (channel.Users.FindIndex(u => u.IRCUser.Name == hostName) < 0)
                {
                    connectionManager.MainChannel.AddMessage(new ChatMessage(
                        ERROR_MESSAGE_COLOR, "房主关闭了房间。"));
                    BtnLeaveGame_LeftClick(this, EventArgs.Empty);
                }
            }
            UpdateDiscordPresence();
        }

        private void Channel_UserAdded(object sender, ChannelUserEventArgs e)
        {
            PlayerInfo pInfo = new PlayerInfo(e.User.IRCUser.Name);
            Players.Add(pInfo);

            if (Players.Count + AIPlayers.Count > MAX_PLAYER_COUNT && AIPlayers.Count > 0)
                AIPlayers.RemoveAt(AIPlayers.Count - 1);

            sndJoinSound.Play();

            WindowManager.FlashWindow();

            if (!IsHost)
            {
                CopyPlayerDataToUI();
                return;
            }

            if (e.User.IRCUser.Name != ProgramConstants.PLAYERNAME)
            {
                // Changing the map applies forced settings (co-op sides etc.) to the
                // new player, and it also sends an options broadcast message
                //CopyPlayerDataToUI(); This is also called by ChangeMap()
                ChangeMap(GameMode, Map);
                BroadcastPlayerOptions();
                UpdateDiscordPresence();
            }
            else
            {
                Players[0].Ready = true;
                CopyPlayerDataToUI();
            }

            if (Players.Count >= playerLimit)
            {
                AddNotice("达到玩家上限，房间已经被锁定。");
                LockGame();
            }
        }

        private void RemovePlayer(string playerName)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

            if (pInfo != null)
            {
                Players.Remove(pInfo);

                CopyPlayerDataToUI();

                // This might not be necessary
                if (IsHost)
                    BroadcastPlayerOptions();
            }

            sndLeaveSound.Play();

            if (IsHost && Locked && !ProgramConstants.IsInGame)
            {
                UnlockGame(true);
            }
        }

        private void Channel_ChannelModesChanged(object sender, ChannelModeEventArgs e)
        {
            if (e.ModeString == "+i")
            {
                if (Players.Count >= playerLimit)
                    AddNotice("达到玩家上限，房间已经被锁定。");
                else
                    AddNotice("房主已经焊死了车门。");
            }
            else if (e.ModeString == "-i")
                AddNotice("房主解锁了房间。");
        }

        private void Channel_CTCPReceived(object sender, ChannelCTCPEventArgs e)
        {
            Logger.Log("CnCNetGameLobby_CTCPReceived");

            foreach (CommandHandlerBase cmdHandler in ctcpCommandHandlers)
            {
                if (cmdHandler.Handle(e.UserName, e.Message))
                {
                    UpdateDiscordPresence();
                    return;
                }
            }

            Logger.Log("Unhandled CTCP command: " + e.Message + " from " + e.UserName);
        }

        private void Channel_MessageAdded(object sender, IRCMessageEventArgs e)
        {
            if (cncnetUserData.IsIgnored(e.Message.SenderIdent))
            {
                lbChatMessages.AddMessage(new ChatMessage(Color.Silver, e.Message.SenderName + "向你发送了信息，但未能成功传达。"));
            }
            else
            {
                lbChatMessages.AddMessage(e.Message);

                if (e.Message.SenderName != null)
                    sndMessageSound.Play();
            }
        }

        /// <summary>
        /// Starts the game for the game host.
        /// </summary>
        protected override void HostLaunchGame()
        {
            if (Players.Count > 1)
            {
                AddNotice("连接至隧道服..");

                List<int> playerPorts = tunnelHandler.CurrentTunnel.GetPlayerPortInfo(Players.Count);
                if (playerPorts.Count < Players.Count)
                {
                    ShowTunnelSelectionWindow("在连接CnCNet隧道服" +
                        "时出现了一个错误。" + Environment.NewLine + 
                        "尝试切换一个隧道服：");
                    AddNotice("在连接CnCNet隧道服" +
                        "时出现了一个错误。尝试切换一个隧道服：" +
                        "(可以在聊天栏中输入 /CHANGETUNNEL 以切换)。", ERROR_MESSAGE_COLOR);
                    return;
                }

                StringBuilder sb = new StringBuilder("START ");
                sb.Append(UniqueGameID);
                for (int pId = 0; pId < Players.Count; pId++)
                {
                    Players[pId].Port = playerPorts[pId];
                    sb.Append(";");
                    sb.Append(Players[pId].Name);
                    sb.Append(";");
                    sb.Append("0.0.0.0:");
                    sb.Append(playerPorts[pId]);
                }
                channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 10);
            }
            else
            {
                Logger.Log("One player MP -- starting!");
            }

            Players.ForEach(pInfo => pInfo.IsInGame = true);

            StartGame();
        }

        protected override void RequestPlayerOptions(int side, int color, int start, int team)
        {
            byte[] value = new byte[]
            {
                (byte)side,
                (byte)color,
                (byte)start,
                (byte)team
            };

            int intValue = BitConverter.ToInt32(value, 0);

            channel.SendCTCPMessage(
                string.Format("OR {0}", intValue),
                QueuedMessageType.GAME_SETTINGS_MESSAGE, 6);
        }

        protected override void RequestReadyStatus()
        {
            if (Map == null || GameMode == null)
            {
                AddNotice("房主需要更换地图，" + 
                    "否则你无法加入游戏。");
                return;
            }

            if (chkAutoReady.Checked)
                channel.SendCTCPMessage("R 2", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 5);
            else
                channel.SendCTCPMessage("R 1", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 5);
        }

        protected override void AddNotice(string message, Color color) => channel.AddMessage(new ChatMessage(color, message));

        /// <summary>
        /// Handles player option requests received from non-host players.
        /// </summary>
        private void HandleOptionsRequest(string playerName, int options)
        {
            if (!IsHost)
                return;

            if (ProgramConstants.IsInGame)
                return;

            PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

            if (pInfo == null)
                return;

            byte[] bytes = BitConverter.GetBytes(options);

            int side = bytes[0];
            int color = bytes[1];
            int start = bytes[2];
            int team = bytes[3];

            if (side < 0 || side > SideCount + RandomSelectorCount)
                return;

            if (color < 0 || color > MPColors.Count)
                return;

            var disallowedSides = GetDisallowedSides();

            if (side > 0 && side <= SideCount && disallowedSides[side - 1])
                return;

            if (Map.CoopInfo != null)
            {
                if (Map.CoopInfo.DisallowedPlayerSides.Contains(side - 1) || side == SideCount + RandomSelectorCount)
                    return;

                if (Map.CoopInfo.DisallowedPlayerColors.Contains(color - 1))
                    return;
            }

            if (start < 0 || start > Map.MaxPlayers)
                return;

            if (team < 0 || team > 4)
                return;

            if (side != pInfo.SideId 
                || start != pInfo.StartingLocation 
                || team != pInfo.TeamId)
            {
                ClearReadyStatuses();
            }

            pInfo.SideId = side;
            pInfo.ColorId = color;
            pInfo.StartingLocation = start;
            pInfo.TeamId = team;

            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
        }

        /// <summary>
        /// Handles "I'm ready" messages received from non-host players.
        /// </summary>
        private void HandleReadyRequest(string playerName, int readyStatus)
        {
            if (!IsHost)
                return;

            PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

            if (pInfo == null)
                return;

            pInfo.Ready = readyStatus > 0;
            pInfo.AutoReady = readyStatus > 1;

            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
        }

        /// <summary>
        /// Broadcasts player options to non-host players.
        /// </summary>
        protected override void BroadcastPlayerOptions()
        {
            // Broadcast player options
            StringBuilder sb = new StringBuilder("PO ");
            foreach (PlayerInfo pInfo in Players.Concat(AIPlayers))
            {
                if (pInfo.IsAI)
                    sb.Append(pInfo.AILevel);
                else
                    sb.Append(pInfo.Name);
                sb.Append(";");

                // Combine the options into one integer to save bandwidth in
                // cases where the player uses default options (this is common for AI players)
                // Will hopefully make GameSurge kicking people a bit less common
                byte[] byteArray = new byte[]
                {
                    (byte)pInfo.TeamId,
                    (byte)pInfo.StartingLocation,
                    (byte)pInfo.ColorId,
                    (byte)pInfo.SideId,
                };

                int value = BitConverter.ToInt32(byteArray, 0);
                sb.Append(value);
                sb.Append(";");
                if (!pInfo.IsAI)
                {
                    if (pInfo.AutoReady)
                        sb.Append(2);
                    else
                        sb.Append(Convert.ToInt32(pInfo.Ready));
                    sb.Append(';');
                }
            }

            channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.GAME_PLAYERS_MESSAGE, 11);
        }

        /// <summary>
        /// Handles player option messages received from the game host.
        /// </summary>
        private void ApplyPlayerOptions(string sender, string message)
        {
            if (sender != hostName)
                return;

            Players.Clear();
            AIPlayers.Clear();

            string[] parts = message.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length;)
            {
                PlayerInfo pInfo = new PlayerInfo();

                string pName = parts[i];
                int converted = Conversions.IntFromString(pName, -1);

                if (converted > -1)
                {
                    pInfo.IsAI = true;
                    pInfo.AILevel = converted;
                    pInfo.Name = AILevelToName(converted);
                }
                else
                {
                    pInfo.Name = pName;

                    // If we can't find the player from the channel user list,
                    // ignore the player
                    // They've either left the channel or got kicked before the 
                    // player options message reached us
                    if (channel.Users.Find(cu => cu.IRCUser.Name == pName) == null)
                    {
                        i += HUMAN_PLAYER_OPTIONS_LENGTH;
                        continue;
                    }
                }

                if (parts.Length <= i + 1)
                {
                    return;
                }

                int playerOptions = Conversions.IntFromString(parts[i + 1], -1);
                if (playerOptions == -1)
                    return;

                byte[] byteArray = BitConverter.GetBytes(playerOptions);

                int team = byteArray[0];
                int start = byteArray[1];
                int color = byteArray[2];
                int side = byteArray[3];

                if (side < 0 || side > SideCount + RandomSelectorCount)
                    return;

                if (color < 0 || color > MPColors.Count)
                    return;

                if (start < 0 || start > MAX_PLAYER_COUNT)
                    return;

                if (team < 0 || team > 4)
                    return;

                pInfo.TeamId = byteArray[0];
                pInfo.StartingLocation = byteArray[1];
                pInfo.ColorId = byteArray[2];
                pInfo.SideId = byteArray[3];

                if (pInfo.IsAI)
                {
                    pInfo.Ready = true;
                    AIPlayers.Add(pInfo);
                    i += AI_PLAYER_OPTIONS_LENGTH;
                }
                else
                {
                    if (parts.Length <= i + 2)
                        return;

                    int readyStatus = Conversions.IntFromString(parts[i + 2], -1);

                    if (readyStatus == -1)
                        return;

                    pInfo.Ready = readyStatus > 0;
                    pInfo.AutoReady = readyStatus > 1;

                    Players.Add(pInfo);
                    i += HUMAN_PLAYER_OPTIONS_LENGTH;
                }
            }

            CopyPlayerDataToUI();
        }

        /// <summary>
        /// Broadcasts game options to non-host players
        /// when the host has changed an option.
        /// </summary>
        protected override void OnGameOptionChanged()
        {
            base.OnGameOptionChanged();

            if (!IsHost)
                return;

            bool[] optionValues = new bool[CheckBoxes.Count];
            for (int i = 0; i < CheckBoxes.Count; i++)
                optionValues[i] = CheckBoxes[i].Checked;

            // Let's pack the booleans into bytes
            List<byte> byteList = Conversions.BoolArrayIntoBytes(optionValues).ToList();
            
            while (byteList.Count % 4 != 0)
            {
                byteList.Add(0);
            }

            int integerCount = byteList.Count / 4;
            byte[] byteArray = byteList.ToArray();

            ExtendedStringBuilder sb = new ExtendedStringBuilder("GO ", true, ';');

            for (int i = 0; i < integerCount; i++)
            {
                sb.Append(BitConverter.ToInt32(byteArray, i * 4));
            }

            // We don't gain much in most cases by packing the drop-down values
            // (because they're bytes to begin with, and usually non-zero),
            // so let's just transfer them as usual

            foreach (GameLobbyDropDown dd in DropDowns)
            {
                sb.Append(dd.SelectedIndex);
            }

            sb.Append(Convert.ToInt32(Map.Official));
            sb.Append(Map.SHA1);
            sb.Append(GameMode.Name);
            sb.Append(FrameSendRate);
            sb.Append(MaxAhead);
            sb.Append(ProtocolVersion);
            sb.Append(RandomSeed);
            sb.Append(Convert.ToInt32(RemoveStartingLocations));

            channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.GAME_SETTINGS_MESSAGE, 11);
        }

        /// <summary>
        /// Handles game option messages received from the game host.
        /// </summary>
        private void ApplyGameOptions(string sender, string message)
        {
            if (sender != hostName)
                return;

            string[] parts = message.Split(';');

            int checkBoxIntegerCount = (CheckBoxes.Count / 32) + 1;

            int partIndex = checkBoxIntegerCount + DropDowns.Count;

            if (parts.Length < partIndex + 6)
            {
                AddNotice("房主发送了无效的游戏选项消息！" +
                    "您的游戏版本可能与房主的不同。", Color.Red);
                return;
            }

            string mapOfficial = parts[partIndex];
            bool isMapOfficial = Conversions.BooleanFromString(mapOfficial, true);

            string mapSHA1 = parts[partIndex + 1];

            string gameMode = parts[partIndex + 2];

            int frameSendRate = Conversions.IntFromString(parts[partIndex + 3], FrameSendRate);
            if (frameSendRate != FrameSendRate)
            {
                FrameSendRate = frameSendRate;
                AddNotice("房主已将帧发送速率（顺序延迟）更改为：" + frameSendRate);
            }

            int maxAhead = Conversions.IntFromString(parts[partIndex + 4], MaxAhead);
            if (maxAhead != MaxAhead)
            {
                MaxAhead = maxAhead;
                AddNotice("房主已将最大提前更改为：" + maxAhead);
            }

            int protocolVersion = Conversions.IntFromString(parts[partIndex + 5], ProtocolVersion);
            if (protocolVersion != ProtocolVersion)
            {
                ProtocolVersion = protocolVersion;
                AddNotice("房主已将协议版本更改为：" + protocolVersion);
            }

            GameMode currentGameMode = GameMode;
            Map currentMap = Map;

            lastGameMode = gameMode;
            lastMapSHA1 = mapSHA1;

            GameMode = GameModes.Find(gm => gm.Name == gameMode);
            if (GameMode == null)
            {
                ChangeMap(null, null);

                if (!isMapOfficial)
                    RequestMap(mapSHA1);
                else
                    ShowOfficialMapMissingMessage(mapSHA1);
            }
            else
            {
                Map = GameMode.Maps.Find(map => map.SHA1 == mapSHA1);

                if (Map == null)
                {
                    ChangeMap(null, null);

                    if (!isMapOfficial)
                        RequestMap(mapSHA1);
                    else
                        ShowOfficialMapMissingMessage(mapSHA1);
                }
                else if (GameMode != currentGameMode || Map != currentMap)
                    ChangeMap(GameMode, Map);
            }

            // By changing the game options after changing the map, we know which
            // game options were changed by the map and which were changed by the game host

            // If the map doesn't exist on the local installation, it's impossible
            // to know which options were set by the host and which were set by the
            // map, so we'll just assume that the host has set all the options.
            // Very few (if any) custom maps force options, so it'll be correct nearly always

            for (int i = 0; i < checkBoxIntegerCount; i++)
            {
                if (parts.Length <= i)
                    return;

                int checkBoxStatusInt;
                bool success = int.TryParse(parts[i], out checkBoxStatusInt);

                if (!success)
                {
                    AddNotice("无法分析房主发送的复选框选项！" +
                        "您的游戏版本可能与房主的不同。", Color.Red);
                    return;
                }

                byte[] byteArray = BitConverter.GetBytes(checkBoxStatusInt);
                bool[] boolArray = Conversions.BytesIntoBoolArray(byteArray);

                for (int optionIndex = 0; optionIndex < boolArray.Length; optionIndex++)
                {
                    int gameOptionIndex = i * 32 + optionIndex;

                    if (gameOptionIndex >= CheckBoxes.Count)
                        break;

                    GameLobbyCheckBox checkBox = CheckBoxes[gameOptionIndex];

                    if (checkBox.Checked != boolArray[optionIndex] && checkBox.Visible)
                    {
                        if (boolArray[optionIndex])
                            AddNotice("房主启用了" + checkBox.Text + "。");
                        else
                            AddNotice("房主禁用了" + checkBox.Text + "。");
                    }

                    CheckBoxes[gameOptionIndex].Checked = boolArray[optionIndex];
                }
            }

            for (int i = checkBoxIntegerCount; i < DropDowns.Count + checkBoxIntegerCount; i++)
            {
                if (parts.Length <= i)
                {
                    AddNotice("房主发送了无效的游戏选项消息！" +
                        "您的游戏版本可能与房主的不同。", Color.Red);
                    return;
                }

                int ddSelectedIndex;
                bool success = int.TryParse(parts[i], out ddSelectedIndex);

                if (!success)
                {
                    AddNotice("无法解析房主发送的下拉选项！ " +
                        "您的游戏版本可能与房主的不同。", Color.Red);
                    return;
                }

                GameLobbyDropDown dd = DropDowns[i - checkBoxIntegerCount];

                if (ddSelectedIndex < -1 || ddSelectedIndex >= dd.Items.Count)
                    continue;

                if (dd.SelectedIndex != ddSelectedIndex)
                {
                    string ddName = dd.OptionName;
                    if (dd.OptionName == null)
                        ddName = dd.Name;

                    AddNotice("房主将" + ddName + "设置为" + dd.Items[ddSelectedIndex].Text + "。");
                }

                DropDowns[i - checkBoxIntegerCount].SelectedIndex = ddSelectedIndex;
            }

            int randomSeed;
            bool parseSuccess = int.TryParse(parts[partIndex + 6], out randomSeed);

            if (!parseSuccess)
            {
                AddNotice("无法分析游戏选项消息中的随机种子！" +
                    "您的游戏版本可能与房主的不同。", Color.Red);
            }

            bool removeStartingLocations = Convert.ToBoolean(Conversions.IntFromString(parts[partIndex + 7],
                Convert.ToInt32(RemoveStartingLocations)));
            SetRandomStartingLocations(removeStartingLocations);

            RandomSeed = randomSeed;
        }

        private void RequestMap(string mapSHA1)
        {
            if (UserINISettings.Instance.EnableMapSharing)
            {
                AddNotice("房主选择了一张你没有的地图。");
                mapSharingConfirmationPanel.ShowForMapDownload();
            }
            else
            {
                AddNotice("房主选择了一张你没有的地图。" +
                    "因为你禁止了地图共享，所以无法传输。" +
                    "房主需要更换地图，否则你无法参加游戏。");
                channel.SendCTCPMessage(MAP_SHARING_DISABLED_MESSAGE, QueuedMessageType.SYSTEM_MESSAGE, 9);
            }
        }

        private void ShowOfficialMapMissingMessage(string sha1)
        {
            AddNotice("房主选择了一张你没有的官方地图。" +
                "这可能意味着房主修改了文件，或者与你的版本不同。" +
                "房主需要更换地图，否则你无法参加游戏。");
            channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + sha1, QueuedMessageType.SYSTEM_MESSAGE, 9);
        }

        private void MapSharingConfirmationPanel_MapDownloadConfirmed(object sender, EventArgs e)
        {
            Logger.Log("Map sharing confirmed.");
            AddNotice("尝试下载地图...");
            mapSharingConfirmationPanel.SetDownloadingStatus();
            MapSharer.DownloadMap(lastMapSHA1, ClientConfiguration.Instance.LoadMapGame);
        }

        protected override void ChangeMap(GameMode gameMode, Map map)
        {
            mapSharingConfirmationPanel.Disable();
            base.ChangeMap(gameMode, map);
        }

        /// <summary>
        /// Signals other players that the local player has returned from the game,
        /// and unlocks the game as well as generates a new random seed as the game host.
        /// </summary>
        protected override void GameProcessExited()
        {
            base.GameProcessExited();

            channel.SendCTCPMessage("RETURN", QueuedMessageType.SYSTEM_MESSAGE, 20);
            ReturnNotification(ProgramConstants.PLAYERNAME);

            if (IsHost)
            {
                RandomSeed = new Random().Next();
                OnGameOptionChanged();
                ClearReadyStatuses();
                CopyPlayerDataToUI();
                BroadcastPlayerOptions();

                if (Players.Count < playerLimit)
                {
                    UnlockGame(true);
                }
            }
        }

        /// <summary>
        /// Handles the "START" (game start) command sent by the game host.  
        /// </summary>
        private void NonHostLaunchGame(string sender, string message)
        {
            if (sender != hostName)
                return;

            string[] parts = message.Split(';');

            if (parts.Length < 1)
                return;

            UniqueGameID = Conversions.IntFromString(parts[0], -1);
            if (UniqueGameID < 0)
                return;

            for (int i = 1; i < parts.Length; i += 2)
            {
                if (parts.Length <= i + 1)
                    return;

                string pName = parts[i];
                string[] ipAndPort = parts[i + 1].Split(':');

                if (ipAndPort.Length < 2)
                    return;

                int port;
                bool success = int.TryParse(ipAndPort[1], out port);

                if (!success)
                    return;

                PlayerInfo pInfo = Players.Find(p => p.Name == pName);

                if (pInfo == null)
                    return;

                pInfo.Port = port;
            }

            StartGame();
        }

        protected override void StartGame()
        {
            AddNotice("开始游戏..");

            FileHashCalculator fhc = new FileHashCalculator();
            fhc.CalculateHashes(GameModes);

            if (gameFilesHash != fhc.GetCompleteHash())
            {
                Logger.Log("Game files modified during client session!");
                channel.SendCTCPMessage(CHEAT_DETECTED_MESSAGE, QueuedMessageType.INSTANT_MESSAGE, 0);
                HandleCheatDetectedMessage(ProgramConstants.PLAYERNAME);
            }

            base.StartGame();
        }

        protected override void WriteSpawnIniAdditions(IniFile iniFile)
        {
            base.WriteSpawnIniAdditions(iniFile);

            iniFile.SetStringValue("Tunnel", "Ip", tunnelHandler.CurrentTunnel.Address);
            iniFile.SetIntValue("Tunnel", "Port", tunnelHandler.CurrentTunnel.Port);


            iniFile.SetIntValue("Settings", "GameID", UniqueGameID);
            iniFile.SetBooleanValue("Settings", "Host", IsHost);

            PlayerInfo localPlayer = FindLocalPlayer();

            if (localPlayer == null)
                return;

            iniFile.SetIntValue("Settings", "Port", localPlayer.Port);
        }

        protected override void SendChatMessage(string message) => channel.SendChatMessage(message, chatColor);

        #region Notifications

        private void HandleNotification(string sender, Action handler)
        {
            if (sender != hostName)
                return;

            handler();
        }

        private void HandleIntNotification(string sender, int parameter, Action<int> handler)
        {
            if (sender != hostName)
                return;

            handler(parameter);
        }

        protected override void GetReadyNotification()
        {
            base.GetReadyNotification();

            WindowManager.FlashWindow();
            TopBar.SwitchToPrimary();

            if (IsHost)
                channel.SendCTCPMessage("GETREADY", QueuedMessageType.GAME_GET_READY_MESSAGE, 0);
        }

        protected override void AISpectatorsNotification()
        {
            base.AISpectatorsNotification();

            if (IsHost)
                channel.SendCTCPMessage("AISPECS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void InsufficientPlayersNotification()
        {
            base.InsufficientPlayersNotification();

            if (IsHost)
                channel.SendCTCPMessage("INSFSPLRS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void TooManyPlayersNotification()
        {
            base.TooManyPlayersNotification();

            if (IsHost)
                channel.SendCTCPMessage("TMPLRS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void SharedColorsNotification()
        {
            base.SharedColorsNotification();

            if (IsHost)
                channel.SendCTCPMessage("CLRS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void SharedStartingLocationNotification()
        {
            base.SharedStartingLocationNotification();

            if (IsHost)
                channel.SendCTCPMessage("SLOC", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void LockGameNotification()
        {
            base.LockGameNotification();

            if (IsHost)
                channel.SendCTCPMessage("LCKGME", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void NotVerifiedNotification(int playerIndex)
        {
            base.NotVerifiedNotification(playerIndex);

            if (IsHost)
                channel.SendCTCPMessage("NVRFY " + playerIndex, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void StillInGameNotification(int playerIndex)
        {
            base.StillInGameNotification(playerIndex);

            if (IsHost)
                channel.SendCTCPMessage("INGM " + playerIndex, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        private void ReturnNotification(string sender)
        {
            AddNotice(sender + "从游戏中返回了。");

            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo != null)
                pInfo.IsInGame = false;
        }

        private void HandleTunnelPing(string sender, int ping)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name.Equals(sender));
            if (pInfo != null)
            {
                pInfo.Ping = ping;
                UpdatePlayerPingIndicator(pInfo);
            }
            else
                AddNotice(sender + "无法连接至隧道服务器。");
        }

        private void FileHashNotification(string sender, string filesHash)
        {
            if (!IsHost)
                return;

            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo != null)
                pInfo.Verified = true;

            if (filesHash != gameFilesHash)
            {
                channel.SendCTCPMessage("MM " + sender, QueuedMessageType.GAME_CHEATER_MESSAGE, 10);
                CheaterNotification(ProgramConstants.PLAYERNAME, sender);
            }
        }

        private void CheaterNotification(string sender, string cheaterName)
        {
            if (sender != hostName)
                return;

            AddNotice("玩家" + cheaterName + "的游戏文件与房主的不一致。" + 
                cheaterName + "和房主之一可能在作弊。", Color.Red);
        }

        protected override void BroadcastDiceRoll(int dieSides, int[] results)
        {
            string resultString = string.Join(",", results);
            channel.SendCTCPMessage($"{DICE_ROLL_MESSAGE} {dieSides},{resultString}", QueuedMessageType.CHAT_MESSAGE, 0);
            PrintDiceRollResult(ProgramConstants.PLAYERNAME, dieSides, results);
        }

        #endregion

        protected override void HandleLockGameButtonClick()
        {
            if (!Locked)
            {
                AddNotice("你焊死了车门。");
                LockGame();
            }
            else
            {
                if (Players.Count < playerLimit)
                {
                    AddNotice("你解锁了房间。");
                    UnlockGame(false);
                }
                else
                    AddNotice(string.Format(
                        "不能解锁房间，已经达到({0})人的上限。", playerLimit));
            }
        }

        protected override void LockGame()
        {
            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("MODE {0} +i", channel.ChannelName), QueuedMessageType.INSTANT_MESSAGE, -1));

            Locked = true;
            btnLockGame.Text = "解锁房间";
            AccelerateGameBroadcasting();
        }

        protected override void UnlockGame(bool announce)
        {
            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("MODE {0} -i", channel.ChannelName), QueuedMessageType.INSTANT_MESSAGE, -1));

            Locked = false;
            if (announce)
                AddNotice("车门已经焊死了。");
            btnLockGame.Text = "锁定房间";
            AccelerateGameBroadcasting();
        }

        protected override void KickPlayer(int playerIndex)
        {
            if (playerIndex >= Players.Count)
                return;

            var pInfo = Players[playerIndex];

            AddNotice("把" + pInfo.Name + "踢出游戏...");
            channel.SendKickMessage(pInfo.Name, 8);
        }

        protected override void BanPlayer(int playerIndex)
        {
            if (playerIndex >= Players.Count)
                return;

            var pInfo = Players[playerIndex];

            var user = connectionManager.UserList.Find(u => u.Name == pInfo.Name);

            if (user != null)
            {
                AddNotice("禁止" + pInfo.Name + "，并把他踢出游戏...");
                channel.SendBanMessage(user.Hostname, 8);
                channel.SendKickMessage(user.Name, 8);
            }
        }

        private void HandleCheatDetectedMessage(string sender)
        {
            AddNotice(sender + "在客户端会话期间修改了游戏文件。他很有可能在作弊！", Color.Red);
        }

        private void HandleTunnelServerChangeMessage(string sender, string tunnelAddressAndPort)
        {
            if (sender != hostName)
                return;

            string[] split = tunnelAddressAndPort.Split(':');
            string tunnelAddress = split[0];
            int tunnelPort = int.Parse(split[1]);

            CnCNetTunnel tunnel = tunnelHandler.Tunnels.Find(t => t.Address == tunnelAddress && t.Port == tunnelPort); if (tunnel == null)
            {
                AddNotice("房主选择了一个无效的隧道服务器！" +
                    "房主需要更换服务器，" +
                    "否则你无法参加游戏。",
                    Color.Yellow);
                btnLaunchGame.AllowClick = false;
                return;
            }

            HandleTunnelServerChange(tunnel);
            btnLaunchGame.AllowClick = true;
        }

        /// <summary>
        /// Changes the tunnel server used for the game.
        /// </summary>
        /// <param name="tunnel">The new tunnel server to use.</param>
        private void HandleTunnelServerChange(CnCNetTunnel tunnel)
        {
            tunnelHandler.CurrentTunnel = tunnel;
            UpdatePing();
        }

        #region CnCNet map sharing

        private void MapSharer_MapDownloadFailed(object sender, SHA1EventArgs e) 
            => WindowManager.AddCallback(new Action<SHA1EventArgs>(MapSharer_HandleMapDownloadFailed), e);

        private void MapSharer_HandleMapDownloadFailed(SHA1EventArgs e)
        {
            // If the host has already uploaded the map, we shouldn't request them to re-upload it
            if (hostUploadedMaps.Contains(e.SHA1))
            {
                AddNotice("下载地图失败。房主需要更换地图，否则你无法参加游戏。");
                mapSharingConfirmationPanel.SetFailedStatus();

                channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + e.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
                return;
            }

            AddNotice("请求房主将地图上传到CnCNet地图数据库...");

            channel.SendCTCPMessage(MAP_SHARING_UPLOAD_REQUEST + " " + e.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
        }

        private void MapSharer_MapDownloadComplete(object sender, SHA1EventArgs e) =>
           WindowManager.AddCallback(new Action<SHA1EventArgs>(MapSharer_HandleMapDownloadComplete), e);
        private void MapSharer_HandleMapDownloadComplete(SHA1EventArgs e)
        {
            Logger.Log("Map " + e.SHA1 + " downloaded, parsing.");
            string mapPath = $"Maps\\{ClientConfiguration.Instance.CustomMapFolderName}\\" + e.SHA1;
            Map map = MapLoader.LoadCustomMap(mapPath, out string returnMessage);
            if (map != null)
            {
                AddNotice(returnMessage);
                if (lastMapSHA1 == e.SHA1)
                {
                    Map = map;
                    GameMode = GameModes.Find(gm => gm.UIName == lastGameMode);
                    ChangeMap(GameMode, Map);
                }
            }
            else
            {
                AddNotice(returnMessage, Color.Red);
                AddNotice("转换自定义地图失败。房主需要更换地图，否则你无法参加游戏。");
                mapSharingConfirmationPanel.SetFailedStatus();
                channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + e.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
            }
        }

        private void MapSharer_MapUploadFailed(object sender, MapEventArgs e)
            => WindowManager.AddCallback(new Action<MapEventArgs>(MapSharer_HandleMapUploadFailed), e);

        private void MapSharer_HandleMapUploadFailed(MapEventArgs e)
        {
            Map map = e.Map;

            hostUploadedMaps.Add(map.SHA1);

            AddNotice("上传地图" + map.Name + "至CnCNet地图数据库失败。");
            if (map == Map)
            {
                AddNotice("你需要更换地图，否则一部分玩家无法参加游戏。");
                channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + map.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
            }
        }

        private void MapSharer_MapUploadComplete(object sender, MapEventArgs e)
            => WindowManager.AddCallback(new Action<MapEventArgs>(MapSharer_HandleMapUploadComplete), e);

        private void MapSharer_HandleMapUploadComplete(MapEventArgs e)
        {
            hostUploadedMaps.Add(e.Map.SHA1);

            AddNotice("成功将地图" + e.Map.Name + "上传至CnCNet地图数据库。");
            if (e.Map == Map)
            {
                channel.SendCTCPMessage(MAP_SHARING_DOWNLOAD_REQUEST + " " + Map.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
            }

            Task task = new Task(
                () =>
                {
                    Thread.Sleep(2000);
                    ChangeMap(GameMode, Map);
                }
                );

            task.Start();

            Task task2 = new Task(
                () =>
                {
                    Thread.Sleep(4000);
                    ChangeMap(GameMode, Map);
                }
                );

            task2.Start();

            Task task3 = new Task(
            () =>
            {
                Thread.Sleep(6000);
                ChangeMap(GameMode, Map);
            }
            );

            task3.Start();

        }

        /// <summary>
        /// Handles a map upload request sent by a player.
        /// </summary>
        /// <param name="sender">The sender of the request.</param>
        /// <param name="mapSHA1">The SHA1 of the requested map.</param>
        private void HandleMapUploadRequest(string sender, string mapSHA1)
        {
            if (hostUploadedMaps.Contains(mapSHA1))
            {
                Logger.Log("HandleMapUploadRequest: Map " + mapSHA1 + " is already uploaded!");
                return;
            }

            Map map = null;

            foreach (GameMode gm in GameModes)
            {
                map = gm.Maps.Find(m => m.SHA1 == mapSHA1);

                if (map != null)
                    break;
            }

            if (map == null)
            {
                Logger.Log("Unknown map upload request from " + sender + ": " + mapSHA1);
                return;
            }

            if (map.Official)
            {
                Logger.Log("HandleMapUploadRequest: Map is official, so skip request");

                AddNotice(string.Format("{0} 没有地图'{1}'。" +
                    "房主需要更换地图，否则 {0} 无法参加游戏。",
                    sender, map.Name));

                return;
            }

            if (!IsHost)
                return;

            AddNotice(string.Format("{0} 没有地图'{1}'。" +
                "尝试将地图上传至CnCNet地图数据库...",
                sender, map.Name));

            MapSharer.UploadMap(map, ClientConfiguration.Instance.LoadMapGame);
        }

        /// <summary>
        /// Handles a map transfer failure message sent by either the player or the game host.
        /// </summary>
        private void HandleMapTransferFailMessage(string sender, string sha1)
        {
            if (sender == hostName)
            {
                AddNotice("地图上传失败。");

                hostUploadedMaps.Add(sha1);

                if (lastMapSHA1 == sha1 && Map == null)
                {
                    AddNotice("房主需要更换地图，否则你无法参加游戏。");
                }

                return;
            }

            if (lastMapSHA1 == sha1)
            {
                if (!IsHost)
                {
                    AddNotice(sender + "下载地图失败。" +
                        "房主需要更换地图，否则" + sender + "无法参加游戏。");
                }
                else
                {
                    AddNotice(sender + "下载地图失败。" +
                        "你需要更换地图，否则" + sender + "无法参加游戏。");
                }
            }
        }

        private void HandleMapDownloadRequest(string sender, string sha1)
        {
            if (sender != hostName)
                return;

            hostUploadedMaps.Add(sha1);

            if (lastMapSHA1 == sha1 && Map == null)
            {
                Logger.Log("The game host has uploaded the map into the database. Re-attempting download...");
                MapSharer.DownloadMap(sha1, ClientConfiguration.Instance.LoadMapGame);
            }
        }

        private void HandleMapSharingBlockedMessage(string sender)
        {
            AddNotice(sender + "没有所选择的地图，并且他" +
                "禁用了地图共享。房主需要更换一张官方地图，" +
                "否则他无法加入游戏。");
        }

        #endregion

        #region Game broadcasting logic

        /// <summary>
        /// Lowers the time until the next game broadcasting message.
        /// </summary>
        private void AccelerateGameBroadcasting()
            => gameBroadcastTimer.Accelerate(TimeSpan.FromSeconds(GAME_BROADCAST_ACCELERATION));

        private void BroadcastGame()
        {
            Channel broadcastChannel = connectionManager.FindChannel(gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGame));

            if (broadcastChannel == null)
                return;

            if (GameMode == null || Map == null)
                return;

            StringBuilder sb = new StringBuilder("GAME ");
            sb.Append(ProgramConstants.CNCNET_PROTOCOL_REVISION);
            sb.Append(";");
            sb.Append(ProgramConstants.GAME_VERSION);
            sb.Append(";");
            sb.Append(playerLimit);
            sb.Append(";");
            sb.Append(channel.ChannelName);
            sb.Append(";");
            sb.Append(channel.UIName);
            sb.Append(";");
            if (Locked)
                sb.Append("1");
            else
                sb.Append("0");
            sb.Append(Convert.ToInt32(isCustomPassword));
            sb.Append(Convert.ToInt32(closed));
            sb.Append("0"); // IsLoadedGame
            sb.Append("0"); // IsLadder
            sb.Append(";");
            foreach (PlayerInfo pInfo in Players)
            {
                sb.Append(pInfo.Name);
                sb.Append(",");
            }

            sb.Remove(sb.Length - 1, 1);
            sb.Append(";");
            sb.Append(Map.Name);
            sb.Append(";");
            sb.Append(GameMode.UIName);
            sb.Append(";");
            sb.Append(tunnelHandler.CurrentTunnel.Address + ":" + tunnelHandler.CurrentTunnel.Port);
            sb.Append(";");
            sb.Append(0); // LoadedGameId

            broadcastChannel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 20);
        }

        #endregion

        public override string GetSwitchName() => "联机房间";
    }
}
