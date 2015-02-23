using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceSim
{
    public class Starfield
    {
        Game Game;
        Texture2D RenderTexture;
        SpriteBatch SpriteBatch;
        Color[] TextureData;
        int Width, Height;
        Random Random;

        class Star
        {
            public float x, y, z, px, py;
        }

        // constants and storage for objects that represent star positions
        int starCount = 500;
        float warpZ = 12f;
        float Z = 0.025f + (1f/25f * 2f);
        Star[] stars = null;

        public Starfield(Game game, SpriteBatch spriteBatch, int width, int height)
        {
            Game = game;
            SpriteBatch = spriteBatch;
            Width = width;
            Height = height;
            Random = new System.Random();

            InitializeStars();

            RenderTexture = new Texture2D(game.GraphicsDevice, width, height, false, SurfaceFormat.Color);
            TextureData = new Color[width * height];
            Clear();
            RenderTexture.SetData(TextureData);
        }

        private void InitializeStars()
        {
            stars = new Star[starCount];
            for (int i = 0; i < starCount; i++)
            {
                Star star = new Star();
                stars[i] = star;
                ResetStar(star);
            }
        }

        private void ResetStar(Star star)
        {
            star.x = ((float)Random.NextDouble() * Width - (Width * 0.5f)) * warpZ;
            star.y = ((float)Random.NextDouble() * Height - (Height * 0.5f)) * warpZ;
            star.z = warpZ;
            star.px = 0;
            star.py = 0;
        }

        private void Clear()
        {
            for (int i = 0; i < TextureData.Length; i++)
            {
                TextureData[i] = Color.Black;
                TextureData[i].A = BackgroundAlpha;
            }
        }

        private bool WarpEngaged = false;
        private DateTime WarpStartDate, WarpEndDate, WarpFullSpeedDate;
        private float MinWarp = 12, MaxWarp = 1;
        private byte BackgroundAlpha
        {
            get
            {
                return (byte)(255 - (((warpZ-1f) / 11f) * 255f));
            }
        }

        public void StartWarp(float duration)
        {
            if (!WarpEngaged)
            {
                WarpEngaged = true;
                WarpStartDate = DateTime.Now;
                WarpFullSpeedDate = WarpStartDate.AddSeconds(duration / 2.0);
                WarpEndDate = WarpStartDate.AddSeconds(duration);
                InitializeStars();
            }
        }

        public void Update(GameTime gameTime)
        {
            if (WarpEndDate <= DateTime.Now) WarpEngaged = false;
            if (!WarpEngaged) return;

            double maxWarpDuration = (WarpFullSpeedDate - WarpStartDate).TotalSeconds;
            double warpDuration = (WarpFullSpeedDate - DateTime.Now).TotalSeconds;
            if (warpDuration > 0)
            {
                warpZ = (float)Math.Max(1, (warpDuration / maxWarpDuration) * MinWarp);
            }
            else
            {
                warpDuration *= -1.0;
                warpZ = (float)Math.Max(1, (warpDuration / maxWarpDuration) * MinWarp);
            }
            
            //int x = 10;
            //int y = 10;
            //colorData[y * vp.Width + x] = 0xFFFFFFFF; 

            //var cx = (mousex - width / 2) + (width / 2), cy = (mousey - height / 2) + (height / 2);

            Clear();

            float cx = Width/2f, cy = Height/2f;
   
            // update all stars
            float sat = (float)Math.Floor(Z * 500f);       // Z range 0.01 -> 0.5
            if (sat > 100) sat = 100;
            for (int i = 0; i < stars.Length; i++)
            {
                Star star = stars[i];
                float xx = star.x / star.z;
                float yy = star.y / star.z;
                float e = (1f / star.z + 1f) * 2f;   // size i.e. z

                
              //if (n.px !== 0)
              //{
              //   // hsl colour from a sine wave
              //   G.strokeStyle = "hsl(" + ((cycle * i) % 360) + "," + sat + "%,80%)";
              //   G.lineWidth = e;
              //   G.beginPath();
              //   G.moveTo(xx + cx, yy + cy);
              //   G.lineTo(n.px + cx, n.py + cy);
              //   G.stroke();
              //}

                int xPos = (int)(xx + cx);
                int yPos = (int)(yy + cy);

                if (xPos >= 0 && xPos < Width && yPos >= 0 && yPos < Height)
                {
                    Color starColor = Color.White;
                    int starSize = 5 - (int)((star.z / warpZ) * 4f);
                    byte starShade = (byte)((200f - ((star.z / warpZ) * 200f))+55f);
                    //starColor.R = starColor.G = starColor.B = starShade;
                    starColor.A = BackgroundAlpha;// starShade;
                    DrawPixel(xPos, yPos, starColor, starSize);
                    //TextureData[yPos * Width + xPos] = starColor;
                }
      
                // update star position values with new settings
                star.px = xx;
                star.py = yy;
                star.z -= Z;

                // reset when star is out of the view field
                if (star.z < Z || star.px > Width || star.py > Height)
                {
                    ResetStar(star);
                }
            }

            RenderTexture.SetData(TextureData);
        }

        private void DrawPixel(int x, int y, Color color, int size)
        {
            float halfSize = (float)size / 2f;
            int minX = (int)((float)x - halfSize), minY = (int)((float)y - halfSize);
            int maxX = (int)((float)minX + halfSize), maxY = (int)((float)minY + halfSize);
            if (minX >= 0 && minY >= 0 && maxX < Width && maxY < Height)
            {
                for (int curX = minX; curX <= maxX; curX++)
                {
                    for (int curY = minY; curY <= maxY; curY++)
                    {
                        TextureData[curY * Width + curX] = color;
                    }
                }
            }
        }

        public void Draw(GameTime gameTime, int x, int y)
        {
            if (!WarpEngaged) return;

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            SpriteBatch.Draw(RenderTexture, new Rectangle(x, y, RenderTexture.Width, RenderTexture.Height), Color.White);
            SpriteBatch.End();
        }
    }

}
