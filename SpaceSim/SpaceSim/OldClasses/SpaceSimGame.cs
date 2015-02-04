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
using System.Threading;
using SpaceSimLibrary.Networking;
using SpaceSim.Old;

namespace SpaceSim
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class SpaceSimGame : Microsoft.Xna.Framework.Game
    {
        public static Space space;
        public static SpaceSimGame SpaceSimGameInstance;

        private int framesPerSecond, frames;
        private TimeSpan elapsedTime = TimeSpan.Zero;

        public CustomGraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        static SpriteFont spriteFont;

        KeyboardState previousKeyboardState = new KeyboardState();
        KeyboardState currentKeyboardState = new KeyboardState();

        static Texture2D textureCockpit;

        //public static BloomPostprocess.BloomComponent bloom;

        public ChaseCamera camera = null;
        Skybox skybox;

        public static bool Paused = false;

        //StarfieldComponent starfieldComponent;
        //Starfield starfield;

        public static Model modelShip, modelEarth, modelAsteroid, modelLaser, modelMoon, modelShipAI, modelProbe, modelMisc, modelStation1, modelClaw, modelAsteroid2, modelBuckRogers, modelSatellite1, modelCygnus, modelBarracuda;

        bool cameraSpringEnabled = false;

        public SecondaryGame SecondaryGame = null;

        public SpaceSimGame(GameDisplayType gameDisplayType)
        {
            SpaceSimGameInstance = this;
            //graphics = new TargetedGraphicsDeviceManager(this, 1);
            //graphics = new GraphicsDeviceManager(this);
            graphics = new CustomGraphicsDeviceManager(this, gameDisplayType);
            //new GraphicsDeviceManager
            Content.RootDirectory = "Content";

            IsFixedTimeStep = false;

            //starfieldComponent = new StarfieldComponent(this);
            //Components.Add(starfieldComponent);

            // Construct our particle system components.
            explosionParticles = new ExplosionParticleSystem(this, Content);
            explosionSmokeParticles = new ExplosionSmokeParticleSystem(this, Content);
            projectileTrailParticles = new ProjectileTrailParticleSystem(this, Content);
            smokePlumeParticles = new SmokePlumeParticleSystem(this, Content);
            fireParticles = new FireParticleSystem(this, Content);
            
            explosionSmallParticleSystem = new CustomParticleSystem(this, Content);
            explosionSmallParticleSystem.settings.MinStartSize = 1;
            explosionSmallParticleSystem.settings.MaxStartSize = 1;
            explosionSmallParticleSystem.settings.MinEndSize = 10;
            explosionSmallParticleSystem.settings.MaxEndSize = 14;

            explosionMediumParticleSystem = new CustomParticleSystem(this, Content);
            explosionMediumParticleSystem.settings.MinStartSize = 10;
            explosionMediumParticleSystem.settings.MaxStartSize = 10;
            explosionMediumParticleSystem.settings.MinEndSize = 100;
            explosionMediumParticleSystem.settings.MaxEndSize = 140;

            explosionLargeParticleSystem = new CustomParticleSystem(this, Content);
            explosionLargeParticleSystem.settings.MinStartSize = 30;
            explosionLargeParticleSystem.settings.MaxStartSize = 30;
            explosionLargeParticleSystem.settings.MinEndSize = 300;
            explosionLargeParticleSystem.settings.MaxEndSize = 420;

            // Set the draw order so the explosions and fire
            // will appear over the top of the smoke.
            smokePlumeParticles.DrawOrder = 100;
            explosionSmokeParticles.DrawOrder = 200;
            projectileTrailParticles.DrawOrder = 300;
            explosionParticles.DrawOrder = 400;
            fireParticles.DrawOrder = 500;
            
            explosionSmallParticleSystem.DrawOrder = 600;
            explosionMediumParticleSystem.DrawOrder = 600;
            explosionLargeParticleSystem.DrawOrder = 600;

            // Register the particle system components.
            Components.Add(explosionParticles);
            Components.Add(explosionSmokeParticles);
            Components.Add(projectileTrailParticles);
            Components.Add(smokePlumeParticles);
            Components.Add(fireParticles);
            Components.Add(explosionSmallParticleSystem);
            Components.Add(explosionMediumParticleSystem);
            Components.Add(explosionLargeParticleSystem);

            //starfield = new Starfield(this, 1000);
            //Components.Add(starfield);

            savedMousePosX = -1;
            savedMousePosY = -1;
            mouseSmoothingCache = new Vector2[MOUSE_SMOOTHING_CACHE_SIZE];
            mouseSmoothingSensitivity = DEFAULT_MOUSE_SMOOTHING_SENSITIVITY;
            mouseIndex = 0;
            mouseMovement = new Vector2[2];
            mouseMovement[0].X = 0.0f;
            mouseMovement[0].Y = 0.0f;
            mouseMovement[1].X = 0.0f;
            mouseMovement[1].Y = 0.0f;

            //bloom = new BloomPostprocess.BloomComponent(this);
            //Components.Add(bloom);
            //bloom.Settings = new BloomPostprocess.BloomSettings(null, 0.25f, 4, 2, 1, 1.5f, 1);
        }

        /// <summary>
        /// Event handler for when the game window acquires input focus.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="e">Ignored.</param>
        private void HandleGameActivatedEvent(object sender, EventArgs e)
        {
            if (savedMousePosX >= 0 && savedMousePosY >= 0)
                Mouse.SetPosition(savedMousePosX, savedMousePosY);
        }

        /// <summary>
        /// Filters the mouse movement based on a weighted sum of mouse
        /// movement from previous frames.
        /// <para>
        /// For further details see:
        ///  Nettle, Paul "Smooth Mouse Filtering", flipCode's Ask Midnight column.
        ///  http://www.flipcode.com/cgi-bin/fcarticles.cgi?show=64462
        /// </para>
        /// </summary>
        /// <param name="x">Horizontal mouse distance from window center.</param>
        /// <param name="y">Vertical mouse distance from window center.</param>
        private void PerformMouseFiltering(float x, float y)
        {
            // Shuffle all the entries in the cache.
            // Newer entries at the front. Older entries towards the back.
            for (int i = mouseSmoothingCache.Length - 1; i > 0; --i)
            {
                mouseSmoothingCache[i].X = mouseSmoothingCache[i - 1].X;
                mouseSmoothingCache[i].Y = mouseSmoothingCache[i - 1].Y;
            }

            // Store the current mouse movement entry at the front of cache.
            mouseSmoothingCache[0].X = x;
            mouseSmoothingCache[0].Y = y;

            float averageX = 0.0f;
            float averageY = 0.0f;
            float averageTotal = 0.0f;
            float currentWeight = 1.0f;

            // Filter the mouse movement with the rest of the cache entries.
            // Use a weighted average where newer entries have more effect than
            // older entries (towards the back of the cache).
            for (int i = 0; i < mouseSmoothingCache.Length; ++i)
            {
                averageX += mouseSmoothingCache[i].X * currentWeight;
                averageY += mouseSmoothingCache[i].Y * currentWeight;
                averageTotal += 1.0f * currentWeight;
                currentWeight *= mouseSmoothingSensitivity;
            }

            // Calculate the new smoothed mouse movement.
            smoothedMouseMovement.X = averageX / averageTotal;
            smoothedMouseMovement.Y = averageY / averageTotal;
        }

        /// <summary>
        /// Averages the mouse movement over a couple of frames to smooth out
        /// the mouse movement.
        /// </summary>
        /// <param name="x">Horizontal mouse distance from window center.</param>
        /// <param name="y">Vertical mouse distance from window center.</param>
        private void PerformMouseSmoothing(float x, float y)
        {
            mouseMovement[mouseIndex].X = x;
            mouseMovement[mouseIndex].Y = y;

            smoothedMouseMovement.X = (mouseMovement[0].X + mouseMovement[1].X) * 0.5f;
            smoothedMouseMovement.Y = (mouseMovement[0].Y + mouseMovement[1].Y) * 0.5f;

            mouseIndex ^= 1;
            mouseMovement[mouseIndex].X = 0.0f;
            mouseMovement[mouseIndex].Y = 0.0f;
        }

        /// <summary>
        /// Resets all mouse states. This is called whenever the mouse input
        /// behavior switches from click-and-drag mode to real-time mode.
        /// </summary>
        private void ResetMouse()
        {
            currentMouseState = Mouse.GetState();
            previousMouseState = currentMouseState;

            for (int i = 0; i < mouseMovement.Length; ++i)
                mouseMovement[i] = Vector2.Zero;

            for (int i = 0; i < mouseSmoothingCache.Length; ++i)
                mouseSmoothingCache[i] = Vector2.Zero;

            savedMousePosX = -1;
            savedMousePosY = -1;

            smoothedMouseMovement = Vector2.Zero;
            mouseIndex = 0;

            Rectangle clientBounds = Window.ClientBounds;

            int centerX = clientBounds.Width / 2;
            int centerY = clientBounds.Height / 2;
            int deltaX = centerX - currentMouseState.X;
            int deltaY = centerY - currentMouseState.Y;

            Mouse.SetPosition(centerX, centerY);
        }

        /// <summary>
        /// Determines which way the mouse wheel has been rolled.
        /// The returned value is in the range [-1,1].
        /// </summary>
        /// <returns>
        /// A positive value indicates that the mouse wheel has been rolled
        /// towards the player. A negative value indicates that the mouse
        /// wheel has been rolled away from the player.
        /// </returns>
        private float GetMouseWheelDirection()
        {
            int currentWheelValue = currentMouseState.ScrollWheelValue;
            int previousWheelValue = previousMouseState.ScrollWheelValue;

            if (currentWheelValue > previousWheelValue)
                return 1.0f;
            else if (currentWheelValue < previousWheelValue)
                return -1.0f;
            else
                return 0.0f;
        }

        /// <summary>
        /// Event hander for when the game window loses input focus.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="e">Ignored.</param>
        private void HandleGameDeactivatedEvent(object sender, EventArgs e)
        {
            MouseState state = Mouse.GetState();

            savedMousePosX = state.X;
            savedMousePosY = state.Y;
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

            Rectangle clientBounds = Window.ClientBounds;
            Mouse.SetPosition(clientBounds.Width / 2, clientBounds.Height / 2);

            graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width / 2;
            graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height / 2;

            // Create the chase camera
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

        public static Entity entityShip, entityEarth, entityAsteroid; //, entityAI, entityProbe;
        public static ManualResetEvent GameInitialized = new ManualResetEvent(false);

        public static Effect effectBlur;

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            skybox = new Skybox("Textures/Background1", Content);

            if (graphics.GameDisplayType != GameDisplayType.Cockpit)
            {
                GameInitialized.WaitOne();
                Program.ConsoleWindow.Log("noncockpit loadcontent");
            }
            else
            {
                Program.ConsoleWindow.Log("cockpit loadcontent");
                textureCockpit = Content.Load<Texture2D>("Textures/Cockpit6");
                spriteFont = Content.Load<SpriteFont>("Fonts/Font1");

                modelShip = Content.Load<Model>("Models/Ship");
                modelEarth = Content.Load<Model>("Models/earth");
                modelAsteroid = Content.Load<Model>("Models/asteroid");
                modelLaser = Content.Load<Model>("Models/Laser");
                modelMoon = Content.Load<Model>("Models/moon");
                modelShipAI = Content.Load<Model>("Models/Shuttle");
                modelProbe = Content.Load<Model>("Models/probe");

                //Scales
                //station1 - 0.1f
                //claw - 0.05f;
                //asteroid2 - 0.01f;
                //buckrogers - 0.05f;
                //satellite1 - 0.05;
                //cygnus - 0.1f;
                //barracuda - 0.3f; // min value for convexhull

                modelStation1 = Content.Load<Model>("Models/station1");
                modelClaw = Content.Load<Model>("Models/claw");
                //modelAsteroid2 = Content.Load<Model>("Models/asteroid2");
                //modelBuckRogers = Content.Load<Model>("Models/buckrogers");
                modelSatellite1 = Content.Load<Model>("Models/satellite1");
                modelCygnus = Content.Load<Model>("Models/cygnus");
                modelBarracuda = Content.Load<Model>("Models/barracuda");

                space = new Space();

                entityShip = AddEntity(space, new Sphere(new BEPUutilities.Vector3(0, 10, 350), 1, 100), modelShip, .001f, GameModelType.Ship, 0, 0, typeof(EntityModel));
                entityShip.AngularDamping = 0.9f;
                //entityShip.LinearDamping = 0.9f;
                entityShip.LinearDamping = 0.0f;
                entityShip.CollisionInformation.Events.InitialCollisionDetected += HandleCollision;
                ShipSpeedIndex = ShipSpeedIndexZero;

                entityEarth = AddEntity(space, new Sphere(new BEPUutilities.Vector3(0, 0, -200), 225), modelEarth, 50f, GameModelType.Planet, 0, 0, typeof(PlanetModel)); ;
                entityEarth.CollisionInformation.Events.InitialCollisionDetected += HandleCollision;
                entityEarth.AngularVelocity = MathConverter.Convert(new Vector3(0.005f, 0.001f, 0.001f));
                entityEarth.AngularDamping = 0f;

                Entity moon = AddEntity(space, new Sphere(new BEPUutilities.Vector3(-550, 100, -350), 20), modelMoon, 0.1f, GameModelType.Planet, 0, 0, typeof(EntityModel));
                moon.AngularVelocity = MathConverter.Convert(new Vector3(0.05f, 0.05f, 0));
                moon.AngularDamping = 0f;

                GenerateAsteroids();
                //GenerateNPCs();

                space.ForceUpdater.Gravity = new BEPUutilities.Vector3(0, 0, 0);

                GameInitialized.Set();
            }

            
        }

        public BoundingBox CalculateBoundingBox(Model model)
        {
            Matrix[] boneTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            // Create variables to hold min and max xyz values for the model. Initialise them to extremes
            Vector3 modelMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3 modelMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

            foreach (ModelMesh mesh in model.Meshes)
            {
                //Create variables to hold min and max xyz values for the mesh. Initialise them to extremes
                Vector3 meshMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                Vector3 meshMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

                // There may be multiple parts in a mesh (different materials etc.) so loop through each
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // The stride is how big, in bytes, one vertex is in the vertex buffer
                    // We have to use this as we do not know the make up of the vertex
                    int stride = part.VertexBuffer.VertexDeclaration.VertexStride;

                    byte[] vertexData = new byte[stride * part.NumVertices];
                    part.VertexBuffer.GetData(part.VertexOffset * stride, vertexData, 0, part.NumVertices, 1); // fixed 13/4/11

                    // Find minimum and maximum xyz values for this mesh part
                    // We know the position will always be the first 3 float values of the vertex data
                    Vector3 vertPosition = new Vector3();
                    for (int ndx = 0; ndx < vertexData.Length; ndx += stride)
                    {
                        vertPosition.X = BitConverter.ToSingle(vertexData, ndx);
                        vertPosition.Y = BitConverter.ToSingle(vertexData, ndx + sizeof(float));
                        vertPosition.Z = BitConverter.ToSingle(vertexData, ndx + sizeof(float) * 2);

                        // update our running values from this vertex
                        meshMin = Vector3.Min(meshMin, vertPosition);
                        meshMax = Vector3.Max(meshMax, vertPosition);
                    }
                }

                // transform by mesh bone transforms
                meshMin = Vector3.Transform(meshMin, boneTransforms[mesh.ParentBone.Index]);
                meshMax = Vector3.Transform(meshMax, boneTransforms[mesh.ParentBone.Index]);

                // Expand model extents by the ones from this mesh
                modelMin = Vector3.Min(modelMin, meshMin);
                modelMax = Vector3.Max(modelMax, meshMax);
            }


            // Create and return the model bounding box
            return new BoundingBox(modelMin, modelMax);

        }

        private List<Entity> AIEntities = new List<Entity>();

        private Random rand = new Random();
        //private Vector3? aiTargetPos = null;
        private void UpdateAI(GameTime gameTime)
        {
            return;

            foreach (Entity aiEntity in AIEntities)
            {
                EntityModel aiEntityModel = (EntityModel)aiEntity.Tag;
                if (aiEntityModel.AITargetPos == null)
                {
                    aiEntityModel.AITargetPos = GetRandomNonCollidingPoint(Max(aiEntityModel.Size.X, aiEntityModel.Size.Y, aiEntityModel.Size.Z), Vector3.Zero, 400, 10, rand);
                }

                float targetPosReachedMinDist = 30f;
                if (aiEntityModel.AITargetPos != null)
                {
                    //entityProbe.Position = MathConverter.Convert(aiTargetPos.Value);

                    float distToTargetPos = Vector3.Distance(aiEntityModel.AITargetPos.Value, MathConverter.Convert(aiEntity.Position));
                    if (distToTargetPos >= targetPosReachedMinDist)
                    {
                        BEPUutilities.Quaternion q;

                        BEPUutilities.Vector3 currentDirection = aiEntity.OrientationMatrix.Forward;
                        BEPUutilities.Vector3 currentPosition = aiEntity.Position;
                        BEPUutilities.Vector3 positionToFace = MathConverter.Convert(aiEntityModel.AITargetPos.Value);
                        BEPUutilities.Vector3 difference = BEPUutilities.Vector3.Normalize(positionToFace - currentPosition);
                        BEPUutilities.Quaternion rot;
                        BEPUutilities.Quaternion.GetQuaternionBetweenNormalizedVectors(ref currentDirection, ref difference, out rot);
                        aiEntity.AngularVelocity = EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(aiEntity.WorldTransform.Forward, 0), rot, 3.0f);
                        aiEntity.LinearVelocity = aiEntity.WorldTransform.Forward * 15;
                    }
                    else
                    {
                        // Target pos reached
                        aiEntityModel.AITargetPos = null;
                    }
                }
            }
        }

        public static float Max(params float[] values)
        {
            float maxValue = 0f;
            foreach (float val in values)
            {
                maxValue = Math.Max(val, maxValue);
            }
            return maxValue;
        }

        public Vector3? GetRandomNonCollidingPoint(float requestedPlacementRadius, Vector3 targetAreaCenter, int targetAreaRadius, int maxPlacementAttempts, Random r)
        {
            IList<BroadPhaseEntry> overlaps = new List<BroadPhaseEntry>();
            for (int j = 0; j < maxPlacementAttempts; j++)
            {
                BEPUutilities.Vector3 pos = new BEPUutilities.Vector3(targetAreaCenter.X + r.Next(-targetAreaRadius / 2, targetAreaRadius / 2), r.Next(-targetAreaRadius / 2, targetAreaRadius / 2), r.Next(-targetAreaRadius / 2, targetAreaRadius / 2));
                space.BroadPhase.QueryAccelerator.GetEntries(new BEPUutilities.BoundingBox(new BEPUutilities.Vector3(pos.X - requestedPlacementRadius, pos.Y - requestedPlacementRadius, pos.Z - requestedPlacementRadius), new BEPUutilities.Vector3(pos.X + requestedPlacementRadius, pos.Y + requestedPlacementRadius, pos.Z + requestedPlacementRadius)), overlaps);
                if (overlaps.Count == 0) return MathConverter.Convert(pos);
            }
            return null;
        }

        private void GenerateNPCs()
        {
            List<NPCData> npcTypes = new List<NPCData>();
            npcTypes.Add(new NPCData() { model = modelShip, scale = 0.001f });
            //npcTypes.Add(new NPCData() { model = modelClaw, scale = 0.05f });
            //npcTypes.Add(new NPCData() { model = modelBuckRogers, scale = 0.05f });
            //npcTypes.Add(new NPCData() { model = modelCygnus, scale = 0.1f });
            //npcTypes.Add(new NPCData() { model = modelBarracuda, scale = 0.3f });

            Random r = new Random();
            int maxNPCs = 10, npcTypeIndex = 0, maxRange = 2000;
            for (int i = 0; i < maxNPCs; i++)
            {
                NPCData npcData = npcTypes[npcTypeIndex++];
                if (npcTypeIndex == npcTypes.Count) npcTypeIndex = 0;

                Vector3 modelSize = GetModelSize(npcData.model, npcData.scale);
                Vector3? pos = GetRandomNonCollidingPoint(Max(modelSize.X,modelSize.Y,modelSize.Z), Vector3.Zero, maxRange, 10, r);
                if (pos != null)
                {
                    AddEntity(space, new Box(MathConverter.Convert(pos.Value), modelSize.X, modelSize.Y, modelSize.Z, 200), npcData.model, npcData.scale, GameModelType.ShipNPC, 0, 0, typeof(EntityModel));
                }
            }

        }

        private void GenerateAsteroids()
        {
            Program.ConsoleWindow.Log("Generating asteroids...");
            Program.ConsoleWindow.TimerStart();
            IList<BroadPhaseEntry> overlaps = new List<BroadPhaseEntry>();

            int maxAsteroids = 1000, maxRange = 2000, asteroidsCreated = 0;
            float minRadius = 0.5f, maxRadius = 10f;
            Random r = new Random();
            for (int i = 0; i < maxAsteroids; i++)
            {
                float radius = ((float)r.NextDouble() * (maxRadius - minRadius)) + minRadius;
                Vector3? pos = GetRandomNonCollidingPoint(radius, Vector3.Zero, maxRange, 10, r);
                if (pos != null)
                {
                    AddEntity(space, new Sphere(MathConverter.Convert(pos.Value), radius, 1000), modelAsteroid, radius * 5.0f, GameModelType.Asteroid, 3, 3, typeof(EntityModel));
                    asteroidsCreated++;
                }
            }
            Program.ConsoleWindow.Log(asteroidsCreated + " asteroids created in " + Program.ConsoleWindow.TimerStop().TotalSeconds + " sec");
        }

        ParticleSystem explosionParticles;
        ParticleSystem explosionSmokeParticles;
        ParticleSystem projectileTrailParticles;
        ParticleSystem smokePlumeParticles;
        ParticleSystem fireParticles;
        ParticleSystem explosionSmallParticleSystem, explosionMediumParticleSystem, explosionLargeParticleSystem;

        void HandleCollision(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
        {
            //This type of event can occur when an entity hits any other object which can be collided with.
            //They aren't always entities; for example, hitting a StaticMesh would trigger this.
            //Entities use EntityCollidables as collision proxies; see if the thing we hit is one.
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                if (sender.Entity == entityEarth)
                {
                    if (otherEntityInformation.Entity.Tag is EntityModel && ((EntityModel)otherEntityInformation.Entity.Tag).GameModelType == GameModelType.Asteroid)
                    {
                        //We hit an entity! remove it.
                        space.Remove(otherEntityInformation.Entity);
                        //Remove the graphics too.
                        Components.Remove((EntityModel)otherEntityInformation.Entity.Tag);

                        //CommandWriter.Reset();
                        //CommandWriter.WriteCommand(Commands.RemoveEntity);
                        //CommandWriter.WriteData(((EntityModel)otherEntityInformation.Entity.Tag).ID);
                        //Server.Broadcast(CommandWriter.GetBytes());

                        //QueueExplosion(GetRandomPosition(MathConverter.Convert(otherEntityInformation.Entity.Position), 10), 5, 0);
                        //QueueExplosion(GetRandomPosition(MathConverter.Convert(otherEntityInformation.Entity.Position), 10), 5, 0.1f);
                        //QueueExplosion(GetRandomPosition(MathConverter.Convert(otherEntityInformation.Entity.Position), 10), 5, 0.2f);
                        ShowExplosion(MathConverter.Convert(otherEntityInformation.Entity.Position), ExplosionSize.Large);

                        //AddEntity(space, new Sphere(new BEPUutilities.Vector3(0, 1000, 21000), 100, 1000), modelAsteroid, 500f, typeof(EntityModel));
                        //AddEntity(space, new Sphere(new BEPUutilities.Vector3(0, 1000, 21000), 100, 1000), modelAsteroid, 500f, typeof(EntityModel));
                    }
                }
                else if (sender.Entity == entityShip)
                {
                    if (otherEntityInformation.Entity.Tag is EntityModel && ((EntityModel)otherEntityInformation.Entity.Tag).GameModelType == GameModelType.Asteroid)
                    {
                        EntityModel parent = ((EntityModel)(otherEntityInformation.Entity.Tag));
                        parent.Strength--;

                        if (parent.Strength <= 0)
                        {
                            space.Remove(otherEntityInformation.Entity);
                            Components.Remove((EntityModel)otherEntityInformation.Entity.Tag);

                            QueueExplosion(MathConverter.Convert(otherEntityInformation.Entity.Position), ExplosionSize.Medium, 0);

                            if (parent.MaxDestructionDivision > 0)
                            {
                                Vector3 pos = MathConverter.Convert(otherEntityInformation.Entity.Position+(otherEntityInformation.Entity.WorldTransform.Up*5));
                                Entity newEntity = AddEntity(space, new Sphere(MathConverter.Convert(pos), ((Sphere)otherEntityInformation.Entity).Radius / 2.0f, 1000), modelAsteroid, parent.Scale / 2.0f, GameModelType.Asteroid, 1, parent.MaxDestructionDivision-1, typeof(EntityModel));
                                newEntity.LinearVelocity = otherEntityInformation.Entity.WorldTransform.Up * 0.5f;

                                pos = MathConverter.Convert(otherEntityInformation.Entity.Position + (otherEntityInformation.Entity.WorldTransform.Down * 5));
                                newEntity = AddEntity(space, new Sphere(MathConverter.Convert(pos), ((Sphere)otherEntityInformation.Entity).Radius / 2.0f, 1000), modelAsteroid, parent.Scale / 2.0f, GameModelType.Asteroid, 1, parent.MaxDestructionDivision - 1, typeof(EntityModel));
                                newEntity.LinearVelocity = otherEntityInformation.Entity.WorldTransform.Down * 0.5f;
                            }
                        }
                    }
                }
                else if (sender.Entity.Tag is ProjectileModel)
                {
                    ((ProjectileModel)sender.Entity.Tag).DestroyNow = true;
                    if (otherEntityInformation.Entity == entityEarth) ShowExplosion(MathConverter.Convert(sender.Entity.Position), ExplosionSize.Large);
                    else ShowExplosion(MathConverter.Convert(sender.Entity.Position), ExplosionSize.Small);
                }
            }
        }

        private List<QueuedExplosion> QueuedExplosions = new List<QueuedExplosion>();
        public void QueueExplosion(Vector3 position, ExplosionSize size, float delaySeconds)
        {
            QueuedExplosions.Add(new QueuedExplosion() { Position = position, Size = size, DetonationTime = DateTime.Now.AddSeconds(delaySeconds) });
        }
        private void ProcessQueuedExplosions()
        {
            for (int i = QueuedExplosions.Count - 1; i >= 0; i--)
            {
                QueuedExplosion explosion = QueuedExplosions[i];
                if (DateTime.Now >= explosion.DetonationTime)
                {
                    ShowExplosion(explosion.Position, explosion.Size);
                    QueuedExplosions.RemoveAt(i);
                }
            }
        }
        public void ShowExplosion(Vector3 position, ExplosionSize size)
        {
            int numExplosionParticles = 5;
            int numExplosionSmokeParticles = 50;
            Random r = new Random();
            ParticleSystem particleSystem = explosionSmallParticleSystem;
            if (size == ExplosionSize.Medium) particleSystem = explosionMediumParticleSystem;
            else if (size == ExplosionSize.Large) particleSystem = explosionLargeParticleSystem;

            for (int i = 0; i < numExplosionParticles; i++)
            {
                //explosionParticles.AddParticle(position, new Vector3(0.01f,0.01f,0.01f));
                //explosionParticles.AddParticle(position, new Vector3(r.Next(-strength, strength), r.Next(-strength, strength), r.Next(-strength, strength)));

                particleSystem.AddParticle(position, new Vector3(2, 2, 2));
            }

            //for (int i = 0; i < numExplosionSmokeParticles; i++)
                //explosionSmokeParticles.AddParticle(position, new Vector3(r.Next(-strength / 2, strength/2), r.Next(-strength / 2, strength / 2), r.Next(-strength / 2, strength / 2)));
        }
        public Vector3 GetRandomPosition(Vector3 center, int maxRange)
        {
            Random r = new Random();
            return new Vector3((float)r.Next((int)center.X - maxRange, (int)center.X + maxRange), (float)r.Next((int)center.Y - maxRange, (int)center.Y + maxRange), (float)r.Next((int)center.Z - maxRange, (int)center.Z + maxRange));
        }

        private Entity AddEntity(Space space, Entity entity, Model model, float scale, GameModelType gameModelType, int strength, int destructionDivisions, Type entityType)
        {
            return AddEntity(space, entity, model, Matrix.CreateScale(scale, scale, scale), gameModelType, strength, destructionDivisions, entityType);
        }

        private Vector3 GetModelSize(Model model, float scale)
        {
            BEPUutilities.Vector3[] vertices;
            int[] indices;
            Vector3 modelMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3 modelMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            ModelDataExtractor.GetVerticesAndIndicesFromModel(model, out vertices, out indices);
            for (int i = 0; i < vertices.Length; i++)
            {
                modelMin = Vector3.Min(modelMin, MathConverter.Convert(vertices[i]));
                modelMax = Vector3.Max(modelMax, MathConverter.Convert(vertices[i]));
            }
            BoundingBox b = new BoundingBox(modelMin, modelMax);

            float width = Math.Abs(b.Min.X - b.Max.X) * scale;
            float height = Math.Abs(b.Min.Y - b.Max.Y) * scale;
            float length = Math.Abs(b.Min.Z - b.Max.Z) * scale;
            return new Vector3(width, height, length);
        }

        private Entity AddEntity(Space space, Entity entity, Model model, Matrix scale, GameModelType gameModelType, int strength, int destructionDivisions, Type entityType)
        {
            int entityID = ++CURID;
            space.Add(entity);
            DrawableGameComponent entityModel = null;
            if (entityType == typeof(EntityModel))
            {
                if (entity is ConvexHull) entityModel = new EntityModel(entity, model, MathConverter.Convert(scale) * BEPUutilities.Matrix.CreateTranslation(-entity.Position), this);
                else
                {
                    entityModel = new EntityModel(entity, model, MathConverter.Convert(scale), this);
                }

                ((EntityModel)entityModel).GameModelType = gameModelType;
                ((EntityModel)entityModel).Scale = scale.M11;
                ((EntityModel)entityModel).Strength = strength;
                ((EntityModel)entityModel).MaxDestructionDivision = destructionDivisions;
                ((EntityModel)entityModel).ID = entityID;
                entity.Tag = entityModel;

                if (gameModelType == GameModelType.Asteroid)
                {
                    Random r = new Random();
                    entity.AngularDamping = 0;
                    entity.LinearDamping = 0;
                    float xRotRnd = ((float)r.NextDouble() * (float)(r.Next(1, 3) == 1 ? 1 : -1));
                    float yRotRnd = ((float)r.NextDouble() * (float)(r.Next(1, 3) == 1 ? 1 : -1));
                    float zRotRnd = ((float)r.NextDouble() * (float)(r.Next(1, 3) == 1 ? 1 : -1));
                    entity.AngularVelocity = new BEPUutilities.Vector3(xRotRnd, yRotRnd, zRotRnd);
                }
                else if (gameModelType == GameModelType.ShipNPC) AIEntities.Add(entity);
            }
            else if (entityType == typeof(PlanetModel))
            {
                entityModel = new PlanetModel(entity, model, MathConverter.Convert(scale), this);
                entity.Tag = entityModel;
                ((PlanetModel)entityModel).ID = entityID;
            }
            else if (entityType == typeof(ProjectileModel))
            {
                //entityModel = new ProjectileModel(entity, model, MathConverter.Convert(Matrix.CreateScale(scale, scale, scale)), this, explosionParticles, explosionSmokeParticles, projectileTrailParticles);
                entityModel = new ProjectileModel(entity, model, MathConverter.Convert(scale), this, explosionParticles, explosionSmokeParticles, explosionSmallParticleSystem);
                ((ProjectileModel)entityModel).ID = entityID;
                entity.Tag = entityModel;
                entity.CollisionInformation.Events.InitialCollisionDetected += HandleCollision;
                entity.Orientation = entityShip.Orientation;
            }
            Components.Add(entityModel);

            //Console.WriteLine("SERVER SEND: RegisterEntity " + entityID + "," + gameModelType.ToString());
            CommandWriter cw = new SpaceSimLibrary.Networking.CommandWriter();
            cw.WriteCommand(Commands.RegisterEntity);
            cw.WriteData(entityID);
            cw.WriteData((byte)gameModelType);
            cw.WriteMatrix(scale);
            cw.WriteMatrix(MathConverter.Convert(entity.WorldTransform));
            lock (CommandWriterQueue)
            {
                CommandWriterQueue.Enqueue(cw);
            }

            return entity;
        }

        private static int CURID = 0;
        private Queue<CommandWriter> CommandWriterQueue = new Queue<CommandWriter>();
        //private CommandWriter CommandWriter = new CommandWriter();
        //private CommandWriter CommandWriterBroadcast = new CommandWriter();

        private float ShipWeaponFireReloadDuration = 0.1f;
        private DateTime ShipWeaponFired = DateTime.MinValue;
        private bool ShipWeaponAvailable { get { return (DateTime.Now - ShipWeaponFired).TotalSeconds >= ShipWeaponFireReloadDuration; } }
        public List<Entity> projectiles = new List<Entity>();

        private void FireWeapon()
        {
            if (ShipWeaponAvailable)
            {
                ShipWeaponFired = DateTime.Now;
                Vector3 pos = MathConverter.Convert(entityShip.Position + (entityShip.WorldTransform.Forward * 1.3f));
                Entity projectile = AddEntity(space, new Cylinder(MathConverter.Convert(pos), 0.5f, 0.1f, 0.01f), modelLaser, Matrix.CreateScale(0.02f, 0.02f, 0.01f), GameModelType.Projectile, 3, 3, typeof(ProjectileModel));
                projectile.LinearVelocity = entityShip.WorldTransform.Forward * (40f + ShipSpeedMax);
                projectiles.Add(projectile);
                //projectiles.Add(new Projectile(MathConverter.Convert(entityShip.Position), MathConverter.Convert(entityShip.WorldTransform.Forward*10), explosionParticles, explosionSmokeParticles, projectileTrailParticles));
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        private const float DEFAULT_MOUSE_SMOOTHING_SENSITIVITY = 0.5f;
        private const int MOUSE_SMOOTHING_CACHE_SIZE = 10;

        private int savedMousePosX;
        private int savedMousePosY;
        private int mouseIndex;
        private float mouseSmoothingSensitivity;
        private Vector2[] mouseMovement;
        private Vector2[] mouseSmoothingCache;
        private Vector2 smoothedMouseMovement;
        private MouseState currentMouseState;
        private MouseState previousMouseState;

        private int ShipSpeedIndex = 0;
        private float[] ShipSpeeds = { -30, -15, 0, 5, 10, 25, 40, 60 };
        private int ShipSpeedIndexZero { get { for (int i = 0; i < ShipSpeeds.Length; i++) { if (ShipSpeeds[i] == 0) return i; } return 0; } }
        private float ShipSpeed { get { return ShipSpeeds[ShipSpeedIndex]; } }
        private float ShipSpeedMax { get { return ShipSpeeds[ShipSpeeds.Length - 1]; } }
        private void ShipSpeedIncrease()
        {
            ShipSpeedIndex++;
            if (ShipSpeedIndex >= ShipSpeeds.Length) ShipSpeedIndex = ShipSpeeds.Length-1;
        }
        private void ShipSpeedDecrease()
        {
            ShipSpeedIndex--;
            if (ShipSpeedIndex < 0) ShipSpeedIndex = 0;
        }
        private void ShipSpeedZero()
        {
            ShipSpeedIndex = ShipSpeedIndexZero;
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            previousKeyboardState = currentKeyboardState;
            previousMouseState = currentMouseState;

            currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();

            // Exit when the Escape key or Back button is pressed
            if (currentKeyboardState.IsKeyDown(Keys.Escape))
            {
                Exit();
            }


            // Pressing the A button or key toggles the spring behavior on and off
            if (previousKeyboardState.IsKeyUp(Keys.P) && (currentKeyboardState.IsKeyDown(Keys.P)))
            {
                Paused = !Paused;
                IsMouseVisible = Paused;
            }

            //if (Paused) return;

            if (!Paused && graphics.GameDisplayType == GameDisplayType.Cockpit)
            {

                if (currentKeyboardState.IsKeyDown(Keys.Space))
                {
                    FireWeapon();
                }

                if (!Paused)
                {
                    Rectangle clientBounds = Window.ClientBounds;

                    int centerX = clientBounds.Width / 2;
                    int centerY = clientBounds.Height / 2;
                    int deltaX = centerX - currentMouseState.X;
                    int deltaY = centerY - currentMouseState.Y;

                    Mouse.SetPosition(centerX, centerY);

                    PerformMouseFiltering((float)deltaX, (float)deltaY);
                    PerformMouseSmoothing(smoothedMouseMovement.X, smoothedMouseMovement.Y);

                    float dx = smoothedMouseMovement.X;
                    float dy = smoothedMouseMovement.Y;

                    if (dx != 0) entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Up, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Up, dx * 0.01f), 1.0f);
                    if (dy != 0) entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Left, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Left, dy * -0.01f), 1.0f);

                    if (currentMouseState.LeftButton == ButtonState.Pressed) entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Forward, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Forward, -0.15f), 1.0f);
                    if (currentMouseState.RightButton == ButtonState.Pressed) entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Forward, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Forward, 0.15f), 1.0f);

                    float maxForwardSpeed = 50;
                    float maxReverseSpeed = -50;
                    int speedIncrement = 2;
                    float mouseWheelDirection = GetMouseWheelDirection();
                    if (mouseWheelDirection > 0) ShipSpeedIncrease();
                    else if (mouseWheelDirection < 0) ShipSpeedDecrease();
                    else if (currentMouseState.MiddleButton == ButtonState.Pressed) ShipSpeedZero();
                    entityShip.LinearVelocity = entityShip.WorldTransform.Forward * ShipSpeed;
                }

                space.Update();
            }
            // Update the camera to chase the new target
            UpdateCameraChaseTarget();

            // The chase camera's update behavior is the springs, but we can
            // use the Reset method to have a locked, spring-less camera
            if (cameraSpringEnabled)
                camera.Update(gameTime);
            else
                camera.Reset();

            ProcessQueuedExplosions();


            UpdateProjectiles(gameTime);
            UpdateFrameRate(gameTime);
            UpdateAI(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// Helper for updating the list of active projectiles.
        /// </summary>
        void UpdateProjectiles(GameTime gameTime)
        {
            int i = 0;

            float maxDistanceToSelfDestruct = 1000f;
            while (i < projectiles.Count)
            {
                Entity projectile = projectiles[i];
                ProjectileModel projectileModel = (ProjectileModel)projectile.Tag;
                if (projectileModel.DestroyNow || projectileModel.DistanceTravelled >= maxDistanceToSelfDestruct)
                {
                    // Remove projectiles at the end of their life.
                    space.Remove(projectile);
                    Components.Remove(projectileModel);
                    projectiles.RemoveAt(i);
                    if (!projectileModel.DestroyNow) ShowExplosion(MathConverter.Convert(projectile.Position), ExplosionSize.Small);

                    //CommandWriter.Reset();
                    //CommandWriter.WriteCommand(Commands.RemoveEntity);
                    //CommandWriter.WriteData(projectileModel.ID);
                    //Server.Broadcast(CommandWriter.GetBytes());
                }
                else
                {
                    // Advance to the next projectile.
                    i++;
                }
            }
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

        Vector2 infoFontPos = new Vector2(1.0f, 1.0f);

        RenderTarget2D renderTarget;
        Texture2D renderTexture;
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            //if (renderTarget == null) renderTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            //GraphicsDevice.SetRenderTarget(renderTarget);

            GraphicsDevice.Clear(Color.Black);

            //bloom.BeginDraw();

            RasterizerState originalRasterizerState = graphics.GraphicsDevice.RasterizerState;
            RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            graphics.GraphicsDevice.RasterizerState = rasterizerState;
            skybox.Draw(camera.View, camera.Projection, camera.Position);
            graphics.GraphicsDevice.RasterizerState = originalRasterizerState;
            
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            GraphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

            //DrawModel(modelShip, ship.World);
            //DrawModel(modelEarth, ship.World);
            //DrawEarth();

            // Pass camera matrices through to the particle system components.
            explosionParticles.SetCamera(camera.View, camera.Projection);
            explosionSmokeParticles.SetCamera(camera.View, camera.Projection);
            projectileTrailParticles.SetCamera(camera.View, camera.Projection);
            smokePlumeParticles.SetCamera(camera.View, camera.Projection);
            fireParticles.SetCamera(camera.View, camera.Projection);
            explosionSmallParticleSystem.SetCamera(camera.View, camera.Projection);
            explosionMediumParticleSystem.SetCamera(camera.View, camera.Projection);
            explosionLargeParticleSystem.SetCamera(camera.View, camera.Projection);

            // TODO: Add your drawing code here
            base.Draw(gameTime);

            /*
            effectBlur.CurrentTechnique = effectBlur.Techniques["Blur"];

            spriteBatch.Begin();
            //spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            GraphicsDevice.BlendState = BlendState.Additive;
            foreach (EffectPass pass in effectBlur.CurrentTechnique.Passes)
            {
                pass.Apply();
                spriteBatch.DrawString(spriteFont, "FPS: " + framesPerSecond, fpsFontPos, Color.Yellow);
            }
            */

            DrawCockpit(gameTime);

            ///*
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.DrawString(spriteFont, "FPS: " + framesPerSecond + "\nShip Speed: " + ShipSpeed, infoFontPos, Color.Yellow);
            // */
            spriteBatch.End();

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            /*
            GraphicsDevice.SetRenderTarget(null);
            renderTexture = (Texture2D)renderTarget;

            spriteBatch.Begin();
            spriteBatch.Draw(renderTexture, new Vector2(0, 0), null, Color.White, 0, new Vector2(0, 0), 0.4f, SpriteEffects.None, 1);
            spriteBatch.End();

            if (SecondaryGame != null && SecondaryGame.Initialized)
            {
                Color[] colors = new Color[renderTexture.Width * renderTexture.Height];
                renderTexture.GetData(colors);
                SecondaryGame.RenderTexture(colors, renderTexture.Width, renderTexture.Height);
                //SecondaryGame.RenderTexture(renderTarget);
            }
            */
            IncrementFrameCounter();
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
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            spriteBatch.Draw(textureCockpit, new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height), Color.White);
            //spriteBatch.Draw(textureCockpit, new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height), null, Color.White, 0, Vector2.Zero, SpriteEffects.None, 1);
            spriteBatch.End();
        }

        /// <summary>
        /// Update the values to be chased by the camera
        /// </summary>
        private void UpdateCameraChaseTarget()
        {
            //camera.ChasePosition = ship.Position;
            //camera.ChaseDirection = ship.Direction;
            //camera.Up = ship.Up;
            //entityShip.
            camera.ChasePosition = MathConverter.Convert(entityShip.Position);
            camera.ChaseDirection = MathConverter.Convert(entityShip.WorldTransform.Forward);
            camera.Up = MathConverter.Convert(entityShip.WorldTransform.Up);
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

    public struct Sunlight
    {
        public Vector4 direction;
        public Vector4 color;
    }

    public class QueuedExplosion
    {
        public DateTime DetonationTime;
        public Vector3 Position;
        public ExplosionSize Size;
    }

    public class RenderCapture
    {
        RenderTarget2D renderTarget;
        GraphicsDevice graphicsDevice;
        public RenderCapture(GraphicsDevice GraphicsDevice)
        {
            this.graphicsDevice = GraphicsDevice;
            renderTarget = new RenderTarget2D(GraphicsDevice,
            GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height,
            false, SurfaceFormat.Color, DepthFormat.Depth24);
        }
        // Begins capturing from the graphics device
        public void Begin()
        {
            graphicsDevice.SetRenderTarget(renderTarget);
        }
        // Stop capturing
        public void End()
        {
            graphicsDevice.SetRenderTarget(null);
        }
        // Returns what was captured
        public Texture2D GetTexture()
        {
            return renderTarget;
        }
    }

    public class PostProcessor
    {
        // Pixel shader
        public Effect Effect { get; protected set; }

        // Texture to process
        public Texture2D Input { get; set; }

        // GraphicsDevice and SpriteBatch for drawing
        protected GraphicsDevice graphicsDevice;
        protected static SpriteBatch spriteBatch;

        public PostProcessor(Effect Effect, GraphicsDevice graphicsDevice)
        {
            this.Effect = Effect;

            if (spriteBatch == null)
                spriteBatch = new SpriteBatch(graphicsDevice);

            this.graphicsDevice = graphicsDevice;
        }

        // Draws the input texture using the pixel shader postprocessor
        public virtual void Draw()
        {
            // Set effect parameters if necessary
            if (Effect.Parameters["ScreenWidth"] != null)
                Effect.Parameters["ScreenWidth"].SetValue(
                    graphicsDevice.Viewport.Width);

            if (Effect.Parameters["ScreenHeight"] != null)
                Effect.Parameters["ScreenHeight"].SetValue(
                    graphicsDevice.Viewport.Height);

            // Initialize the spritebatch and effect
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            Effect.CurrentTechnique.Passes[0].Apply();

            // For reach compatibility
            graphicsDevice.SamplerStates[0] = SamplerState.AnisotropicClamp;

            // Draw the input texture
            spriteBatch.Draw(Input, Vector2.Zero, Color.White);

            // End the spritebatch and effect
            spriteBatch.End();

            // Clean up render states changed by the spritebatch
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphicsDevice.BlendState = BlendState.Opaque;
        }
    }

    public class GaussianBlur : PostProcessor
    {
        float blurAmount;

        float[] weightsH, weightsV;
        Vector2[] offsetsH, offsetsV;

        RenderCapture capture;

        public GaussianBlur(GraphicsDevice graphicsDevice, ContentManager Content,
            float BlurAmount)
            : base(Content.Load<Effect>("GaussianBlur"), graphicsDevice)
        {
            this.blurAmount = BlurAmount;

            // Calculate weights/offsets for horizontal pass
            calcSettings(1.0f / (float)graphicsDevice.Viewport.Width, 0,
                out weightsH, out offsetsH);

            // Calculate weights/offsets for vertical pass
            calcSettings(0, 1.0f / (float)graphicsDevice.Viewport.Height,
                out weightsV, out offsetsV);

            capture = new RenderCapture(graphicsDevice);
        }

        void calcSettings(float w, float h,
            out float[] weights, out Vector2[] offsets)
        {
            // 15 Samples
            weights = new float[15];
            offsets = new Vector2[15];

            // Calulate values for center pixel
            weights[0] = gaussianFn(0);
            offsets[0] = new Vector2(0, 0);

            float total = weights[0];

            // Calculate samples in pairs
            for (int i = 0; i < 7; i++)
            {
                // Weight each pair of samples according to Gaussian function
                float weight = gaussianFn(i + 1);
                weights[i * 2 + 1] = weight;
                weights[i * 2 + 2] = weight;
                total += weight * 2;

                // Samples are offset by 1.5 pixels, to make use of
                // filtering halfway between pixels
                float offset = i * 2 + 1.5f;
                Vector2 offsetVec = new Vector2(w, h) * offset;
                offsets[i * 2 + 1] = offsetVec;
                offsets[i * 2 + 2] = -offsetVec;
            }

            // Divide all weights by total so they will add up to 1
            for (int i = 0; i < weights.Length; i++)
                weights[i] /= total;
        }

        float gaussianFn(float x)
        {
            return (float)((1.0f / Math.Sqrt(2 * Math.PI * blurAmount * blurAmount)) *
                Math.Exp(-(x * x) / (2 * blurAmount * blurAmount)));
        }

        public override void Draw()
        {
            // Set values for horizontal pass
            Effect.Parameters["Offsets"].SetValue(offsetsH);
            Effect.Parameters["Weights"].SetValue(weightsH);

            // Render this pass into the RenderCapture
            capture.Begin();
            base.Draw();
            capture.End();

            // Get the results of the first pass
            Input = capture.GetTexture();

            // Set values for the vertical pass
            Effect.Parameters["Offsets"].SetValue(offsetsV);
            Effect.Parameters["Weights"].SetValue(weightsV);

            // Render the final pass
            base.Draw();
        }
    }

    public class CModel
    {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }

        public Model Model { get; private set; }

        private Matrix[] modelTransforms;
        private GraphicsDevice graphicsDevice;
        private BoundingSphere boundingSphere;

        public BoundingSphere BoundingSphere
        {
            get
            {
                // No need for rotation, as this is a sphere
                Matrix worldTransform = Matrix.CreateScale(Scale)
                    * Matrix.CreateTranslation(Position);

                BoundingSphere transformed = boundingSphere;
                transformed = transformed.Transform(worldTransform);

                return transformed;
            }
        }

        public CModel(Model Model, Vector3 Position, Vector3 Rotation,
            Vector3 Scale, GraphicsDevice graphicsDevice)
        {
            this.Model = Model;

            modelTransforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(modelTransforms);

            buildBoundingSphere();
            generateTags();

            this.Position = Position;
            this.Rotation = Rotation;
            this.Scale = Scale;

            this.graphicsDevice = graphicsDevice;
        }

        private void buildBoundingSphere()
        {
            BoundingSphere sphere = new BoundingSphere(Vector3.Zero, 0);

            // Merge all the model's built in bounding spheres
            foreach (ModelMesh mesh in Model.Meshes)
            {
                BoundingSphere transformed = mesh.BoundingSphere.Transform(
                    modelTransforms[mesh.ParentBone.Index]);

                sphere = BoundingSphere.CreateMerged(sphere, transformed);
            }

            this.boundingSphere = sphere;
        }

        public void Draw(Matrix View, Matrix Projection, Vector3 CameraPosition) { Draw(View, Projection, CameraPosition, null); }
        public void Draw(Matrix View, Matrix Projection, Vector3 CameraPosition, Matrix? _baseWorld)
        {
            // Calculate the base transformation by combining
            // translation, rotation, and scaling
            Matrix baseWorld = _baseWorld != null ? _baseWorld.Value : (Matrix.CreateScale(Scale)
                * Matrix.CreateFromYawPitchRoll(
                    Rotation.Y, Rotation.X, Rotation.Z)
                * Matrix.CreateTranslation(Position));

            foreach (ModelMesh mesh in Model.Meshes)
            {
                Matrix localWorld = modelTransforms[mesh.ParentBone.Index]
                    * baseWorld;

                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    Effect effect = meshPart.Effect;

                    if (effect is BasicEffect)
                    {
                        ((BasicEffect)effect).World = localWorld;
                        ((BasicEffect)effect).View = View;
                        ((BasicEffect)effect).Projection = Projection;
                        ((BasicEffect)effect).EnableDefaultLighting();
                    }
                    else
                    {
                        setEffectParameter(effect, "World", localWorld);
                        setEffectParameter(effect, "View", View);
                        setEffectParameter(effect, "Projection", Projection);
                        setEffectParameter(effect, "CameraPosition", CameraPosition);
                    }

                    ((MeshTag)meshPart.Tag).Material.SetEffectParameters(effect);
                }

                mesh.Draw();
            }
        }

        // Sets the specified effect parameter to the given effect, if it
        // has that parameter
        void setEffectParameter(Effect effect, string paramName, object val)
        {
            if (effect.Parameters[paramName] == null)
                return;

            if (val is Vector3)
                effect.Parameters[paramName].SetValue((Vector3)val);
            else if (val is bool)
                effect.Parameters[paramName].SetValue((bool)val);
            else if (val is Matrix)
                effect.Parameters[paramName].SetValue((Matrix)val);
            else if (val is Texture2D)
                effect.Parameters[paramName].SetValue((Texture2D)val);
        }

        public void SetModelEffect(Effect effect, bool CopyEffect)
        {
            foreach (ModelMesh mesh in Model.Meshes)
                SetMeshEffect(mesh.Name, effect, CopyEffect);
        }

        public void SetModelMaterial(Material material)
        {
            foreach (ModelMesh mesh in Model.Meshes)
                SetMeshMaterial(mesh.Name, material);
        }

        public void SetMeshEffect(string MeshName, Effect effect, bool CopyEffect)
        {
            foreach (ModelMesh mesh in Model.Meshes)
            {
                if (mesh.Name != MeshName)
                    continue;

                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    Effect toSet = effect;

                    // Copy the effect if necessary
                    if (CopyEffect)
                        toSet = effect.Clone();

                    MeshTag tag = ((MeshTag)part.Tag);

                    // If this ModelMeshPart has a texture, set it to the effect
                    if (tag.Texture != null)
                    {
                        setEffectParameter(toSet, "BasicTexture", tag.Texture);
                        setEffectParameter(toSet, "TextureEnabled", true);
                    }
                    else
                        setEffectParameter(toSet, "TextureEnabled", false);

                    // Set our remaining parameters to the effect
                    setEffectParameter(toSet, "DiffuseColor", tag.Color);
                    setEffectParameter(toSet, "SpecularPower", tag.SpecularPower);

                    part.Effect = toSet;
                }
            }
        }

        public void SetMeshMaterial(string MeshName, Material material)
        {
            foreach (ModelMesh mesh in Model.Meshes)
            {
                if (mesh.Name != MeshName)
                    continue;

                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                    ((MeshTag)meshPart.Tag).Material = material;
            }
        }

        private void generateTags()
        {
            foreach (ModelMesh mesh in Model.Meshes)
                foreach (ModelMeshPart part in mesh.MeshParts)
                    if (part.Effect is BasicEffect)
                    {
                        BasicEffect effect = (BasicEffect)part.Effect;
                        MeshTag tag = new MeshTag(effect.DiffuseColor,
                            effect.Texture, effect.SpecularPower);
                        part.Tag = tag;
                    }
        }

        // Store references to all of the model's current effects
        public void CacheEffects()
        {
            foreach (ModelMesh mesh in Model.Meshes)
                foreach (ModelMeshPart part in mesh.MeshParts)
                    ((MeshTag)part.Tag).CachedEffect = part.Effect;
        }

        // Restore the effects referenced by the model's cache
        public void RestoreEffects()
        {
            foreach (ModelMesh mesh in Model.Meshes)
                foreach (ModelMeshPart part in mesh.MeshParts)
                    if (((MeshTag)part.Tag).CachedEffect != null)
                        part.Effect = ((MeshTag)part.Tag).CachedEffect;
        }
    }

    public class MeshTag
    {
        public Vector3 Color;
        public Texture2D Texture;
        public float SpecularPower;
        public Effect CachedEffect = null;
        public Material Material = new Material();

        public MeshTag(Vector3 Color, Texture2D Texture, float SpecularPower)
        {
            this.Color = Color;
            this.Texture = Texture;
            this.SpecularPower = SpecularPower;
        }
    }

    public class Material
    {
        public virtual void SetEffectParameters(Effect effect)
        {
        }
    }

    public class LightingMaterial : Material
    {
        public Vector3 AmbientColor { get; set; }
        public Vector3 LightDirection { get; set; }
        public Vector3 LightColor { get; set; }
        public Vector3 SpecularColor { get; set; }

        public LightingMaterial()
        {
            AmbientColor = new Vector3(.1f, .1f, .1f);
            LightDirection = new Vector3(1, 1, 1);
            LightColor = new Vector3(.9f, .9f, .9f);
            SpecularColor = new Vector3(1, 1, 1);
        }

        public override void SetEffectParameters(Effect effect)
        {
            if (effect.Parameters["AmbientColor"] != null)
                effect.Parameters["AmbientColor"].SetValue(AmbientColor);

            if (effect.Parameters["LightDirection"] != null)
                effect.Parameters["LightDirection"].SetValue(LightDirection);

            if (effect.Parameters["LightColor"] != null)
                effect.Parameters["LightColor"].SetValue(LightColor);

            if (effect.Parameters["SpecularColor"] != null)
                effect.Parameters["SpecularColor"].SetValue(SpecularColor);
        }
    }

    public class NormalMapMaterial : LightingMaterial
    {
        public Texture2D NormalMap { get; set; }

        public NormalMapMaterial(Texture2D NormalMap)
        {
            this.NormalMap = NormalMap;
        }

        public override void SetEffectParameters(Effect effect)
        {
            base.SetEffectParameters(effect);

            if (effect.Parameters["NormalMap"] != null)
                effect.Parameters["NormalMap"].SetValue(NormalMap);
        }
    }

    public enum ExplosionSize
    {
        Small,
        Medium,
        Large
    }

    public class NPCData
    {
        public Model model;
        public float scale;
    }
}
