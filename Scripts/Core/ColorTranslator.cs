
using Microsoft.Xna.Framework;

namespace Crispy.Scripts.Core
{
    public class ColorTranslator
    {
        public static Color FromHexString(string str)
        {
            str = str.Replace("#", "");

            byte r = byte.Parse(str.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(str.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(str.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            Color color = new Color
            {
                R = r,
                G = g,
                B = b,
                A = 0xFF
            };
            return color;
        }
    }
}
