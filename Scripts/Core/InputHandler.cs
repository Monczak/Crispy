using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Input;

namespace Crispy.Scripts.Core
{
    public class InputHandler
    {
        public static Dictionary<int, Keys> bindings;
        public static Dictionary<Keys, bool> heldKeys;

        public static void SetBindings(Dictionary<int, Keys> newBindings)
        {
            bindings = newBindings;
            heldKeys = new Dictionary<Keys, bool>();
        }

        public static bool[] GetKeyboardState(KeyboardState state)
        {
            bool[] result = new bool[16];

            for (int i = 0; i < 16; i++)
            {
                result[i] = state.IsKeyDown(bindings[i]);
            }

            return result;
        }

        public static void HandleKeypress(Keys key, Action action)
        {
            if (!heldKeys.ContainsKey(key))
                heldKeys.Add(key, false);

            if (Keyboard.GetState().IsKeyDown(key) && !heldKeys[key])
            {
                action.Invoke();
                heldKeys[key] = true;
            }
            else if (Keyboard.GetState().IsKeyUp(key))
            {
                heldKeys[key] = false;
            }
        }
    }
}
