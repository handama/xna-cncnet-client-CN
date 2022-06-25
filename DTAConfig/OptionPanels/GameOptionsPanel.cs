using ClientCore;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;

namespace DTAConfig.OptionPanels
{
    class GameOptionsPanel : XNAOptionsPanel
    {
        private const int TEXT_BACKGROUND_COLOR_TRANSPARENT = 0;
        private const int TEXT_BACKGROUND_COLOR_BLACK = 12;
        private const int MAX_SCROLL_RATE = 6;
        private IniFile clientDefinitionsIni;
        private string ExtraExeCommandLineParameters => clientDefinitionsIni.GetStringValue("Settings", "ExtraCommandLineParams", string.Empty);

        private List<string> essentialECLs = new List<string>() { "SPAWN", "CD", "LOG" };

        private List<string> ExtraExeCommandLineTrimed(string eclText)
        {

            string[] ecls = eclText.Split('-');
            List<string> configurableECLs = new List<string>();
            int n = 1;
            for (; n < ecls.Length;)
            {
                ecls[n] = ecls[n].Trim();
                bool isEssentialECL = false;
                foreach (string eecl in essentialECLs)
                {
                    if (String.Equals(eecl, ecls[n], StringComparison.CurrentCultureIgnoreCase))
                    {
                        isEssentialECL = true;
                    }
                }
                if (!isEssentialECL)
                {
                    configurableECLs.Add(ecls[n]);
                }
                n++;
            }
            return configurableECLs;
        }
        private void WriteExtraCommandLines(string ecls)
        {
            string clt = "";
            foreach (string ecl in essentialECLs)
            {
                clt += "-" + ecl + " ";
            }
            string fullecls = clt + ecls;
            clientDefinitionsIni.SetStringValue("Settings", "ExtraCommandLineParams", fullecls);
            clientDefinitionsIni.WriteIniFile();
        }
        private bool CompareExtraCommandLines(string ecls)
        {
            bool isAllTheSame = true;
            List<string> oldecls = ExtraExeCommandLineTrimed(ExtraExeCommandLineParameters);
            oldecls.Sort();
            List<string> newecls = ExtraExeCommandLineTrimed(ecls);
            newecls.Sort();
            string newecl = CommandLineText(ecls);
            if (oldecls.Count != newecls.Count)
            {
                WriteExtraCommandLines(newecl);
                isAllTheSame = false;
            }
            else
            {
                int i = 0;
                for (;i< oldecls.Count;)
                {
                    if (!String.Equals(oldecls[i], newecls[i], StringComparison.CurrentCultureIgnoreCase))
                    {
                        isAllTheSame = false;
                    }
                    i++;
                }
                if (!isAllTheSame) WriteExtraCommandLines(newecl);
            }
            return !isAllTheSame;
        }

        private string CommandLineText(string eclText)
        {
            string clt = "";
            foreach (string ecl in ExtraExeCommandLineTrimed(eclText))
            {
                clt += "-" + ecl + " ";
            }
            return clt;
        }


    public GameOptionsPanel(WindowManager windowManager, UserINISettings iniSettings, XNAControl topBar)
            : base(windowManager, iniSettings)
        {
            this.topBar = topBar;
        }

        private XNALabel lblScrollRateValue;

        private XNATrackbar trbScrollRate;
        private XNAClientCheckBox chkTargetLines;
        private XNAClientCheckBox chkScrollCoasting;
        private XNAClientCheckBox chkTooltips;
        private XNAClientCheckBox chkToolTipDescriptions;
        private XNAClientCheckBox chkLoadRootFolderMaps;
#if YR
        private XNAClientCheckBox chkShowHiddenObjects;
#elif DTA || TS || TI
        private XNAClientCheckBox chkAltToUndeploy;
        private XNAClientCheckBox chkBlackChatBackground;
#endif

        private XNAControl topBar;

