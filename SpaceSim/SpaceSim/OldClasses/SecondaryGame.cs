using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceSim
{
    public class SecondaryGame : Game
    {
        public CustomGraphicsDeviceManager graphics;
        public bool Initialized = false;

        private SpriteBatch spriteBatch;

        private Texture2D renderTexture = null;

        private Color[] renderColors = null;
        private int renderWidth, renderHeight;

        public SecondaryGame(GameDisplayType gameDisplayType)
        {
            graphics = new CustomGraphicsDeviceManager(this, gameDisplayType);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Initialize()
        {
            base.Initialize();
            Initialized = true;
        }

        public void RenderTexture(Color [] colors, int screenWidth, int screenHeight)
        {
            renderColors = colors;
            renderWidth = screenWidth;
            renderHeight = screenHeight;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            if (renderTexture == null && renderColors != null) renderTexture = new Texture2D(GraphicsDevice, renderWidth, renderHeight);

            if (renderTexture != null)
            {
                GraphicsDevice.Textures[0] = null;
                renderTexture.SetData(renderColors);
                spriteBatch.Begin();
                spriteBatch.Draw(renderTexture, new Vector2(0, 0), null, Color.White, 0, new Vector2(0, 0), 0.4f, SpriteEffects.None, 1);
                spriteBatch.End();
            }
            base.Draw(gameTime);
        }
    }
}
