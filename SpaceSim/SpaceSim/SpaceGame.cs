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
        //List<GameEntity> gameEntities;
        Space space;
        Dictionary<EntityType, List<GameEntity>> gameEntities;
        Dictionary<EntityType, GameModel> gameModels;

        //temp
        Matrix[] instanceTransforms;
        DynamicVertexBuffer instanceVertexBuffer;

        // To store instance transform matrices in a vertex buffer, we use this custom
        // vertex type which encodes 4x4 matrices as a set of four Vector4 values.
        static VertexDeclaration instanceVertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
            new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3)
        );

        public SpaceGame()
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
            // TODO: Add your initialization logic here

            base.Initialize();

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

            gameModels = new Dictionary<EntityType, GameModel>();
            gameModels.Add(EntityType.Asteroid, new GameModel(Content, "Models/Cats"));
            //modelAsteroid = Content.Load<Model>("Models/asteroid");
            //modelLaser = Content.Load<Model>("Models/Laser");

            gameEntities = new Dictionary<EntityType, List<GameEntity>>();
            gameEntities.Add(EntityType.Asteroid, new List<GameEntity>());

            space = new Space();

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

            space.Update();

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
            List<GameEntity> asteroids = gameEntities[EntityType.Asteroid];
            Array.Resize(ref instanceTransforms, asteroids.Count);

            for (int i = 0; i < asteroids.Count; i++)
            {
                instanceTransforms[i] = asteroids[i].World;
            }

            GameModel gameModel = gameModels[EntityType.Asteroid];
            DrawModelHardwareInstancing(gameModel.Model, gameModel.ModelBones, instanceTransforms, camera.View, camera.Projection);
        }

        /// <summary>
        /// Efficiently draws several copies of a piece of geometry using hardware instancing.
        /// </summary>
        void DrawModelHardwareInstancing(Model model, Matrix[] modelBones,
                                         Matrix[] instances, Matrix view, Matrix projection)
        {
            if (instances.Length == 0)
                return;

            // If we have more instances than room in our vertex buffer, grow it to the neccessary size.
            if ((instanceVertexBuffer == null) ||
                (instances.Length > instanceVertexBuffer.VertexCount))
            {
                if (instanceVertexBuffer != null)
                    instanceVertexBuffer.Dispose();

                instanceVertexBuffer = new DynamicVertexBuffer(GraphicsDevice, instanceVertexDeclaration,
                                                               instances.Length, BufferUsage.WriteOnly);
            }

            // Transfer the latest instance transform matrices into the instanceVertexBuffer.
            instanceVertexBuffer.SetData(instances, 0, instances.Length, SetDataOptions.Discard);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    // Tell the GPU to read from both the model vertex buffer plus our instanceVertexBuffer.
                    GraphicsDevice.SetVertexBuffers(
                        new VertexBufferBinding(meshPart.VertexBuffer, meshPart.VertexOffset, 0),
                        new VertexBufferBinding(instanceVertexBuffer, 0, 1)
                    );

                    GraphicsDevice.Indices = meshPart.IndexBuffer;

                    // Set up the instance rendering effect.
                    Effect effect = meshPart.Effect;

                    effect.CurrentTechnique = effect.Techniques["HardwareInstancing"];

                    effect.Parameters["World"].SetValue(modelBones[mesh.ParentBone.Index]);
                    effect.Parameters["View"].SetValue(view);
                    effect.Parameters["Projection"].SetValue(projection);

                    // Draw all the instance copies in a single call.
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();

                        GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0,
                                                               meshPart.NumVertices, meshPart.StartIndex,
                                                               meshPart.PrimitiveCount, instances.Length);
                    }
                }
            }
        }

        private void GenerateAsteroids()
        {
            Program.ConsoleWindow.Log("Generating asteroids...");
            Program.ConsoleWindow.TimerStart();
            IList<BroadPhaseEntry> overlaps = new List<BroadPhaseEntry>();

            int maxAsteroids = 10000, maxRange = 2000, asteroidsCreated = 0;
            float minRadius = 0.5f, maxRadius = 10f;
            Random r = new Random();
            for (int i = 0; i < maxAsteroids; i++)
            {
                float radius = ((float)r.NextDouble() * (maxRadius - minRadius)) + minRadius;
                Vector3? pos = GetRandomNonCollidingPoint(radius, Vector3.Zero, maxRange, 10, r);
                if (pos != null)
                {
                    GameEntity gameEntity = new GameEntity(EntityType.Asteroid, new Sphere(MathConverter.Convert(pos.Value), radius, 1000));
                    gameEntity.SetScale(radius * 5.0f);
                    //space.Add(gameEntity.PhysicsEntity);
                    gameEntities[EntityType.Asteroid].Add(gameEntity);

                    if (cameraTarget == null)
                    {
                        space.Add(gameEntity.PhysicsEntity);
                        cameraTarget = gameEntity;

                        Random rnd = new Random();
                        gameEntity.PhysicsEntity.AngularDamping = 0;
                        gameEntity.PhysicsEntity.LinearDamping = 0;
                        float xRotRnd = ((float)rnd.NextDouble() * (float)(rnd.Next(1, 3) == 1 ? 1 : -1));
                        float yRotRnd = ((float)rnd.NextDouble() * (float)(rnd.Next(1, 3) == 1 ? 1 : -1));
                        float zRotRnd = ((float)rnd.NextDouble() * (float)(rnd.Next(1, 3) == 1 ? 1 : -1));
                        gameEntity.PhysicsEntity.AngularVelocity = new BEPUutilities.Vector3(xRotRnd, yRotRnd, zRotRnd);

                    }
                    asteroidsCreated++;
                }
            }
            Program.ConsoleWindow.Log(asteroidsCreated + " asteroids created in " + Program.ConsoleWindow.TimerStop().TotalSeconds + " sec");
        }

        public Vector3? GetRandomNonCollidingPoint(float requestedPlacementRadius, Vector3 targetAreaCenter, int targetAreaRadius, int maxPlacementAttempts, Random r)
        {
            IList<BroadPhaseEntry> overlaps = new List<BroadPhaseEntry>();
            for (int j = 0; j < maxPlacementAttempts; j++)
            {
                BEPUutilities.Vector3 pos = new BEPUutilities.Vector3(targetAreaCenter.X + r.Next(-targetAreaRadius / 2, targetAreaRadius / 2), r.Next(-targetAreaRadius / 2, targetAreaRadius / 2), r.Next(-targetAreaRadius / 2, targetAreaRadius / 2));
                space.BroadPhase.QueryAccelerator.GetEntries(new BEPUutilities.BoundingBox(new BEPUutilities.Vector3(pos.X - requestedPlacementRadius, pos.Y - requestedPlacementRadius, pos.Z - requestedPlacementRadius), new BEPUutilities.Vector3(pos.X + requestedPlacementRadius, pos.Y + requestedPlacementRadius, pos.Z + requestedPlacementRadius)), overlaps);
                //if (overlaps.Count == 0)
                return MathConverter.Convert(pos);
            }
            return null;
        }

        private void UpdateCamera()
        {
            if (cameraTarget != null)
            {
                camera.ChasePosition = cameraTarget.World.Translation;
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
    }
}
