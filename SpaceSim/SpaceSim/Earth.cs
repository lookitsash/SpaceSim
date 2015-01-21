#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System;
#endregion

namespace SpaceSim
{
    public class Earth : Entity
    {

        public float Scale = 1;

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

        public Earth(GraphicsDevice device, Model model) : base(device, model) { }

        public override void Reset()
        {
            Position = new Vector3(0, 0, 0);
        }

        public void LoadContent(ContentManager Content)
        {
            // Load the assets for the Earth.
            model = Content.Load<Model>(@"Models\earth");
            effect = Content.Load<Effect>(@"Effects\earth");
            dayTexture = Content.Load<Texture2D>(@"Textures\earth_day_color_spec");
            nightTexture = Content.Load<Texture2D>(@"Textures\earth_night_color");
            cloudTexture = Content.Load<Texture2D>(@"Textures\earth_clouds_alpha");
            normalMapTexture = Content.Load<Texture2D>(@"Textures\earth_nrm");

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

        /// <summary>
        /// Applies a simple rotation to the ship and animates position based
        /// on simple linear motion physics.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            rotation += (float)gameTime.ElapsedGameTime.TotalMilliseconds * MathHelper.ToRadians(0.001f);

            //Matrix rotation = Matrix.CreateRotationY(earth.rotation) * Matrix.CreateRotationZ(MathHelper.ToRadians(-23.4f));

            // Reconstruct the ship's world matrix
            //world = Matrix.CreateRotationY(rotation) * Matrix.CreateRotationZ(MathHelper.ToRadians(-23.4f));
            //world.Translation = Position;
            world = Matrix.CreateScale(Scale) * Matrix.CreateRotationY(rotation) * Matrix.CreateRotationZ(MathHelper.ToRadians(-23.4f)) * Matrix.CreateTranslation(Position);
        }
    }
}
