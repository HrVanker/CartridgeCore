using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace TiledCS
{
    // A custom, lightweight parser tailored for CartridgeCore's Embedded Tilesets
    [XmlRoot("map")]
    public class TiledMap
    {
        [XmlAttribute("width")] public int Width { get; set; }
        [XmlAttribute("height")] public int Height { get; set; }
        [XmlAttribute("tilewidth")] public int TileWidth { get; set; }
        [XmlAttribute("tileheight")] public int TileHeight { get; set; }

        [XmlElement("tileset")] public List<TiledMapTileset> Tilesets { get; set; }
        [XmlElement("layer")] public List<TiledLayer> Layers { get; set; }

        public TiledMap() { }
        public TiledMap(string path)
        {
            var serializer = new XmlSerializer(typeof(TiledMap));
            using (var stream = new FileStream(path, FileMode.Open))
            {
                var map = (TiledMap)serializer.Deserialize(stream);
                this.Width = map.Width;
                this.Height = map.Height;
                this.TileWidth = map.TileWidth;
                this.TileHeight = map.TileHeight;
                this.Tilesets = map.Tilesets;
                this.Layers = map.Layers;
            }
        }
    }

    public class TiledMapTileset
    {
        // These are the properties the NuGet package was missing!
        [XmlAttribute("firstgid")] public int FirstGid { get; set; }
        [XmlAttribute("name")] public string Name { get; set; }
        [XmlAttribute("tilewidth")] public int TileWidth { get; set; }
        [XmlAttribute("tileheight")] public int TileHeight { get; set; }
        [XmlAttribute("tilecount")] public int TileCount { get; set; }
        [XmlAttribute("columns")] public int Columns { get; set; }
        [XmlAttribute("source")] public string Source { get; set; }

        [XmlElement("image")] public TiledImage Image { get; set; }
    }

    public class TiledImage
    {
        [XmlAttribute("source")] public string Source { get; set; }
        [XmlAttribute("width")] public int Width { get; set; }
        [XmlAttribute("height")] public int Height { get; set; }
    }

    public class TiledLayer
    {
        [XmlAttribute("id")] public int Id { get; set; }
        [XmlAttribute("name")] public string Name { get; set; }
        [XmlAttribute("width")] public int Width { get; set; }
        [XmlAttribute("height")] public int Height { get; set; }

        // We use string here to avoid Enum parsing headaches
        [XmlAttribute("type")] public string Type { get; set; }

        [XmlElement("data")] public TiledData Data { get; set; }
    }

    public class TiledData
    {
        [XmlAttribute("encoding")] public string Encoding { get; set; }
        [XmlText] public string Value { get; set; }

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
                return new int[0];
            }
        }
    }
}