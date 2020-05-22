﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using Crispy.Scripts.Core;
using Crispy.Scripts.GUI;

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

        List<Message> messages;

        private SpriteFont messageFont;
        private int messageHeight = 20;

        public CrispyEmu()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            messages = new List<Message>();

            _graphics.PreferredBackBufferWidth = 640;
            _graphics.PreferredBackBufferHeight = 320;

            _graphics.ApplyChanges();

            offColor = new Color(186, 194, 172);
            onColor = new Color(65, 66, 52);

            InputHandler.SetBindings(new Dictionary<int, Keys>
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

            byte[] program = File.ReadAllBytes("Tetris [Fran Dachille, 1991].ch8");
            cpu.LoadProgram(program);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            messageFont = Content.Load<SpriteFont>("MessageFont");
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
            HandleMessages(gameTime);

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

            RenderCHIP8();
            RenderMessages();

            _spriteBatch.End();

            cpu.drawFlag = false;

            base.Draw(gameTime);
        }

        private void RenderCHIP8()
        {
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    _spriteBatch.Draw(pixel, new Vector2(x * 10, y * 10), cpu.graphicsMemory[y * 64 + x] ? onColor : offColor);
                }
            }
        }

        private void RenderMessages()
        {
            int messageIndex = 0;
            for (int y = _graphics.PreferredBackBufferHeight; y > 0 && messageIndex < messages.Count; y -= messageHeight, messageIndex++)
            {
                _spriteBatch.DrawString(messageFont, messages[messages.Count - messageIndex - 1].text, new Vector2(10, y - messageHeight), Color.White);
            }
        }


        private void HandleSavestates()
        {
            if (Keyboard.GetState().IsKeyDown(Keys.T) && !InputHandler.heldSavestateKey)
            {
                saveState = cpu.GetState();
                InputHandler.heldSavestateKey = true;
                ShowMessage("Saved state", 2.5f);
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.T))
            {
                InputHandler.heldSavestateKey = false;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Y) && !InputHandler.heldLoadstateKey)
            {
                cpu.ApplyState(saveState);
                InputHandler.heldLoadstateKey = true;
                ShowMessage("Loaded state", 2.5f);
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.Y))
            {
                InputHandler.heldLoadstateKey = false;
            }
        }

        private void HandleMessages(GameTime gameTime)
        {
            for (int i = 0; i < messages.Count; i++)
            {
                messages[i].showTime -= (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (messages[i].showTime < 0)
                    messages.RemoveAt(i--);
            }
        }

        private void ShowMessage(string message, float showTime, Color color)
        {
            messages.Add(new Message()
            {
                text = message,
                showTime = showTime,
                color = color
            });
        }

        private void ShowMessage(string message, float showTime)
        {
            messages.Add(new Message()
            {
                text = message,
                showTime = showTime,
                color = Color.White
            });
        }
    }
}
