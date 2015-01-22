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

        KeyboardState lastKeyboardState = new KeyboardState();
        MouseState lastMouseState = new MouseState();
        KeyboardState currentKeyboardState = new KeyboardState();
        MouseState currentMouseState = new MouseState();

        Texture2D textureCockpit;

        //Ship ship;
        Earth earth;
        public static ChaseCamera camera;
        Skybox skybox;

        Model modelShip, modelEarth, modelAsteroid;

        public List<SpaceEntity> EntityCollection = new List<SpaceEntity>();

        private Vector4 globalAmbient;
        private Sunlight sunlight;

        bool cameraSpringEnabled = true;

        public SpaceSimGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            (ConsoleWindow = new ConsoleWindow()).Show();
            ConsoleWindow.OnInput += new ConsoleWindow.ConsoleInputEventHandler(OnConsoleInput);

            // Construct our particle system components.
            explosionParticles = new ExplosionParticleSystem(this, Content);
            explosionSmokeParticles = new ExplosionSmokeParticleSystem(this, Content);
            projectileTrailParticles = new ProjectileTrailParticleSystem(this, Content);
            smokePlumeParticles = new SmokePlumeParticleSystem(this, Content);
            fireParticles = new FireParticleSystem(this, Content);

            // Set the draw order so the explosions and fire
            // will appear over the top of the smoke.
            smokePlumeParticles.DrawOrder = 100;
            explosionSmokeParticles.DrawOrder = 200;
            projectileTrailParticles.DrawOrder = 300;
            explosionParticles.DrawOrder = 400;
            fireParticles.DrawOrder = 500;

            // Register the particle system components.
            Components.Add(explosionParticles);
            Components.Add(explosionSmokeParticles);
            Components.Add(projectileTrailParticles);
            Components.Add(smokePlumeParticles);
            Components.Add(fireParticles);
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

            graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width / 2;
            graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height / 2;

            // Create the chase camera
            camera = new ChaseCamera();

            // Set the camera offsets
            camera.DesiredPositionOffset = new Vector3(0.0f, 100.0f, 350.0f);
            //camera.DesiredPositionOffset = new Vector3(100.0f, 100.0f, 350.0f);
            camera.LookAtOffset = new Vector3(0.0f, 50.0f, 0.0f);

            // Set camera perspective
            camera.NearPlaneDistance = 10.0f;
            camera.FarPlaneDistance = 100000.0f;

            //EntityCollection.Add(ship = new Ship(GraphicsDevice, modelShip));
            //ship.Position = new Vector3(0, 0, 44000);

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

        Entity entityShip, entityEarth, entityAsteroid;

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

            /*
            BoundingSphere bounds = new BoundingSphere();
            foreach (ModelMesh mesh in modelEarth.Meshes)
                bounds = BoundingSphere.CreateMerged(bounds, mesh.BoundingSphere);
            float shipRadius = bounds.Radius;
            */
            skybox = new Skybox("Textures/suninspace2", Content);

            // TODO: use this.Content to load your game content here
            space = new Space();
            //Box ground = new Box(BEPUutilities.Vector3.Zero, 30, 1, 30);
            //space.Add(ground);

            entityShip = AddEntity(space, new Sphere(new BEPUutilities.Vector3(0, 1000, 25000), 100, 100), modelShip, 0.1f, GameModelType.Ship, 0, 0, typeof(EntityModel));
            entityShip.AngularDamping = 0.9f;
            entityShip.LinearDamping = 0.9f;
            entityShip.CollisionInformation.Events.InitialCollisionDetected += HandleCollision;

            entityEarth = AddEntity(space, new Sphere(new BEPUutilities.Vector3(0, 0, -20000), 20500), modelEarth, 5000f, GameModelType.Planet, 0, 0, typeof(PlanetModel)); ;
            entityEarth.CollisionInformation.Events.InitialCollisionDetected += HandleCollision;

            AddEntity(space, new Sphere(new BEPUutilities.Vector3(0, 1000, 21000), 200, 1000), modelAsteroid, 1000f, GameModelType.Asteroid, 1, 3, typeof(EntityModel));
            AddEntity(space, new Sphere(new BEPUutilities.Vector3(-1000, 0, 21000), 200, 1000), modelAsteroid, 1000f, GameModelType.Asteroid, 1, 3, typeof(EntityModel));
            AddEntity(space, new Sphere(new BEPUutilities.Vector3(1000, 0, 21000), 200, 1000), modelAsteroid, 1000f, GameModelType.Asteroid, 1, 3, typeof(EntityModel));

            space.ForceUpdater.Gravity = new BEPUutilities.Vector3(0, 0, 0);

            //BEPUphysics.Ex

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

                        QueueExplosion(GetRandomPosition(MathConverter.Convert(otherEntityInformation.Entity.Position), 100), 50, 0);
                        QueueExplosion(GetRandomPosition(MathConverter.Convert(otherEntityInformation.Entity.Position), 100), 50, 0.1f);
                        QueueExplosion(GetRandomPosition(MathConverter.Convert(otherEntityInformation.Entity.Position), 100), 50, 0.2f);

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

                            QueueExplosion(MathConverter.Convert(otherEntityInformation.Entity.Position), 50, 0);

                            if (parent.MaxDestructionDivision > 0)
                            {
                                Vector3 pos = MathConverter.Convert(otherEntityInformation.Entity.Position+(otherEntityInformation.Entity.WorldTransform.Up*50));
                                Entity newEntity = AddEntity(space, new Sphere(MathConverter.Convert(pos), ((Sphere)otherEntityInformation.Entity).Radius / 2.0f, 1000), modelAsteroid, parent.Scale / 2.0f, GameModelType.Asteroid, 1, parent.MaxDestructionDivision-1, typeof(EntityModel));
                                newEntity.LinearVelocity = otherEntityInformation.Entity.WorldTransform.Up * 50;

                                pos = MathConverter.Convert(otherEntityInformation.Entity.Position + (otherEntityInformation.Entity.WorldTransform.Down * 50));
                                newEntity = AddEntity(space, new Sphere(MathConverter.Convert(pos), ((Sphere)otherEntityInformation.Entity).Radius / 2.0f, 1000), modelAsteroid, parent.Scale / 2.0f, GameModelType.Asteroid, 1, parent.MaxDestructionDivision - 1, typeof(EntityModel));
                                newEntity.LinearVelocity = otherEntityInformation.Entity.WorldTransform.Down * 50;
                            }
                        }
                    }
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
            int numExplosionParticles = 100;
            int numExplosionSmokeParticles = 50;
            Random r = new Random();
            for (int i = 0; i < numExplosionParticles; i++)
                explosionParticles.AddParticle(position, new Vector3(r.Next(-strength, strength), r.Next(-strength, strength), r.Next(-strength, strength)));

            for (int i = 0; i < numExplosionSmokeParticles; i++)
                explosionSmokeParticles.AddParticle(position, new Vector3(r.Next(-strength / 2, strength/2), r.Next(-strength / 2, strength / 2), r.Next(-strength / 2, strength / 2)));
        }
        public Vector3 GetRandomPosition(Vector3 center, int maxRange)
        {
            Random r = new Random();
            return new Vector3((float)r.Next((int)center.X - maxRange, (int)center.X + maxRange), (float)r.Next((int)center.Y - maxRange, (int)center.Y + maxRange), (float)r.Next((int)center.Z - maxRange, (int)center.Z + maxRange));
        }

        private Entity AddEntity(Space space, Entity entity, Model model, float scale, GameModelType gameModelType, int strength, int destructionDivisions, Type entityType)
        {
            space.Add(entity);
            DrawableGameComponent entityModel = null;
            if (entityType == typeof(EntityModel))
            {
                entityModel = new EntityModel(entity, model, MathConverter.Convert(Matrix.CreateScale(scale, scale, scale)), this);
                ((EntityModel)entityModel).GameModelType = gameModelType;
                ((EntityModel)entityModel).Scale = scale;
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
                entityModel = new PlanetModel(entity, model, MathConverter.Convert(Matrix.CreateScale(scale, scale, scale)), this);
                entity.Tag = entityModel;
            }
            Components.Add(entityModel);

            return entity;
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

            if (currentKeyboardState.IsKeyDown(Keys.A))
            {
                entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Up, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Up, 0.25f), 1.0f);
            }
            else if (currentKeyboardState.IsKeyDown(Keys.D))
            {
                entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Up, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Up, -0.25f), 1.0f);
            }
            
            if (currentKeyboardState.IsKeyDown(Keys.W))
            {
                entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Left, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Left, 0.15f), 1.0f);
            }
            else if (currentKeyboardState.IsKeyDown(Keys.S))
            {
                entityShip.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Left, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(entityShip.WorldTransform.Left, -0.15f), 1.0f);
            }

            if (currentKeyboardState.IsKeyDown(Keys.Space))
            {
                entityShip.LinearVelocity = entityShip.WorldTransform.Forward * 500;
            }
            else if (currentKeyboardState.IsKeyDown(Keys.LeftControl))
            {
                entityShip.LinearVelocity = entityShip.WorldTransform.Forward * -500;
            }

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