        private XNATextBox tbPlayerName;
        private XNATextBox tbCommandLine;

        private HotkeyConfigurationWindow hotkeyConfigWindow;

        public override void Initialize()
        {
            base.Initialize();
            clientDefinitionsIni = new IniFile(ProgramConstants.GetBaseResourcePath() + "ClientDefinitions.ini");

            Name = "GameOptionsPanel";

            var lblScrollRate = new XNALabel(WindowManager);
            lblScrollRate.Name = "lblScrollRate";
            lblScrollRate.ClientRectangle = new Rectangle(12,
                14, 0, 0);
            lblScrollRate.Text = "屏幕滚动速率：";

            lblScrollRateValue = new XNALabel(WindowManager);
            lblScrollRateValue.Name = "lblScrollRateValue";
            lblScrollRateValue.FontIndex = 1;
            lblScrollRateValue.Text = "3";
            lblScrollRateValue.ClientRectangle = new Rectangle(
                Width - lblScrollRateValue.Width - 12,
                lblScrollRate.Y, 0, 0);

            trbScrollRate = new XNATrackbar(WindowManager);
            trbScrollRate.Name = "trbClientVolume";
            trbScrollRate.ClientRectangle = new Rectangle(
                lblScrollRate.Right + 32,
                lblScrollRate.Y - 2,
                lblScrollRateValue.X - lblScrollRate.Right - 47,
                22);
            trbScrollRate.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            trbScrollRate.MinValue = 0;
            trbScrollRate.MaxValue = MAX_SCROLL_RATE;
            trbScrollRate.ValueChanged += TrbScrollRate_ValueChanged;

            chkScrollCoasting = new XNAClientCheckBox(WindowManager);
            chkScrollCoasting.Name = "chkScrollCoasting";
            chkScrollCoasting.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                trbScrollRate.Bottom + 20, 0, 0);
            chkScrollCoasting.Text = "惯性滚动";

            chkTargetLines = new XNAClientCheckBox(WindowManager);
            chkTargetLines.Name = "chkTargetLines";
            chkTargetLines.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkScrollCoasting.Bottom + 24, 0, 0);
            chkTargetLines.Text = "显示目标线";

            chkTooltips = new XNAClientCheckBox(WindowManager);
            chkTooltips.Name = "chkTooltips";
            chkTooltips.Text = "工具提示";

            chkToolTipDescriptions = new XNAClientCheckBox(WindowManager);
            chkToolTipDescriptions.Name = "chkToolTipDescriptions";
            chkToolTipDescriptions.Text = "单位信息提示";

            chkLoadRootFolderMaps = new XNAClientCheckBox(WindowManager);
            chkLoadRootFolderMaps.Name = "chkLoadRootFolderMaps";
            chkLoadRootFolderMaps.Text = "加载根目录地图";
            
            var lblPlayerName = new XNALabel(WindowManager);
            lblPlayerName.Name = "lblPlayerName";
            lblPlayerName.Text = "玩家名称*:";

            var lblCommandLine = new XNALabel(WindowManager);
            lblCommandLine.Name = "lblCommandLine";
            lblCommandLine.Text = "附加命令行参数:";

#if YR
            chkShowHiddenObjects = new XNAClientCheckBox(WindowManager);
            chkShowHiddenObjects.Name = "chkShowHiddenObjects";
            chkShowHiddenObjects.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkTargetLines.Bottom + 24, 0, 0);
            chkShowHiddenObjects.Text = "显示被遮挡物品";

            chkTooltips.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkShowHiddenObjects.Bottom + 24, 0, 0);

            chkLoadRootFolderMaps.ClientRectangle = new Rectangle(
            lblScrollRate.X,
            chkTooltips.Bottom + 24, 0, 0);

            lblPlayerName.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkLoadRootFolderMaps.Bottom + 30, 0, 0);

            chkToolTipDescriptions.ClientRectangle = new Rectangle(
            lblScrollRate.X + 260,
            trbScrollRate.Bottom + 24, 0, 0);

            AddChild(chkShowHiddenObjects);
