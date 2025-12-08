using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input; // For Keys
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using TTRPG.Client.Services;
using TTRPG.Client.Systems;

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
            // 1. Build the Layout (Grid)
            var grid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8,
                Padding = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            grid.RowsProportions.Add(new Proportion(ProportionType.Pixels, 200));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            // 2. Chat Widgets
            _chatList = new ListView { Width = 300 };
            Grid.SetRow(_chatList, 0);

            _chatInput = new TextBox { Width = 300, TextColor = Color.White };
            Grid.SetRow(_chatInput, 1);

            // 3. Wiring
            _chatInput.KeyDown += (s, a) =>
            {
                if (a.Data == Keys.Enter)
                {
                    string text = _chatInput.Text;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        _network?.SendChat(text);
                        _chatInput.Text = "";
                        _desktop.FocusedKeyboardWidget = null; // Release focus
                    }
                }
            };

            grid.Widgets.Add(_chatList);
            grid.Widgets.Add(_chatInput);
            _desktop.Root = grid;

            // 4. Hook Events
            EventBus.OnChatReceived += OnChatReceived;
            // We will hook Inspection later in Step 6
        }

        public void Update()
        {
            // Sync InputManager state with UI state
            _input.IsInputCaptured = IsInputCaptured;
        }

        public void Draw()
        {
            _desktop.Render();
        }

        public void Unfocus()
        {
            _desktop.FocusedKeyboardWidget = null;
        }

        public bool IsMouseOverUI()
        {
            return _desktop.IsMouseOverGUI;
        }

        private void OnChatReceived(string sender, string msg)
        {
            var label = new Label
            {
                Text = $"{sender}: {msg}",
                Wrap = true,
                TextColor = Color.White
            };
            _chatList.Widgets.Add(label);

            if (_chatList.Widgets.Count > 0)
            {
                _chatList.SelectedIndex = _chatList.Widgets.Count - 1;
            }
        }
    }
}