using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TiledCS; // This now refers to your local file

namespace TTRPG.Client.Services
{
    public class TiledMapRenderer
    {
        // 1. Make nullable
        private TiledMap? _map;
        private Dictionary<int, LoadedTileset> _tilesets;
        private TextureManager _textureManager;
        private GraphicsDevice _graphicsDevice;

        // 2. Use null-conditional operator (?.) and coalescing (??)
        public int PixelWidth => _map?.Width * _map?.TileWidth ?? 0;
        public int PixelHeight => _map?.Height * _map?.TileHeight ?? 0;

        public TiledMapRenderer(GraphicsDevice graphicsDevice, TextureManager textureManager)
        {
            _graphicsDevice = graphicsDevice;
            _textureManager = textureManager;
            _tilesets = new Dictionary<int, TiledMapTileset>();
        }

        public void LoadMap(string path)
        {
            _map = new TiledMap(path);
            _tilesets.Clear();

            foreach (var tileset in _map.Tilesets)
            {
                // Our local parser guarantees these properties exist!
                if (string.IsNullOrEmpty(tileset.Source))
                {
                    _tilesets[tileset.FirstGid] = tileset;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Matrix transform)
        {
            if (_map == null) return;

            foreach (var layer in _map.Layers)
            {
                // Our custom parser returns the tiles array directly via the getter
                if (layer.Data != null)
                {
                    DrawLayer(spriteBatch, layer);
                }
            }
        }

        private void DrawLayer(SpriteBatch spriteBatch, TiledLayer layer)
        {
            if (_map == null) return;

            var tileData = layer.Data.Tiles;

            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    int index = (y * _map.Width) + x;
                    if (index >= tileData.Length) continue;

                    int gid = tileData[index];
                    if (gid == 0) continue;

                    var tileset = GetTilesetForGid(gid);
                    if (tileset == null) continue;

                    int localId = gid - tileset.FirstGid;
                    int columns = tileset.Columns;

                    int tileX = localId % columns;
                    int tileY = localId / columns;

                    var sourceRect = new Rectangle(
                        tileX * _map.TileWidth,
                        tileY * _map.TileHeight,
                        _map.TileWidth,
                        _map.TileHeight
                    );

                    var destPos = new Vector2(x * _map.TileWidth, y * _map.TileHeight);

                    string textureName = Path.GetFileNameWithoutExtension(tileset.Image.Source);
                    var texture = _textureManager.GetTexture(textureName);

                    if (texture != null)
                    {
                        spriteBatch.Draw(texture, destPos, sourceRect, Color.White);
                    }
                }
            }
        }

        private TiledMapTileset GetTilesetForGid(int gid)
        {
            TiledMapTileset bestMatch = null;
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
    }
}