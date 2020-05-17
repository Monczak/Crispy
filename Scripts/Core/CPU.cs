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
                case 0x0000:
                    if ((opcode & 0x00F0) == 0x00E0)
                    {
                        switch (opcode & 0x000F)
                        {
                            case 0x0000:    // 0x00E0
                                Opcode_ClearScreen();
                                break;
                            case 0x000E:    // 0x00EE
                                Opcode_ReturnFromSubroutine();
                                break;
                            default:
                                ThrowUnknownOpcodeException();
                                break;
                        }
                    }
                    else
                    {
                        Opcode_Call((ushort)(opcode & 0x0FFF));
                    }
                    break;

                case 0x1000:
                    Opcode_JumpToAddress((ushort)(opcode & 0x0FFF));
                    break;

                case 0x2000:
                    Opcode_CallSubroutine((ushort)(opcode & 0x0FFF));
                    break;

                case 0x3000:
                    Opcode_SkipNextInstructionEqualToValue((byte)((opcode & 0x0F00) >> 8), (byte)(opcode & 0x00FF));
                    break;

                case 0x4000:
                    Opcode_SkipNextInstructionNotEqualToValue((byte)((opcode & 0x0F00) >> 8), (byte)(opcode & 0x00FF));
                    break;

                case 0x5000:
                    Opcode_SkipNextInstructionXEqualToY((byte)((opcode & 0x0F00) >> 8), (byte)((opcode & 0x00F0) >> 4));
                    break;

                case 0x6000:
                    Opcode_Set((byte)((opcode & 0x0F00) >> 8), (byte)(opcode & 0x00FF));
                    break;

                case 0x7000:
                    Opcode_Add((byte)((opcode & 0x0F00) >> 8), (byte)(opcode & 0x00FF));
                    break;

                case 0x8000:
                    switch (opcode & 0x000F)
                    {
                        case 0x0000:    // 8XY0
                            Opcode_SetXY((byte)((opcode & 0x0F00) >> 8), (byte)((opcode & 0x00F0) >> 4));
                            break;
                        case 0x0001:    // 8XY1
                            Opcode_SetXORY((byte)((opcode & 0x0F00) >> 8), (byte)((opcode & 0x00F0) >> 4));
                            break;
                        case 0x0002:    // 8XY2
                            Opcode_SetXANDY((byte)((opcode & 0x0F00) >> 8), (byte)((opcode & 0x00F0) >> 4));
                            break;
                        case 0x0003:    // 8XY3
                            Opcode_SetXXORY((byte)((opcode & 0x0F00) >> 8), (byte)((opcode & 0x00F0) >> 4));
                            break;
                        case 0x0004:    // 8XY4
                            Opcode_AddCarry((byte)((opcode & 0x0F00) >> 8), (byte)((opcode & 0x00F0) >> 4));
                            break;
                        case 0x0005:    // 8XY5
                            Opcode_SubtractCarry((byte)((opcode & 0x0F00) >> 8), (byte)((opcode & 0x00F0) >> 4));
                            break;
                        case 0x0006:    // 8XY6
                            Opcode_RightShift((byte)((opcode & 0x0F00) >> 8));
                            break;
                        case 0x0007:    // 8XY7
                            Opcode_SetXY((byte)((opcode & 0x0F00) >> 8), (byte)((opcode & 0x00F0) >> 4));
                            break;
                        case 0x000E:    // 8XYE
                            Opcode_LeftShift((byte)((opcode & 0x0F00) >> 8));
                            break;
                        default:
                            ThrowUnknownOpcodeException();
                            break;
                    }
                    break;

                case 0x9000:
                    if ((opcode & 0x000F) != 0x0000)
                    {
                        ThrowUnknownOpcodeException();
                        break;
                    }

                    Opcode_SkipNextInstructionXNotEqualY((byte)((opcode & 0x0F00) >> 8), (byte)((opcode & 0x00F0) >> 4));
                    break;

                case 0xA000:
                    Opcode_SetIndexRegister((ushort)(opcode & 0x0FFF));
                    break;

                case 0xB000:
                    Opcode_JumpToAddressV0((ushort)(opcode & 0x0FFF));
                    break;

                case 0xC000:
                    Opcode_SetRandom((byte)((opcode & 0x0F00) >> 8), (byte)(opcode & 0x00FF));
                    break;

                case 0xD000:
                    Opcode_Draw((byte)((opcode & 0x0F00) >> 8), (byte)((opcode & 0x00F0) >> 4), (byte)(opcode & 0x000F));
                    break;

                case 0xE000:
                    switch (opcode & 0x00FF)
                    {
                        case 0x009E:    // EX9E
                            Opcode_SkipNextInstructionKeyPressed((byte)((opcode & 0x0F00) >> 8));
                            break;
                        case 0x00A1:    // EXA1
                            Opcode_SkipNextInstructionKeyNotPressed((byte)((opcode & 0x0F00) >> 8));
                            break;
                        default:
                            ThrowUnknownOpcodeException();
                            break;
                    }
                    break;

                case 0xF000:
                    switch (opcode & 0x00FF)
                    {
                        case 0x0007:    // FX07
                            Opcode_SetXToDelayTimer((byte)((opcode & 0x0F00) >> 8));
                            break;
                        case 0x000A:    // FX0A
                            Opcode_WaitForKeyPress((byte)((opcode & 0x0F00) >> 8));
                            break;
                        case 0x0015:    // FX15
                            Opcode_SetDelayTimer((byte)((opcode & 0x0F00) >> 8));
                            break;
                        case 0x0018:    // FX18
                            Opcode_SetSoundTimer((byte)((opcode & 0x0F00) >> 8));
                            break;
                        case 0x001E:    // FX1E
                            Opcode_AddXToIndexRegister((byte)((opcode & 0x0F00) >> 8));
                            break;
                        case 0x0029:    // FX29
                            Opcode_SetIndexRegisterSprite((byte)((opcode & 0x0F00) >> 8));
                            break;
                        case 0x0033:    // FX33
                            Opcode_StoreBCD((byte)((opcode & 0x0F00) >> 8));
                            break;
                        case 0x0055:    // FX55
                            Opcode_StoreRegisters((byte)((opcode & 0x0F00) >> 8));
                            break;
                        case 0x0065:    // FX65
                            Opcode_ReadRegisters((byte)((opcode & 0x0F00) >> 8));
                            break;

                        default:
                            ThrowUnknownOpcodeException();
                            break;
                    }
                    break;

                default:
                    ThrowUnknownOpcodeException();
                    break;
            }
        }

        private void ThrowUnknownOpcodeException()
        {
            throw new Exception($"Unknown opcode 0x{opcode.ToString("X4")} at 0x{programCounter.ToString("X3")}");
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
        private void Opcode_SkipNextInstructionXEqualToY(byte registerX, byte registerY)
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

        // Left shift VX by 1 (0x8XYE)
        private void Opcode_LeftShift(byte registerX)
        {

        }

        // Skip next instruction if VX != VY (0x9XY0)
        private void Opcode_SkipNextInstructionXNotEqualY(byte registerX, byte registerY)
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

        // Set delay timer to VX (0xFX15)
        private void Opcode_SetDelayTimer(byte register)
        {

        }

        // Set delay timer to VX (0xFX18)
        private void Opcode_SetSoundTimer(byte register)
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
