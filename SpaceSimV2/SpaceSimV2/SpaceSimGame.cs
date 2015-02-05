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
        Skybox skybox;
        CameraDirection CameraDirection;
        int ClientPort;

        public Model modelAsteroid, modelLaser;

        public SpaceSimGame(CameraDirection cameraDirection, int clientPort)
        {
            graphics = new GraphicsDeviceManager(this);
            CameraDirection = cameraDirection;
            ClientPort = clientPort;

            Content.RootDirectory = "SpaceSimLibraryContent";
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

            skybox = new Skybox("Textures/Background1", Content);

            modelAsteroid = Content.Load<Model>("Models/asteroid");
            modelLaser = Content.Load<Model>("Models/Laser");

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

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            UpdateCameraChaseTarget();
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

            RasterizerState originalRasterizerState = graphics.GraphicsDevice.RasterizerState;
            RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            graphics.GraphicsDevice.RasterizerState = rasterizerState;
            skybox.Draw(camera.View, camera.Projection, camera.Position);
            graphics.GraphicsDevice.RasterizerState = originalRasterizerState;

            int entityCount = ServerEntityIDs.Count;
            for (int i = 0; i < entityCount; i++)
            {
                if (i >= ServerEntityIDs.Count) break;
                int entityID = ServerEntityIDs[i];

                if (!ServerEntities.ContainsKey(entityID)) continue;
                ServerEntity entity = ServerEntities[entityID];
                if (entity.Model == null || entity.EntityType == EntityType.Player) continue;

                entity.Model.CopyAbsoluteBoneTransformsTo(entity.BoneTransforms);
                foreach (ModelMesh mesh in entity.Model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.EnableDefaultLighting();
                        effect.World = entity.BoneTransforms[mesh.ParentBone.Index] * entity.World;
                        effect.View = camera.View;
                        effect.Projection = camera.Projection;
                    }
                    mesh.Draw();
                }
            }

            base.Draw(gameTime);
        }

        private Model GetModel(EntityType entityType)
        {
            if (entityType == EntityType.Asteroid) return modelAsteroid;
            //else if (entityType == EntityType.Planet) return modelLaser;
            else return null;
        }

        private void CommandReceived(CommandReader cr)
        {
            Commands cmd = cr.ReadCommand();
            /*            
            if (cmd == Commands.RegisterEntity)
            {
                int entityID = cr.ReadData<int>();
                GameModelType gameModelType = (GameModelType)cr.ReadData<byte>();
                Matrix modelTransform = cr.ReadMatrix();
                Matrix world = cr.ReadMatrix();
                Model model = modelAsteroid;
                //Console.WriteLine("RegisterEntity " + entityID + "," + gameModelType.ToString());
                if (gameModelType == GameModelType.Ship || gameModelType == GameModelType.Asteroid || gameModelType == GameModelType.Projectile)
                {
                    if (gameModelType == GameModelType.Projectile) model = modelLaser;
                    if (!ServerEntities.ContainsKey(entityID))
                    {
                        ServerEntities.Add(entityID, new ServerEntity() { ID = entityID, GameModelType = gameModelType, ModelTransform = modelTransform, World = world, Model = model, BoneTransforms = new Matrix[model.Bones.Count] });
                        ServerEntityIDs.Add(entityID);
                        if (gameModelType == GameModelType.Ship) CameraTarget = ServerEntities[entityID];
                    }
                }
            }
            else if (cmd == Commands.RemoveEntity)
            {
                int entityID = cr.ReadData<int>();
                if (ServerEntities.ContainsKey(entityID))
                {
                    ServerEntities.Remove(entityID);
                    ServerEntityIDs.Remove(entityID);
                }
            }
            */
            if (cmd == Commands.UpdateEntity)
            {
                int entityID = cr.ReadData<int>();
                EntityType entityType = (EntityType)cr.ReadData<byte>();
                
                Matrix world = cr.ReadMatrix();
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

                /*
                if (ServerEntities.ContainsKey(entityID))
                {
                    ServerEntity entity = ServerEntities[entityID];
                    if (entity.GameModelType == GameModelType.Projectile)
                    {
                        //Console.WriteLine(world.Translation.X + "," + world.Translation.Y + "," + world.Translation.Z);
                    }
                    entity.World = world;
                }
                else
                {
                    if (!missingEntityIDs.Contains(entityID))
                    {
                        Console.WriteLine("entityID " + entityID + " not found");
                        missingEntityIDs.Add(entityID);
                    }
                }
                 */
            }
        }
        private List<int> missingEntityIDs = new List<int>();

        private Dictionary<int,ServerEntity> ServerEntities = new Dictionary<int,ServerEntity>();
        private List<int> ServerEntityIDs = new List<int>();
        private ServerEntity CameraTarget;

        private void UpdateCameraChaseTarget()
        {
            if (CameraTarget != null)
            {
                camera.ChasePosition = CameraTarget.World.Translation;
                if (CameraDirection == SpaceSimV2.CameraDirection.Forward) camera.ChaseDirection = CameraTarget.World.Forward;
                else if (CameraDirection == SpaceSimV2.CameraDirection.Backward) camera.ChaseDirection = CameraTarget.World.Backward;
                else if (CameraDirection == SpaceSimV2.CameraDirection.Left) camera.ChaseDirection = CameraTarget.World.Left;
                else if (CameraDirection == SpaceSimV2.CameraDirection.Right) camera.ChaseDirection = CameraTarget.World.Right;
                camera.Up = CameraTarget.World.Up;
            }
        }
    }

    public class ServerEntity
    {
        public int ID;
        public EntityType EntityType;
        public Matrix World;
        public Model Model;
        public Matrix[] BoneTransforms;
    }

    public enum GameModelType
    {
        Planet,
        Ship,
        Asteroid,
        Projectile,
        ShipNPC
    }

    public enum CameraDirection
    {
        Forward,
        Backward,
        Left,
        Right
    }
}
