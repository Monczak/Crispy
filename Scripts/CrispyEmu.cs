using Microsoft.Xna.Framework;
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

using Myra;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;

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

        private CPUState saveState;

        private List<Message> messages;

        private SpriteFont messageFont;
        private int messageHeight = 20;

        private bool isPaused;
        private bool isRunning;

        private string currentRomPath;

        private readonly Keys
            helpKey = Keys.F1,
            screenshotKey = Keys.F2,
            loadRomKey = Keys.F3,
            previousSaveStateSlotKey = Keys.F5,
            nextSaveStateSlotKey = Keys.F6,
            loadStateKey = Keys.F7,
            saveStateKey = Keys.F8,
            pauseKey = Keys.Space;

        public CrispyEmu()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            MyraEnvironment.Game = this;

            messages = new List<Message>();

            _graphics.PreferredBackBufferWidth = 640;
            _graphics.PreferredBackBufferHeight = 320;

            _graphics.ApplyChanges();

            offColor = new Color(186, 194, 172);
            onColor = new Color(65, 66, 52);

            isPaused = false;
            isRunning = false;

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
            cpu.Reset();

            apu = new APU();
            apu.Initialize();

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

            if (isRunning)
            {
                if (!isPaused)
                {
                    cpu.Cycle();
                    HandleTimers(gameTime);
                }
            }

            HandleAudio();
            HandleSavestates();
            HandleMessages(gameTime);
            HandlePause();

            HandleFunctionKeys();

            base.Update(gameTime);
        }

        private void HandleFunctionKeys()
        {
            InputHandler.HandleKeypress(loadRomKey, () =>
            {
                FileDialog dialog = new FileDialog(FileDialogMode.OpenFile)
                {
                    Filter = "*.ch8"
                };

                dialog.Closed += (s, a) =>
                {
                    isPaused = false;
                    if (!dialog.Result) return;

                    currentRomPath = dialog.FilePath;

                    cpu.Reset();

                    byte[] program = File.ReadAllBytes(currentRomPath);
                    cpu.LoadProgram(program);

                    isRunning = true;
                };
                isPaused = true;

                dialog.ShowModal();
            });
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

            Desktop.Render();

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
            InputHandler.HandleKeypress(saveStateKey, () => 
            {
                saveState = cpu.GetState();
                ShowMessage("Saved state", 2.5f);
            });

            InputHandler.HandleKeypress(loadStateKey, () =>
            {
                cpu.ApplyState(saveState);
                ShowMessage("Loaded state", 2.5f);
            });
        }

        private void HandlePause()
        {
            InputHandler.HandleKeypress(pauseKey, () =>
            {
                TogglePause();
                ShowMessage(isPaused ? "Paused emulation" : "Unpaused emulation", 2.5f);
            });
        }

        private void TogglePause()
        {
            isPaused ^= true;
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
