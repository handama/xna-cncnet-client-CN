using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;

namespace DTAConfig.OptionPanels
{
    class CnCNetOptionsPanel : XNAOptionsPanel
    {
        public CnCNetOptionsPanel(WindowManager windowManager, UserINISettings iniSettings,
            GameCollection gameCollection)
            : base(windowManager, iniSettings)
        {
            this.gameCollection = gameCollection;
        }

        XNAClientCheckBox chkPingUnofficialTunnels;
        XNAClientCheckBox chkWriteInstallPathToRegistry;
        XNAClientCheckBox chkPlaySoundOnGameHosted;

        XNAClientCheckBox chkNotifyOnUserListChange;

        XNAClientCheckBox chkSkipLoginWindow;
        XNAClientCheckBox chkPersistentMode;
        XNAClientCheckBox chkConnectOnStartup;

        GameCollection gameCollection;

        List<XNAClientCheckBox> followedGameChks = new List<XNAClientCheckBox>();

        public override void Initialize()
        {
            base.Initialize();

            Name = "CnCNetOptionsPanel";

            chkPingUnofficialTunnels = new XNAClientCheckBox(WindowManager);
            chkPingUnofficialTunnels.Name = "chkPingUnofficialTunnels";
            chkPingUnofficialTunnels.ClientRectangle = new Rectangle(12, 12, 0, 0);
            chkPingUnofficialTunnels.Text = "探测非官方隧道服";

            AddChild(chkPingUnofficialTunnels);

            chkWriteInstallPathToRegistry = new XNAClientCheckBox(WindowManager);
            chkWriteInstallPathToRegistry.Name = "chkWriteInstallPathToRegistry";
            chkWriteInstallPathToRegistry.ClientRectangle = new Rectangle(
                chkPingUnofficialTunnels.X,
                chkPingUnofficialTunnels.Bottom + 12, 0, 0);
            chkWriteInstallPathToRegistry.Text = "注册游戏目录" + Environment.NewLine +
                "(允许你加入其他游戏的房间)";

            AddChild(chkWriteInstallPathToRegistry);

            chkPlaySoundOnGameHosted = new XNAClientCheckBox(WindowManager);
            chkPlaySoundOnGameHosted.Name = "chkPlaySoundOnGameHosted";
            chkPlaySoundOnGameHosted.ClientRectangle = new Rectangle(
                chkPingUnofficialTunnels.X,
                chkWriteInstallPathToRegistry.Bottom + 12, 0, 0);
            chkPlaySoundOnGameHosted.Text = "当新房间被创建时播放声音";

            AddChild(chkPlaySoundOnGameHosted);

            chkNotifyOnUserListChange = new XNAClientCheckBox(WindowManager);
            chkNotifyOnUserListChange.Name = "chkNotifyOnUserListChange";
            chkNotifyOnUserListChange.ClientRectangle = new Rectangle(
                chkPingUnofficialTunnels.X,
                chkPlaySoundOnGameHosted.Bottom + 12, 0, 0);
            chkNotifyOnUserListChange.Text = "显示大厅中玩家进入/" + Environment.NewLine +
                "退出信息";

            AddChild(chkNotifyOnUserListChange);

            chkSkipLoginWindow = new XNAClientCheckBox(WindowManager);
            chkSkipLoginWindow.Name = "chkSkipLoginWindow";
            chkSkipLoginWindow.ClientRectangle = new Rectangle(
                276,
                12, 0, 0);
            chkSkipLoginWindow.Text = "跳过登录对话框";
            chkSkipLoginWindow.CheckedChanged += ChkSkipLoginWindow_CheckedChanged;

            AddChild(chkSkipLoginWindow);

            chkPersistentMode = new XNAClientCheckBox(WindowManager);
            chkPersistentMode.Name = "chkPersistentMode";
            chkPersistentMode.ClientRectangle = new Rectangle(
                chkSkipLoginWindow.X,
                chkSkipLoginWindow.Bottom + 12, 0, 0);
            chkPersistentMode.Text = "在大厅外也保持连接";
            chkPersistentMode.CheckedChanged += ChkPersistentMode_CheckedChanged;

            AddChild(chkPersistentMode);

            chkConnectOnStartup = new XNAClientCheckBox(WindowManager);
            chkConnectOnStartup.Name = "chkConnectOnStartup";
            chkConnectOnStartup.ClientRectangle = new Rectangle(
                chkSkipLoginWindow.X,
                chkPersistentMode.Bottom + 12, 0, 0);
            chkConnectOnStartup.Text = "启动时自动连接大厅";
            chkConnectOnStartup.AllowChecking = false;

            AddChild(chkConnectOnStartup);

            var lblFollowedGames = new XNALabel(WindowManager);
            lblFollowedGames.Name = "lblFollowedGames";
            lblFollowedGames.ClientRectangle = new Rectangle(
                chkNotifyOnUserListChange.X,
                chkNotifyOnUserListChange.Bottom + 24, 0, 0);
            lblFollowedGames.Text = "展示下列游戏的房间：";

            AddChild(lblFollowedGames);

            int chkCount = 0;
            int chkCountPerColumn = 5;
            int nextColumnXOffset = 0;
            int columnXOffset = 0;
            foreach (CnCNetGame game in gameCollection.GameList)
            {
                if (!game.Supported || string.IsNullOrEmpty(game.GameBroadcastChannel))
                    continue;

                if (chkCount == chkCountPerColumn)
                {
                    chkCount = 0;
                    columnXOffset += nextColumnXOffset + 6;
                    nextColumnXOffset = 0;
                }

                var panel = new XNAPanel(WindowManager);
                panel.Name = "panel" + game.InternalName;
                panel.ClientRectangle = new Rectangle(chkPingUnofficialTunnels.X + columnXOffset,
                    lblFollowedGames.Bottom + 12 + chkCount * 22, 16, 16);
                panel.DrawBorders = false;
                panel.BackgroundTexture = game.Texture;

                var chkBox = new XNAClientCheckBox(WindowManager);
                chkBox.Name = game.InternalName.ToUpper();
                chkBox.ClientRectangle = new Rectangle(
                    panel.Right + 6,
                    panel.Y, 0, 0);
                chkBox.Text = game.UIName;

                chkCount++;

                AddChild(panel);
                AddChild(chkBox);
                followedGameChks.Add(chkBox);

                if (chkBox.Right > columnXOffset + nextColumnXOffset)
                    nextColumnXOffset = chkBox.Width + panel.Width + 6;
            }
        }

