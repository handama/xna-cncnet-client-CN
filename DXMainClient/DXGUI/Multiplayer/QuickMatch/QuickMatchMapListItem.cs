using System;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchMapListItem : XNAPanel
    {
        public event EventHandler LeftClickMap;

        private readonly QmLadderMap ladderMap;
        private readonly QmLadder ladder;
        private XNAClientCheckBox cbVeto;
        private XNAClientDropDown ddSide;
        private XNAPanel panelMap;
        private XNALabel lblMap;

        private XNAPanel topBorder;
        private XNAPanel bottomBorder;

        public QmMap Map => ladderMap.Map;

        private bool selected;

        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                panelMap.BackgroundTexture = selected ? AssetLoader.CreateTexture(new Color(255, 0, 0), 1, 1) : null;
            }
        }

        public QuickMatchMapListItem(WindowManager windowManager, QmLadderMap ladderMap, QmLadder ladder) : base(windowManager)
        {
            this.ladderMap = ladderMap;
            this.ladder = ladder;
        }


        public override void Initialize()
        {
            base.Initialize();
            DrawBorders = false;

            topBorder = new XNAPanel(WindowManager);
            topBorder.DrawBorders = true;
            AddChild(topBorder);

            bottomBorder = new XNAPanel(WindowManager);
            bottomBorder.DrawBorders = true;
            AddChild(bottomBorder);

            cbVeto = new XNAClientCheckBox(WindowManager);

            ddSide = new XNAClientDropDown(WindowManager);
            AddChild(ddSide);

            panelMap = new XNAPanel(WindowManager);
            panelMap.LeftClick += (sender, args) => LeftClickMap?.Invoke(this, EventArgs.Empty);
            panelMap.DrawBorders = false;
            AddChild(panelMap);

            lblMap = new XNALabel(WindowManager);
            lblMap.LeftClick += (sender, args) => LeftClickMap?.Invoke(this, EventArgs.Empty);
            lblMap.ClientRectangle = new Rectangle(4, 2, panelMap.Width, panelMap.Height);
            panelMap.AddChild(lblMap);
            AddChild(cbVeto);

            InitUI();
        }

        public void SetLocations(Rectangle vetoR, Rectangle ddSideR, Rectangle mapR)
        {
            cbVeto.ClientRectangle = vetoR;
            ddSide.ClientRectangle = ddSideR;
            panelMap.ClientRectangle = mapR;

            topBorder.ClientRectangle = new Rectangle(panelMap.X, panelMap.Y, panelMap.Width, 1);
            bottomBorder.ClientRectangle = new Rectangle(panelMap.X, panelMap.Bottom, panelMap.Width, 1);
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
