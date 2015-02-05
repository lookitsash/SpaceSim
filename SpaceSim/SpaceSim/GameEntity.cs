using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;
using ConversionHelper;

namespace SpaceSim
{
    public class GameEntity
    {
        private static int CURRENT_ID = 0;

        public int ID;
        public Entity PhysicsEntity;
        public EntityType EntityType;
        public Matrix Scale;

        public Matrix World
        {
            get
            {
                return Scale * MathConverter.Convert(PhysicsEntity.WorldTransform);
            }
        }

        public GameEntity(EntityType entityType, Entity physicsEntity)
        {
            ID = ++CURRENT_ID;
            EntityType = entityType;
            PhysicsEntity = physicsEntity;
            Scale = Matrix.Identity;

            PhysicsEntity.Tag = this;
        }

        public void SetScale(float scale)
        {
            SetScale(scale, scale, scale);
        }

        public void SetScale(float x, float y, float z)
        {
            Matrix.CreateScale(x, y, z, out Scale);
        }
    }
}
