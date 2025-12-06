using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace TTRPG.Server.Services
{
    public class MapService
    {
        private bool[,]? _collisionGrid;
        public int Width { get; private set; }
        public int Height { get; private set; }

        public void LoadMap(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[Map] Error: File not found {filePath}");
                return;
            }

            try
            {
                // Load XML
                var doc = XDocument.Load(filePath);
                var mapNode = doc.Element("map");

                Width = int.Parse(mapNode.Attribute("width").Value);
                Height = int.Parse(mapNode.Attribute("height").Value);

                _collisionGrid = new bool[Width, Height];

                // Find the layer named "Collisions"
                var layer = mapNode.Elements("layer")
                    .FirstOrDefault(l => l.Attribute("name")?.Value == "Collisions");

                if (layer != null)
                {
                    var dataNode = layer.Element("data");
                    string encoding = dataNode.Attribute("encoding")?.Value;

                    if (encoding == "csv")
                    {
                        ParseCsvData(dataNode.Value);
                    }
                    else
                    {
                        Console.WriteLine("[Map] Error: Only CSV encoding is supported.");
                    }
                }

                Console.WriteLine($"[Map] Loaded {Width}x{Height} map.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Map] Failed to load: {ex.Message}");
            }
        }

        private void ParseCsvData(string csvData)
        {
            // 1. Clean up newlines/whitespace
            var lines = csvData.Trim().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // 2. Flatten the lines into a single stream of IDs
            // The CSV looks like "1,0,0,\n1,0,1" -> We make it a flat array
            var tileIds = string.Join(",", lines)
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(int.Parse)
                .ToArray();

            // 3. Populate Grid
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = x + (y * Width);
                    if (index < tileIds.Length)
                    {
                        // In Tiled, 0 = Empty. Anything > 0 represents a tile ID.
                        // For the Collision layer, if a tile exists (ID > 0), it is a Wall.
                        _collisionGrid[x, y] = tileIds[index] > 0;
                    }
                }
            }
        }

        public bool IsWalkable(int x, int y)
        {
            // FIX: If no map is loaded, allow movement everywhere (Open Void)
            if (_collisionGrid == null) return true;

            // 1. Check Bounds (Only if map is loaded)
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return false;

            // 2. Check Collision Grid
            return !_collisionGrid[x, y];
        }
    }
}