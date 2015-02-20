using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceSimLibrary
{
    public class PlanetEntity : GameEntity
    {
        public Vector4 ambient;
        public Vector4 diffuse;
        public Vector4 specular;
        public float shininess;
        public float cloudStrength;

        private Vector4 globalAmbient;
        private Sunlight sunlight;

        private static bool ResourcesLoaded = false;
        private static Model Model;
        private static Effect Effect;
        private static Texture2D DayTexture, NightTexture, CloudTexture, NormalMapTexture;

        public PlanetEntity(Game game, EntityType entityType, Entity physicsEntity)
            : base(game, entityType, physicsEntity)
        {
            globalAmbient = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);
            sunlight.direction = new Vector4(Vector3.Forward, 0.0f);
            sunlight.color = new Vector4(1.0f, 0.941f, 0.898f, 1.0f);

            if (!ResourcesLoaded)
            {
                Model = Game.Content.Load<Model>(@"Models\earth");
                Effect = Game.Content.Load<Effect>(@"Effects\earth");
                DayTexture = Game.Content.Load<Texture2D>(@"Textures\earth_day_color_spec");
                NightTexture = Game.Content.Load<Texture2D>(@"Textures\earth_night_color");
                CloudTexture = Game.Content.Load<Texture2D>(@"Textures\earth_clouds_alpha");
                NormalMapTexture = Game.Content.Load<Texture2D>(@"Textures\earth_nrm");
                ResourcesLoaded = true;
            }

            // Setup material settings for the Earth.
            ambient = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
            diffuse = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            specular = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            shininess = 20.0f;
            cloudStrength = 1.15f;

            // Calculate the bounding sphere of the Earth model and bind the
            // custom Earth effect file to the model.
            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                    part.Effect = Effect;
            }
        }

        public override void Draw(Matrix view, Matrix projection, Vector3 cameraPosition, GameTime gameTime)
        {
            foreach (ModelMesh m in Model.Meshes)
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

                    e.Parameters["world"].SetValue(WorldWithScale);
                    e.Parameters["view"].SetValue(view);
                    e.Parameters["projection"].SetValue(projection);
                    e.Parameters["cameraPos"].SetValue(new Vector4(cameraPosition, 1.0f));
                    e.Parameters["globalAmbient"].SetValue(globalAmbient);
                    e.Parameters["lightDir"].SetValue(sunlight.direction);
                    e.Parameters["lightColor"].SetValue(sunlight.color);
                    e.Parameters["materialAmbient"].SetValue(ambient);
                    e.Parameters["materialDiffuse"].SetValue(diffuse);
                    e.Parameters["materialSpecular"].SetValue(specular);
                    e.Parameters["materialShininess"].SetValue(shininess);
                    e.Parameters["landOceanColorGlossMap"].SetValue(DayTexture);
                    e.Parameters["cloudColorMap"].SetValue(CloudTexture);
                    e.Parameters["nightColorMap"].SetValue(NightTexture);
                    e.Parameters["normalMap"].SetValue(NormalMapTexture);
                }

                m.Draw();
            }

            base.Draw(view, projection, cameraPosition, gameTime);
        }
    }

    public struct Sunlight
    {
        public Vector4 direction;
        public Vector4 color;
    }
}
