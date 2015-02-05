using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;
using ConversionHelper;

namespace SpaceSimLibrary
{
    public class GameEntity
    {
        private static int CURRENT_ID = 0;

        public int ID;
        public Entity PhysicsEntity;
        public EntityType EntityType;
        public Matrix Scale;

        private Matrix _World = Matrix.Identity;
        public Matrix World
        {
            get
            {
                if (PhysicsEntity == null) return _World;
                else return MathConverter.Convert(PhysicsEntity.WorldTransform);
            }
            set
            {
                _World = value;
            }
        }

        public Matrix WorldWithScale
        {
            get
            {
                return Scale * World;
            }
        }

        public Vector3 Position
        {
            get
            {
                return World.Translation;
            }
        }

        public GameEntity(EntityType entityType, Entity physicsEntity)
        {
            ID = ++CURRENT_ID;
            EntityType = entityType;
            PhysicsEntity = physicsEntity;
            Scale = Matrix.Identity;

            if (PhysicsEntity != null) PhysicsEntity.Tag = this;
        }

        public GameEntity(EntityType entityType, int id, Matrix world, float scaleX, float scaleY, float scaleZ) : this(entityType, null)
        {
            ID = id;
            World = world;
            SetScale(scaleX, scaleY, scaleZ);
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
