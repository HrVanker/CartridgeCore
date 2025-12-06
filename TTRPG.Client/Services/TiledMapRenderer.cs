using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TiledCS;

namespace TTRPG.Client.Services
{
    public class TiledMapRenderer
    {
        private TiledMap _map;
        private Dictionary<int, LoadedTileset> _tilesets; // Use our custom wrapper
        private TextureManager _textureManager;
        private GraphicsDevice _graphicsDevice;

        public int PixelWidth => _map.Width * _map.TileWidth;
        public int PixelHeight => _map.Height * _map.TileHeight;

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

            // TiledCS v3 uses PascalCase for properties
            foreach (var mapTileset in _map.Tilesets)
            {
                if (mapTileset.Source != null)
                {
                    // External TSX not supported in this demo yet
                    continue;
                }

                // Internal (Embedded) Tileset
                // We extract the data we need into our custom struct
                var loadedData = new LoadedTileset
                {
                    FirstGid = mapTileset.FirstGid,
                    TileWidth = mapTileset.TileWidth,
                    TileHeight = mapTileset.TileHeight,
                    Columns = mapTileset.Columns,
                    TileCount = mapTileset.TileCount,
                    // The image property might be null if not an image tileset, 
                    // but for our test map it exists.
                    ImageSource = mapTileset.Image.Source
                };

                _tilesets[mapTileset.FirstGid] = loadedData;
            }
        }

        public void Draw(SpriteBatch spriteBatch, Matrix transform)
        {
            if (_map == null) return;

            foreach (var layer in _map.Layers)
            {
                if (layer.Type == TiledLayerType.TileLayer)
                {
                    DrawTileLayer(spriteBatch, layer);
                }
            }
        }

        private void DrawTileLayer(SpriteBatch spriteBatch, TiledLayer layer)
        {
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    int index = (y * _map.Width) + x;
                    int gid = layer.Data[index]; // 'Data' is PascalCase in v3

                    if (gid == 0) continue;

                    var tileset = GetTilesetForGid(gid);
                    if (tileset == null) continue;

                    // Math to find the specific rectangle in the tileset image
                    int localId = gid - tileset.Value.FirstGid;
                    int columns = tileset.Value.Columns;

                    int tileX = localId % columns;
                    int tileY = localId / columns;

                    var sourceRect = new Rectangle(
                        tileX * _map.TileWidth,
                        tileY * _map.TileHeight,
                        _map.TileWidth,
                        _map.TileHeight
                    );

                    var destPos = new Vector2(x * _map.TileWidth, y * _map.TileHeight);

                    string textureName = Path.GetFileNameWithoutExtension(tileset.Value.ImageSource);
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

        // Custom internal wrapper to avoid modifying TiledCS classes
        private struct LoadedTileset
        {
            public int FirstGid;
            public int TileWidth;
            public int TileHeight;
            public int Columns;
            public int TileCount;
            public string ImageSource;
        }
    }
}