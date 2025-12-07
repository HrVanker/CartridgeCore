using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TTRPG.Client.Services
{
    public class Camera2D
    {
        private readonly GraphicsDevice _graphicsDevice;

        public Vector2 Position { get; set; } = Vector2.Zero;
        public float Zoom { get; set; } = 1.0f;
        public float Rotation { get; set; } = 0f;

        // "Virtual" Resolution for Pixel Art scaling
        public int VirtualWidth { get; set; } = 640;
        public int VirtualHeight { get; set; } = 360;

        public Camera2D(GraphicsDevice graphicsDevice, int width, int height)
        {
            _graphicsDevice = graphicsDevice;
            VirtualWidth = width;
            VirtualHeight = height;
        }

        public Matrix GetViewMatrix()
        {
            // 1. Move World so 'Position' is at (0,0)
            // 2. Rotate
            // 3. Scale (Zoom)
            // 4. Move (0,0) to Center of Screen

            var screenCenter = new Vector3(VirtualWidth * 0.5f, VirtualHeight * 0.5f, 0);
            var objectPosition = new Vector3(-Position.X, -Position.Y, 0);

            return Matrix.CreateTranslation(objectPosition) *
                   Matrix.CreateRotationZ(Rotation) *
                   Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
                   Matrix.CreateTranslation(screenCenter);
        }

        // UI/Mouse -> Game World
        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            // Invert the matrix to go backwards from Screen to World
            return Vector2.Transform(screenPosition, Matrix.Invert(GetViewMatrix()));
        }

        // Game World -> UI/Screen
        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return Vector2.Transform(worldPosition, GetViewMatrix());
        }
    }
}