using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace SpaceSim
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class SpaceSimGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont spriteFont;

        KeyboardState lastKeyboardState = new KeyboardState();
        MouseState lastMouseState = new MouseState();
        KeyboardState currentKeyboardState = new KeyboardState();
        MouseState currentMouseState = new MouseState();

        Texture2D textureCockpit;

        Ship ship;
        ChaseCamera camera;
        Skybox skybox;

        Model shipModel;

        bool cameraSpringEnabled = true;

        public SpaceSimGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width / 2;
            graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height / 2;

            // Create the chase camera
            camera = new ChaseCamera();

            // Set the camera offsets
            camera.DesiredPositionOffset = new Vector3(0.0f, 2000.0f, 3500.0f);
            camera.LookAtOffset = new Vector3(0.0f, 150.0f, 0.0f);

            // Set camera perspective
            camera.NearPlaneDistance = 10.0f;
            camera.FarPlaneDistance = 100000.0f;
            
            ship = new Ship(GraphicsDevice);

            // Set the camera aspect ratio
            // This must be done after the class to base.Initalize() which will
            // initialize the graphics device.
            camera.AspectRatio = (float)graphics.GraphicsDevice.Viewport.Width / graphics.GraphicsDevice.Viewport.Height;

            // Perform an inital reset on the camera so that it starts at the resting
            // position. If we don't do this, the camera will start at the origin and
            // race across the world to get behind the chased object.
            // This is performed here because the aspect ratio is needed by Reset.
            UpdateCameraChaseTarget();
            camera.Reset();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            textureCockpit = Content.Load<Texture2D>("Textures/Cockpit6");

            shipModel = Content.Load<Model>("Models/Ship");

            skybox = new Skybox("Textures/suninspace2", Content);

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            lastKeyboardState = currentKeyboardState;
            lastMouseState = currentMouseState;

            currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();

            // Exit when the Escape key or Back button is pressed
            if (currentKeyboardState.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            bool touchTopLeft = currentMouseState.LeftButton == ButtonState.Pressed &&
                    lastMouseState.LeftButton != ButtonState.Pressed &&
                    currentMouseState.X < GraphicsDevice.Viewport.Width / 10 &&
                    currentMouseState.Y < GraphicsDevice.Viewport.Height / 10;


            // Pressing the A button or key toggles the spring behavior on and off
            if (lastKeyboardState.IsKeyUp(Keys.A) &&
                (currentKeyboardState.IsKeyDown(Keys.A)) || touchTopLeft)
            {
                cameraSpringEnabled = !cameraSpringEnabled;
            }

            // Reset the ship on R key or right thumb stick clicked
            if (currentKeyboardState.IsKeyDown(Keys.R))
            {
                ship.Reset();
                camera.Reset();
            }

            // Update the ship
            ship.Update(gameTime);

            // Update the camera to chase the new target
            UpdateCameraChaseTarget();

            // The chase camera's update behavior is the springs, but we can
            // use the Reset method to have a locked, spring-less camera
            if (cameraSpringEnabled)
                camera.Update(gameTime);
            else
                camera.Reset();

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            RasterizerState originalRasterizerState = graphics.GraphicsDevice.RasterizerState;
            RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            graphics.GraphicsDevice.RasterizerState = rasterizerState;
            skybox.Draw(camera.View, camera.Projection, camera.Position);
            graphics.GraphicsDevice.RasterizerState = originalRasterizerState;

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            DrawModel(shipModel, ship.World);
            //DrawCockpit(gameTime);

            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }

        /// <summary>
        /// Simple model drawing method. The interesting part here is that
        /// the view and projection matrices are taken from the camera object.
        /// </summary>        
        private void DrawModel(Model model, Matrix world)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.World = transforms[mesh.ParentBone.Index] * world;

                    // Use the matrices provided by the chase camera
                    effect.View = camera.View;
                    effect.Projection = camera.Projection;
                }
                mesh.Draw();
            }
        }

        private void DrawCockpit(GameTime gameTime)
        {
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

            spriteBatch.Draw(textureCockpit, new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height), Color.White);
            spriteBatch.End();
        }

        /// <summary>
        /// Update the values to be chased by the camera
        /// </summary>
        private void UpdateCameraChaseTarget()
        {
            camera.ChasePosition = ship.Position;
            camera.ChaseDirection = ship.Direction;
            camera.Up = ship.Up;
        }

        /// <summary>
        /// Displays an overlay showing what the controls are,
        /// and which settings are currently selected.
        /// </summary>
        private void DrawOverlayText()
        {
            spriteBatch.Begin();

            string text = "-Touch, Right Trigger, or Spacebar = thrust\n" +
                          "-Screen edges, Left Thumb Stick,\n  or Arrow keys = steer\n" +
                          "-Press A or touch the top left corner\n  to toggle camera spring (" + (cameraSpringEnabled ?
                              "on" : "off") + ")";

            // Draw the string twice to create a drop shadow, first colored black
            // and offset one pixel to the bottom right, then again in white at the
            // intended position. This makes text easier to read over the background.
            spriteBatch.DrawString(spriteFont, text, new Vector2(65, 65), Color.Black);
            spriteBatch.DrawString(spriteFont, text, new Vector2(64, 64), Color.White);

            spriteBatch.End();
        }
    }
}
