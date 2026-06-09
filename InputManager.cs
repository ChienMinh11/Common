using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ChieChie;

public class InputManager
{
    private static KeyboardState _currentKeyMap;
    private static KeyboardState _oldKeyMap;

    public static void Update()
    {
        _oldKeyMap = _currentKeyMap;
        _currentKeyMap = Keyboard.GetState();
    }
    
    public static bool IsKeyDown(Keys key)
    {
        return _currentKeyMap.IsKeyDown(key);
    }

    public static bool IsKeyPressed(Keys key)
    {
        return _currentKeyMap.IsKeyDown(key) && _oldKeyMap.IsKeyUp(key);
    }

    public static bool IsKeyReleased(Keys key)
    {
        return _currentKeyMap.IsKeyUp(key) && _currentKeyMap.IsKeyDown(key);
    }
    
}