#else
            chkTooltips.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkTargetLines.Bottom + 24, 0, 0);
#endif

#if DTA || TI || TS
            chkBlackChatBackground = new XNAClientCheckBox(WindowManager);
            chkBlackChatBackground.Name = "chkBlackChatBackground";
            chkBlackChatBackground.ClientRectangle = new Rectangle(
                chkScrollCoasting.X,
                chkTooltips.Bottom + 24, 0, 0);
            chkBlackChatBackground.Text = "Use black background for in-game chat messages";

            AddChild(chkBlackChatBackground);
#endif

#if DTA || TS || TI
            chkAltToUndeploy = new XNAClientCheckBox(WindowManager);
            chkAltToUndeploy.Name = "chkAltToUndeploy";
            chkAltToUndeploy.ClientRectangle = new Rectangle(
                chkScrollCoasting.X,
                chkBlackChatBackground.Bottom + 24, 0, 0);
            chkAltToUndeploy.Text = "Undeploy units by holding Alt key instead of a regular move command";

            AddChild(chkAltToUndeploy);

            lblPlayerName.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkAltToUndeploy.Bottom + 30, 0, 0);
#endif




            tbPlayerName = new XNATextBox(WindowManager);
            tbPlayerName.Name = "tbPlayerName";
            tbPlayerName.MaximumTextLength = ClientConfiguration.Instance.MaxNameLength;
            tbPlayerName.ClientRectangle = new Rectangle(trbScrollRate.X,
                lblPlayerName.Y - 2, 200, 19);
            tbPlayerName.Text = ProgramConstants.PLAYERNAME;

            var lblNotice = new XNALabel(WindowManager);
            lblNotice.Name = "lblNotice";
            lblNotice.ClientRectangle = new Rectangle(lblPlayerName.X,
                lblPlayerName.Bottom + 30, 0, 0);
            lblNotice.Text = "* 如果你已连接到CnCNet，需要重新登录才能使新名称生效。";

            lblCommandLine.ClientRectangle = new Rectangle(
            lblScrollRate.X,
            lblNotice.Bottom + 12, 0, 0);

            tbCommandLine = new XNATextBox(WindowManager);
            tbCommandLine.Name = "tbCommandLine";
            tbCommandLine.MaximumTextLength = 99;
            tbCommandLine.ClientRectangle = new Rectangle(trbScrollRate.X,
                lblCommandLine.Y - 2, 200, 19);

            tbCommandLine.Text = CommandLineText(ExtraExeCommandLineParameters);

            hotkeyConfigWindow = new HotkeyConfigurationWindow(WindowManager);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, hotkeyConfigWindow);
            hotkeyConfigWindow.Disable();

            var btnConfigureHotkeys = new XNAClientButton(WindowManager);
            btnConfigureHotkeys.Name = "btnConfigureHotkeys";
            btnConfigureHotkeys.ClientRectangle = new Rectangle(lblCommandLine.X, lblNotice.Bottom + 36, 160, 23);
            btnConfigureHotkeys.Text = "编辑热键";
            btnConfigureHotkeys.LeftClick += BtnConfigureHotkeys_LeftClick;

            AddChild(lblScrollRate);
            AddChild(lblScrollRateValue);
            AddChild(trbScrollRate);
            AddChild(chkScrollCoasting);
            AddChild(chkTargetLines);
            AddChild(chkTooltips);
            AddChild(chkToolTipDescriptions);
            AddChild(chkLoadRootFolderMaps);
            AddChild(lblPlayerName);
            AddChild(lblCommandLine);
            AddChild(tbCommandLine);
            AddChild(tbPlayerName);
            AddChild(lblNotice);
            AddChild(btnConfigureHotkeys);
        }

        private void BtnConfigureHotkeys_LeftClick(object sender, EventArgs e)
        {
            hotkeyConfigWindow.Enable();

            if (topBar.Enabled)
            {
                topBar.Disable();
                hotkeyConfigWindow.EnabledChanged += HotkeyConfigWindow_EnabledChanged;
            }
        }

        private void HotkeyConfigWindow_EnabledChanged(object sender, EventArgs e)
        {
            hotkeyConfigWindow.EnabledChanged -= HotkeyConfigWindow_EnabledChanged;
            topBar.Enable();
        }

        private void TrbScrollRate_ValueChanged(object sender, EventArgs e)
        {
            lblScrollRateValue.Text = trbScrollRate.Value.ToString();
        }

        public override void Load()
        {
            base.Load();
            
            int scrollRate = ReverseScrollRate(IniSettings.ScrollRate);

            if (scrollRate >= trbScrollRate.MinValue && scrollRate <= trbScrollRate.MaxValue)
            {
                trbScrollRate.Value = scrollRate;
                lblScrollRateValue.Text = scrollRate.ToString();
            }

            chkScrollCoasting.Checked = !Convert.ToBoolean(IniSettings.ScrollCoasting);
            chkTargetLines.Checked = IniSettings.TargetLines;
            chkTooltips.Checked = IniSettings.Tooltips;
            chkToolTipDescriptions.Checked = IniSettings.ToolTipDescriptions;
            chkLoadRootFolderMaps.Checked = IniSettings.LoadRootFolderMaps;
#if YR
            chkShowHiddenObjects.Checked = IniSettings.ShowHiddenObjects;
#endif

#if DTA || TS || TI
            chkAltToUndeploy.Checked = !IniSettings.MoveToUndeploy;
            chkBlackChatBackground.Checked = IniSettings.TextBackgroundColor == TEXT_BACKGROUND_COLOR_BLACK;
#endif
            tbPlayerName.Text = UserINISettings.Instance.PlayerName;
            tbCommandLine.Text = CommandLineText(ExtraExeCommandLineParameters);
        }

        public override bool Save()
        {
            base.Save();
            bool restartRequired = false;

            IniSettings.ScrollRate.Value = ReverseScrollRate(trbScrollRate.Value);

            IniSettings.ScrollCoasting.Value = Convert.ToInt32(!chkScrollCoasting.Checked);
            IniSettings.TargetLines.Value = chkTargetLines.Checked;
            IniSettings.Tooltips.Value = chkTooltips.Checked;

            IniSettings.ToolTipDescriptions.Value = chkToolTipDescriptions.Checked;

            if (IniSettings.LoadRootFolderMaps.Value != chkLoadRootFolderMaps.Checked)
                restartRequired = true;
            IniSettings.LoadRootFolderMaps.Value = chkLoadRootFolderMaps.Checked;
#if YR
            IniSettings.ShowHiddenObjects.Value = chkShowHiddenObjects.Checked;
#endif

#if DTA || TS || TI
            IniSettings.MoveToUndeploy.Value = !chkAltToUndeploy.Checked;
            if (chkBlackChatBackground.Checked)
                IniSettings.TextBackgroundColor.Value = TEXT_BACKGROUND_COLOR_BLACK;
            else
                IniSettings.TextBackgroundColor.Value = TEXT_BACKGROUND_COLOR_TRANSPARENT;
#endif

            string playerName = tbPlayerName.Text;
            playerName = playerName.Replace(",", string.Empty);
            playerName = Renderer.GetSafeString(playerName, 0);
            playerName.Trim();

            if (playerName.Length > 0)
                IniSettings.PlayerName.Value = tbPlayerName.Text;

            restartRequired = restartRequired || CompareExtraCommandLines(tbCommandLine.Text);
            return restartRequired;
        }

        private int ReverseScrollRate(int scrollRate)
        {
            return Math.Abs(scrollRate - MAX_SCROLL_RATE);
        }
    }
}
