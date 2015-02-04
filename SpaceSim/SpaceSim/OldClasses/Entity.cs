using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceSim
{
    public class SpaceEntity
    {
        /// <summary>
        /// Location of entity in world space.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Entity world transform matrix.
        /// </summary>
        public Matrix World
        {
            get { return world; }
        }
        protected Matrix world;

        public Model Model;

        /// <summary>
        /// A reference to the graphics device used to access the viewport for touch input.
        /// </summary>
        protected GraphicsDevice graphicsDevice;

        public SpaceEntity(GraphicsDevice device, Model model)
        {
            graphicsDevice = device;
            Model = model;
            Reset();
        }

        /// <summary>
        /// Restore the ship to its original starting state
        /// </summary>
        public virtual void Reset()
        {
        }
    }
}
