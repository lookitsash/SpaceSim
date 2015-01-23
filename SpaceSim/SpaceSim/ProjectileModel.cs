using ConversionHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BEPUphysics.Entities;
using ParticleEngine;

namespace SpaceSim
{
    /// <summary>
    /// Component that draws a model following the position and orientation of a BEPUphysics entity.
    /// </summary>
    public class ProjectileModel : DrawableGameComponent
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

        public GameModelType GameModelType;
        public int Strength = 3;
        public int MaxDestructionDivision = 2;
        public float Scale = 0;

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

        float trailParticlesPerSecond = 20;
        ParticleSystem explosionParticles;
        ParticleSystem explosionSmokeParticles;
        ParticleEmitter trailEmitter;

        private BEPUutilities.Vector3 startPosition;

        public float DistanceTravelled
        {
            get
            {
                return BEPUutilities.Vector3.Distance(startPosition, entity.Position);
            }
        }

        public bool DestroyNow = false;

        /// <summary>
        /// Creates a new EntityModel.
        /// </summary>
        /// <param name="entity">Entity to attach the graphical representation to.</param>
        /// <param name="model">Graphical representation to use for the entity.</param>
        /// <param name="transform">Base transformation to apply to the model before moving to the entity.</param>
        /// <param name="game">Game to which this component will belong.</param>
        public ProjectileModel(Entity entity, Model model, BEPUutilities.Matrix transform, Game game, ParticleSystem explosionParticles, ParticleSystem explosionSmokeParticles, ParticleSystem projectileTrailParticles)
            : base(game)
        {
            this.entity = entity;
            this.model = model;
            this.Transform = transform;

            startPosition = new BEPUutilities.Vector3(entity.Position.X, entity.Position.Y, entity.Position.Z);


            this.explosionParticles = explosionParticles;
            this.explosionSmokeParticles = explosionSmokeParticles;
            //this.trailEmitter = new ParticleEmitter(projectileTrailParticles,trailParticlesPerSecond, MathConverter.Convert(entity.Position));

            //Collect any bone transformations in the model itself.
            //The default cube model doesn't have any, but this allows the EntityModel to work with more complicated shapes.
            boneTransforms = new Matrix[model.Bones.Count];
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Update the particle emitter, which will create our particle trail.
            //trailEmitter.Update(gameTime, MathConverter.Convert(entity.Position));

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            //SpaceSimGame.bloom.BeginDraw();
            //Notice that the entity's worldTransform property is being accessed here.
            //This property is returns a rigid transformation representing the orientation
            //and translation of the entity combined.
            //There are a variety of properties available in the entity, try looking around
            //in the list to familiarize yourself with it.

            Matrix worldMatrix = MathConverter.Convert(Transform * entity.WorldTransform);


            model.CopyAbsoluteBoneTransformsTo(boneTransforms);
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = boneTransforms[mesh.ParentBone.Index] * worldMatrix;
                    effect.View = SpaceSimGame.camera.View;
                    effect.Projection = SpaceSimGame.camera.Projection;
                }
                mesh.Draw();
            }
            base.Draw(gameTime);
        }
    }
}
