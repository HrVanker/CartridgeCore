using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace TTRPG.Client.Services
{
    public class TextureManager
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();

        public TextureManager(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
        }

        public void LoadContent()
        {
            // Simulate loading a "World Cartridge"
            // We scan the "Assets" folder for ALL .png files
            string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

            if (!Directory.Exists(assetsPath))
            {
                System.Diagnostics.Debug.WriteLine($"[Assets] Warning: Folder not found at {assetsPath}");
                return;
            }

            string[] files = Directory.GetFiles(assetsPath, "*.png");

            foreach (string file in files)
            {
                try
                {
                    // ID is the filename without extension (e.g. "goblin.png" -> "goblin")
                    string id = Path.GetFileNameWithoutExtension(file).ToLower();

                    using (var stream = new FileStream(file, FileMode.Open))
                    {
                        var texture = Texture2D.FromStream(_graphicsDevice, stream);
                        _textures[id] = texture;
                        System.Diagnostics.Debug.WriteLine($"[Assets] Loaded: {id}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Assets] Failed to load {file}: {ex.Message}");
                }
            }
        }

        public Texture2D? GetTexture(string id)
        {
            if (_textures.TryGetValue(id.ToLower(), out var texture))
            {
                return texture;
            }
            // Return a purple square (Error Texture) if missing? 
            // For now, return null to keep it simple.
            return null;
        }
    }
}