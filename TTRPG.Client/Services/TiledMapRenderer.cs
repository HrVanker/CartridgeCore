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
        private Dictionary<int, TiledTileset> _tilesets;
        private TextureManager _textureManager;
        private GraphicsDevice _graphicsDevice;

        public int PixelWidth => _map.Width * _map.TileWidth;
        public int PixelHeight => _map.Height * _map.TileHeight;

        public TiledMapRenderer(GraphicsDevice graphicsDevice, TextureManager textureManager)
        {
            _graphicsDevice = graphicsDevice;
            _textureManager = textureManager;
            _tilesets = new Dictionary<int, TiledTileset>();
        }

        public void LoadMap(string path)
        {
            // 1. Load the Map Data
            // TiledCS parses the XML for us
            string mapDirectory = Path.GetDirectoryName(path);
            _map = new TiledMap(path);

            // 2. Load Tilesets
            // The map references tilesets (e.g., "ground.tsx" or embedded images)
            // We need to load the data AND ensure the textures are loaded.
            _tilesets.Clear();
            foreach (var mapTileset in _map.Tilesets)
            {
                if (mapTileset.Source != null)
                {
                    // External TSX file (not supported in this simple demo yet)
                    // You would load new TiledTileset(path) here
                }
                else
                {
                    // Embedded tileset (Simple way)
                    // We map the "FirstGid" to the Tileset data
                    _tilesets[mapTileset.FirstGid] = mapTileset;

                    // Logic: The image path in TMX is usually relative (e.g. "..\Assets\ground.png")
                    // We just want the filename "ground" to ask TextureManager for it.
                    string filename = Path.GetFileNameWithoutExtension(mapTileset.Image.Source);

                    // Ensure TextureManager has it (It auto-loads from Assets folder, so we are good)
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Matrix transform)
        {
            if (_map == null) return;

            // 3. Iterate Layers
            foreach (var layer in _map.Layers)
            {
                if (layer.type == TiledLayerType.TileLayer)
                {
                    DrawTileLayer(spriteBatch, layer);
                }
            }
        }

        private void DrawTileLayer(SpriteBatch spriteBatch, TiledLayer layer)
        {
            // Tiled data is a 1D array. We loop X/Y based on map width.
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    // Calculate index in the 1D array
                    int index = (y * _map.Width) + x;
                    int gid = layer.data[index]; // Global Tile ID

                    // GID 0 = Empty tile
                    if (gid == 0) continue;

                    // 4. Resolve GID to Texture
                    // We need to find which tileset this GID belongs to
                    var tileset = GetTilesetForGid(gid);
                    if (tileset == null) continue;

                    // Math to find the specific rectangle in the tileset image
                    // Normalize GID (remove the FirstGid offset)
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

                    // Destination on screen
                    var destPos = new Vector2(x * _map.TileWidth, y * _map.TileHeight);

                    // Get Texture
                    string textureName = Path.GetFileNameWithoutExtension(tileset.Image.Source);
                    var texture = _textureManager.GetTexture(textureName);

                    if (texture != null)
                    {
                        spriteBatch.Draw(texture, destPos, sourceRect, Color.White);
                    }
                }
            }
        }

        private TiledTileset GetTilesetForGid(int gid)
        {
            // Find the tileset with the highest FirstGid that is <= our GID
            // e.g. Gid 5. Tileset A starts at 1. Tileset B starts at 100. It must be A.
            TiledTileset bestMatch = null;
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