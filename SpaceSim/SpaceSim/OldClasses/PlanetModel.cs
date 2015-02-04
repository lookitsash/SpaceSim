using ConversionHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BEPUphysics.Entities;

namespace SpaceSim
{
    public enum GameModelType
    {
        Planet,
        Ship,
        Asteroid,
        Projectile,
        ShipNPC
    }

    /// <summary>
    /// Component that draws a model following the position and orientation of a BEPUphysics entity.
    /// </summary>
    public class PlanetModel : DrawableGameComponent
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
        public Model model;
        public Effect effect;
        public BoundingSphere bounds;

        public Texture2D dayTexture;
        public Texture2D nightTexture;
        public Texture2D cloudTexture;
        public Texture2D normalMapTexture;

        public Vector4 ambient;
        public Vector4 diffuse;
        public Vector4 specular;
        public float shininess;
        public float cloudStrength;

        public float rotation;

        private Vector4 globalAmbient;
        private Sunlight sunlight;

        /// <summary>
        /// Entity that this model follows.
        /// </summary>
        Entity entity;
        //Model model;
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
        public PlanetModel(Entity entity, Model model, BEPUutilities.Matrix transform, Game game)
            : base(game)
        {
            this.entity = entity;
            this.model = model;
            this.Transform = transform;

            globalAmbient = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);
            sunlight.direction = new Vector4(Vector3.Forward, 0.0f);
            sunlight.color = new Vector4(1.0f, 0.941f, 0.898f, 1.0f);

            /// Load the assets for the Earth.
            model = game.Content.Load<Model>(@"Models\earth");
            effect = game.Content.Load<Effect>(@"Effects\earth");
            dayTexture = game.Content.Load<Texture2D>(@"Textures\earth_day_color_spec");
            nightTexture = game.Content.Load<Texture2D>(@"Textures\earth_night_color");
            cloudTexture = game.Content.Load<Texture2D>(@"Textures\earth_clouds_alpha");
            normalMapTexture = game.Content.Load<Texture2D>(@"Textures\earth_nrm");

            // Setup material settings for the Earth.
            ambient = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
            diffuse = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            specular = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            shininess = 20.0f;
            cloudStrength = 1.15f;

            // Calculate the bounding sphere of the Earth model and bind the
            // custom Earth effect file to the model.
            foreach (ModelMesh mesh in model.Meshes)
            {
                bounds = BoundingSphere.CreateMerged(bounds, mesh.BoundingSphere);

                foreach (ModelMeshPart part in mesh.MeshParts)
                    part.Effect = effect;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            //Notice that the entity's worldTransform property is being accessed here.
            //This property is returns a rigid transformation representing the orientation
            //and translation of the entity combined.
            //There are a variety of properties available in the entity, try looking around
            //in the list to familiarize yourself with it.

            Matrix worldMatrix = MathConverter.Convert(Transform * entity.WorldTransform);

            foreach (ModelMesh m in model.Meshes)
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
                        e.Parameters["cloudStrength"].SetValue(cloudStrength);
                    }

                    e.Parameters["world"].SetValue(worldMatrix);
                    e.Parameters["view"].SetValue(((SpaceSimGame)this.Game).camera.View);
                    e.Parameters["projection"].SetValue(((SpaceSimGame)this.Game).camera.Projection);
                    e.Parameters["cameraPos"].SetValue(new Vector4(((SpaceSimGame)this.Game).camera.Position, 1.0f));
                    e.Parameters["globalAmbient"].SetValue(globalAmbient);
                    e.Parameters["lightDir"].SetValue(sunlight.direction);
                    e.Parameters["lightColor"].SetValue(sunlight.color);
                    e.Parameters["materialAmbient"].SetValue(ambient);
                    e.Parameters["materialDiffuse"].SetValue(diffuse);
                    e.Parameters["materialSpecular"].SetValue(specular);
                    e.Parameters["materialShininess"].SetValue(shininess);
                    e.Parameters["landOceanColorGlossMap"].SetValue(dayTexture);
                    e.Parameters["cloudColorMap"].SetValue(cloudTexture);
                    e.Parameters["nightColorMap"].SetValue(nightTexture);
                    e.Parameters["normalMap"].SetValue(normalMapTexture);
                }

                m.Draw();
            }

            /*
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
             */
            base.Draw(gameTime);
        }
    }
}
