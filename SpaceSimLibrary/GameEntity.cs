using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;
using ConversionHelper;
using BEPUphysics.Entities.Prefabs;

namespace SpaceSimLibrary
{
    public class GameEntity
    {
        private static int CURRENT_ID = 0;

        public int ID;
        public Entity PhysicsEntity;
        public EntityType EntityType;
        public Matrix Scale;
        public Game Game;

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

        public GameEntity(Game game, EntityType entityType, Entity physicsEntity)
        {
            ID = ++CURRENT_ID;
            EntityType = entityType;
            PhysicsEntity = physicsEntity;
            Scale = Matrix.Identity;
            Game = game;

            if (PhysicsEntity != null) PhysicsEntity.Tag = this;
        }

        public GameEntity(Game game, EntityType entityType, int id, Matrix world, float scaleX, float scaleY, float scaleZ) : this(game, entityType, null)
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

        public PhysicsShape PhysicsShape
        {
            get
            {
                if (PhysicsEntity is Box) return SpaceSimLibrary.PhysicsShape.Box;
                else return SpaceSimLibrary.PhysicsShape.Sphere;
            }
        }

        public virtual void Update(GameTime gameTime)
        {
        }

        public virtual void Draw(Matrix view, Matrix projection, Vector3 cameraPosition, GameTime gameTime)
        {
        }
    }

    public enum PhysicsShape
    {
        Sphere = 1,
        Box = 2
    }
}
