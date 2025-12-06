using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace TiledCS
{
    [XmlRoot("map")]
    public class TiledMap
    {
        [XmlAttribute("width")] public int Width { get; set; }
        [XmlAttribute("height")] public int Height { get; set; }
        [XmlAttribute("tilewidth")] public int TileWidth { get; set; }
        [XmlAttribute("tileheight")] public int TileHeight { get; set; }

        // Initialize lists to avoid null warnings
        [XmlElement("tileset")] public List<TiledMapTileset> Tilesets { get; set; } = new();
        [XmlElement("layer")] public List<TiledLayer> Layers { get; set; } = new();

        public TiledMap() { }
        public TiledMap(string path)
        {
            var serializer = new XmlSerializer(typeof(TiledMap));
            using (var stream = new FileStream(path, FileMode.Open))
            {
                var map = (TiledMap?)serializer.Deserialize(stream);
                if (map != null)
                {
                    this.Width = map.Width;
                    this.Height = map.Height;
                    this.TileWidth = map.TileWidth;
                    this.TileHeight = map.TileHeight;
                    this.Tilesets = map.Tilesets;
                    this.Layers = map.Layers;
                }
            }
        }
    }

    public class TiledMapTileset
    {
        [XmlAttribute("firstgid")] public int FirstGid { get; set; }
        [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
        [XmlAttribute("tilewidth")] public int TileWidth { get; set; }
        [XmlAttribute("tileheight")] public int TileHeight { get; set; }
        [XmlAttribute("tilecount")] public int TileCount { get; set; }
        [XmlAttribute("columns")] public int Columns { get; set; }
        [XmlAttribute("source")] public string? Source { get; set; } // Nullable because embedded tilesets don't have source

        [XmlElement("image")] public TiledImage? Image { get; set; } // Nullable
    }

    public class TiledImage
    {
        [XmlAttribute("source")] public string Source { get; set; } = string.Empty;
        [XmlAttribute("width")] public int Width { get; set; }
        [XmlAttribute("height")] public int Height { get; set; }
    }

    public class TiledLayer
    {
        [XmlAttribute("id")] public int Id { get; set; }
        [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
        [XmlAttribute("width")] public int Width { get; set; }
        [XmlAttribute("height")] public int Height { get; set; }
        [XmlAttribute("type")] public string Type { get; set; } = string.Empty;

        [XmlElement("data")] public TiledData? Data { get; set; }
    }

    public class TiledData
    {
        [XmlAttribute("encoding")] public string Encoding { get; set; } = string.Empty;
        [XmlText] public string Value { get; set; } = string.Empty;

        public int[] Tiles
        {
            get
            {
                if (Encoding == "csv")
                {
                    return Array.ConvertAll(
                        Value.Replace("\n", "").Trim().Split(','),
                        int.Parse
                    );
                }
                return Array.Empty<int>();
            }
        }
    }
}