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
        private Dictionary<int, EntityRenderData> _entities = new Dictionary<int, EntityRenderData>();
        private Color _goblinColor = Color.Red;
        private double _currentGameTime;

        private Color _backgroundColor = Color.CornflowerBlue; // Default Explore
        private Texture2D? _whitePixel;

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
                var screenPos = new Vector2(pos.X * 16, pos.Y * 16);
                _entities[id] = new EntityRenderData
                {
                    Position = screenPos,
                    LastUpdate = _currentGameTime // We need to capture GameTime, see below
                };
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
            _currentGameTime = gameTime.TotalGameTime.TotalSeconds;

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

            // CLEANUP GHOSTS
            // Create a list of IDs to remove
            var toRemove = new System.Collections.Generic.List<int>();

            foreach (var kvp in _entities)
            {
                // If we haven't heard from this entity in 1.0 seconds, cull it.
                if (_currentGameTime - kvp.Value.LastUpdate > 1.0)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var id in toRemove)
            {
                _entities.Remove(id);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (_renderTarget == null || _spriteBatch == null) return;

            // --- PASS 1: Low Res World ---
            GraphicsDevice.SetRenderTarget(_renderTarget);

            // We clear to Black so we can draw our own zones on top
            GraphicsDevice.Clear(Color.Black);

            // CAMERA TRANSFORM: Center the world on the screen
            // This moves (0,0) from the top-left corner to the middle of the view (320, 180)
            var centerTransform = Matrix.CreateTranslation(VIRTUAL_WIDTH / 2f, VIRTUAL_HEIGHT / 2f, 0);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: centerTransform);

            // 1. DETERMINE COLORS (Combat vs Explore)
            // Currently our state is global, so both turn red, but we draw them separately.
            Color zoneAColor = (_backgroundColor == Color.CornflowerBlue) ? Color.CornflowerBlue : Color.DarkRed;
            Color zoneBColor = (_backgroundColor == Color.CornflowerBlue) ? Color.SaddleBrown : Color.DarkRed;

            // 2. DRAW ZONE A (The Blue Void)
            // Covers X from -1000 to 0
            // We use a 1x1 white pixel texture stretched out (or just a basic rect if you have a white texture)
            // If you don't have a white pixel, we can assume the "Goblin" texture has a white pixel or create one.
            // For this test, let's assume we create a 1x1 texture on the fly in Initialize or just use 'ground' tinted.

            // HACK: Create a 1x1 white texture on the fly if needed (Do this in Initialize in real code)
            if (_whitePixel == null)
            {
                _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
                _whitePixel.SetData(new[] { Color.White });
            }

            // Draw Zone A (Left of 0)
            _spriteBatch.Draw(_whitePixel, new Rectangle(-1000, -1000, 1000, 2000), zoneAColor);

            // 3. DRAW ZONE B (The Ground)
            // Covers X from 0 to 1000
            // Draw Zone B (Right of 0)
            _spriteBatch.Draw(_whitePixel, new Rectangle(0, -1000, 1000, 2000), zoneBColor);

            // Optional: Draw the "Border" line at X=0
            _spriteBatch.Draw(_whitePixel, new Rectangle(-1, -1000, 2, 2000), Color.White);


            // 4. DRAW ENTITIES
            var goblinTex = _textureManager?.GetTexture("goblin");
            if (goblinTex != null)
            {
                foreach (var kvp in _entities)
                {
                    // kvp.Value is now the struct, so access .Position
                    _spriteBatch.Draw(goblinTex, kvp.Value.Position, Color.White);
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
        private struct EntityRenderData
        {
            public Vector2 Position;
            public double LastUpdate;
        }
    }
}