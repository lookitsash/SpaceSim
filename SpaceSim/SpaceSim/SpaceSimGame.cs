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

namespace SpaceSim
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class SpaceSimGame : Microsoft.Xna.Framework.Game
    {
        Space space;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont spriteFont;
        public static ConsoleWindow ConsoleWindow;

        KeyboardState previousKeyboardState = new KeyboardState();
        KeyboardState currentKeyboardState = new KeyboardState();

        Texture2D textureCockpit;

        //public static BloomPostprocess.BloomComponent bloom;

        //Ship ship;
        Earth earth;
        public static ChaseCamera camera = null;
        Skybox skybox;

        public static bool Paused = false;

        //StarfieldComponent starfieldComponent;
        //Starfield starfield;

        Model modelShip, modelEarth, modelAsteroid, modelLaser;

        public List<SpaceEntity> EntityCollection = new List<SpaceEntity>();

        private Vector4 globalAmbient;
        private Sunlight sunlight;

        bool cameraSpringEnabled = true;

        public SpaceSimGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            IsFixedTimeStep = false;

            //starfieldComponent = new StarfieldComponent(this);
            //Components.Add(starfieldComponent);

            (ConsoleWindow = new ConsoleWindow()).Show();
            ConsoleWindow.OnInput += new ConsoleWindow.ConsoleInputEventHandler(OnConsoleInput);

            ConsoleWindow.LogTimestamp = false;
            ConsoleWindow.Log("Welcome to my SpaceSim game in development.\r\nUse the keys below to navigate:\r\n");
            ConsoleWindow.Log("W - Pitch down");
            ConsoleWindow.Log("S - Pitch up");
            ConsoleWindow.Log("A - Turn left");
            ConsoleWindow.Log("D - Turn right");
            ConsoleWindow.Log("Q - Roll left");
            ConsoleWindow.Log("E - Roll right");
            ConsoleWindow.Log("Space - Move forwards");
            ConsoleWindow.Log("LeftCtrl - Move backwards");
            ConsoleWindow.Log("\r\nIf you fly into asteroids, they will move using realistic physics.\r\nIf you bump them too much, they will explode into smaller asteroids");
            ConsoleWindow.LogTimestamp = true;

            // Construct our particle system components.
            explosionParticles = new ExplosionParticleSystem(this, Content);
            explosionSmokeParticles = new ExplosionSmokeParticleSystem(this, Content);
            projectileTrailParticles = new ProjectileTrailParticleSystem(this, Content);
            smokePlumeParticles = new SmokePlumeParticleSystem(this, Content);
            fireParticles = new FireParticleSystem(this, Content);
            customParticleSystem = new CustomParticleSystem(this, Content);

            // Set the draw order so the explosions and fire
            // will appear over the top of the smoke.
            smokePlumeParticles.DrawOrder = 100;
            explosionSmokeParticles.DrawOrder = 200;
            projectileTrailParticles.DrawOrder = 300;
            explosionParticles.DrawOrder = 400;
            fireParticles.DrawOrder = 500;
            customParticleSystem.DrawOrder = 600;

            // Register the particle system components.
            Components.Add(explosionParticles);
            Components.Add(explosionSmokeParticles);
            Components.Add(projectileTrailParticles);
            Components.Add(smokePlumeParticles);
            Components.Add(fireParticles);
            Components.Add(customParticleSystem);

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

        private void OnConsoleInput(string str)
        {
            if (str == "exit" || str == "quit") Exit();
        }

        public SpaceEntity GetCollidingEntity(SpaceEntity source)
        {
            foreach (SpaceEntity target in EntityCollection)
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
            camera.FarPlaneDistance = 1000.0f;

            //EntityCollection.Add(ship = new Ship(GraphicsDevice, modelShip));
            //ship.Position = new Vector3(0, 0, 44000);

            EntityCollection.Add(earth = new Earth(GraphicsDevice, modelEarth));
            earth.LoadContent(Content);
            earth.Scale = 50;
            earth.Position = new Vector3(0, 0, -200);

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

        public static Entity entityShip, entityEarth, entityAsteroid;

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
            modelAsteroid = Content.Load<Model>("Models/asteroid");
            modelLaser = Content.Load<Model>("Models/Laser");

            /*
            BoundingSphere bounds = new BoundingSphere();
            foreach (ModelMesh mesh in modelEarth.Meshes)
                bounds = BoundingSphere.CreateMerged(bounds, mesh.BoundingSphere);
            float shipRadius = bounds.Radius;
            */
            skybox = new Skybox("Textures/Background1", Content);

            // TODO: use this.Content to load your game content here
            space = new Space();
            //Box ground = new Box(BEPUutilities.Vector3.Zero, 30, 1, 30);
            //space.Add(ground);

            entityShip = AddEntity(space, new Sphere(new BEPUutilities.Vector3(0, 10, 250), 1, 100), modelShip, .001f, GameModelType.Ship, 0, 0, typeof(EntityModel));
            entityShip.AngularDamping = 0.9f;
            //entityShip.LinearDamping = 0.9f;
            entityShip.LinearDamping = 0.0f;
            entityShip.CollisionInformation.Events.InitialCollisionDetected += HandleCollision;

            entityEarth = AddEntity(space, new Sphere(new BEPUutilities.Vector3(0, 0, -200), 205), modelEarth, 50f, GameModelType.Planet, 0, 0, typeof(PlanetModel)); ;
            entityEarth.CollisionInformation.Events.InitialCollisionDetected += HandleCollision;

            AddEntity(space, new Sphere(new BEPUutilities.Vector3(0, 10, 210), 2, 1000), modelAsteroid, 10f, GameModelType.Asteroid, 3, 3, typeof(EntityModel));
            AddEntity(space, new Sphere(new BEPUutilities.Vector3(-10, 0, 210), 2, 1000), modelAsteroid, 10f, GameModelType.Asteroid, 3, 3, typeof(EntityModel));
            AddEntity(space, new Sphere(new BEPUutilities.Vector3(10, 0, 210), 2, 1000), modelAsteroid, 10f, GameModelType.Asteroid, 3, 3, typeof(EntityModel));

            space.ForceUpdater.Gravity = new BEPUutilities.Vector3(0, 0, 0);

            //starfieldComponent.Generate(10000, 40000);

            /*
            //Go through the list of entities in the space and create a graphical representation for them.
            foreach (Entity e in space.Entities)
            {
                Box box = e as Box;
                Sphere sphere = e as Sphere;
                if (box != null) //This won't create any graphics for an entity that isn't a box since the model being used is a box.
                {
                    if (entityShip == null)
                    {
                        entityShip = box;
                        entityShip.AngularDamping = 0.9f;
                        entityShip.LinearDamping = 0.9f;
                    }

                    Matrix scaling = Matrix.CreateScale(0.1f, 0.1f, 0.1f); //Since the cube model is 1x1x1, it needs to be scaled to match the size of each individual box.
                    EntityModel model = new EntityModel(e, modelShip, MathConverter.Convert(scaling), this);
                    //Add the drawable game component for this entity to the game.
                    Components.Add(model);
                    e.Tag = model; //set the object tag of this entity to the model so that it's easy to delete the graphics component later if the entity is removed.
                }
                else if (sphere != null)
                {
                    Matrix scaling = Matrix.CreateScale(5000f, 5000f, 5000f); //Since the cube model is 1x1x1, it needs to be scaled to match the size of each individual box.
                    PlanetModel model = new PlanetModel(e, modelEarth, MathConverter.Convert(scaling), this);
                    //Add the drawable game component for this entity to the game.
                    Components.Add(model);
                    e.Tag = model; //set the object tag of this entity to the model so that it's easy to delete the graphics component later if the entity is removed.
                }
            }
            */
        }

        ParticleSystem explosionParticles;
        ParticleSystem explosionSmokeParticles;
        ParticleSystem projectileTrailParticles;
        ParticleSystem smokePlumeParticles;
        ParticleSystem fireParticles;
        ParticleSystem customParticleSystem;

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

                        QueueExplosion(GetRandomPosition(MathConverter.Convert(otherEntityInformation.Entity.Position), 10), 5, 0);
                        QueueExplosion(GetRandomPosition(MathConverter.Convert(otherEntityInformation.Entity.Position), 10), 5, 0.1f);
                        QueueExplosion(GetRandomPosition(MathConverter.Convert(otherEntityInformation.Entity.Position), 10), 5, 0.2f);

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

                            QueueExplosion(MathConverter.Convert(otherEntityInformation.Entity.Position), 5, 0);

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
                }
            }
        }

        private List<QueuedExplosion> QueuedExplosions = new List<QueuedExplosion>();
        public void QueueExplosion(Vector3 position, int strength, float delaySeconds)
        {
            QueuedExplosions.Add(new QueuedExplosion() { Position = position, Strength = strength, DetonationTime = DateTime.Now.AddSeconds(delaySeconds) });
        }
        private void ProcessQueuedExplosions()
        {
            for (int i = QueuedExplosions.Count - 1; i >= 0; i--)
            {
                QueuedExplosion explosion = QueuedExplosions[i];
                if (DateTime.Now >= explosion.DetonationTime)
                {
                    ShowExplosion(explosion.Position, explosion.Strength);
                    QueuedExplosions.RemoveAt(i);
                }
            }
        }
        public void ShowExplosion(Vector3 position, int strength)
        {
            int numExplosionParticles = 10;
            int numExplosionSmokeParticles = 50;
            Random r = new Random();
            for (int i = 0; i < numExplosionParticles; i++)
                //explosionParticles.AddParticle(position, new Vector3(0.01f,0.01f,0.01f));
                //explosionParticles.AddParticle(position, new Vector3(r.Next(-strength, strength), r.Next(-strength, strength), r.Next(-strength, strength)));

                customParticleSystem.AddParticle(position, new Vector3(r.Next(-strength, strength), r.Next(-strength, strength), r.Next(-strength, strength)));

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

        private Entity AddEntity(Space space, Entity entity, Model model, Matrix scale, GameModelType gameModelType, int strength, int destructionDivisions, Type entityType)
        {
            space.Add(entity);
            DrawableGameComponent entityModel = null;
            if (entityType == typeof(EntityModel))
            {
                entityModel = new EntityModel(entity, model, MathConverter.Convert(scale), this);
                ((EntityModel)entityModel).GameModelType = gameModelType;
                ((EntityModel)entityModel).Scale = scale.M11;
                ((EntityModel)entityModel).Strength = strength;
                ((EntityModel)entityModel).MaxDestructionDivision = destructionDivisions;
                entity.Tag = entityModel;

                if (gameModelType == GameModelType.Asteroid)
                {
                    entity.AngularDamping = 0;
                    entity.LinearDamping = 0;
                    float xRotRnd = (new Random().NextDouble() * new Random().Next(0, 1) == 1 ? 1 : -1);
                    float yRotRnd = (new Random().NextDouble() * new Random().Next(0, 1) == 1 ? 1 : -1);
                    float zRotRnd = (new Random().NextDouble() * new Random().Next(0, 1) == 1 ? 1 : -1);
                    entity.AngularVelocity = new BEPUutilities.Vector3(xRotRnd, yRotRnd, zRotRnd);
                }
            }
            else if (entityType == typeof(PlanetModel))
            {
                entityModel = new PlanetModel(entity, model, MathConverter.Convert(scale), this);
                entity.Tag = entityModel;
            }
            else if (entityType == typeof(ProjectileModel))
            {
                //entityModel = new ProjectileModel(entity, model, MathConverter.Convert(Matrix.CreateScale(scale, scale, scale)), this, explosionParticles, explosionSmokeParticles, projectileTrailParticles);
                entityModel = new ProjectileModel(entity, model, MathConverter.Convert(scale), this, explosionParticles, explosionSmokeParticles, customParticleSystem);
                entity.Tag = entityModel;
                entity.CollisionInformation.Events.InitialCollisionDetected += HandleCollision;
                entity.Orientation = entityShip.Orientation;
            }
            Components.Add(entityModel);

            return entity;
        }

        private float ShipSpeed = 0, ShipWeaponFireReloadDuration = 0.1f;
        private DateTime ShipWeaponFired = DateTime.MinValue;
        private bool ShipWeaponAvailable { get { return (DateTime.Now - ShipWeaponFired).TotalSeconds >= ShipWeaponFireReloadDuration; } }
        List<Entity> projectiles = new List<Entity>();

        private void FireWeapon()
        {
            if (ShipWeaponAvailable)
            {
                ShipWeaponFired = DateTime.Now;
                Vector3 pos = MathConverter.Convert(entityShip.Position + (entityShip.WorldTransform.Forward * 1.3f));
                Entity projectile = AddEntity(space, new Cylinder(MathConverter.Convert(pos), 0.5f, 0.1f, 0.01f), modelLaser, Matrix.CreateScale(0.02f, 0.02f, 0.01f), GameModelType.Projectile, 3, 3, typeof(ProjectileModel));
                projectile.LinearVelocity = entityShip.WorldTransform.Forward * 40f;
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

            if (Paused) return;

            if (currentKeyboardState.IsKeyDown(Keys.Space))
            {
                FireWeapon();
            }

            /*
            if (previousKeyboardState.IsKeyUp(Keys.RightControl) && (currentKeyboardState.IsKeyDown(Keys.RightControl)))
            {
                FireWeapon();
            }
            
            if (currentKeyboardState.IsKeyDown(Keys.A))
            {
                entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Up, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Up, 0.05f), 1.0f);
            }
            else if (currentKeyboardState.IsKeyDown(Keys.D))
            {
                entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Up, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Up, -0.05f), 1.0f);
            }
            
            if (currentKeyboardState.IsKeyDown(Keys.W))
            {
                entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Left, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Left, 0.05f), 1.0f);
            }
            else if (currentKeyboardState.IsKeyDown(Keys.S))
            {
                entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Left, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Left, -0.05f), 1.0f);
            }

            if (currentKeyboardState.IsKeyDown(Keys.E))
            {
                entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Forward, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Forward, 0.15f), 1.0f);
            }
            else if (currentKeyboardState.IsKeyDown(Keys.Q))
            {
                entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Forward, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Forward, -0.15f), 1.0f);
            }

            if (currentKeyboardState.IsKeyDown(Keys.Space))
            {
                entityShip.LinearVelocity = entityShip.WorldTransform.Forward * 10;
            }
            else if (currentKeyboardState.IsKeyDown(Keys.LeftControl))
            {
                entityShip.LinearVelocity = entityShip.WorldTransform.Forward * -5;
            }
            */

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

                float maxForwardSpeed = 20;
                float maxReverseSpeed = -5;
                float mouseWheelDirection = GetMouseWheelDirection();
                if (mouseWheelDirection > 0) ShipSpeed++;
                else if (mouseWheelDirection < 0) ShipSpeed--;
                else if (currentMouseState.MiddleButton == ButtonState.Pressed) ShipSpeed = 0;
                ShipSpeed = Math.Max(maxReverseSpeed, Math.Min(ShipSpeed, maxForwardSpeed));
                entityShip.LinearVelocity = entityShip.WorldTransform.Forward * ShipSpeed;
            }
            //ConsoleWindow.Log(dx + "," + dy);
            
            
            //ConsoleWindow.Log(entityShip.WorldTransform.Up.ToString());
            //entityShip.WorldTransform.U

            // Reset the ship on R key or right thumb stick clicked
            /*
            if (currentKeyboardState.IsKeyDown(Keys.R))
            {
                ship.Reset();
                camera.Reset();
            }

            // Update the ship
            ship.Update(gameTime);
            */
            earth.Update(gameTime);

            // Update the camera to chase the new target
            UpdateCameraChaseTarget();

            // The chase camera's update behavior is the springs, but we can
            // use the Reset method to have a locked, spring-less camera
            if (cameraSpringEnabled)
                camera.Update(gameTime);
            else
                camera.Reset();

            space.Update();

            ProcessQueuedExplosions();


            UpdateProjectiles(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// Helper for updating the list of active projectiles.
        /// </summary>
        void UpdateProjectiles(GameTime gameTime)
        {
            int i = 0;

            float maxDistanceToSelfDestruct = 200f;
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
                    ShowExplosion(MathConverter.Convert(projectile.Position), 1);
                }
                else
                {
                    // Advance to the next projectile.
                    i++;
                }
            }
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
            
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            GraphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

            //DrawModel(modelShip, ship.World);
            //DrawModel(modelEarth, ship.World);
            //DrawEarth();
            //DrawCockpit(gameTime);

            // Pass camera matrices through to the particle system components.
            explosionParticles.SetCamera(camera.View, camera.Projection);
            explosionSmokeParticles.SetCamera(camera.View, camera.Projection);
            projectileTrailParticles.SetCamera(camera.View, camera.Projection);
            smokePlumeParticles.SetCamera(camera.View, camera.Projection);
            fireParticles.SetCamera(camera.View, camera.Projection);
            customParticleSystem.SetCamera(camera.View, camera.Projection);

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

    public class QueuedExplosion
    {
        public DateTime DetonationTime;
        public Vector3 Position;
        public int Strength;
    }
}
