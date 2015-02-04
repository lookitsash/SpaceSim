using ConversionHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BEPUphysics.Entities;

namespace SpaceSim
{
    /// <summary>
    /// Component that draws a model following the position and orientation of a BEPUphysics entity.
    /// </summary>
    public class EntityModel : DrawableGameComponent
    {
        /*
        public static BEPUutilities.Matrix Convert(Matrix matrix)
        {
            return new BEPUutilities.Matrix(matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24, matrix.M31, matrix.M32, matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44);
        }

        public static Matrix Convert(BEPUutilities.Matrix matrix)
        {
            return new Matrix(matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24, matrix.M31, matrix.M32, matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44);
        }
        */

        public int ID;

        public GameModelType GameModelType;
        public int Strength = 3;
        public int MaxDestructionDivision = 2;
        public float Scale = 0;
        public Vector3? AITargetPos = null;
        public Vector3 Size;
        
        /// <summary>
        /// Entity that this model follows.
        /// </summary>
        Entity entity;
        Model model;
        /// <summary>
        /// Base transformation to apply to the model.
        /// </summary>
        public BEPUutilities.Matrix Transform;
        Matrix[] boneTransforms;


        /// <summary>
        /// Creates a new EntityModel.
        /// </summary>
        /// <param name="entity">Entity to attach the graphical representation to.</param>
        /// <param name="model">Graphical representation to use for the entity.</param>
        /// <param name="transform">Base transformation to apply to the model before moving to the entity.</param>
        /// <param name="game">Game to which this component will belong.</param>
        public EntityModel(Entity entity, Model model, BEPUutilities.Matrix transform, Game game)
            : base(game)
        {
            this.entity = entity;
            this.model = model;
            this.Transform = transform;

            /*
            cModel = new CModel(model, MathConverter.Convert(entity.Position), Vector3.Zero, Vector3.One, game.GraphicsDevice);

            renderCapture = new RenderCapture(game.GraphicsDevice);
            glowCapture = new RenderCapture(game.GraphicsDevice);
            glowEffect = Game.Content.Load<Effect>("GlowEffect");
            glowTexture = Game.Content.Load<Texture2D>("glow_map");
            glowEffect.Parameters["GlowTexture"].SetValue(glowTexture);
            blur = new GaussianBlur(game.GraphicsDevice, Game.Content, 4);
            */

            //Collect any bone transformations in the model itself.
            //The default cube model doesn't have any, but this allows the EntityModel to work with more complicated shapes.
            
            boneTransforms = new Matrix[model.Bones.Count];

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    //effect.TextureEnabled = true;
                    //effect.Texture = glowTexture;
                }
            }

            
        }

        RenderCapture renderCapture;
        RenderCapture glowCapture;
        Effect glowEffect;
        GaussianBlur blur;
        Texture2D glowTexture;

        protected override void LoadContent()
        {
            
        }

        CModel cModel;

        public override void Initialize()
        {
            base.Initialize();

            
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public bool ShouldRender
        {
            get
            {
                bool abortDraw = false;

                if (GameModelType == SpaceSim.GameModelType.Asteroid || GameModelType == SpaceSim.GameModelType.ShipNPC)
                {
                    float distanceFromPlayer = BEPUutilities.Vector3.Distance(SpaceSimGame.entityShip.Position, entity.Position);
                    if (distanceFromPlayer >= 1000) abortDraw = true;
                }
                else if (GameModelType == SpaceSim.GameModelType.Ship) abortDraw = true;

                return !abortDraw;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Matrix worldMatrix = MathConverter.Convert(Transform * entity.WorldTransform);
            bool shouldRender = ShouldRender;
            if (shouldRender || GameModelType == SpaceSim.GameModelType.Ship)
            {
                SpaceSimLibrary.Networking.Server.UpdateServerEntity(ID, (byte)GameModelType, worldMatrix);
            }
            
            if (shouldRender)
            {
                
                /*
                // Begin capturing the glow render
                glowCapture.Begin();
                Game.GraphicsDevice.Clear(Color.Black);

                //cModel.Model.
                cModel.CacheEffects();
                cModel.SetModelEffect(glowEffect, false);
                cModel.Draw(SpaceSimGame.camera.View, SpaceSimGame.camera.Projection, SpaceSimGame.camera.Position, worldMatrix);
                cModel.RestoreEffects();

                // Finish capturing the glow
                glowCapture.End();

                //SpaceSimGame.graphics.GraphicsDevice.BlendState = BlendState.Opaque;
                //SpaceSimGame.graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                //Notice that the entity's worldTransform property is being accessed here.
                //This property is returns a rigid transformation representing the orientation
                //and translation of the entity combined.
                //There are a variety of properties available in the entity, try looking around
                //in the list to familiarize yourself with it.
                */

                model.CopyAbsoluteBoneTransformsTo(boneTransforms);
                foreach (ModelMesh mesh in model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.World = boneTransforms[mesh.ParentBone.Index] * worldMatrix;
                        effect.View = ((SpaceSimGame)this.Game).camera.View;
                        effect.Projection = ((SpaceSimGame)this.Game).camera.Projection;
                    }
                    mesh.Draw();
                }
            }

            base.Draw(gameTime);
        }
    }
}
