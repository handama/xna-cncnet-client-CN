using System;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchMapItem : XNAPanel
    {
        public event EventHandler LeftClickMap;

        private readonly QmLadderMap ladderMap;
        private readonly QmLadder ladder;
        private XNAClientDropDown ddSide;
        private XNAPanel panelMap;
        private XNALabel lblMap;

        public QmMap Map => ladderMap.Map;

        private bool selected;

        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                BackgroundTexture = selected ? AssetLoader.CreateTexture(new Color(255, 0, 0), 1, 1) : null;
            }
        }

        public QuickMatchMapItem(WindowManager windowManager, QmLadderMap ladderMap, QmLadder ladder) : base(windowManager)
        {
            this.ladderMap = ladderMap;
            this.ladder = ladder;
        }


        public override void Initialize()
        {
            base.Initialize();

            int ratioDivider = 4;

            ddSide = new XNAClientDropDown(WindowManager);
            ddSide.ClientRectangle = new Rectangle(0, 0, Width / ratioDivider, QuickMatchMapList.ItemHeight);
            AddChild(ddSide);

            panelMap = new XNAPanel(WindowManager);
            panelMap.DrawBorders = false;
            panelMap.ClientRectangle = new Rectangle(ddSide.Right + 4, 0, Width - ddSide.Width - 4, QuickMatchMapList.ItemHeight);
            panelMap.LeftClick += (sender, args) => LeftClickMap?.Invoke(this, EventArgs.Empty);
            AddChild(panelMap);

            lblMap = new XNALabel(WindowManager);
            lblMap.ClientRectangle = new Rectangle(0, 0, panelMap.Width, panelMap.Height);
            lblMap.AnchorPoint = new Vector2(0, (float)panelMap.Height / 2);
            lblMap.TextAnchor = LabelTextAnchorInfo.VERTICAL_CENTER;
            lblMap.LeftClick += (sender, args) => LeftClickMap?.Invoke(this, EventArgs.Empty);
            panelMap.AddChild(lblMap);

            InitUI();
        }

        private void InitUI()
        {
            ddSide.Items.Clear();
            foreach (int ladderMapAllowedSideId in ladderMap.AllowedSideIds)
            {
                var side = ladder.Sides.FirstOrDefault(s => s.LocalId == ladderMapAllowedSideId);
                if (side == null)
                    continue;

                ddSide.AddItem(new XNADropDownItem()
                {
                    Text = side.Name,
                    Tag = side
                });
            }

            if (ddSide.Items.Count > 0)
                ddSide.SelectedIndex = 0;

            lblMap.Text = ladderMap.Description;
        }

        public void SetOpenUp(bool openUp) => ddSide.OpenUp = openUp;

        public int OpenedDownWindowBottom => GetWindowRectangle().Bottom + (ddSide.ItemHeight * ddSide.Items.Count);

        public bool ContainsPointVertical(Point point) => Y < point.Y && Y + Height < point.Y;
    }
}
