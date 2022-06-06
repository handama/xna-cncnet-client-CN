using System;
using System.Collections.Generic;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchLobbyPanel : INItializableWindow
    {
        private readonly QuickMatchService quickMatchService;
        private readonly MapLoader mapLoader;

        public event EventHandler Exit;

        private QuickMatchMapList mapList;
        private XNAClientButton btnLogout;
        private XNAClientButton btnExit;
        private XNAClientDropDown ddUserAccounts;
        private XNAClientDropDown ddNicknames;
        private XNAClientDropDown ddSides;
        private XNAPanel mapPreviewBox;

        public QuickMatchLobbyPanel(WindowManager windowManager) : base(windowManager)
        {
            quickMatchService = QuickMatchService.GetInstance();
            quickMatchService.UserAccountsEvent += UserAccountsEvent;
            quickMatchService.LadderMapsEvent += LadderMapsEvent;

            mapLoader = MapLoader.GetInstance();
        }

        public override void Initialize()
        {
            Name = nameof(QuickMatchLobbyPanel);

            base.Initialize();

            mapList = FindChild<QuickMatchMapList>(nameof(QuickMatchMapList));
            mapList.MapSelected += MapSelected;

            btnLogout = FindChild<XNAClientButton>(nameof(btnLogout));
            btnLogout.LeftClick += BtnLogout_LeftClick;

            btnExit = FindChild<XNAClientButton>(nameof(btnExit));
            btnExit.LeftClick += (sender, args) => Exit?.Invoke(sender, args);

            ddUserAccounts = FindChild<XNAClientDropDown>(nameof(ddUserAccounts));
            ddUserAccounts.SelectedIndexChanged += UserAccountSelected;

            ddNicknames = FindChild<XNAClientDropDown>(nameof(ddNicknames));

            ddSides = FindChild<XNAClientDropDown>(nameof(ddSides));

            mapPreviewBox = FindChild<XNAPanel>(nameof(mapPreviewBox));
            mapPreviewBox.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.CENTERED;
        }

        private void BtnLogout_LeftClick(object sender, EventArgs eventArgs)
        {
            XNAMessageBox
                .ShowYesNoDialog(WindowManager, "Confirmation", "Are you sure you want to log out?", box => { quickMatchService.Logout(); });
        }

        /// <summary>
        /// Called when the QM service has finished the login process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="qmUserAccountsEventArgs"></param>
        private void UserAccountsEvent(object sender, QmUserAccountsEventArgs qmUserAccountsEventArgs)
        {
            ddUserAccounts.Items.Clear();
            var userAccounts = qmUserAccountsEventArgs.UserAccounts;
            foreach (QmUserAccount userAccount in userAccounts)
            {
                ddUserAccounts.AddItem(new XNADropDownItem()
                {
                    Text = userAccount.Ladder.Name,
                    Tag = userAccount
                });
            }

            if (ddUserAccounts.Items.Count == 0)
                return;

            string cachedLadder = quickMatchService.GetCachedLadder();
            if (!string.IsNullOrEmpty(cachedLadder))
                ddUserAccounts.SelectedIndex = ddUserAccounts.Items.FindIndex(i => (i.Tag as QmUserAccount)?.Ladder.Abbreviation == cachedLadder);

            if (ddUserAccounts.SelectedIndex < 0)
                ddUserAccounts.SelectedIndex = 0;
        }

        /// <summary>
        /// Called when the user has selected a UserAccount from the drop down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void UserAccountSelected(object sender, EventArgs eventArgs)
        {
            if (!(ddUserAccounts.SelectedItem?.Tag is QmUserAccount selectedUserAccount))
                return;

            UpdateNickames(selectedUserAccount);
            UpdateSides(selectedUserAccount);
            quickMatchService.SetLadder(selectedUserAccount.Ladder.Abbreviation);
        }

        /// <summary>
        /// Update the nicknames drop down
        /// </summary>
        /// <param name="selectedUserAccount"></param>
        private void UpdateNickames(QmUserAccount selectedUserAccount)
        {
            ddNicknames.Items.Clear();

            ddNicknames.AddItem(new XNADropDownItem()
            {
                Text = selectedUserAccount.Username,
                Tag = selectedUserAccount
            });

            ddNicknames.SelectedIndex = 0;
        }

        /// <summary>
        /// Update the top Sides dropdown
        /// </summary>
        /// <param name="selectedUserAccount"></param>
        private void UpdateSides(QmUserAccount selectedUserAccount)
        {
            ddSides.Items.Clear();

            var ladder = quickMatchService.GetLadderForId(selectedUserAccount.LadderId);

            foreach (QmSide side in ladder.Sides)
            {
                ddSides.AddItem(new XNADropDownItem
                {
                    Text = side.Name,
                    Tag = side
                });
            }

            if (ddSides.Items.Count > 0)
                ddSides.SelectedIndex = 0;
        }

        /// <summary>
        /// Called when the QM service has fetched new ladder maps for the ladder selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="qmLadderMapsEventArgs"></param>
        private void LadderMapsEvent(object sender, QmLadderMapsEventArgs qmLadderMapsEventArgs)
        {
            mapList.Clear();
            var ladderMaps = qmLadderMapsEventArgs?.Maps?.ToList() ?? new List<QmLadderMap>();
            if (!ladderMaps.Any())
                return;

            if (!(ddUserAccounts.SelectedItem.Tag is QmUserAccount selectedUserAccount))
                return;

            var ladder = quickMatchService.GetLadderForId(selectedUserAccount.LadderId);

            foreach (QmLadderMap ladderMap in ladderMaps)
                mapList.AddItem(new QuickMatchMapListItem(WindowManager, ladderMap, ladder));
        }

        /// <summary>
        /// Called when the user selects a map in the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="qmMap"></param>
        private void MapSelected(object sender, QmMapSelectedEventArgs qmMapSelectedEventArgs)
        {
            if (qmMapSelectedEventArgs?.Map == null)
                return;

            var map = mapLoader.GetMapForSHA(qmMapSelectedEventArgs.Map.Hash);

            mapPreviewBox.BackgroundTexture = map?.LoadPreviewTexture();
        }
    }
}
