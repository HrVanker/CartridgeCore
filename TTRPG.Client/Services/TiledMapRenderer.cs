using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TiledCS; // This references TiledCS.cs, NOT the NuGet package

namespace TTRPG.Client.Services
{
    public class TiledMapRenderer
    {
        private TiledMap? _map;
        private Dictionary<int, LoadedTileset> _tilesets;
        private TextureManager _textureManager;
        private GraphicsDevice _graphicsDevice;

        // Use Null Coalescing for safety
        public int PixelWidth => _map?.Width * _map?.TileWidth ?? 0;
        public int PixelHeight => _map?.Height * _map?.TileHeight ?? 0;

        public TiledMapRenderer(GraphicsDevice graphicsDevice, TextureManager textureManager)
        {
            _graphicsDevice = graphicsDevice;
            _textureManager = textureManager;
            _tilesets = new Dictionary<int, LoadedTileset>();
        }

        public void LoadMap(string path)
        {
            _map = new TiledMap(path);
            _tilesets.Clear();

            foreach (var tileset in _map.Tilesets)
            {
                // Safety check: Only process tilesets with an image (Embedded)
                if (string.IsNullOrEmpty(tileset.Source) && tileset.Image != null)
                {
                    var loaded = new LoadedTileset
                    {
                        FirstGid = tileset.FirstGid,
                        TileWidth = tileset.TileWidth,
                        TileHeight = tileset.TileHeight,
                        Columns = tileset.Columns,
                        ImageSource = tileset.Image.Source
                    };
                    _tilesets[tileset.FirstGid] = loaded;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Matrix transform)
        {
            if (_map == null) return;

            foreach (var layer in _map.Layers)
            {
                if (layer.Data != null)
                {
                    DrawLayer(spriteBatch, layer);
                }
            }
        }

        private void DrawLayer(SpriteBatch spriteBatch, TiledLayer layer)
        {
            var tileData = layer.Data!.Tiles;

            for (int y = 0; y < _map!.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    int index = (y * _map.Width) + x;
                    if (index >= tileData.Length) continue;

                    int gid = tileData[index];
                    if (gid == 0) continue;

                    var tileset = GetTilesetForGid(gid);
                    if (tileset == null) continue;

                    var ts = tileset.Value; // Unpack struct
                    int localId = gid - ts.FirstGid;

                    int tileX = localId % ts.Columns;
                    int tileY = localId / ts.Columns;

                    var sourceRect = new Rectangle(
                        tileX * ts.TileWidth,
                        tileY * ts.TileHeight,
                        ts.TileWidth,
                        ts.TileHeight
                    );

                    var destPos = new Vector2(x * ts.TileWidth, y * ts.TileHeight);

                    string textureName = Path.GetFileNameWithoutExtension(ts.ImageSource);
                    var texture = _textureManager.GetTexture(textureName);

                    if (texture != null)
                    {
                        spriteBatch.Draw(texture, destPos, sourceRect, Color.White);
                    }
                }
            }
        }

        private LoadedTileset? GetTilesetForGid(int gid)
        {
            LoadedTileset? bestMatch = null;
            int maxFirstGid = -1;

            foreach (var kvp in _tilesets)
            {
                if (kvp.Key <= gid && kvp.Key > maxFirstGid)
                {
                    maxFirstGid = kvp.Key;
                    bestMatch = kvp.Value;
                }
            }
            return bestMatch;
        }

        // --- STRUCT DEFINITION MUST BE HERE ---
        private struct LoadedTileset
        {
            public int FirstGid;
            public int TileWidth;
            public int TileHeight;
            public int Columns;
            public string ImageSource;
        }
    }
}