﻿using System;

namespace Crispy
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new CrispyEmu())
                game.Run();
        }
    }
}
