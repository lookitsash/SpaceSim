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
using SpaceSimLibrary.Networking;
using SpaceSimLibrary;
using BEPUphysics.Entities.Prefabs;
using ConversionHelper;

namespace SpaceSimV2
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class SpaceSimGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Client client;
        ChaseCamera camera = null;
        GameEntity cameraTarget;
        Skybox skybox;
        CameraDirection CameraDirection;
        int ClientPort;
        GameManager gameManager;

        public SpaceSimGame(CameraDirection cameraDirection, int clientPort)
        {
            graphics = new GraphicsDeviceManager(this);
            CameraDirection = cameraDirection;
            ClientPort = clientPort;

            Content.RootDirectory = "SpaceSimLibraryContent";

            gameManager = new GameManager(true);
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

            gameManager.RegisterModel(EntityType.Asteroid, new GameModel(GraphicsDevice, Content, "Models/asteroid"));
            //gameManager.RegisterModel(EntityType.Player, new GameModel(GraphicsDevice, Content, "Models/Ship"));
            gameManager.RegisterModel(EntityType.PlanetEarth, new GameModel(GraphicsDevice, Content, "Models/earth"));

            client = new Client(ClientPort);
            client.OnCommandReceived += new SpaceSimLibrary.Networking.CommandReceived(CommandReceived);

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

        protected override void Update(GameTime gameTime)
        {
            UpdateCamera();

            gameManager.Update(gameTime);

            //UpdateFrameRate(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            try
            {
                GraphicsDevice.Clear(Color.Black);

                DrawSkybox(gameTime);
                DrawGameEntities(gameTime);


                base.Draw(gameTime);
            }
            catch (Exception ex)
            {
            }
            //DrawOverlay(gameTime);

            //IncrementFrameCounter();
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
                    List<GameEntity> instanceDrawingModelEntities = new List<GameEntity>();
                    foreach (GameEntity entity in entities)
                    {
                        if (entity == cameraTarget || Vector3.Distance(camera.Position, entity.Position) <= 1000)
                        {
                            if (entity.EntityType == EntityType.PlanetEarth)
                            {
                                entity.Draw(camera.View, camera.Projection, camera.Position, gameTime);
                            }
                            else instanceDrawingModelEntities.Add(entity);
                        }
                    }

                    if (instanceDrawingModelEntities.Count > 0)
                    {
                        GameModel gameModel = gameManager.GetModel(entityType);
                        gameModel.SetInstanceTransforms(instanceDrawingModelEntities);
                        Utilities.DrawModelHardwareInstancing(GraphicsDevice, gameModel.Model, gameModel.ModelBones, gameModel.InstanceTransforms, gameModel.InstanceVertexBuffer, camera.View, camera.Projection);
                    }
                }
            }
        }

        private void CommandReceived(CommandReader cr)
        {
            Commands cmd = cr.ReadCommand();
            
            if (cmd == Commands.UpdateEntity)
            {
                int entityID = cr.ReadData<int>();
                EntityType entityType = (EntityType)cr.ReadData<byte>();
                Matrix world = cr.ReadMatrix();
                Vector3 scale = cr.ReadVector3();

                PhysicsShape physicsShape = (PhysicsShape)cr.ReadData<byte>();
                float radius = cr.ReadData<float>();
                float angularDamping = cr.ReadData<float>();
                float linearDamping = cr.ReadData<float>();                
                Vector3 angularVelocity = cr.ReadVector3();
                Vector3 linearVelocity = cr.ReadVector3();

                GameEntity entity = gameManager.GetEntity(entityID);
                if (entity == null)
                {
                    lock (gameManager)
                    {
                        if (entityType == EntityType.PlanetEarth) entity = new PlanetEntity(this, entityType, new Sphere(MathConverter.Convert(world.Translation), radius));
                        else entity = new GameEntity(this, entityType, new Sphere(MathConverter.Convert(world.Translation), radius));
                        entity.ID = entityID;
                        gameManager.RegisterEntity(entity);
                    }
                }
                else
                {
                    entity.SetScale(scale.X, scale.Y, scale.Z);
                    entity.PhysicsEntity.AngularDamping = angularDamping;
                    entity.PhysicsEntity.LinearDamping = linearDamping;
                    entity.PhysicsEntity.AngularVelocity = MathConverter.Convert(angularVelocity);
                    entity.PhysicsEntity.LinearVelocity = MathConverter.Convert(linearVelocity);
                    entity.PhysicsEntity.WorldTransform = MathConverter.Convert(world);
                    //BEPUutilities.Matrix.
                    //entity.World = world;
                    //entity.PhysicsEntity.WorldTransform = new BEPUutilities.Matrix(world.M11, world.M12, world.M13, world.M14, world.M21, world.M22, world.M23, world.M24, world.M31, world.M32, world.M33, world.M34, world.M41, world.M42, world.M43, world.M44);

                }
                if (entityType == EntityType.Player) cameraTarget = entity;
                /*
                ServerEntity entity = ServerEntities.ContainsKey(entityID) ? ServerEntities[entityID] : null;
                if (entity == null)
                {
                    entity = new ServerEntity() { ID = entityID, EntityType = entityType, World = world, Model = GetModel(entityType) };
                    if (entity.Model != null) entity.BoneTransforms = new Matrix[entity.Model.Bones.Count];
                    ServerEntities.Add(entityID, entity);
                    ServerEntityIDs.Add(entityID);
                    if (entityType == EntityType.Player) CameraTarget = entity;
                }
                else entity.World = world;
                 */
            }
        }

        private void UpdateCamera()
        {
            if (cameraTarget != null)
            {
                camera.ChasePosition = cameraTarget.Position;
                if (CameraDirection == SpaceSimV2.CameraDirection.Forward) camera.ChaseDirection = cameraTarget.World.Forward;
                else if (CameraDirection == SpaceSimV2.CameraDirection.Backward) camera.ChaseDirection = cameraTarget.World.Backward;
                else if (CameraDirection == SpaceSimV2.CameraDirection.Left) camera.ChaseDirection = cameraTarget.World.Left;
                else if (CameraDirection == SpaceSimV2.CameraDirection.Right) camera.ChaseDirection = cameraTarget.World.Right;
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
    }

    public enum CameraDirection
    {
        Forward,
        Backward,
        Left,
        Right
    }
}