        private void ChkSkipLoginWindow_CheckedChanged(object sender, EventArgs e)
        {
            CheckConnectOnStartupAllowance();
        }

        private void ChkPersistentMode_CheckedChanged(object sender, EventArgs e)
        {
            CheckConnectOnStartupAllowance();
        }

        private void CheckConnectOnStartupAllowance()
        {
            if (!chkSkipLoginWindow.Checked || !chkPersistentMode.Checked)
            {
                chkConnectOnStartup.AllowChecking = false;
                chkConnectOnStartup.Checked = false;
                return;
            }

            chkConnectOnStartup.AllowChecking = true;
        }

        public override void Load()
        {
            base.Load();

            chkPingUnofficialTunnels.Checked = IniSettings.PingUnofficialCnCNetTunnels;
            chkWriteInstallPathToRegistry.Checked = IniSettings.WritePathToRegistry;
            chkPlaySoundOnGameHosted.Checked = IniSettings.PlaySoundOnGameHosted;
            chkNotifyOnUserListChange.Checked = IniSettings.NotifyOnUserListChange;
            chkConnectOnStartup.Checked = IniSettings.AutomaticCnCNetLogin;
            chkSkipLoginWindow.Checked = IniSettings.SkipConnectDialog;
            chkPersistentMode.Checked = IniSettings.PersistentMode;

            string localGame = ClientConfiguration.Instance.LocalGame;

            foreach (var chkBox in followedGameChks)
            {
                if (chkBox.Name == localGame)
                {
                    chkBox.AllowChecking = false;
                    chkBox.Checked = true;
                    IniSettings.SettingsIni.SetBooleanValue("Channels", localGame, true);
                    continue;
                }

                chkBox.Checked = IniSettings.IsGameFollowed(chkBox.Name);
            }
        }

        public override bool Save()
        {
            base.Save();

            IniSettings.PingUnofficialCnCNetTunnels.Value = chkPingUnofficialTunnels.Checked;
            IniSettings.WritePathToRegistry.Value = chkWriteInstallPathToRegistry.Checked;
            IniSettings.PlaySoundOnGameHosted.Value = chkPlaySoundOnGameHosted.Checked;
            IniSettings.NotifyOnUserListChange.Value = chkNotifyOnUserListChange.Checked;
            IniSettings.AutomaticCnCNetLogin.Value = chkConnectOnStartup.Checked;
            IniSettings.SkipConnectDialog.Value = chkSkipLoginWindow.Checked;
            IniSettings.PersistentMode.Value = chkPersistentMode.Checked;

            foreach (var chkBox in followedGameChks)
            {
                IniSettings.SettingsIni.SetBooleanValue("Channels", chkBox.Name, chkBox.Checked);
            }

            return false;
        }
    }
}
