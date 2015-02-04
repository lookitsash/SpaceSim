using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace SpaceSim
{
    public class GameModel
    {
        public Model Model;
        public Matrix[] ModelBones;

        public GameModel(ContentManager content, string modelPath)
        {
            Model = content.Load<Model>(modelPath);
            ModelBones = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(ModelBones);
        }
    }
}
