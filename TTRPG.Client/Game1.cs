using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TTRPG.Client.Services;
using System.IO;
using TTRPG.Client.Systems;
using Myra;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;

namespace TTRPG.Client
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch? _spriteBatch;
        private RenderTarget2D? _renderTarget;
        private ClientNetworkService? _networkService;

        // --- SYSTEMS ---
        private InputManager _inputManager;
        private Desktop? _desktop;
        private TextureManager? _textureManager;
        private TiledMapRenderer? _mapRenderer;
        private Camera2D? _camera; // <--- RESTORED CAMERA

        // --- UI WIDGETS ---
        private ListView? _chatList;
        private TextBox? _chatInput;

        // --- GAME STATE ---
        private const int VIRTUAL_WIDTH = 640;
        private const int VIRTUAL_HEIGHT = 360;
        private Rectangle _destinationRectangle;

        private Dictionary<int, EntityRenderData> _entities = new Dictionary<int, EntityRenderData>();
        private Color _backgroundColor = Color.CornflowerBlue;
        private Texture2D? _whitePixel;

        private double _currentGameTime;
        private double _lastMoveTime;

        private string _currentTooltip = string.Empty;
        private Vector2 _tooltipPosition;
        private double _tooltipTimer;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnResize;
            Exiting += OnGameExiting;
        }

        protected override void Initialize()
        {
            _inputManager = new InputManager();

            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();

            _renderTarget = new RenderTarget2D(GraphicsDevice, VIRTUAL_WIDTH, VIRTUAL_HEIGHT);
            UpdateDestinationRectangle();

            // --- RESTORED CAMERA INIT ---
            _camera = new Camera2D(GraphicsDevice, VIRTUAL_WIDTH, VIRTUAL_HEIGHT);
            _camera.Position = new Vector2(0, 0);

            // Networking Events
            EventBus.OnServerJoined += (msg) => System.Diagnostics.Debug.WriteLine($"[UI] Joined: {msg}");
            EventBus.OnGameStateChanged += (state) => _backgroundColor = (state == Shared.Enums.GameState.Combat) ? Color.DarkRed : Color.CornflowerBlue;

            EventBus.OnEntityMoved += (id, pos) =>
            {
                var screenPos = new Vector2(pos.X * 16, pos.Y * 16);
                _entities[id] = new EntityRenderData { Position = screenPos, LastUpdate = _currentGameTime };
            };

            EventBus.OnEntityInspected += (id, details) =>
            {
                _currentTooltip = details;
                _tooltipTimer = 3.0;
            };

            _networkService = new ClientNetworkService();
            _networkService.Connect("localhost", 9050);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });

            _textureManager = new TextureManager(GraphicsDevice);
            _textureManager.LoadContent();

            _mapRenderer = new TiledMapRenderer(GraphicsDevice, _textureManager);
            try
            {
                string mapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "test_map.tmx");
                _mapRenderer.LoadMap(mapPath);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Map Error] {ex.Message}"); }

            // --- MYRA SETUP ---
            MyraEnvironment.Game = this;
            _desktop = new Desktop();

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
                        _networkService?.SendChat(text);
                        _chatInput.Text = "";
                        _desktop.FocusedKeyboardWidget = null; // Release Focus
                    }
                }
            };

            grid.Widgets.Add(_chatList);
            grid.Widgets.Add(_chatInput);
            _desktop.Root = grid;

            EventBus.OnChatReceived += (sender, msg) =>
            {
                var label = new Label { Text = $"{sender}: {msg}", Wrap = true, TextColor = Color.White };
                _chatList.Widgets.Add(label);
                if (_chatList.Widgets.Count > 0) _chatList.SelectedIndex = _chatList.Widgets.Count - 1;
            };
        }

        protected override void Update(GameTime gameTime)
        {
            _inputManager.Update();

            // UI Input Capture Logic
            if (_desktop != null)
            {
                _inputManager.IsInputCaptured = (_desktop.FocusedKeyboardWidget != null);
            }

            // Escape Handling
            if (_inputManager.IsKeyPressedRaw(Keys.Escape))
            {
                if (_desktop != null && _desktop.FocusedKeyboardWidget != null)
                {
                    _desktop.FocusedKeyboardWidget = null;
                    _inputManager.IsInputCaptured = false;
                }
                else Exit();
            }

            _currentGameTime = gameTime.TotalGameTime.TotalSeconds;

            if (_networkService != null && !_inputManager.IsInputCaptured)
            {
                // Movement
                if (_currentGameTime - _lastMoveTime > 0.15)
                {
                    if (_inputManager.IsKeyDown(Keys.Up)) { _networkService.SendMove(Shared.Enums.MoveDirection.Up); _lastMoveTime = _currentGameTime; }
                    else if (_inputManager.IsKeyDown(Keys.Down)) { _networkService.SendMove(Shared.Enums.MoveDirection.Down); _lastMoveTime = _currentGameTime; }
                    else if (_inputManager.IsKeyDown(Keys.Left)) { _networkService.SendMove(Shared.Enums.MoveDirection.Left); _lastMoveTime = _currentGameTime; }
                    else if (_inputManager.IsKeyDown(Keys.Right)) { _networkService.SendMove(Shared.Enums.MoveDirection.Right); _lastMoveTime = _currentGameTime; }
                }

                // Mouse Logic (Inspect)
                var mState = _inputManager.GetMouseState();
                var prevMState = _inputManager.GetPreviousMouseState();

                if (mState.RightButton == ButtonState.Pressed &&
                    prevMState.RightButton == ButtonState.Released &&
                    !_desktop.IsMouseOverGUI)
                {
                    HandleMouseClick(mState);
                }
            }

            // --- RESTORED CAMERA FOLLOW LOGIC ---
            if (_entities.Count > 0 && _camera != null)
            {
                // Simple: Follow the first entity we know about (Usually the player if they spawned first)
                // In Pass 3, we will check entity.IsMine
                var target = _entities.Values.First().Position;
                _camera.Position = Vector2.Lerp(_camera.Position, target, 0.1f);
            }

            // Cleanup Ghosts
            var toRemove = new System.Collections.Generic.List<int>();
            foreach (var kvp in _entities)
            {
                if (_currentGameTime - kvp.Value.LastUpdate > 1.0) toRemove.Add(kvp.Key);
            }
            foreach (var id in toRemove) _entities.Remove(id);

            _networkService?.Poll();
            base.Update(gameTime);
        }

        private void HandleMouseClick(MouseState mState)
        {
            if (_camera == null) return;

            // 1. Get Screen Coordinates adjusted for Resolution scaling
            float scaleX = (float)VIRTUAL_WIDTH / _destinationRectangle.Width;
            float scaleY = (float)VIRTUAL_HEIGHT / _destinationRectangle.Height;
            float relativeX = (mState.X - _destinationRectangle.X) * scaleX;
            float relativeY = (mState.Y - _destinationRectangle.Y) * scaleY;

            // 2. Convert to World Coordinates using Camera Matrix
            // This handles the camera pan/zoom automatically
            Vector2 mouseScreenPos = new Vector2(relativeX, relativeY);
            Vector2 mouseWorldPos = _camera.ScreenToWorld(mouseScreenPos);

            // 3. Hit Test
            foreach (var kvp in _entities)
            {
                // Entity positions are already in World Space pixels
                if (Vector2.Distance(mouseWorldPos, kvp.Value.Position) < 20)
                {
                    _networkService?.InspectEntity(kvp.Key);
                    _tooltipPosition = mouseScreenPos; // Draw tooltip at screen location
                    break;
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            if (_renderTarget == null || _spriteBatch == null) return;

            GraphicsDevice.SetRenderTarget(_renderTarget);
            GraphicsDevice.Clear(Color.Black);

            // --- RESTORED CAMERA MATRIX ---
            // Use the Camera's matrix instead of the static center transform
            var viewMatrix = _camera?.GetViewMatrix() ?? Matrix.Identity;

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: viewMatrix);

            // Draw Map & Entities using Camera View
            _mapRenderer?.Draw(_spriteBatch, viewMatrix);

            var goblinTex = _textureManager?.GetTexture("goblin");
            if (goblinTex != null)
            {
                foreach (var kvp in _entities)
                {
                    _spriteBatch.Draw(goblinTex, kvp.Value.Position, Color.White);
                }
            }
            _spriteBatch.End();

            // --- UI PASS (Screen Space) ---
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Draw Tooltip (Manually rendered UI)
            if (!string.IsNullOrEmpty(_currentTooltip))
            {
                // We draw this in screen space (no camera transform)
                _spriteBatch.Draw(_whitePixel, new Rectangle((int)_tooltipPosition.X, (int)_tooltipPosition.Y, 120, 60), Color.Black * 0.8f);
            }
            _spriteBatch.End();

            // --- FINAL PASS: Upscale to Monitor ---
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_renderTarget, _destinationRectangle, Color.White);
            _spriteBatch.End();

            // Myra UI (Always draws on top, automatically handles screen space)
            _desktop?.Render();

            // --- FIX: RESTORE TOOLTIP TIMER ---
            if (_tooltipTimer > 0)
            {
                _tooltipTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_tooltipTimer <= 0)
                {
                    _currentTooltip = string.Empty;
                }
            }

            base.Draw(gameTime);
        }

        private void UpdateDestinationRectangle()
        {
            var screenSize = GraphicsDevice.PresentationParameters.Bounds;
            float scaleX = (float)screenSize.Width / VIRTUAL_WIDTH;
            float scaleY = (float)screenSize.Height / VIRTUAL_HEIGHT;
            float finalScale = System.Math.Min(scaleX, scaleY);
            int newWidth = (int)(VIRTUAL_WIDTH * finalScale);
            int newHeight = (int)(VIRTUAL_HEIGHT * finalScale);
            int posX = (screenSize.Width - newWidth) / 2;
            int posY = (screenSize.Height - newHeight) / 2;
            _destinationRectangle = new Rectangle(posX, posY, newWidth, newHeight);
        }

        private void OnResize(object? sender, EventArgs e) => UpdateDestinationRectangle();
        private void OnGameExiting(object? sender, EventArgs e) => _networkService?.Stop();

        private struct EntityRenderData
        {
            public Vector2 Position;
            public double LastUpdate;
        }
    }
}