using System;
using System.Collections.Generic;
using System.Text;

namespace Crispy.Scripts.Core
{
    public class CPU
    {
        // This emulates the CHIP-8 interpreter

        private readonly int memorySize = 4096;

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

        public bool[] graphicsMemory;

        private readonly byte[] fontSet = new byte[80]
        {
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        };

        public void Initialize()
        {
            registers = new byte[16];
            stack = new ushort[16];
            memory = new byte[memorySize];

            keypadState = new bool[16] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };

            programCounter = 0x200;     // CHIP-8 programs start at 0x200 in memory
            opcode = 0;                 // Set current opcode to nothing
            indexRegister = 0;          // Reset index register
            stackPointer = 0;           // Reset stack pointer to top of stack

            graphicsMemory = new bool[64 * 32]; // The screen is 64 x 32 pixels

            soundTimer = delayTimer = 0;

            // Load fontset
            for (int i = 0; i < 80; i++)
                memory[i] = fontSet[i];
        }

        public void LoadProgram(byte[] program)
        {
            if (program.Length > memorySize - 512)
                throw new Exception("Program too big to fit in memory");

            for (int i = 0; i < program.Length; i++)
                memory[i + 512] = program[i];
        }

        public void Cycle()
        {
            FetchOpcode();

            ExecuteOpcode();

            UpdateTimers();
        }

        private void FetchOpcode()
        {
            opcode = (ushort)(memory[programCounter] << 8 | memory[programCounter + 1]);
        }

        private void ExecuteOpcode()
        {
            switch (opcode & 0xF000)
            {

                default:
                    throw new Exception($"Unknown opcode 0x{opcode.ToString("X4")} at 0x{programCounter.ToString("X3")}");
            }
        }

        private void UpdateTimers()
        {
            if (delayTimer > 0) delayTimer--;
            if (soundTimer > 0) soundTimer--;
        }

        // --------
        // Opcodes
        // --------

        // Call program at address (0x0NNN)
        private void Opcode_Call(ushort address)
        {
            programCounter = address;
        }

        // Clear the screen (0x00E0)
        private void Opcode_ClearScreen()
        {

        }

        // Return from a subroutine (0x00EE)
        private void Opcode_ReturnFromSubroutine()
        {

        }

        // Jump to address (0x1NNN)
        private void Opcode_JumpToAddress(ushort address)
        {

        }

        // Call subroutine at address (0x2NNN)
        private void Opcode_CallSubroutine(ushort address)
        {

        }

        // Skip next instruction if VX = NN (0x3XNN)
        private void Opcode_SkipNextInstructionEqualToValue(byte register, byte value)
        {

        }

        // Skip next instruction if VX != NN (0x4XNN)
        private void Opcode_SkipNextInstructionNotEqualToValue(byte register, byte value)
        {

        }

        // Skip next instruction if VX == VY (0x5XY0)
        private void Opcode_SkipNextInstructionXEqualY(byte registerX, byte registerY)
        {

        }

        // Set VX to NN (0x6XNN)
        private void Opcode_Set(byte register, byte value)
        {

        }

        // Add NN to VX (0x7XNN)
        private void Opcode_Add(byte register, byte value)
        {

        }

        // Set VX to VY (0x8XY0)
        private void Opcode_SetXY(byte registerX, byte registerY)
        {

        }

        // Set VX to VX OR VY (0x8XY1)
        private void Opcode_SetXORY(byte registerX, byte registerY)
        {

        }

        // Set VX to VX AND VY (0x8XY2)
        private void Opcode_SetXANDY(byte registerX, byte registerY)
        {

        }

        // Set VX to VX XOR VY (0x8XY3)
        private void Opcode_SetXXORY(byte registerX, byte registerY)
        {

        }

        // Add VY to VX with carry (0x8XY4)
        private void Opcode_AddCarry(byte registerX, byte registerY)
        {

        }

        // Subtract VY from VX with carry (0x8XY5)
        private void Opcode_SubtractCarry(byte registerX, byte registerY)
        {

        }

        // Right shift VX by 1 (0x8XY6)
        private void Opcode_RightShift(byte registerX)
        {

        }

        // Subtract VX from VY and store result in VX (0x8XY7)
        private void Opcode_SubtractCarryInverted(byte registerX, byte registerY)
        {

        }

        // Left shift VX by 1 (0x8XY8)
        private void Opcode_LeftShift(byte registerX)
        {

        }

        // Skip next instruction if VX != VY (0x9XY0)
        private void SkipNextInstructionXNotEqualY(byte registerX, byte registerY)
        {

        }

        // Set index register to address (0xANNN)
        private void Opcode_SetIndexRegister(ushort address)
        {

        }

        // Jump to address + V0 (0xBNNN)
        private void Opcode_JumpToAddressV0(ushort address)
        {

        }

        // Set VX to random byte AND NN (0xCXNN)
        private void Opcode_SetRandom(byte register, byte value)
        {

        }

        // Draw sprite at (VX, VY) (width 8, height N) (0xDXYN)
        private void Opcode_Draw(byte registerX, byte registerY, byte height)
        {

        }

        // Skip next instruction if key with value of VX is pressed (0xEX9E)
        private void Opcode_SkipNextInstructionKeyPressed(byte register)
        {

        }

        // Skip next instruction if key with value of VX is not pressed (0xEXA1)
        private void Opcode_SkipNextInstructionKeyNotPressed(byte register)
        {

        }

        // Set VX to delay timer (0xFX07)
        private void Opcode_SetXToDelayTimer(byte register)
        {

        }

        // Wait for a key press, then store the key value in VX (0xFX0A)
        private void Opcode_WaitForKeyPress(byte register)
        {

        }

        // Set delay timer to VX (0xFX18)
        private void Opcode_SetDelayTimer(byte register)
        {

        }

        // Add VX to index register (0xFX1E)
        private void Opcode_AddXToIndexRegister(byte register)
        {

        }

        // Set index register to location of sprite for digit VX (0xFX29)
        private void Opcode_SetIndexRegisterSprite(byte register)
        {

        }
        
        // Store BCD representation of VX in memory locations I, I + 1 and I + 2 (0xFX33)
        private void Opcode_StoreBCD(byte register)
        {

        }

        // Store registers V0 to VX in memory, starting at location I (0xFX55)
        private void Opcode_StoreRegisters(byte register)
        {

        }

        // Read registers V0 through VX from memory, starting at location I (0xFX65)
        private void Opcode_ReadRegisters(byte register)
        {

        }
    }
}
