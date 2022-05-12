using System;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchLoginPanel : INItializableWindow
    {
        private readonly QuickMatchService quickMatchService;

        public event EventHandler Exit;

        private XNATextBox tbEmail;
        private XNAPasswordBox tbPassword;
        private bool loginInitialized;

        public QuickMatchLoginPanel(WindowManager windowManager) : base(windowManager)
        {
            quickMatchService = QuickMatchService.GetInstance();
            quickMatchService.LoginEvent += LoginEvent;
        }

        public override void Initialize()
        {
            Name = nameof(QuickMatchLoginPanel);

            base.Initialize();

            XNAClientButton btnLogin;
            btnLogin = FindChild<XNAClientButton>(nameof(btnLogin));
            btnLogin.LeftClick += BtnLogin_LeftClick;

            XNAClientButton btnCancel;
            btnCancel = FindChild<XNAClientButton>(nameof(btnCancel));
            btnCancel.LeftClick += (sender, args) => Exit?.Invoke(sender, args);

            tbEmail = FindChild<XNATextBox>(nameof(tbEmail));
            tbEmail.Text = quickMatchService.GetCachedEmail() ?? string.Empty;

            tbPassword = FindChild<XNAPasswordBox>(nameof(tbPassword));
        }

        public void InitLogin()
        {
            if (loginInitialized)
                return;

            if (quickMatchService.IsLoggedIn())
                quickMatchService.RefreshAsync();

            loginInitialized = true;
        }

        private void BtnLogin_LeftClick(object sender, EventArgs eventArgs)
        {
            if (!ValidateForm())
                return;

            quickMatchService.LoginAsync(tbEmail.Text, tbPassword.Password);
        }

        public void LoginEvent(object sender, QmLoginEventArgs qmLoginEventArgs)
        {
            tbPassword.Text = string.Empty;
            ShowLoginEventMessage(qmLoginEventArgs?.Status ?? QmLoginEventStatusEnum.Unknown);
        }

        private void ShowLoginEventMessage(QmLoginEventStatusEnum status)
        {
            string message = null;
            switch (status)
            {
                case QmLoginEventStatusEnum.Unauthorized:
                    message = "Invalid username/password. Please try again.";
                    break;
                case QmLoginEventStatusEnum.FailedDataFetch:
                    message = "Unable to fetch QuickMatch data. Please try again.";
                    break;
            }

            if (!string.IsNullOrEmpty(message))
                XNAMessageBox.Show(WindowManager, "Login Error", message);
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrEmpty(tbEmail.Text))
            {
                XNAMessageBox.Show(WindowManager, "Error", "No Email specified");
                return false;
            }

            if (string.IsNullOrEmpty(tbPassword.Text))
            {
                XNAMessageBox.Show(WindowManager, "Error", "No Password specified");
                return false;
            }

            return true;
        }
    }
}
