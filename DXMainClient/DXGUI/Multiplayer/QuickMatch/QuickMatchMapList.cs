using System;
using System.Collections.Generic;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchMapList : INItializableWindow
    {
        private const int ScrollRate = 6;
        public const int ItemHeight = 22;
        public event EventHandler<QmMapSelectedEventArgs> MapSelected;

        private XNAScrollBar scrollBar;

        public QuickMatchMapList(WindowManager windowManager) : base(windowManager)
        {
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
        }

        public override void Initialize()
        {
            base.Initialize();

            scrollBar = new XNAScrollBar(WindowManager);
            scrollBar.Name = nameof(scrollBar);
            scrollBar.ClientRectangle = new Rectangle(Width - scrollBar.ScrollWidth - 1, 1, scrollBar.ScrollWidth, Height - 2);
            scrollBar.Scrolled += ScrollBarScrolled;
            AddChild(scrollBar);

            MouseScrolled += OnMouseScrolled;
        }

        private void OnMouseScrolled(object sender, EventArgs e)
        {
            int scrollWheelValue = Cursor.ScrollWheelValue;
            int viewTop = scrollBar.ViewTop - (scrollWheelValue * ScrollRate);
            int maxViewTop = scrollBar.Length - scrollBar.DisplayedPixelCount;
            
            if (viewTop < 0)
                viewTop = 0;
            else if (viewTop > maxViewTop)
                viewTop = maxViewTop;
            
            if (viewTop == scrollBar.ViewTop)
                return;
            
            scrollBar.ViewTop = viewTop;
            RefreshScrollbar();
            RefreshItemLocations();
        }

        public void AddItem(QuickMatchMapItem item)
        {
            item.ClientRectangle = new Rectangle(0, MapItemCount * ItemHeight, Width - scrollBar.ScrollWidth, ItemHeight);
            item.LeftClickMap += MapItem_LeftClick;
            AddChild(item);

            RefreshScrollbar();
            RefreshItemOpenUp(item);
        }

        private void RefreshItemLocations()
        {
            int index = 0;
            foreach (QuickMatchMapItem quickMatchMapItem in MapItemChildren)
            {
                quickMatchMapItem.Y = (index++ * ItemHeight) - scrollBar.ViewTop;
                RefreshItemOpenUp(quickMatchMapItem);
            }
        }

        private void RefreshItemOpenUp(QuickMatchMapItem quickMatchMapItem) 
            => quickMatchMapItem.SetOpenUp(quickMatchMapItem.OpenedDownWindowBottom > scrollBar.GetWindowRectangle().Bottom);

        private void ScrollBarScrolled(object sender, EventArgs eventArgs) => RefreshItemLocations();

        public void RefreshScrollbar()
        {
            scrollBar.Length = MapItemChildren.Count() * ItemHeight;
            scrollBar.DisplayedPixelCount = Height - 4;
            scrollBar.Refresh();
        }

        private void MapItem_LeftClick(object sender, EventArgs eventArgs)
        {
            var selectedItem = sender as QuickMatchMapItem;
            foreach (QuickMatchMapItem quickMatchMapItem in MapItemChildren)
                quickMatchMapItem.Selected = quickMatchMapItem == selectedItem;

            MapSelected?.Invoke(this, new QmMapSelectedEventArgs(selectedItem?.Map));
        }

        public void Clear()
        {
            foreach (QuickMatchMapItem child in MapItemChildren.ToList())
                RemoveChild(child);

            RefreshScrollbar();
        }

        private int MapItemCount => MapItemChildren.Count();

        private IEnumerable<QuickMatchMapItem> MapItemChildren
            => Children.Select(c => c as QuickMatchMapItem).Where(i => i != null);
    }
}
