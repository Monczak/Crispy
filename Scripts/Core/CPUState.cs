using System;
using System.Collections.Generic;
using System.Text;

namespace Crispy.Scripts.Core
{
    public struct CPUState
    {
        public byte[] memory;           // 4K RAM

        public ushort opcode;           // The currently executed opcode
        public ushort programCounter;   // Program counter (from 0x000 to 0xFFF)
        public ushort indexRegister;    // Index register (from 0x000 to 0xFFF)

        public byte[] registers;        // Registers from V0 to VF (VF used by carry)

        public ushort[] stack;          // The stack (16 levels)
        public byte stackPointer;       // Stack pointer

        public bool[] keypadState;      // The keypad state (from 0x0 to 0xF)

        public byte delayTimer;         // Generic timer
        public byte soundTimer;         // As long as this timer is above 0, a beep is played

        public bool[] graphicsMemory;   // Monochrome screen (64x32, 64x64 in hi-res mode)

        public bool superChipMode;      // Some instructions behave differently in some implementations of CHIP-8
        public bool hiResMode;          // Use a 64x64 screen instead of the usual 64x32

        public bool drawFlag;           // Set whenever to update the screen

        public Random random;
    }
}
