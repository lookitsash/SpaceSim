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
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.Entities;
using ConversionHelper;
using BEPUphysics.Paths.PathFollowing;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using ParticleEngine;
using SpaceSimLibrary.Networking;
using SpaceSimLibrary;
namespace SpaceSim
{
    public class SpaceGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        ChaseCamera camera;
        GameEntity cameraTarget;
        Skybox skybox;        
        GameManager gameManager;
        InputManager inputManager;

        SpriteFont spriteFont;
        int framesPerSecond, frames;
        TimeSpan elapsedTime = TimeSpan.Zero;
        Vector2 infoFontPos = new Vector2(1.0f, 1.0f);

        public SpaceGame()
        {
            graphics = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";

            gameManager = new GameManager(true);

            //IsFixedTimeStep = false;
            //graphics.SynchronizeWithVerticalRetrace = false;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
            //TargetElapsedTime = new TimeSpan(0, 0, 0, 0, 60); //new TimeSpan(TimeSpan.TicksPerSecond / 60);
            //graphics.SynchronizeWithVerticalRetrace = false;

            graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width / 2;
            graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height / 2;

            InitializeCamera();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            skybox = new Skybox("Textures/Background1", Content);

            spriteFont = Content.Load<SpriteFont>("Fonts/Font1");

            gameManager.RegisterModel(EntityType.Asteroid, new GameModel(GraphicsDevice, Content, "Models/asteroid"));
            gameManager.RegisterModel(EntityType.Player, new GameModel(GraphicsDevice, Content, "Models/Ship"));

            ShipEntity playerEntity = new ShipEntity(new Sphere(new BEPUutilities.Vector3(0, 0, 0), 1, 100));
            playerEntity.SetScale(0.001f);
            cameraTarget = gameManager.RegisterEntity(playerEntity);

            inputManager = new InputManager(this, playerEntity);

            GenerateAsteroids();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void OnExiting(Object sender, EventArgs args)
        {
            Server.StopServer();
            base.OnExiting(sender, args);

            // Stop the threads
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            UpdateCamera();

            gameManager.Update(gameTime);

            if (inputManager != null) inputManager.Update(gameTime);

            UpdateFrameRate(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            
            DrawSkybox(gameTime);
            DrawGameEntities(gameTime);
            

            base.Draw(gameTime);

            DrawOverlay(gameTime);

            IncrementFrameCounter();
        }

        private void DrawOverlay(GameTime gameTime)
        {
            DepthStencilState ds = graphics.GraphicsDevice.DepthStencilState;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.DrawString(spriteFont, "FPS: " + framesPerSecond, infoFontPos, Color.Yellow);
            spriteBatch.End();
            graphics.GraphicsDevice.DepthStencilState = ds;

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        private void DrawSkybox(GameTime gameTime)
        {
            RasterizerState originalRasterizerState = graphics.GraphicsDevice.RasterizerState;
            RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            graphics.GraphicsDevice.RasterizerState = rasterizerState;
            skybox.Draw(camera.View, camera.Projection, camera.Position);
            graphics.GraphicsDevice.RasterizerState = originalRasterizerState;
        }

        private void DrawGameEntities(GameTime gameTime)
        {
            foreach (EntityType entityType in gameManager.GetEntityTypes())
            {
                List<GameEntity> entities = gameManager.GetEntitiesByType(entityType);
                if (entities != null && entities.Count > 0)
                {
                    List<GameEntity> nearbyEntities = new List<GameEntity>();
                    foreach (GameEntity entity in entities)
                    {
                        if (entity == cameraTarget || Vector3.Distance(camera.Position, entity.Position) <= 1000)
                        {
                            SpaceSimLibrary.Networking.Server.UpdateServerEntity(entity.ID, entity.EntityType, entity.World, entity.Scale.M11, entity.Scale.M22, entity.Scale.M33);
                            nearbyEntities.Add(entity);
                        }
                    }

                    if (nearbyEntities.Count > 0)
                    {
                        GameModel gameModel = gameManager.GetModel(entityType);
                        gameModel.SetInstanceTransforms(nearbyEntities);
                        Utilities.DrawModelHardwareInstancing(GraphicsDevice, gameModel.Model, gameModel.ModelBones, gameModel.InstanceTransforms, gameModel.InstanceVertexBuffer, camera.View, camera.Projection);
                    }
                }
            }
        }

        private void GenerateAsteroids()
        {
            //Program.ConsoleWindow.Log("Generating asteroids...");
            //Program.ConsoleWindow.TimerStart();

            int maxAsteroids = 1000, maxRange = 2000, asteroidsCreated = 0;
            float minRadius = 0.1f, maxRadius = 10f;
            Random rnd = new Random();
            for (int i = 0; i < maxAsteroids; i++)
            {
                float radius = ((float)rnd.NextDouble() * (maxRadius - minRadius)) + minRadius;
                Vector3? pos = gameManager.GetRandomNonCollidingPoint(radius, Vector3.Zero, maxRange, 10, rnd);
                if (pos != null)
                {
                    GameEntity gameEntity = new GameEntity(EntityType.Asteroid, new Sphere(MathConverter.Convert(pos.Value), radius, 1000));
                    gameEntity.SetScale(radius * 1f);
                    //gameEntity.SetScale(0.5f);
                    gameManager.RegisterEntity(gameEntity);

                    gameEntity.PhysicsEntity.AngularDamping = 0;
                    gameEntity.PhysicsEntity.LinearDamping = 0;
                    float xRotRnd = ((float)rnd.NextDouble() * (float)(rnd.Next(1, 3) == 1 ? 1 : -1));
                    float yRotRnd = ((float)rnd.NextDouble() * (float)(rnd.Next(1, 3) == 1 ? 1 : -1));
                    float zRotRnd = ((float)rnd.NextDouble() * (float)(rnd.Next(1, 3) == 1 ? 1 : -1));
                    gameEntity.PhysicsEntity.AngularVelocity = new BEPUutilities.Vector3(xRotRnd, yRotRnd, zRotRnd);
                    
                    asteroidsCreated++;
                }
            }
            //Program.ConsoleWindow.Log(asteroidsCreated + " asteroids created in " + Program.ConsoleWindow.TimerStop().TotalSeconds + " sec");
        }

        private void UpdateCamera()
        {
            if (cameraTarget != null)
            {
                camera.ChasePosition = cameraTarget.Position;
                camera.ChaseDirection = cameraTarget.World.Forward;
                camera.Up = cameraTarget.World.Up;
            }
            camera.Reset();
        }

        private void InitializeCamera()
        {
            camera = new ChaseCamera();

            // Set the camera offsets
            camera.DesiredPositionOffset = new Vector3(0.0f, 1.0f, 3.5f);
            //camera.DesiredPositionOffset = new Vector3(100.0f, 100.0f, 350.0f);
            camera.LookAtOffset = new Vector3(0.0f, 0.5f, 0.0f);

            // Set camera perspective
            camera.NearPlaneDistance = 0.1f;
            camera.FarPlaneDistance = 100000.0f;

            // Set the camera aspect ratio
            // This must be done after the class to base.Initalize() which will
            // initialize the graphics device.
            camera.AspectRatio = (float)graphics.GraphicsDevice.Viewport.Width / graphics.GraphicsDevice.Viewport.Height;

            UpdateCamera();
        }

        private void UpdateFrameRate(GameTime gameTime)
        {
            elapsedTime += gameTime.ElapsedGameTime;

            if (elapsedTime > TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);
                framesPerSecond = frames;
                frames = 0;
            }
        }

        private void IncrementFrameCounter()
        {
            ++frames;
        }
    }
}
