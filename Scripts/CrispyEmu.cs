﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System;
using System.Threading;
using System.IO;
using System.Diagnostics;

using Crispy.Scripts.Core;

namespace Crispy
{
    public class CrispyEmu : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private CPU cpu;
        private APU apu;

        private Texture2D pixel;

        private Color onColor, offColor;

        private uint cyclesPerSecond = 500;
        private uint timerUpdatesPerSecond = 60;

        private double timeSinceLastTimerUpdate;

        CPUState saveState;

        public CrispyEmu()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 640;
            _graphics.PreferredBackBufferHeight = 320;

            _graphics.ApplyChanges();

            offColor = new Color(186, 194, 172);
            onColor = new Color(65, 66, 52);

            InputHandler.SetBindings(new System.Collections.Generic.Dictionary<int, Keys>
            {
                { 0, Keys.X },
                { 1, Keys.D1 },
                { 2, Keys.D2 },
                { 3, Keys.D3 },
                { 4, Keys.Q },
                { 5, Keys.W },
                { 6, Keys.E },
                { 7, Keys.A },
                { 8, Keys.S },
                { 9, Keys.D },
                { 10, Keys.Z },
                { 11, Keys.C },
                { 12, Keys.D4 },
                { 13, Keys.R },
                { 14, Keys.F },
                { 15, Keys.V }
            });

            pixel = new Texture2D(_graphics.GraphicsDevice, 10, 10);
            Color[] data = new Color[10 * 10];
            for (int i = 0; i < data.Length; i++) data[i] = Color.White;
            pixel.SetData(data);

            TargetElapsedTime = TimeSpan.FromSeconds(1f / cyclesPerSecond);
            IsFixedTimeStep = true;

            cpu = new CPU();
            cpu.hiResMode = true;
            cpu.Initialize();

            apu = new APU();
            apu.Initialize();

            byte[] program = File.ReadAllBytes("Space Invaders [David Winter].ch8");
            cpu.LoadProgram(program);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            cpu.keypadState = InputHandler.GetKeyboardState(Keyboard.GetState());

            cpu.Cycle();

            HandleTimers(gameTime);
            HandleAudio();
            HandleSavestates();

            base.Update(gameTime);
        }

        private void HandleTimers(GameTime gameTime)
        {
            if (timeSinceLastTimerUpdate > 1.0 / timerUpdatesPerSecond)
            {
                timeSinceLastTimerUpdate = 0;
                cpu.UpdateTimers();
            }
            else timeSinceLastTimerUpdate += gameTime.ElapsedGameTime.TotalSeconds;
        }

        private void HandleAudio()
        {
            if (cpu.soundTimer > 0) apu.StartTone();
            else apu.StopTone();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(offColor);

            _spriteBatch.Begin();
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    _spriteBatch.Draw(pixel, new Vector2(x * 10, y * 10), cpu.graphicsMemory[y * 64 + x] ? onColor : offColor);
                }
            }
            _spriteBatch.End();

            cpu.drawFlag = false;

            base.Draw(gameTime);
        }


        private void HandleSavestates()
        {
            if (Keyboard.GetState().IsKeyDown(Keys.T) && !InputHandler.heldSavestateKey)
            {
                saveState = cpu.GetState();
                InputHandler.heldSavestateKey = true;
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.T))
            {
                InputHandler.heldSavestateKey = false;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Y) && !InputHandler.heldLoadstateKey)
            {
                cpu.ApplyState(saveState);
                InputHandler.heldLoadstateKey = true;
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.Y))
            {
                InputHandler.heldLoadstateKey = false;
            }
        }
    }
}
