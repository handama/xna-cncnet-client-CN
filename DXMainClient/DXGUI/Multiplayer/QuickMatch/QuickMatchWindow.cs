using System;
using System.Linq;
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
            quickMatchService.UserAccountsEvent += UserAccountsEvent;
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

        private void RefreshForLogin(QmLoginEventStatusEnum loginStatus)
        {
            switch (loginStatus)
            {
                case QmLoginEventStatusEnum.Success:
                    lobbyPanel.Enable();
                    loginPanel.Disable();
                    return;
                case QmLoginEventStatusEnum.Logout:
                    loginPanel.Enable();
                    lobbyPanel.Disable();
                    return;
            }
        }

        private void LoginEvent(object sender, QmLoginEventArgs qmLoginEventArgs) => RefreshForLogin(qmLoginEventArgs.Status);

        private void UserAccountsEvent(object sender, QmUserAccountsEventArgs e)
        {
            if (!e.UserAccounts.Any())
                XNAMessageBox.Show(WindowManager, "No User Accounts", "No user accounts found in quick match. Are you registered for this month?");
        }
    }
}
