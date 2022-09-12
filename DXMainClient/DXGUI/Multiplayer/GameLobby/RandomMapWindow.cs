using ClientGUI;
using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;
using ClientCore;
using System.IO;
using Rampastring.Tools;
using System.Diagnostics;
using System.Collections.Generic;
using Localization;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A window that makes it possible for a LAN player who's hosting a game
    /// to pick between hosting a new game and hosting a loaded game.
    /// </summary>
    public class RandomMapWindow : XNAWindow
    {
        public static string GeneratorPath = ProgramConstants.GetBaseResourcePath() + "randomMapGenerator\\";
        public RandomMapWindow(WindowManager windowManager) : base(windowManager)
        {
        }
        private XNALabel lblDescription;

        public XNAButton btnGenerateMap;
        private XNAButton btnCancel;

        protected XNALabel lblMapSize;
        public XNAClientDropDown ddMapSize;

        protected XNALabel lblMapMode;
        public XNAClientDropDown ddMapMode;

        protected XNALabel lblPlayerLocation;

        protected XNALabel lblPlayerNW;
        public XNAClientDropDown ddPlayerNW;

        protected XNALabel lblPlayerNE;
        public XNAClientDropDown ddPlayerNE;

        protected XNALabel lblPlayerSW;
        public XNAClientDropDown ddPlayerSW;

        protected XNALabel lblPlayerSE;
        public XNAClientDropDown ddPlayerSE;

        protected XNALabel lblPlayerN;
        public XNAClientDropDown ddPlayerN;

        protected XNALabel lblPlayerS;
        public XNAClientDropDown ddPlayerS;

        protected XNALabel lblPlayerW;
        public XNAClientDropDown ddPlayerW;

        protected XNALabel lblPlayerE;
        public XNAClientDropDown ddPlayerE;

        public List<XNAClientDropDown> ddPlayers;

        public XNAClientCheckBox chkDamagedBuilding;

        public XNAClientCheckBox chkNoThumbnail;

        //Domain.Multiplayer.MapLoader mapLoader;

        public override void Initialize()
        {
            ddPlayers = new List<XNAClientDropDown>();

            Name = "RandomMapCreationWindow";
            BackgroundTexture = AssetLoader.LoadTexture("randommapcreationoptionsbg.png");
            ClientRectangle = new Rectangle(0, 0, 355, 270);

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.FontIndex = 1;
            lblDescription.Text = "创建随机地图";

            AddChild(lblDescription);

            lblDescription.CenterOnParent();
            lblDescription.ClientRectangle = new Rectangle(
                lblDescription.X,
                12,
                lblDescription.Width,
                lblDescription.Height);

            btnGenerateMap = new XNAButton(WindowManager);
            btnGenerateMap.Name = "btnNewGame";
            btnGenerateMap.ClientRectangle = new Rectangle(12, Height - 30, 75, 23);
            btnGenerateMap.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            btnGenerateMap.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            btnGenerateMap.FontIndex = 1;
            btnGenerateMap.Text = "生成";
            btnGenerateMap.HoverSoundEffect = new EnhancedSoundEffect("button.wav");
            btnGenerateMap.Disable();
            btnGenerateMap.Visible = true;


            btnCancel = new XNAButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - 75 - 12,
                btnGenerateMap.Y, 75, 23);
            btnCancel.IdleTexture = btnGenerateMap.IdleTexture;
            btnCancel.HoverTexture = btnGenerateMap.HoverTexture;
            btnCancel.FontIndex = 1;
            btnCancel.Text = "取消";
            btnCancel.HoverSoundEffect = btnGenerateMap.HoverSoundEffect;
            btnCancel.LeftClick += BtnCancel_LeftClick;


            lblMapMode = new XNALabel(WindowManager);
            lblMapMode.Name = "lblMapMode";
            lblMapMode.ClientRectangle = new Rectangle(12, 45, 0, 0);
            lblMapMode.FontIndex = 1;
            lblMapMode.Text = "地形类型：";
            AddChild(lblMapMode);


            ddMapMode = new XNAClientDropDown(WindowManager);
            ddMapMode.Name = "ddMapMode";
            ddMapMode.ClientRectangle = new Rectangle(lblMapMode.X + 80, lblMapMode.Y - 4, 110, 21);
            

            var mapUnitPath = GeneratorPath + "MapUnits";
            var MapUnitsDir = new DirectoryInfo(mapUnitPath);
            var dirs = MapUnitsDir.GetDirectories();
            foreach (var dir in dirs)
            {
                ddMapMode.AddItem(dir.Name);
            }
            foreach (var item in ddMapMode.Items)
            {
                item.Tag = item.Text;
                item.Text = item.Text.L10N($"UI:RandomMap:{item.Text}");
            }
            
            ddMapMode.AllowDropDown = true;
            ddMapMode.SelectedIndex = 0;
            AddChild(ddMapMode);


            lblMapSize = new XNALabel(WindowManager);
            lblMapSize.Name = "lblMapSize";
            lblMapSize.ClientRectangle = new Rectangle(ddMapMode.Right + 15, lblMapMode.Y, 0, 0);
            lblMapSize.FontIndex = 1;
            lblMapSize.Text = "尺寸：";
            AddChild(lblMapSize);


            ddMapSize = new XNAClientDropDown(WindowManager);
            ddMapSize.Name = "ddMapSize";
            ddMapSize.ClientRectangle = new Rectangle(lblMapSize.X + 50, lblMapSize.Y - 4, 55, 21);
            ddMapSize.AddItem("巨大");
            ddMapSize.AddItem("大");
            ddMapSize.AddItem("中");
            ddMapSize.AddItem("小");
            ddMapSize.AllowDropDown = true;
            ddMapSize.SelectedIndex = 1;
            ddMapSize.Tag = true;
            AddChild(ddMapSize);

            lblPlayerLocation = new XNALabel(WindowManager);
            lblPlayerLocation.Name = "lblPlayerLocation";
            lblPlayerLocation.ClientRectangle = new Rectangle(lblMapMode.X, lblMapMode.Y + 30, 0, 0);
            lblPlayerLocation.FontIndex = 1;
            lblPlayerLocation.Text = "玩家位置与数量：";
            AddChild(lblPlayerLocation);

            //-----------
            lblPlayerNW = new XNALabel(WindowManager);
            lblPlayerNW.Name = "lblPlayerNW";
            lblPlayerNW.ClientRectangle = new Rectangle(lblPlayerLocation.X, lblPlayerLocation.Y + 30, 0, 0);
            lblPlayerNW.FontIndex = 1;
            lblPlayerNW.Text = "西北：";
            AddChild(lblPlayerNW);


            ddPlayerNW = new XNAClientDropDown(WindowManager);
            ddPlayerNW.Name = "ddPlayerNW";
            ddPlayerNW.ClientRectangle = new Rectangle(lblPlayerNW.X + 50, lblPlayerNW.Y - 4, 50, 21);
            ddPlayerNW.AddItem("0");
            ddPlayerNW.AddItem("1");
            ddPlayerNW.AddItem("2");
            ddPlayerNW.AddItem("3");
            ddPlayerNW.AddItem("4");
            ddPlayerNW.AllowDropDown = true;
            ddPlayerNW.SelectedIndex = 0;
            ddPlayerNW.Tag = true;
            ddPlayerNW.SelectedIndexChanged += PlayerNumberChanged;
            AddChild(ddPlayerNW);
            ddPlayers.Add(ddPlayerNW);
            //-----------
            lblPlayerN = new XNALabel(WindowManager);
            lblPlayerN.Name = "lblPlayerN";
            lblPlayerN.ClientRectangle = new Rectangle(lblPlayerNW.X + 115, lblPlayerNW.Y, 0, 0);
            lblPlayerN.FontIndex = 1;
            lblPlayerN.Text = "正北：";
            AddChild(lblPlayerN);


            ddPlayerN = new XNAClientDropDown(WindowManager);
            ddPlayerN.Name = "ddPlayerN";
            ddPlayerN.ClientRectangle = new Rectangle(lblPlayerN.X + 50, lblPlayerN.Y - 4, 50, 21);
            ddPlayerN.AddItem("0");
            ddPlayerN.AddItem("1");
            ddPlayerN.AddItem("2");
            ddPlayerN.AddItem("3");
            ddPlayerN.AddItem("4");
            ddPlayerN.AllowDropDown = true;
            ddPlayerN.SelectedIndex = 0;
            ddPlayerN.Tag = true;
            ddPlayerN.SelectedIndexChanged += PlayerNumberChanged;
            AddChild(ddPlayerN);
            ddPlayers.Add(ddPlayerN);

            //-----------
            lblPlayerNE = new XNALabel(WindowManager);
            lblPlayerNE.Name = "lblPlayerNE";
            lblPlayerNE.ClientRectangle = new Rectangle(lblPlayerN.X + 115, lblPlayerN.Y, 0, 0);
            lblPlayerNE.FontIndex = 1;
            lblPlayerNE.Text = "东北：";
            AddChild(lblPlayerNE);


            ddPlayerNE = new XNAClientDropDown(WindowManager);
            ddPlayerNE.Name = "ddPlayerNE";
            ddPlayerNE.ClientRectangle = new Rectangle(lblPlayerNE.X + 50, lblPlayerNE.Y - 4, 50, 21);
            ddPlayerNE.AddItem("0");
            ddPlayerNE.AddItem("1");
            ddPlayerNE.AddItem("2");
            ddPlayerNE.AddItem("3");
            ddPlayerNE.AddItem("4");
            ddPlayerNE.AllowDropDown = true;
            ddPlayerNE.SelectedIndex = 0;
            ddPlayerNE.Tag = true;
            ddPlayerNE.SelectedIndexChanged += PlayerNumberChanged;
            AddChild(ddPlayerNE);
            ddPlayers.Add(ddPlayerNE);

            //-----------
            lblPlayerE = new XNALabel(WindowManager);
            lblPlayerE.Name = "lblPlayerE";
            lblPlayerE.ClientRectangle = new Rectangle(lblPlayerNE.X, lblPlayerNE.Y + 30, 0, 0);
            lblPlayerE.FontIndex = 1;
            lblPlayerE.Text = "正东：";
            AddChild(lblPlayerE);


            ddPlayerE = new XNAClientDropDown(WindowManager);
            ddPlayerE.Name = "ddPlayerE";
            ddPlayerE.ClientRectangle = new Rectangle(lblPlayerE.X + 50, lblPlayerE.Y - 4, 50, 21);
            ddPlayerE.AddItem("0");
            ddPlayerE.AddItem("1");
            ddPlayerE.AddItem("2");
            ddPlayerE.AddItem("3");
            ddPlayerE.AddItem("4");
            ddPlayerE.AllowDropDown = true;
            ddPlayerE.SelectedIndex = 0;
            ddPlayerE.Tag = true;
            ddPlayerE.SelectedIndexChanged += PlayerNumberChanged;
            AddChild(ddPlayerE);
            ddPlayers.Add(ddPlayerE);

            //-----------
            lblPlayerSE = new XNALabel(WindowManager);
            lblPlayerSE.Name = "lblPlayerSE";
            lblPlayerSE.ClientRectangle = new Rectangle(lblPlayerE.X, lblPlayerE.Y + 30, 0, 0);
            lblPlayerSE.FontIndex = 1;
            lblPlayerSE.Text = "东南：";
            AddChild(lblPlayerSE);


            ddPlayerSE = new XNAClientDropDown(WindowManager);
            ddPlayerSE.Name = "ddPlayerSE";
            ddPlayerSE.ClientRectangle = new Rectangle(lblPlayerSE.X + 50, lblPlayerSE.Y - 4, 50, 21);
            ddPlayerSE.AddItem("0");
            ddPlayerSE.AddItem("1");
            ddPlayerSE.AddItem("2");
            ddPlayerSE.AddItem("3");
            ddPlayerSE.AddItem("4");
            ddPlayerSE.AllowDropDown = true;
            ddPlayerSE.SelectedIndex = 0;
            ddPlayerSE.Tag = true;
            ddPlayerSE.SelectedIndexChanged += PlayerNumberChanged;
            AddChild(ddPlayerSE);
            ddPlayers.Add(ddPlayerSE);

            //-----------
            lblPlayerS = new XNALabel(WindowManager);
            lblPlayerS.Name = "lblPlayerS";
            lblPlayerS.ClientRectangle = new Rectangle(lblPlayerSE.X - 115, lblPlayerSE.Y, 0, 0);
            lblPlayerS.FontIndex = 1;
            lblPlayerS.Text = "正南：";
            AddChild(lblPlayerS);


            ddPlayerS = new XNAClientDropDown(WindowManager);
            ddPlayerS.Name = "ddPlayerS";
            ddPlayerS.ClientRectangle = new Rectangle(lblPlayerS.X + 50, lblPlayerS.Y - 4, 50, 21);
            ddPlayerS.AddItem("0");
            ddPlayerS.AddItem("1");
            ddPlayerS.AddItem("2");
            ddPlayerS.AddItem("3");
            ddPlayerS.AddItem("4");
            ddPlayerS.AllowDropDown = true;
            ddPlayerS.SelectedIndex = 0;
            ddPlayerS.Tag = true;
            ddPlayerS.SelectedIndexChanged += PlayerNumberChanged;
            AddChild(ddPlayerS);
            ddPlayers.Add(ddPlayerS);

            //-----------
            lblPlayerSW = new XNALabel(WindowManager);
            lblPlayerSW.Name = "lblPlayerSW";
            lblPlayerSW.ClientRectangle = new Rectangle(lblPlayerS.X - 115, lblPlayerS.Y, 0, 0);
            lblPlayerSW.FontIndex = 1;
            lblPlayerSW.Text = "西南：";
            AddChild(lblPlayerSW);


            ddPlayerSW = new XNAClientDropDown(WindowManager);
            ddPlayerSW.Name = "ddPlayerSW";
            ddPlayerSW.ClientRectangle = new Rectangle(lblPlayerSW.X + 50, lblPlayerSW.Y - 4, 50, 21);
            ddPlayerSW.AddItem("0");
            ddPlayerSW.AddItem("1");
            ddPlayerSW.AddItem("2");
            ddPlayerSW.AddItem("3");
            ddPlayerSW.AddItem("4");
            ddPlayerSW.AllowDropDown = true;
            ddPlayerSW.SelectedIndex = 0;
            ddPlayerSW.Tag = true;
            ddPlayerSW.SelectedIndexChanged += PlayerNumberChanged;
            AddChild(ddPlayerSW);
            ddPlayers.Add(ddPlayerSW);

            //-----------
            lblPlayerW = new XNALabel(WindowManager);
            lblPlayerW.Name = "lblPlayerW";
            lblPlayerW.ClientRectangle = new Rectangle(lblPlayerSW.X, lblPlayerSW.Y - 30, 0, 0);
            lblPlayerW.FontIndex = 1;
            lblPlayerW.Text = "正西：";
            AddChild(lblPlayerW);


            ddPlayerW = new XNAClientDropDown(WindowManager);
            ddPlayerW.Name = "ddPlayerW";
            ddPlayerW.ClientRectangle = new Rectangle(lblPlayerW.X + 50, lblPlayerW.Y - 4, 50, 21);
            ddPlayerW.AddItem("0");
            ddPlayerW.AddItem("1");
            ddPlayerW.AddItem("2");
            ddPlayerW.AddItem("3");
            ddPlayerW.AddItem("4");
            ddPlayerW.AllowDropDown = true;
            ddPlayerW.SelectedIndex = 0;
            ddPlayerW.Tag = true;
            ddPlayerW.SelectedIndexChanged += PlayerNumberChanged;
            AddChild(ddPlayerW);
            ddPlayers.Add(ddPlayerW);


            chkDamagedBuilding = new XNAClientCheckBox(WindowManager);
            chkDamagedBuilding.Name = "chkDamagedBuilding";
            chkDamagedBuilding.ClientRectangle = new Rectangle(lblPlayerSW.X, lblPlayerSW.Y + 30, 0, 0);
            chkDamagedBuilding.Text = "建筑物破坏";
            AddChild(chkDamagedBuilding);

            chkNoThumbnail = new XNAClientCheckBox(WindowManager);
            chkNoThumbnail.Name = "chkNoThumbnail";
            chkNoThumbnail.ClientRectangle = new Rectangle(chkDamagedBuilding.X + 110, chkDamagedBuilding.Y, 0, 0);
            chkNoThumbnail.Text = "不渲染缩略图（加快生成）";
            //AddChild(chkNoThumbnail);

            AddChild(btnGenerateMap);
            AddChild(btnCancel);

            base.Initialize();

            CenterOnParent();

        }
        private void PlayerNumberChanged(object sender, EventArgs e)
        {
            int totalPlayer = 0;
            foreach (var player in ddPlayers)
            {
                totalPlayer += player.SelectedIndex;
            }
            if (totalPlayer > 8 || totalPlayer == 0)
            {
                btnGenerateMap.Disable();
            }
            else
            {
                btnGenerateMap.Enable();
            }

        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Disable();
        }

        public void Open()
        {
            Enable();
        }

    }
}
