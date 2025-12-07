using Microsoft.Xna.Framework.Input;

namespace TTRPG.Client.Systems
{
    public class InputManager
    {
        private KeyboardState _currentKeyboard;
        private KeyboardState _previousKeyboard;

        private MouseState _currentMouse;
        private MouseState _previousMouse;

        // Global flag: If true, the Game World ignores input
        public bool IsInputCaptured { get; set; } = false;

        public void Update()
        {
            _previousKeyboard = _currentKeyboard;
            _currentKeyboard = Keyboard.GetState();

            _previousMouse = _currentMouse;
            _currentMouse = Mouse.GetState();
        }

        // Returns true if the key is held down (Movement)
        public bool IsKeyDown(Keys key)
        {
            if (IsInputCaptured) return false;
            return _currentKeyboard.IsKeyDown(key);
        }

        // Returns true ONLY on the frame the key was pressed (Menu Toggles)
        public bool IsKeyPressed(Keys key)
        {
            if (IsInputCaptured) return false;
            return _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
        }
        public bool IsKeyPressedRaw(Keys key)
        {
            return _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
        }

        public MouseState GetMouseState()
        {
            // We might want to block mouse clicks too if hovering over UI
            return _currentMouse;
        }

        public MouseState GetPreviousMouseState()
        {
            return _previousMouse;
        }
    }
}