using Cyotek.Drawing.BitmapFont;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using TTRPG.Client.Services;
using TTRPG.Client.Systems;
using TTRPG.Core.DTOs;

namespace TTRPG.Client.Managers
{
    public class UIManager
    {
        private readonly Desktop _desktop;
        private readonly ClientNetworkService _network;
        private readonly InputManager _input;

        // Widgets
        private ListView _chatList;
        private TextBox _chatInput;

        // NEW: Tooltip Widget
        private Label _tooltipLabel;
        private double _tooltipTimer;

        //Character Sheet Widgets
        private Window _characterWindow;
        private Grid _statsGrid; // Where we list the stats

        public bool IsInputCaptured => _desktop.FocusedKeyboardWidget != null;

        public UIManager(Game game, ClientNetworkService network, InputManager input)
        {
            _network = network;
            _input = input;
            MyraEnvironment.Game = game;
            _desktop = new Desktop();
        }

        public void LoadContent()
        {
            // 1. Build Chat Layout (Existing)
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

            _chatInput.KeyDown += (s, a) =>
            {
                if (a.Data == Keys.Enter)
                {
                    string text = _chatInput.Text;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        _network?.SendChat(text);
                        _chatInput.Text = "";
                        _desktop.FocusedKeyboardWidget = null;
                    }
                }
            };

            mainGrid.Widgets.Add(_chatList);
            mainGrid.Widgets.Add(_chatInput);

            // 2. Build Tooltip (Existing)
            _tooltipLabel = new Label
            {
                Visible = false,
                TextColor = Color.Yellow,
                Background = new SolidBrush(Color.Black * 0.8f),
                Padding = new Thickness(5),
                Wrap = true,
                Width = 200
            };

            // 3. NEW: Build Character Sheet Window (Hidden by default)
            _characterWindow = new Window
            {
                Title = "Character Sheet",
                Width = 400,
                Height = 500,
                Visible = false
            };

            // The content of the window will be a ScrollViewer containing a Grid
            var scroll = new ScrollViewer();
            _statsGrid = new Grid
            {
                ColumnSpacing = 10,
                RowSpacing = 5
            };
            _statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Label
            _statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill)); // Value

            scroll.Content = _statsGrid;
            _characterWindow.Content = scroll;

            // 4. Add to Desktop
            _desktop.Widgets.Add(mainGrid);
            _desktop.Widgets.Add(_tooltipLabel);
            _desktop.Widgets.Add(_characterWindow);

            // 5. Hook Events
            EventBus.OnChatReceived += OnChatReceived;
            EventBus.OnEntityInspected += ShowTooltip;

            // NEW: Hook Sheet Data
            EventBus.OnSheetReceived += RefreshCharacterSheet;
        }

        // NEW: Show tooltip at specific coordinates
        public void ShowTooltip(int entityId, string details)
        {
            // We can't easily get the mouse position here without passing it in.
            // For simplicity, let's just show it in the Top-Right corner or 
            // query the InputManager if we want it at the mouse.

            var mouse = _input.GetMouseState();
            _tooltipLabel.Left = mouse.X + 10;
            _tooltipLabel.Top = mouse.Y + 10;

            _tooltipLabel.Text = details;
            _tooltipLabel.Visible = true;
            _tooltipTimer = 3.0; // 3 Seconds
        }

        public void Update(GameTime gameTime)
        {
            _input.IsInputCaptured = IsInputCaptured;

            // Tooltip Timer
            if (_tooltipLabel.Visible)
            {
                _tooltipTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_tooltipTimer <= 0) _tooltipLabel.Visible = false;
            }

            // NEW: Toggle Character Sheet with 'C'
            // Use IsKeyPressedRaw so we can toggle it even if something is focused (like itself)
            if (_input.IsKeyPressedRaw(Keys.C))
            {
                if (_characterWindow.Visible)
                {
                    _characterWindow.Visible = false;
                }
                else
                {
                    // Open and Request Data
                    _characterWindow.Visible = true;
                    _statsGrid.Widgets.Clear();
                    _statsGrid.Widgets.Add(new Label { Text = "Loading..." });
                    _network.RequestCharacterSheet();
                }
            }
        }

        public void Draw() => _desktop.Render();
        public void Unfocus() => _desktop.FocusedKeyboardWidget = null;
        public bool IsMouseOverUI() => _desktop.IsMouseOverGUI;

        private void OnChatReceived(string sender, string msg)
        {
            var label = new Label { Text = $"{sender}: {msg}", Wrap = true, TextColor = Color.White };
            _chatList.Widgets.Add(label);
            if (_chatList.Widgets.Count > 0) _chatList.SelectedIndex = _chatList.Widgets.Count - 1;
        }

        // NEW: Populate the Window
        private void RefreshCharacterSheet(CharacterSheetData data)
        {
            _characterWindow.Title = data.Name;
            _statsGrid.Widgets.Clear();
            _statsGrid.RowsProportions.Clear();

            int currentRow = 0;

            foreach (var category in data.Categories)
            {
                // Add Category Header
                _statsGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

                var header = new Label
                {
                    Text = category.Key,
                    TextColor = Color.Cyan,
                    Padding = new Thickness(0, 10, 0, 5) // Top padding to separate sections
                };
                Grid.SetRow(header, currentRow);
                Grid.SetColumnSpan(header, 2); // Span across Label and Value columns
                _statsGrid.Widgets.Add(header);
                currentRow++;

                // Add Stats in this Category
                foreach (var stat in category.Value)
                {
                    _statsGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

                    // Label (e.g. "STR")
                    var lbl = new Label { Text = stat.Label, TextColor = Color.Gray };
                    Grid.SetRow(lbl, currentRow);
                    Grid.SetColumn(lbl, 0);
                    _statsGrid.Widgets.Add(lbl);

                    // Value (e.g. "18 (+4)")
                    var val = new Label { Text = stat.Value, TextColor = Color.White };
                    Grid.SetRow(val, currentRow);
                    Grid.SetColumn(val, 1);
                    _statsGrid.Widgets.Add(val);

                    currentRow++;
                }
            }
        }
    }
}