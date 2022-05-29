using System;
using System.Collections.Generic;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchMapList : XNAPanel
    {
        private const int MouseScrollRate = 6;
        public const int ItemHeight = 22;
        public event EventHandler<QmMapSelectedEventArgs> MapSelected;

        private XNALabel lblVeto;
        private XNALabel lblSides;
        private XNALabel lblMaps;
        private XNAPanel panelMaps;
        private XNAScrollBar scrollBar;

        private List<QuickMatchMapListItem> mapListItems;

        public int SidesX => lblSides?.X ?? 0;
        public int SidesWidth => lblSides?.Width ?? 0;
        public int MapsX => lblMaps?.X ?? 0;
        public int MapsWidth => lblMaps?.Width ?? 0;

        public QuickMatchMapList(WindowManager windowManager) : base(windowManager)
        {
            mapListItems = new List<QuickMatchMapListItem>();
        }

        public override void Initialize()
        {
            base.Initialize();

            const int vetoGap = 16;
            const int scrollBarWidth = 18;

            lblVeto = new XNALabel(WindowManager);
            lblVeto.Text = "Veto";
            lblVeto.ClientRectangle = new Rectangle(0, 0, 18, 18);
            AddChild(lblVeto);

            lblSides = new XNALabel(WindowManager);
            lblSides.Text = "Sides";
            lblSides.ClientRectangle = new Rectangle(lblVeto.Right + vetoGap, 0, 100, 18);
            AddChild(lblSides);

            lblMaps = new XNALabel(WindowManager);
            lblMaps.Text = "Maps";
            lblMaps.ClientRectangle = new Rectangle(lblSides.Right, 0, Width - lblVeto.Width - lblSides.Width - vetoGap - scrollBarWidth, 18);
            AddChild(lblMaps);

            panelMaps = new XNAPanel(WindowManager);
            panelMaps.DrawBorders = true;
            panelMaps.DrawBorders = false;
            panelMaps.ClientRectangle = new Rectangle(0, lblVeto.Bottom, Width, Height - lblVeto.Height);
            panelMaps.DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;

            scrollBar = new XNAScrollBar(WindowManager);
            scrollBar.ClientRectangle = new Rectangle(panelMaps.Width - 18, 0, scrollBarWidth, panelMaps.Height);
            scrollBar.Scrolled += ScrollBarScrolled;
            panelMaps.AddChild(scrollBar);

            AddChild(panelMaps);

            MouseScrolled += OnMouseScrolled;
        }

        private void OnMouseScrolled(object sender, EventArgs e)
        {
            int viewTop = GetNewScrollBarViewTop();
            if (viewTop == scrollBar.ViewTop)
                return;

            scrollBar.ViewTop = viewTop;
            RefreshScrollbar();
            RefreshItemLocations();
        }

        public void AddItem(QuickMatchMapListItem listItem)
        {
            listItem.ClientRectangle = new Rectangle(0, MapItemCount * ItemHeight, Width - scrollBar.ScrollWidth, ItemHeight);
            listItem.LeftClickMap += MapItem_LeftClick;
            panelMaps.AddChild(listItem);

            listItem.SetLocations(
                new Rectangle(lblVeto.X, 0, lblVeto.Width, ItemHeight),
                new Rectangle(lblSides.X, 0, lblSides.Width, ItemHeight),
                new Rectangle(lblMaps.X, 0, lblMaps.Width, ItemHeight)
            );
            RefreshScrollbar();
            RefreshItemOpenUp(listItem);
        }

        private void RefreshItemLocations()
        {
            int index = 0;
            foreach (QuickMatchMapListItem quickMatchMapItem in MapItemChildren)
            {
                quickMatchMapItem.Y = (index++ * ItemHeight) - scrollBar.ViewTop;
                RefreshItemOpenUp(quickMatchMapItem);
            }
        }

        private void RefreshItemOpenUp(QuickMatchMapListItem quickMatchMapListItem)
            => quickMatchMapListItem.SetOpenUp(quickMatchMapListItem.OpenedDownWindowBottom > scrollBar.GetWindowRectangle().Bottom);

        private void ScrollBarScrolled(object sender, EventArgs eventArgs) => RefreshItemLocations();

        private int GetNewScrollBarViewTop()
        {
            int scrollWheelValue = Cursor.ScrollWheelValue;
            int viewTop = scrollBar.ViewTop - (scrollWheelValue * MouseScrollRate);
            int maxViewTop = scrollBar.Length - scrollBar.DisplayedPixelCount;

            if (viewTop < 0)
                viewTop = 0;
            else if (viewTop > maxViewTop)
                viewTop = maxViewTop;

            return viewTop;
        }

        public void RefreshScrollbar()
        {
            scrollBar.Length = MapItemChildren.Count() * ItemHeight;
            scrollBar.DisplayedPixelCount = panelMaps.Height - 4;
            scrollBar.Refresh();
        }

        private void MapItem_LeftClick(object sender, EventArgs eventArgs)
        {
            var selectedItem = sender as QuickMatchMapListItem;
            foreach (QuickMatchMapListItem quickMatchMapItem in MapItemChildren)
                quickMatchMapItem.Selected = quickMatchMapItem == selectedItem;

            MapSelected?.Invoke(this, new QmMapSelectedEventArgs(selectedItem?.Map));
        }

        public void Clear()
        {
            foreach (QuickMatchMapListItem child in MapItemChildren.ToList())
                panelMaps.RemoveChild(child);

            RefreshScrollbar();
        }

        private int MapItemCount => MapItemChildren.Count();

        private IEnumerable<QuickMatchMapListItem> MapItemChildren
            => panelMaps.Children.Select(c => c as QuickMatchMapListItem).Where(i => i != null);
    }
}
