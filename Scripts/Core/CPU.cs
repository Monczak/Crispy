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

        public bool[] graphicsMemory;   // Monochrome screen (64x32, 64x64 in hi-res mode)

        public bool superChipMode;      // Some instructions behave differently in some implementations of CHIP-8
        public bool hiResMode;          // Use a 64x64 screen instead of the usual 64x32

        public bool drawFlag;           // Set whenever to update the screen

        private Random random;

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

        public void Reset()
        {
            registers = new byte[16];
            stack = new ushort[16];         // The stack only stores pointers to subroutines, allows for 16 levels of nesting
            memory = new byte[memorySize];

            keypadState = new bool[16] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };

            programCounter = 0x200;     // CHIP-8 programs start at 0x200 in memory
            opcode = 0;                 // Set current opcode to nothing
            indexRegister = 0;          // Reset index register
            stackPointer = 0;           // Reset stack pointer to top of stack

            graphicsMemory = new bool[64 * (hiResMode ? 64 : 32)]; // The screen is 64x32 pixels (64x64 if in hi-res mode)

            soundTimer = delayTimer = 0;

            random = new Random();

            // Load fontset
            for (int i = 80; i < 160; i++)
                memory[i] = fontSet[i - 80];
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
        }

        private void FetchOpcode()
        {
            opcode = (ushort)(memory[programCounter] << 8 | memory[programCounter + 1]);

            programCounter += 2;
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
                            Opcode_SubtractCarryInverted((byte)((opcode & 0x0F00) >> 8), (byte)((opcode & 0x00F0) >> 4));
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

        public void UpdateTimers()
        {
            if (delayTimer > 0) delayTimer--;
            if (soundTimer > 0) soundTimer--;
        }

        public void ApplyState(CPUState state)
        {
            state.memory.CopyTo(memory, 0);
            state.registers.CopyTo(registers, 0);
            state.graphicsMemory.CopyTo(graphicsMemory, 0);
            state.stack.CopyTo(stack, 0);
            state.keypadState.CopyTo(keypadState, 0);

            opcode = state.opcode;
            programCounter = state.programCounter;
            indexRegister = state.indexRegister;
            stackPointer = state.stackPointer;
            delayTimer = state.delayTimer;
            soundTimer = state.soundTimer;
            superChipMode = state.superChipMode;
            hiResMode = state.hiResMode;
            drawFlag = state.drawFlag;
            random = state.random;
        }

        public CPUState GetState()
        {
            CPUState state = new CPUState()
            {
                memory = new byte[memorySize],
                registers = new byte[16],
                graphicsMemory = new bool[64 * (hiResMode ? 64 : 32)],
                stack = new ushort[16],
                keypadState = new bool[16],

                opcode = opcode,
                programCounter = programCounter,
                indexRegister = indexRegister,
                stackPointer = stackPointer,
                delayTimer = delayTimer,
                soundTimer = soundTimer,
                superChipMode = superChipMode,
                hiResMode = hiResMode,
                drawFlag = drawFlag,
                random = random
            };

            memory.CopyTo(state.memory, 0);
            registers.CopyTo(state.registers, 0);
            graphicsMemory.CopyTo(state.graphicsMemory, 0);
            stack.CopyTo(state.stack, 0);
            keypadState.CopyTo(state.keypadState, 0);

            return state;
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
            for (int i = 0; i < graphicsMemory.Length; i++)
                graphicsMemory[i] = false;
        }

        // Return from a subroutine (0x00EE)
        private void Opcode_ReturnFromSubroutine()
        {
            stackPointer--;
            programCounter = stack[stackPointer];
        }

        // Jump to address (0x1NNN)
        private void Opcode_JumpToAddress(ushort address)
        {
            programCounter = address;
        }

        // Call subroutine at address (0x2NNN)
        private void Opcode_CallSubroutine(ushort address)
        {
            stack[stackPointer] = programCounter;
            stackPointer++;
            programCounter = address;
        }

        // Skip next instruction if VX = NN (0x3XNN)
        private void Opcode_SkipNextInstructionEqualToValue(byte register, byte value)
        {
            if (registers[register] == value)
                programCounter += 2;
        }

        // Skip next instruction if VX != NN (0x4XNN)
        private void Opcode_SkipNextInstructionNotEqualToValue(byte register, byte value)
        {
            if (registers[register] != value)
                programCounter += 2;
        }

        // Skip next instruction if VX == VY (0x5XY0)
        private void Opcode_SkipNextInstructionXEqualToY(byte registerX, byte registerY)
        {
            if (registers[registerX] == registers[registerY])
                programCounter += 2;
        }

        // Set VX to NN (0x6XNN)
        private void Opcode_Set(byte register, byte value)
        {
            registers[register] = value;
        }

        // Add NN to VX (0x7XNN)
        private void Opcode_Add(byte register, byte value)
        {
            registers[register] += value;
        }

        // Set VX to VY (0x8XY0)
        private void Opcode_SetXY(byte registerX, byte registerY)
        {
            registers[registerX] = registers[registerY];
        }

        // Set VX to VX OR VY (0x8XY1)
        private void Opcode_SetXORY(byte registerX, byte registerY)
        {
            registers[registerX] |= registers[registerY];
        }

        // Set VX to VX AND VY (0x8XY2)
        private void Opcode_SetXANDY(byte registerX, byte registerY)
        {
            registers[registerX] &= registers[registerY];
        }

        // Set VX to VX XOR VY (0x8XY3)
        private void Opcode_SetXXORY(byte registerX, byte registerY)
        {
            registers[registerX] ^= registers[registerY];
        }

        // Add VY to VX with carry (0x8XY4)
        private void Opcode_AddCarry(byte registerX, byte registerY)
        {
            ushort result = unchecked((ushort)(registers[registerX] + registers[registerY]));

            if (result > 0xFF)
                registers[0xF] = 1;
            else
                registers[0xF] = 0;

            registers[registerX] = (byte)(result & 0xFF);
        }

        // Subtract VY from VX with carry (0x8XY5)
        private void Opcode_SubtractCarry(byte registerX, byte registerY)
        {
            if (registers[registerX] > registers[registerY])
                registers[0xF] = 1;
            else
                registers[0xF] = 0;

            registers[registerX] = unchecked((byte)(registers[registerX] - registers[registerY]));
        }

        // Right shift VX by 1 (0x8XY6)
        // Probably behaves differently with SuperChip mode
        private void Opcode_RightShift(byte registerX)
        {
            registers[0xF] = (byte)(registers[registerX] & 0x1);
            registers[registerX] >>= 1;
        }

        // Subtract VX from VY and store result in VX (0x8XY7)
        private void Opcode_SubtractCarryInverted(byte registerX, byte registerY)
        {
            if (registers[registerX] > registers[registerY])
                registers[0xF] = 1;
            else
                registers[0xF] = 0;

            registers[registerX] = unchecked((byte)(registers[registerY] - registers[registerX]));
        }

        // Left shift VX by 1 (0x8XYE)
        private void Opcode_LeftShift(byte registerX)
        {
            registers[0xF] = (byte)((registers[registerX] & 0x80) >> 7);
            registers[registerX] <<= 1;
        }

        // Skip next instruction if VX != VY (0x9XY0)
        private void Opcode_SkipNextInstructionXNotEqualY(byte registerX, byte registerY)
        {
            if (registers[registerX] != registers[registerY])
                programCounter += 2;
        }

        // Set index register to address (0xANNN)
        private void Opcode_SetIndexRegister(ushort address)
        {
            indexRegister = address;
        }

        // Jump to address + V0 (0xBNNN)
        private void Opcode_JumpToAddressV0(ushort address)
        {
            programCounter = (ushort)(address + registers[0x0]);
        }

        // Set VX to random byte AND NN (0xCXNN)
        private void Opcode_SetRandom(byte register, byte value)
        {
            registers[register] = (byte)((byte)random.Next(0x00, 0xFF) & value);
        }

        // Draw sprite at (VX, VY) (width 8, height N) (0xDXYN)
        private void Opcode_Draw(byte registerX, byte registerY, byte height)
        {
            byte xPos = (byte)(registers[registerX] % 64);
            byte yPos = (byte)(registers[registerY] % (hiResMode ? 64 : 32));

            registers[0xF] = 0;

            for (byte row = 0; row < height; row++)
            {
                byte pixel = memory[indexRegister + row];

                for (byte col = 0; col < 8; col++)
                {
                    if ((byte)(pixel & (0x80 >> col)) != 0)
                    {
                        if (graphicsMemory[(yPos + row) * 64 + xPos + col])
                            registers[0xF] = 1;

                        graphicsMemory[(yPos + row) * 64 + xPos + col] ^= true;
                    }
                }
            }

            drawFlag = true;
        }

        // Skip next instruction if key with value of VX is pressed (0xEX9E)
        private void Opcode_SkipNextInstructionKeyPressed(byte register)
        {
            byte key = registers[register];

            if (keypadState[key])
                programCounter += 2;
        }

        // Skip next instruction if key with value of VX is not pressed (0xEXA1)
        private void Opcode_SkipNextInstructionKeyNotPressed(byte register)
        {
            byte key = registers[register];

            if (!keypadState[key])
                programCounter += 2;
        }

        // Set VX to delay timer (0xFX07)
        private void Opcode_SetXToDelayTimer(byte register)
        {
            registers[register] = delayTimer;
        }

        // Wait for a key press, then store the key value in VX (0xFX0A)
        private void Opcode_WaitForKeyPress(byte register)
        {
            if (keypadState[0])
                registers[register] = 0;
            else if (keypadState[1])
                registers[register] = 1;
            else if (keypadState[2])
                registers[register] = 2;
            else if (keypadState[3])
                registers[register] = 3;
            else if (keypadState[4])
                registers[register] = 4;
            else if (keypadState[5])
                registers[register] = 5;
            else if (keypadState[6])
                registers[register] = 6;
            else if (keypadState[7])
                registers[register] = 7;
            else if (keypadState[8])
                registers[register] = 8;
            else if (keypadState[9])
                registers[register] = 9;
            else if (keypadState[10])
                registers[register] = 10;
            else if (keypadState[11])
                registers[register] = 11;
            else if (keypadState[12])
                registers[register] = 12;
            else if (keypadState[13])
                registers[register] = 13;
            else if (keypadState[14])
                registers[register] = 14;
            else if (keypadState[15])
                registers[register] = 15;
            else
                programCounter -= 2;
        }

        // Set delay timer to VX (0xFX15)
        private void Opcode_SetDelayTimer(byte register)
        {
            delayTimer = registers[register];
        }

        // Set delay timer to VX (0xFX18)
        private void Opcode_SetSoundTimer(byte register)
        {
            soundTimer = registers[register];
        }

        // Add VX to index register (0xFX1E)
        private void Opcode_AddXToIndexRegister(byte register)
        {
            indexRegister += registers[register];
        }

        // Set index register to location of sprite for digit VX (0xFX29)
        private void Opcode_SetIndexRegisterSprite(byte register)
        {
            byte digit = registers[register];
            indexRegister = unchecked((ushort)(0x50 + (5 * digit)));
        }
        
        // Store BCD representation of VX in memory locations I, I + 1 and I + 2 (0xFX33)
        private void Opcode_StoreBCD(byte register)
        {
            byte value = registers[register];

            memory[indexRegister + 2] = unchecked((byte)(value % 10));
            value /= 10;

            memory[indexRegister + 1] = unchecked((byte)(value % 10));
            value /= 10;

            memory[indexRegister] = unchecked((byte)(value % 10));
        }

        // Store registers V0 to VX in memory, starting at location I (0xFX55)
        private void Opcode_StoreRegisters(byte register)
        {
            for (byte i = 0; i <= register; i++)
            {
                memory[indexRegister + i] = registers[i];
            }
        }

        // Read registers V0 through VX from memory, starting at location I (0xFX65)
        private void Opcode_ReadRegisters(byte register)
        {
            for (byte i = 0; i <= register; i++)
            {
                registers[i] = memory[indexRegister + i];
            }
        }
    }
}
