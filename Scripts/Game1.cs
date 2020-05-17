using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System.IO;

using Crispy.Scripts.Core;

namespace Crispy
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private CPU cpu;

        private Texture2D pixel;

        public Game1()
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

            pixel = new Texture2D(_graphics.GraphicsDevice, 10, 10);
            Color[] data = new Color[10 * 10];
            for (int i = 0; i < data.Length; i++) data[i] = Color.White;
            pixel.SetData(data);

            cpu = new CPU();
            cpu.Initialize();

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

            cpu.Cycle();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    _spriteBatch.Draw(pixel, new Vector2(x * 10, y * 10), cpu.graphicsMemory[y * 64 + x] ? Color.White : Color.Black);
                }
            }
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
