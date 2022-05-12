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
        }

        public void AddItem(QuickMatchMapItem item)
        {
            item.ClientRectangle = new Rectangle(0, MapItemCount * ItemHeight, Width - scrollBar.ScrollWidth, ItemHeight);
            item.LeftClickMap += MapItem_LeftClick;
            AddChild(item);

            RefreshScrollbar();
        }

        private void RefreshItemLocations()
        {
            int index = 0;
            foreach (QuickMatchMapItem quickMatchMapItem in MapItemChildren)
                quickMatchMapItem.Y = (index++ * ItemHeight) - scrollBar.ViewTop;
        }

        private void ScrollBarScrolled(object sender, EventArgs eventArgs)
        {
            RefreshItemLocations();
        }

        public void RefreshScrollbar()
        {
            scrollBar.Length = MapItemChildren.Count() * ItemHeight;
            scrollBar.DisplayedPixelCount = this.Height - 4;
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
