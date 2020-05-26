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

using XNAssets.Utility;

using Color = Microsoft.Xna.Framework.Color;

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
        private uint rewindFrequency = 60;

        private int rewindBufferSize = 600;

        private double timeSinceLastTimerUpdate;
        private double timeSinceLastRewindCycle;

        private List<Message> messages;

        private SpriteFont messageFont;
        private int messageHeight = 20;

        private bool isPaused;
        private bool isRunning;
        private bool isRewinding;
        private bool frameAdvance = false;

        private string currentRomPath;
        private string currentRomName;
        private byte[] currentRom;

        private FileDialog fileDialog;
        private Window helpMenu;

        private bool fileDialogVisible = false;
        private bool helpMenuVisible = false;

        private int savestateSlots = 6;
        private string readme;

        private bool renderOnlyCHIP8 = false;

        private readonly Keys
            helpKey = Keys.F1,
            screenshotKey = Keys.F2,
            loadRomKey = Keys.F3,
            resetKey = Keys.F4,
            previousSaveStateSlotKey = Keys.F5,
            nextSaveStateSlotKey = Keys.F6,
            loadStateKey = Keys.F7,
            saveStateKey = Keys.F8,
            frameAdvanceKey = Keys.F9,
            rewindKey = Keys.OemTilde,
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

            SavestateManager.Initialize(savestateSlots);

            RewindManager.Initialize(rewindBufferSize);

            ShowMessage("Welcome to Crispy! Press F3 to load a game, or press F1 for help.", 9999);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            messageFont = Content.Load<SpriteFont>("MessageFont");
            readme = typeof(CrispyEmu).Assembly.ReadResourceAsString("Readme.txt");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            cpu.keypadState = InputHandler.GetKeyboardState(Keyboard.GetState());

            if (isRunning)
            {
                if (!isPaused && !isRewinding)
                {
                    cpu.Cycle();
                    if (frameAdvance && cpu.drawFlag) isPaused = true;
                    HandleTimers(gameTime);
                }

                HandleAudio();
                HandleSavestates();
                HandlePause();
                HandleFrameAdvance();
                HandleRewind(gameTime);
            }

            HandleFunctionKeys();
            HandleMessages(gameTime);

            SetTitle($"{currentRomName}{(frameAdvance ? $" (frame advance{(!isPaused ? ", running" : "")}) " : "")}{(cpu.infiniteLoopFlag ? " (stopped running)" : "")}");

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(offColor);

            _spriteBatch.Begin();

            RenderCHIP8();
            if (!renderOnlyCHIP8) RenderMessages();

            _spriteBatch.End();

            if (!renderOnlyCHIP8) Desktop.Render();

            cpu.drawFlag = false;

            base.Draw(gameTime);
        }

        private void HandleRewind(GameTime gameTime)
        {
            if (timeSinceLastRewindCycle > 1.0 / rewindFrequency)
            {
                timeSinceLastRewindCycle = 0;
                if (Keyboard.GetState().IsKeyDown(rewindKey))
                {
                    isRewinding = true;
                    cpu.ApplyState(RewindManager.Rewind());
                }
                else
                {
                    isRewinding = false;
                    if (!frameAdvance && !isPaused)
                    {
                        RewindManager.Record(cpu.GetState());
                    }
                }
            }
            else timeSinceLastRewindCycle += gameTime.ElapsedGameTime.TotalSeconds; 
        }

        private void HandleFrameAdvance()
        {
            InputHandler.HandleKeypress(frameAdvanceKey, () =>
            {
                if (!frameAdvance) ShowMessage("Frame advance on", 2.5f);

                frameAdvance = true;
                isPaused = false;
            });
        }

        private void HandleFunctionKeys()
        {
            InputHandler.HandleKeypress(loadRomKey, () =>
            {
                if (!fileDialogVisible && !helpMenuVisible)
                {
                    fileDialog = new FileDialog(FileDialogMode.OpenFile)
                    {
                        Filter = "*.ch8"
                    };

                    fileDialog.Closed += (s, a) =>
                    {
                        isPaused = false;
                        fileDialogVisible = false;
                        if (!fileDialog.Result) return;

                        currentRomPath = fileDialog.FilePath;

                        cpu.Reset();

                        currentRom = File.ReadAllBytes(currentRomPath);
                        currentRomName = Path.GetFileNameWithoutExtension(currentRomPath);
                        SavestateManager.romName = currentRomName;
                        cpu.LoadProgram(currentRom);

                        isRunning = true;
                        RemoveAllMessages();
                    };
                    isPaused = true;

                    fileDialog.ShowModal();
                    fileDialogVisible = true;
                }
                else if (fileDialogVisible)
                {
                    fileDialog.Close();
                    fileDialogVisible = false;
                }
                
            });

            InputHandler.HandleKeypress(resetKey, () => 
            {
                if (isRunning)
                {
                    cpu.Reset();
                    cpu.LoadProgram(currentRom);
                    ShowMessage("Reset", 2.5f);
                }
            });

            InputHandler.HandleKeypress(nextSaveStateSlotKey, () =>
            {
                if (isRunning)
                {
                    SavestateManager.SelectNextSlot();
                    ShowMessage($"Selected slot {SavestateManager.selectedSlot} {(SavestateManager.IsSelectedSlotEmpty() ? "(empty)" : "")}", 2.5f);
                }
            });
            InputHandler.HandleKeypress(previousSaveStateSlotKey, () =>
            {
                if (isRunning)
                {
                    SavestateManager.SelectPreviousSlot();
                    ShowMessage($"Selected slot {SavestateManager.selectedSlot} {(SavestateManager.IsSelectedSlotEmpty() ? "(empty)" : "")}", 2.5f);
                }
            });

            InputHandler.HandleKeypress(helpKey, () =>
            {
                if (!helpMenuVisible && !fileDialogVisible)
                {
                    helpMenu = new Window
                    {
                        Title = "Welcome to Crispy!"
                    };
                    Label text = new Label
                    {
                        Text = readme,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Wrap = true
                    };
                    ScrollViewer scrollViewer = new ScrollViewer
                    {
                        Content = text
                    };

                    helpMenu.Content = scrollViewer;

                    helpMenu.ShowModal();
                    helpMenuVisible = true;
                }
                else if (helpMenuVisible)
                {
                    helpMenu.Close();
                    helpMenuVisible = false;
                }
                
            });

            InputHandler.HandleKeypress(screenshotKey, () =>
            {
                string screenshotPath = $"Screenshots/{DateTime.Now.ToString().Replace("/", "-").Replace(":", "-")}.jpg";

                if (!File.Exists(screenshotPath))
                {
                    try
                    {
                        File.Create(screenshotPath).Dispose();
                    }
                    catch (DirectoryNotFoundException)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath));
                    }
                }

                using (Texture2D screenshot = TakeScreenshot())
                    SaveScreenshot(screenshot, File.OpenWrite(screenshotPath));

                ShowMessage($"Saved screenshot as {Path.GetFileName(screenshotPath)}", 2.5f);
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
                if (isRunning)
                {
                    SavestateManager.Savestate(SavestateManager.selectedSlot, cpu.GetState());
                    ShowMessage($"Saved state to slot {SavestateManager.selectedSlot}", 2.5f);
                }
            });
            InputHandler.HandleKeypress(loadStateKey, () =>
            {
                if (isRunning)
                {
                    try
                    {
                        SavestateManager.LoadSavestate(SavestateManager.selectedSlot);
                        cpu.ApplyState(SavestateManager.GetSelectedState());
                        ShowMessage($"Loaded state from slot {SavestateManager.selectedSlot}", 2.5f);
                    }
                    catch (SlotIsEmptyException)
                    {
                        ShowMessage($"Slot {SavestateManager.selectedSlot} is empty", 2.5f);
                    }
                }
            });
        }

        private void HandlePause()
        {
            InputHandler.HandleKeypress(pauseKey, () =>
            {
                if (frameAdvance)
                {
                    frameAdvance = false;
                    isPaused = false;
                    ShowMessage("Frame advance off", 2.5f);
                }
                else
                {
                    TogglePause();
                    ShowMessage(isPaused ? "Paused emulation" : "Unpaused emulation", 2.5f);
                }
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

        private void RemoveAllMessages()
        {
            messages.Clear();
        }

        private Texture2D TakeScreenshot()
        {
            int width = _graphics.PreferredBackBufferWidth, height = _graphics.PreferredBackBufferHeight;

            RenderTarget2D screenshot = new RenderTarget2D(GraphicsDevice, width, height);
            GraphicsDevice.SetRenderTarget(screenshot);

            renderOnlyCHIP8 = true;
            Draw(new GameTime());
            renderOnlyCHIP8 = false;

            GraphicsDevice.SetRenderTarget(null);

            return screenshot;
        }

        private void SaveScreenshot(Texture2D screenshot, Stream stream)
        {
            screenshot.SaveAsJpeg(stream, screenshot.Width, screenshot.Height);
            stream.Close();
        }

        private void SetTitle(string title)
        {
            if (title == null || title == "")
                Window.Title = "Crispy";
            else
                Window.Title = $"Crispy - {title}";
        }
    }
}
