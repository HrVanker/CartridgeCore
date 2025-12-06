using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TTRPG.Client.Services;
using System.IO;
using TTRPG.Client.Systems;
using TTRPG.Client.Services; // Ensure this is there
private TiledMapRenderer? _mapRenderer;

namespace TTRPG.Client
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;

        // NULLABLE FIX: We add '?' because these are created in Initialize(), not the Constructor.
        private SpriteBatch? _spriteBatch;
        private RenderTarget2D? _renderTarget;
        private ClientNetworkService? _networkService;
        private KeyboardState _lastKeyState;

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

        //UI Elements
        private MouseState _lastMouseState;
        private string _currentTooltip = string.Empty;
        private Vector2 _tooltipPosition;
        private double _tooltipTimer; // Hide after 3 seconds

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
            EventBus.OnEntityInspected += (id, details) =>
            {
                _currentTooltip = details;
                _tooltipTimer = 3.0; // Show for 3 seconds
            };

            _networkService = new ClientNetworkService();
            _networkService.Connect("localhost", 9050);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // 1. Initialize Texture Manager
            _textureManager = new TextureManager(GraphicsDevice);
            _textureManager.LoadContent(); // Loads 'goblin', 'ground', etc.

            // 2. Initialize Map Renderer
            _mapRenderer = new TiledMapRenderer(GraphicsDevice, _textureManager);

            try
            {
                // Load the map we copied to the output folder
                // Use Path.Combine to be OS-safe
                string mapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "test_map.tmx");
                _mapRenderer.LoadMap(mapPath);
                System.Diagnostics.Debug.WriteLine("[Client] Map Loaded via TiledCS!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Client] Map Error: {ex.Message}");
            }
        }

        protected override void Update(GameTime gameTime)
        {
            // 1. Exit Logic
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // 2. Capture Time & Input
            _currentGameTime = gameTime.TotalGameTime.TotalSeconds; // Update for TTL logic
            var kState = Keyboard.GetState();

            if (_networkService != null)
            {
                // --- MOVEMENT LOGIC (Restored) ---
                if (_currentGameTime - _lastMoveTime > 0.15) // 150ms delay
                {
                    if (kState.IsKeyDown(Keys.Up))
                    {
                        _networkService.SendMove(Shared.Enums.MoveDirection.Up);
                        _lastMoveTime = _currentGameTime;
                    }
                    else if (kState.IsKeyDown(Keys.Down))
                    {
                        _networkService.SendMove(Shared.Enums.MoveDirection.Down);
                        _lastMoveTime = _currentGameTime;
                    }
                    else if (kState.IsKeyDown(Keys.Left))
                    {
                        _networkService.SendMove(Shared.Enums.MoveDirection.Left);
                        _lastMoveTime = _currentGameTime;
                    }
                    else if (kState.IsKeyDown(Keys.Right))
                    {
                        _networkService.SendMove(Shared.Enums.MoveDirection.Right);
                        _lastMoveTime = _currentGameTime;
                    }
                }
                //Entity Inspection Logic
                var mState = Mouse.GetState();

                // DETECT RIGHT CLICK
                if (mState.RightButton == ButtonState.Pressed && _lastMouseState.RightButton == ButtonState.Released)
                {
                    // 1. Screen -> World Coordinates
                    // We rendered to a DestinationRectangle, so first remove that offset
                    float scaleX = (float)VIRTUAL_WIDTH / _destinationRectangle.Width;
                    float scaleY = (float)VIRTUAL_HEIGHT / _destinationRectangle.Height;

                    // Mouse relative to the Game Window (0,0 is top left of window)
                    float relativeX = (mState.X - _destinationRectangle.X) * scaleX;
                    float relativeY = (mState.Y - _destinationRectangle.Y) * scaleY;

                    // Apply Camera Offset (We shifted (0,0) to center of screen)
                    // Camera Center is (VIRTUAL_WIDTH/2, VIRTUAL_HEIGHT/2)
                    float worldX = relativeX - (VIRTUAL_WIDTH / 2f);
                    float worldY = relativeY - (VIRTUAL_HEIGHT / 2f);

                    // 2. Hit Test
                    // Find closest entity within 20 pixels
                    foreach (var kvp in _entities)
                    {
                        var entPos = kvp.Value.Position; // This is already in "World Screen Pixels" (x*16)

                        if (Vector2.Distance(new Vector2(worldX, worldY), entPos) < 20)
                        {
                            System.Diagnostics.Debug.WriteLine($"[UI] Inspecting Entity {kvp.Key}...");
                            _networkService.InspectEntity(kvp.Key);
                            _tooltipPosition = new Vector2(relativeX, relativeY); // Draw tooltip where we clicked
                            break;
                        }
                    }
                }
                _lastMouseState = mState;

                // Timer Logic
                if (_tooltipTimer > 0) _tooltipTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                else _currentTooltip = string.Empty;

                // --- CHAT & DM TOOLS LOGIC ---
                // Press 'T' to talk
                if (kState.IsKeyDown(Keys.T) && _lastKeyState.IsKeyUp(Keys.T))
                {
                    _networkService.SendChat("Hello World!");
                }

                // Press '1' to Teleport to Zone A
                if (kState.IsKeyDown(Keys.D1) && _lastKeyState.IsKeyUp(Keys.D1))
                {
                    _networkService.SendChat("/tp Zone_A");
                }

                // Press '2' to Teleport to Zone B
                if (kState.IsKeyDown(Keys.D2) && _lastKeyState.IsKeyUp(Keys.D2))
                {
                    _networkService.SendChat("/tp Zone_B");
                }
            }

            // 3. GHOST CLEANUP LOGIC (TTL)
            var toRemove = new System.Collections.Generic.List<int>();
            foreach (var kvp in _entities)
            {
                if (_currentGameTime - kvp.Value.LastUpdate > 1.0)
                    toRemove.Add(kvp.Key);
            }
            foreach (var id in toRemove)
            {
                _entities.Remove(id);
            }

            // 4. Update State for next frame
            _lastKeyState = kState;

            // 5. Poll Network
            _networkService?.Poll();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (_renderTarget == null || _spriteBatch == null) return;

            // --- PASS 1: Low Res World ---
            GraphicsDevice.SetRenderTarget(_renderTarget);

            // We clear to Black so we can draw our own zones on top
            GraphicsDevice.SetRenderTarget(_renderTarget);
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
                    _spriteBatch.Draw(goblinTex, kvp.Value.Position, Color.White);
                }
            }
            // DRAW TOOLTIP
            if (!string.IsNullOrEmpty(_currentTooltip))
            {
                // Draw a simple black box background
                // Ideally measure string size, but we'll use a fixed box for the test
                _spriteBatch.Draw(_whitePixel, new Rectangle((int)_tooltipPosition.X, (int)_tooltipPosition.Y, 120, 60), Color.Black * 0.8f);

                // Draw Text
                // Since we don't have a SpriteFont loaded, we must use a debug method OR load a font.
                // CRITICAL: MonoGame crashes if you DrawString without a Font.
                // FOR THIS TEST: We will print to Debug Console unless you have a SpriteFont.
                // Assuming NO FONT: We draw colored squares to represent data or just use the Console.

                // ... Wait, do we have a font? No. 
                // Let's use the Console Output for the text, and a Yellow Box on screen to confirm visual hit.
                _spriteBatch.Draw(_whitePixel, new Rectangle((int)_tooltipPosition.X, (int)_tooltipPosition.Y, 10, 10), Color.Yellow);
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