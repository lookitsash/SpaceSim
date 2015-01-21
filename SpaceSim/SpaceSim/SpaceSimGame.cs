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
        public static ConsoleWindow ConsoleWindow;

        KeyboardState lastKeyboardState = new KeyboardState();
        MouseState lastMouseState = new MouseState();
        KeyboardState currentKeyboardState = new KeyboardState();
        MouseState currentMouseState = new MouseState();

        Texture2D textureCockpit;

        Ship ship;
        Earth earth;
        ChaseCamera camera;
        Skybox skybox;

        Model modelShip, modelEarth;

        public List<Entity> EntityCollection = new List<Entity>();

        private Vector4 globalAmbient;
        private Sunlight sunlight;

        bool cameraSpringEnabled = true;

        public SpaceSimGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            (ConsoleWindow = new ConsoleWindow()).Show();
            ConsoleWindow.OnInput += new ConsoleWindow.ConsoleInputEventHandler(OnConsoleInput);
        }

        private void OnConsoleInput(string str)
        {
            if (str == "exit" || str == "quit") Exit();
        }

        public Entity GetCollidingEntity(Entity source)
        {
            foreach (Entity target in EntityCollection)
            {
                if (target != source)
                {
                    //BoundingSphere c1BoundingSphere = c1.model.Meshes[i].BoundingSphere;
                    //c1BoundingSphere.Center += c1.position;
                }
            }
            return null;
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
            camera.DesiredPositionOffset = new Vector3(0.0f, 100.0f, 350.0f);
            camera.LookAtOffset = new Vector3(0.0f, 50.0f, 0.0f);

            // Set camera perspective
            camera.NearPlaneDistance = 10.0f;
            camera.FarPlaneDistance = 100000.0f;

            EntityCollection.Add(ship = new Ship(GraphicsDevice, modelShip));
            ship.Position = new Vector3(0, 0, 44000);

            EntityCollection.Add(earth = new Earth(GraphicsDevice, modelEarth));
            earth.LoadContent(Content);
            earth.Scale = 5000;
            earth.Position = new Vector3(0, 0, -20000);

            sunlight.direction = new Vector4(Vector3.Forward, 0.0f);
            sunlight.color = new Vector4(1.0f, 0.941f, 0.898f, 1.0f);

            // Setup scene's global ambient.
            globalAmbient = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);

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

            modelShip = Content.Load<Model>("Models/Ship");
            modelEarth = Content.Load<Model>("Models/earth");

            /*
            BoundingSphere bounds = new BoundingSphere();
            foreach (ModelMesh mesh in modelEarth.Meshes)
                bounds = BoundingSphere.CreateMerged(bounds, mesh.BoundingSphere);
            float shipRadius = bounds.Radius;
            */
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
            if (lastKeyboardState.IsKeyUp(Keys.A) && (currentKeyboardState.IsKeyDown(Keys.A)) || touchTopLeft)
            {
                //cameraSpringEnabled = !cameraSpringEnabled;
            }

            // Reset the ship on R key or right thumb stick clicked
            if (currentKeyboardState.IsKeyDown(Keys.R))
            {
                ship.Reset();
                camera.Reset();
            }

            // Update the ship
            ship.Update(gameTime);

            earth.Update(gameTime);

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
            GraphicsDevice.Clear(Color.Black);
            /*
            RasterizerState originalRasterizerState = graphics.GraphicsDevice.RasterizerState;
            RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            graphics.GraphicsDevice.RasterizerState = rasterizerState;
            skybox.Draw(camera.View, camera.Projection, camera.Position);
            graphics.GraphicsDevice.RasterizerState = originalRasterizerState;
            */
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            GraphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

            DrawModel(modelShip, ship.World);
            //DrawModel(modelEarth, ship.World);
            DrawEarth();
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

        private void DrawEarth()
        {
            //Matrix rotation = Matrix.CreateRotationY(earth.rotation) * Matrix.CreateRotationZ(MathHelper.ToRadians(-23.4f));

            foreach (ModelMesh m in earth.model.Meshes)
            {
                foreach (Effect e in m.Effects)
                {
                    if (false) //hideClouds)
                    {
                        e.CurrentTechnique = e.Techniques["EarthWithoutClouds"];
                    }
                    else
                    {
                        e.CurrentTechnique = e.Techniques["EarthWithClouds"];
                        e.Parameters["cloudStrength"].SetValue(earth.cloudStrength);
                    }

                    e.Parameters["world"].SetValue(earth.World);
                    e.Parameters["view"].SetValue(camera.View);
                    e.Parameters["projection"].SetValue(camera.Projection);
                    e.Parameters["cameraPos"].SetValue(new Vector4(camera.Position, 1.0f));
                    e.Parameters["globalAmbient"].SetValue(globalAmbient);
                    e.Parameters["lightDir"].SetValue(sunlight.direction);
                    e.Parameters["lightColor"].SetValue(sunlight.color);
                    e.Parameters["materialAmbient"].SetValue(earth.ambient);
                    e.Parameters["materialDiffuse"].SetValue(earth.diffuse);
                    e.Parameters["materialSpecular"].SetValue(earth.specular);
                    e.Parameters["materialShininess"].SetValue(earth.shininess);
                    e.Parameters["landOceanColorGlossMap"].SetValue(earth.dayTexture);
                    e.Parameters["cloudColorMap"].SetValue(earth.cloudTexture);
                    e.Parameters["nightColorMap"].SetValue(earth.nightTexture);
                    e.Parameters["normalMap"].SetValue(earth.normalMapTexture);
                }

                m.Draw();
            }
        }
    }

    public struct Sunlight
    {
        public Vector4 direction;
        public Vector4 color;
    }
}
