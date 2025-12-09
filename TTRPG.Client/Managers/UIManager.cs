using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using TTRPG.Client.Services;
using TTRPG.Client.Systems;
using TTRPG.Core.DTOs;
using TTRPG.Shared.DTOs;

namespace TTRPG.Client.Managers
{
    public class UIManager
    {
        private readonly Desktop _desktop;
        private readonly ClientNetworkService _network;
        private readonly InputManager _input;
        private TextureManager? _textureManager; // Store reference

        // Widgets
        private ListView _chatList;
        private TextBox _chatInput;
        private Label _tooltipLabel;
        private Window _characterWindow;
        private Grid _statsGrid;

        // Inventory Widgets
        private Window _inventoryWindow;
        private Grid _inventoryGrid;

        private double _tooltipTimer;

        public bool IsInputCaptured => _desktop.FocusedKeyboardWidget != null;

        public UIManager(Game game, ClientNetworkService network, InputManager input)
        {
            _network = network;
            _input = input;
            MyraEnvironment.Game = game;
            _desktop = new Desktop();
        }

        // FIX 1: Ensure this method accepts the argument!
        public void LoadContent(TextureManager textureManager)
        {
            _textureManager = textureManager; // Now this line works

            // 1. Build Chat Layout
            var mainGrid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8,
                Padding = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            mainGrid.RowsProportions.Add(new Proportion(ProportionType.Pixels, 200));
            mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            _chatList = new ListView { Width = 300 };
            Grid.SetRow(_chatList, 0);

            _chatInput = new TextBox { Width = 300, TextColor = Color.White };
            Grid.SetRow(_chatInput, 1);

            _chatInput.KeyDown += (s, a) => {
                if (a.Data == Keys.Enter)
                {
                    if (!string.IsNullOrWhiteSpace(_chatInput.Text))
                    {
                        _network?.SendChat(_chatInput.Text); _chatInput.Text = ""; _desktop.FocusedKeyboardWidget = null;
                    }
                }
            };

            mainGrid.Widgets.Add(_chatList);
            mainGrid.Widgets.Add(_chatInput);

            // 2. Tooltip
            _tooltipLabel = new Label { Visible = false, TextColor = Color.Yellow, Background = new SolidBrush(Color.Black * 0.8f), Padding = new Thickness(5), Wrap = true, Width = 200 };

            // 3. Character Sheet
            _characterWindow = new Window { Title = "Character Sheet", Width = 400, Height = 500, Visible = false };
            var scroll = new ScrollViewer();
            _statsGrid = new Grid { ColumnSpacing = 10, RowSpacing = 5 };
            _statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            _statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
            scroll.Content = _statsGrid;
            _characterWindow.Content = scroll;

            // 4. Inventory Window
            _inventoryWindow = new Window
            {
                Title = "Inventory",
                Width = 400,
                Height = 300,
                Visible = false
            };

            var invScroll = new ScrollViewer();
            _inventoryGrid = new Grid
            {
                ColumnSpacing = 5,
                RowSpacing = 5,
                Width = 380
            };
            for (int i = 0; i < 4; i++) _inventoryGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part));

            invScroll.Content = _inventoryGrid;
            _inventoryWindow.Content = invScroll;

            // Add all to Desktop
            _desktop.Widgets.Add(mainGrid);
            _desktop.Widgets.Add(_tooltipLabel);
            _desktop.Widgets.Add(_characterWindow);
            _desktop.Widgets.Add(_inventoryWindow);

            // Hook Events
            EventBus.OnChatReceived += OnChatReceived;
            EventBus.OnEntityInspected += ShowTooltip;
            EventBus.OnSheetReceived += RefreshCharacterSheet;
            EventBus.OnInventoryReceived += RefreshInventory;
        }

        public void Update(GameTime gameTime)
        {
            _input.IsInputCaptured = IsInputCaptured;
            if (_tooltipLabel.Visible)
            {
                _tooltipTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_tooltipTimer <= 0) _tooltipLabel.Visible = false;
            }

            if (_input.IsKeyPressedRaw(Keys.C))
            {
                _characterWindow.Visible = !_characterWindow.Visible;
                if (_characterWindow.Visible) _network.RequestCharacterSheet();
            }

            if (_input.IsKeyPressedRaw(Keys.I))
            {
                _inventoryWindow.Visible = !_inventoryWindow.Visible;
                if (_inventoryWindow.Visible)
                {
                    _inventoryGrid.Widgets.Clear();
                    _inventoryGrid.Widgets.Add(new Label { Text = "Loading..." });
                    _network.RequestInventory();
                }
            }
        }

        public void ShowTooltip(int id, string details) { var m = _input.GetMouseState(); _tooltipLabel.Left = m.X + 10; _tooltipLabel.Top = m.Y + 10; _tooltipLabel.Text = details; _tooltipLabel.Visible = true; _tooltipTimer = 3.0; }
        public void Draw() => _desktop.Render();
        public void Unfocus() => _desktop.FocusedKeyboardWidget = null;
        public bool IsMouseOverUI() => _desktop.IsMouseOverGUI;
        private void OnChatReceived(string s, string m) { var l = new Label { Text = $"{s}: {m}", Wrap = true, TextColor = Color.White }; _chatList.Widgets.Add(l); if (_chatList.Widgets.Count > 0) _chatList.SelectedIndex = _chatList.Widgets.Count - 1; }

        private void RefreshCharacterSheet(CharacterSheetData data)
        {
            _characterWindow.Title = data.Name;
            _statsGrid.Widgets.Clear();
            _statsGrid.RowsProportions.Clear();
            int row = 0;
            foreach (var cat in data.Categories)
            {
                _statsGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
                var h = new Label { Text = cat.Key, TextColor = Color.Cyan };
                Grid.SetRow(h, row); Grid.SetColumnSpan(h, 2); _statsGrid.Widgets.Add(h); row++;
                foreach (var stat in cat.Value)
                {
                    _statsGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
                    var l = new Label { Text = stat.Label, TextColor = Color.Gray };
                    Grid.SetRow(l, row); Grid.SetColumn(l, 0); _statsGrid.Widgets.Add(l);
                    var v = new Label { Text = stat.Value, TextColor = Color.White };
                    Grid.SetRow(v, row); Grid.SetColumn(v, 1); _statsGrid.Widgets.Add(v);
                    row++;
                }
            }
        }

        private void RefreshInventory(InventoryData data)
        {
            _inventoryWindow.Title = $"Inventory ({data.Items.Count}/{data.Capacity})";
            _inventoryGrid.Widgets.Clear();
            _inventoryGrid.RowsProportions.Clear();

            int col = 0;
            int row = 0;
            _inventoryGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            foreach (var item in data.Items)
            {
                var stack = new VerticalStackPanel
                {
                    Spacing = 2,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // Icon
                var texture = _textureManager?.GetTexture(item.Icon);
                if (texture != null)
                {
                    var image = new Image
                    {
                        Renderable = new TextureRegion(texture),
                        Width = 32,
                        Height = 32,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    stack.Widgets.Add(image);
                }

                // Name
                var lbl = new Label
                {
                    Text = item.Name,
                    TextColor = Color.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                    // FIX 2: Removed FontScale property
                };
                stack.Widgets.Add(lbl);

                Grid.SetColumn(stack, col);
                Grid.SetRow(stack, row);
                _inventoryGrid.Widgets.Add(stack);

                col++;
                if (col >= 4)
                {
                    col = 0;
                    row++;
                    _inventoryGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
                }
            }
        }
    }
}