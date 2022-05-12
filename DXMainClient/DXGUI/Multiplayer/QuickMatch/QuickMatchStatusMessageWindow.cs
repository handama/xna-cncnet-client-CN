using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchStatusMessageWindow : INItializableWindow
    {
        private XNALabel statusMessage { get; set; }
        
        public QuickMatchStatusMessageWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            statusMessage = FindChild<XNALabel>(nameof(statusMessage));
        }

        public void SetStatusMessage(string message)
        {
            statusMessage.Text = message;
        }
    }
}
