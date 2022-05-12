using System;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;
using DTAClient.DXGUI.Generic;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchWindow : INItializableWindow
    {
        private readonly TopBar topBar;
        private readonly QuickMatchService quickMatchService;

        private QuickMatchLoginPanel loginPanel;
        private QuickMatchLobbyPanel lobbyPanel;
        private QuickMatchStatusMessageWindow statusWindow;

        public QuickMatchWindow(WindowManager windowManager, TopBar topBar) : base(windowManager)
        {
            this.topBar = topBar;
            quickMatchService = QuickMatchService.GetInstance();
            quickMatchService.LoginEvent += LoginEvent;
        }

        public override void Initialize()
        {
            Name = nameof(QuickMatchWindow);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            base.Initialize();

            loginPanel = FindChild<QuickMatchLoginPanel>(nameof(QuickMatchLoginPanel));
            loginPanel.Exit += (sender, args) => Disable();

            lobbyPanel = FindChild<QuickMatchLobbyPanel>(nameof(QuickMatchLobbyPanel));
            lobbyPanel.Exit += (sender, args) => Disable();

            statusWindow = FindChild<QuickMatchStatusMessageWindow>(nameof(statusWindow));

            WindowManager.CenterControlOnScreen(this);

            loginPanel.Enable();

            EnabledChanged += EnabledChangedEvent;

            quickMatchService.StatusMessageEvent += StatusMessageEvent;
        }

        private void StatusMessageEvent(object sender, QmStatusMessageEventArgs qmStatusMessageEventArgs)
        {
            if (string.IsNullOrEmpty(qmStatusMessageEventArgs?.Message))
            {
                statusWindow.Disable();
                return;
            }

            statusWindow.SetStatusMessage(qmStatusMessageEventArgs.Message);
            statusWindow.Enable();
        }

        public void EnabledChangedEvent(object sender, EventArgs eventArgs)
        {
            if (Enabled)
                loginPanel.InitLogin();
        }

        private void RefreshForLogin()
        {
            if (!quickMatchService.IsLoggedIn())
            {
                loginPanel.Enable();
                lobbyPanel.Disable();
            }
            else
            {
                lobbyPanel.Enable();
                loginPanel.Disable();
            }
        }

        private void LoginEvent(object sender, QmLoginEventArgs qmLoginEventArgs) => RefreshForLogin();
    }
}
