using ClientGUI;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A panel that is used to verify and display map sharing status.
    /// </summary>
    class MapSharingConfirmationPanel : XNAPanel
    {
        public MapSharingConfirmationPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        private readonly string MapSharingRequestText =
            "房主选择了一张" + Environment.NewLine +
            "你没有的地图。";

        private readonly string MapSharingDownloadText =
            "下载地图中...";

        private readonly string MapSharingFailedText =
            "地图下载失败。房主" + Environment.NewLine +
            "需要更换地图，或者" + Environment.NewLine +
            "你需要退出这场比赛。";

        public event EventHandler MapDownloadConfirmed;

        private XNALabel lblDescription;
        private XNAClientButton btnDownload;

        public override void Initialize()
        {
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.TILED;

            Name = nameof(MapSharingConfirmationPanel);
            BackgroundTexture = AssetLoader.LoadTexture("MapConfirmation.png");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = nameof(lblDescription);
            lblDescription.X = UIDesignConstants.EMPTY_SPACE_SIDES;
            lblDescription.Y = UIDesignConstants.EMPTY_SPACE_TOP;
            lblDescription.Text = MapSharingRequestText;
            AddChild(lblDescription);

            Width = 150;//(lblDescription.Right + UIDesignConstants.EMPTY_SPACE_SIDES) * 2;

            btnDownload = new XNAClientButton(WindowManager);
            btnDownload.Name = nameof(btnDownload);
            btnDownload.Width = UIDesignConstants.BUTTON_WIDTH_92;
            btnDownload.Y = 85;//lblDescription.Bottom + UIDesignConstants.EMPTY_SPACE_TOP * 4;
            btnDownload.Text = "下载";
            btnDownload.LeftClick += (s, e) => MapDownloadConfirmed?.Invoke(this, EventArgs.Empty);
            AddChild(btnDownload);
            btnDownload.CenterOnParentHorizontally();

            Height = 120;//(btnDownload.Bottom + UIDesignConstants.EMPTY_SPACE_BOTTOM) * 2;

            base.Initialize();

            CenterOnParent();

            Disable();
        }

        public void ShowForMapDownload()
        {
            lblDescription.Text = MapSharingRequestText;
            btnDownload.AllowClick = true;
            Enable();
        }

        public void SetDownloadingStatus()
        {
            lblDescription.Text = MapSharingDownloadText;
            btnDownload.AllowClick = false;
        }

        public void SetFailedStatus()
        {
            lblDescription.Text = MapSharingFailedText;
            btnDownload.AllowClick = false;
        }
    }
}
