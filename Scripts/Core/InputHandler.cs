using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Input;

namespace Crispy.Scripts.Core
{
    public class InputHandler
    {
        public static Dictionary<int, Keys> bindings;

        public static void SetBindings(Dictionary<int, Keys> newBindings)
        {
            bindings = newBindings;
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
    }
}
