using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TTRPG.Client.Services;
using System.IO;
using TTRPG.Client.Systems;

namespace TTRPG.Client
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;

        // NULLABLE FIX: We add '?' because these are created in Initialize(), not the Constructor.
        private SpriteBatch? _spriteBatch;
        private RenderTarget2D? _renderTarget;
        private ClientNetworkService? _networkService;

        // Resolution Settings
        private const int VIRTUAL_WIDTH = 640;
        private const int VIRTUAL_HEIGHT = 360;
        private Rectangle _destinationRectangle;
        private TextureManager? _textureManager;
        private Dictionary<int, Vector2> _entityPositions = new Dictionary<int, Vector2>();
        private Color _goblinColor = Color.Red;

        private Color _backgroundColor = Color.CornflowerBlue; // Default Explore

        //Movement
        private double _lastMoveTime;
        private const double MOVE_DELAY = 0.15; // 200ms between steps

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            Window.AllowUserResizing = true;

            // FIX: Match strict signature (object? sender)
            Window.ClientSizeChanged += OnResize;

            // FIX: Hook the Exit event here instead of overriding the protected method
            Exiting += OnGameExiting;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();

            _renderTarget = new RenderTarget2D(GraphicsDevice, VIRTUAL_WIDTH, VIRTUAL_HEIGHT);
            UpdateDestinationRectangle();

            EventBus.OnServerJoined += (message) =>
            {
                // Update the "UI" (Visuals)
                _goblinColor = Color.Green; // Turn Green on success
                System.Diagnostics.Debug.WriteLine($"[UI] Connection confirmed: {message}");
            };

            EventBus.OnGameStateChanged += (newState) =>
            {
                if (newState == Shared.Enums.GameState.Combat)
                {
                    _backgroundColor = Color.DarkRed; // DANGER!
                    System.Diagnostics.Debug.WriteLine("[UI] Entering Combat Mode");
                }
                else
                {
                    _backgroundColor = Color.CornflowerBlue; // Safe
                    System.Diagnostics.Debug.WriteLine("[UI] Entering Exploration Mode");
                }
            };
            EventBus.OnEntityMoved += (id, pos) =>
            {
                // Update or Add the entity position
                // Convert Grid (16px) to Screen
                _entityPositions[id] = new Vector2(pos.X * 16, pos.Y * 16);
            };

            _networkService = new ClientNetworkService();
            _networkService.Connect("localhost", 9050);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // 1. Initialize Manager
            _textureManager = new TextureManager(GraphicsDevice);

            // 2. Load ALL assets found in the folder
            _textureManager.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            if (_networkService != null)
            {
                double currentTime = gameTime.TotalGameTime.TotalSeconds;
                if (currentTime - _lastMoveTime > MOVE_DELAY)
                {
                    var kState = Keyboard.GetState();

                    if (kState.IsKeyDown(Keys.Up))
                    {
                        _networkService.SendMove(Shared.Enums.MoveDirection.Up);
                        _lastMoveTime = currentTime;
                    }
                    else if (kState.IsKeyDown(Keys.Down))
                    {
                        _networkService.SendMove(Shared.Enums.MoveDirection.Down);
                        _lastMoveTime = currentTime;
                    }
                    else if (kState.IsKeyDown(Keys.Left))
                    {
                        _networkService.SendMove(Shared.Enums.MoveDirection.Left);
                        _lastMoveTime = currentTime;
                    }
                    else if (kState.IsKeyDown(Keys.Right))
                    {
                        _networkService.SendMove(Shared.Enums.MoveDirection.Right);
                        _lastMoveTime = currentTime;
                    }
                }
            }
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Null check required now
            _networkService?.Poll();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Safety checks
            if (_renderTarget == null || _spriteBatch == null) return;

            // --- PASS 1: Low Res World ---
            GraphicsDevice.SetRenderTarget(_renderTarget);
            GraphicsDevice.Clear(_backgroundColor);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            var texture = _textureManager?.GetTexture("goblin");
            if (texture != null)
            {
                // Draw EVERY entity we know about
                foreach (var kvp in _entityPositions)
                {
                    // kvp.Value is the Position
                    // Optional: We could tint them based on ID (kvp.Key) to tell them apart
                    _spriteBatch.Draw(texture, kvp.Value, Color.White);
                }
            }

            _spriteBatch.End();

            // --- PASS 2: Upscale to Monitor ---
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_renderTarget, _destinationRectangle, Color.White);
            _spriteBatch.End();

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

        // FIX: Strict Nullability Signature
        private void OnResize(object? sender, EventArgs e)
        {
            UpdateDestinationRectangle();
        }

        // FIX: Event Handler instead of Override
        private void OnGameExiting(object? sender, EventArgs e)
        {
            _networkService?.Stop();
        }
    }
}