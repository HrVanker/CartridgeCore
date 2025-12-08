using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TTRPG.Client.Services;
using System.IO;
using TTRPG.Client.Systems;
using TTRPG.Client.Managers; // <--- NEW Namespace

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
        private UIManager? _uiManager; // <--- REPLACES Desktop/Widgets
        private TextureManager? _textureManager;
        private TiledMapRenderer? _mapRenderer;
        private Camera2D? _camera;

        // --- GAME STATE ---
        private const int VIRTUAL_WIDTH = 640;
        private const int VIRTUAL_HEIGHT = 360;
        private Rectangle _destinationRectangle;

        private Dictionary<int, EntityRenderData> _entities = new Dictionary<int, EntityRenderData>();
        private Color _backgroundColor = Color.CornflowerBlue;
        private Texture2D? _whitePixel;

        private double _currentGameTime;
        private double _lastMoveTime;

        // Tooltips (These stay here for 3.3 Step 6, then move to UI)
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

            _camera = new Camera2D(GraphicsDevice, VIRTUAL_WIDTH, VIRTUAL_HEIGHT);
            _camera.Position = new Vector2(0, 0);

            EventBus.OnServerJoined += (msg) => System.Diagnostics.Debug.WriteLine($"[UI] Joined: {msg}");
            EventBus.OnGameStateChanged += (state) => _backgroundColor = (state == Shared.Enums.GameState.Combat) ? Color.DarkRed : Color.CornflowerBlue;

            EventBus.OnEntityMoved += (id, pos, spriteId) =>
            {
                var screenPos = new Vector2(pos.X * 16, pos.Y * 16);

                // Update or Create
                if (_entities.ContainsKey(id))
                {
                    var existing = _entities[id];
                    existing.Position = screenPos;
                    existing.LastUpdate = _currentGameTime;
                    existing.SpriteId = spriteId; // Update sprite (e.g. if player changes armor)
                    _entities[id] = existing;
                }
                else
                {
                    _entities[id] = new EntityRenderData
                    {
                        Position = screenPos,
                        LastUpdate = _currentGameTime,
                        SpriteId = spriteId
                    };
                }
            };

            _networkService = new ClientNetworkService();
            _networkService.Connect("localhost", 9050);

            // --- NEW UI MANAGER ---
            _uiManager = new UIManager(this, _networkService, _inputManager);

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

            // --- LOAD UI ---
            _uiManager?.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            _inputManager.Update();
            _uiManager?.Update(gameTime); // Allow UI to sync focus state

            // Escape Handling via UI Manager
            if (_inputManager.IsKeyPressedRaw(Keys.Escape))
            {
                if (_uiManager != null && _uiManager.IsInputCaptured)
                {
                    _uiManager.Unfocus();
                }
                else Exit();
            }

            _currentGameTime = gameTime.TotalGameTime.TotalSeconds;

            if (_networkService != null && !_inputManager.IsInputCaptured)
            {
                if (_currentGameTime - _lastMoveTime > 0.15)
                {
                    if (_inputManager.IsKeyDown(Keys.Up)) { _networkService.SendMove(Shared.Enums.MoveDirection.Up); _lastMoveTime = _currentGameTime; }
                    else if (_inputManager.IsKeyDown(Keys.Down)) { _networkService.SendMove(Shared.Enums.MoveDirection.Down); _lastMoveTime = _currentGameTime; }
                    else if (_inputManager.IsKeyDown(Keys.Left)) { _networkService.SendMove(Shared.Enums.MoveDirection.Left); _lastMoveTime = _currentGameTime; }
                    else if (_inputManager.IsKeyDown(Keys.Right)) { _networkService.SendMove(Shared.Enums.MoveDirection.Right); _lastMoveTime = _currentGameTime; }
                }
                if (_inputManager.IsKeyPressed(Keys.E))
                {
                    _networkService.SendAction(Shared.Enums.ActionType.Pickup);
                }
                var mState = _inputManager.GetMouseState();
                var prevMState = _inputManager.GetPreviousMouseState();

                if (mState.RightButton == ButtonState.Pressed &&
                    prevMState.RightButton == ButtonState.Released &&
                    !(_uiManager?.IsMouseOverUI() ?? false))
                {
                    HandleMouseClick(mState);
                }
            }

            if (_entities.Count > 0 && _camera != null)
            {
                var target = _entities.Values.First().Position;
                _camera.Position = Vector2.Lerp(_camera.Position, target, 0.1f);
            }

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

            float scaleX = (float)VIRTUAL_WIDTH / _destinationRectangle.Width;
            float scaleY = (float)VIRTUAL_HEIGHT / _destinationRectangle.Height;
            float relativeX = (mState.X - _destinationRectangle.X) * scaleX;
            float relativeY = (mState.Y - _destinationRectangle.Y) * scaleY;

            Vector2 mouseScreenPos = new Vector2(relativeX, relativeY);
            Vector2 mouseWorldPos = _camera.ScreenToWorld(mouseScreenPos);

            foreach (var kvp in _entities)
            {
                if (Vector2.Distance(mouseWorldPos, kvp.Value.Position) < 20)
                {
                    _networkService?.InspectEntity(kvp.Key);
                    _tooltipPosition = mouseScreenPos;
                    break;
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            if (_renderTarget == null || _spriteBatch == null) return;

            GraphicsDevice.SetRenderTarget(_renderTarget);
            GraphicsDevice.Clear(Color.Black);

            var viewMatrix = _camera?.GetViewMatrix() ?? Matrix.Identity;

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: viewMatrix);
            _mapRenderer?.Draw(_spriteBatch, viewMatrix);

            foreach (var kvp in _entities)
            {
                // Use specific sprite, or fallback to 'goblin' if texture missing
                var tex = _textureManager?.GetTexture(kvp.Value.SpriteId)
                       ?? _textureManager?.GetTexture("goblin");

                if (tex != null)
                {
                    _spriteBatch.Draw(tex, kvp.Value.Position, Color.White);
                }
            }
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_renderTarget, _destinationRectangle, Color.White);
            _spriteBatch.End();

            // --- DRAW UI MANAGER ---
            _uiManager?.Draw();

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
            public string SpriteId;
        }
    }
}