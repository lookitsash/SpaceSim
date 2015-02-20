using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSimLibrary;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;

namespace SpaceSim
{
    public class ShipEntity : GameEntity
    {
        private int ShipSpeedIndex = 0;
        private float[] ShipSpeeds = { -30, -15, 0, 5, 10, 25, 40, 60 };
        private int ShipSpeedIndexZero { get { for (int i = 0; i < ShipSpeeds.Length; i++) { if (ShipSpeeds[i] == 0) return i; } return 0; } }
        public float ShipSpeed { get { return ShipSpeeds[ShipSpeedIndex]; } }
        private float ShipSpeedMax { get { return ShipSpeeds[ShipSpeeds.Length - 1]; } }
        
        public void ShipSpeedIncrease()
        {
            ShipSpeedIndex++;
            if (ShipSpeedIndex >= ShipSpeeds.Length) ShipSpeedIndex = ShipSpeeds.Length - 1;
        }
        public void ShipSpeedDecrease()
        {
            ShipSpeedIndex--;
            if (ShipSpeedIndex < 0) ShipSpeedIndex = 0;
        }
        public void ShipSpeedZero()
        {
            ShipSpeedIndex = ShipSpeedIndexZero;
        }

        public ShipEntity(Game game, Entity physicsEntity) : base(game, EntityType.Player, physicsEntity)
        {
            PhysicsEntity.AngularDamping = 0.9f;
            PhysicsEntity.LinearDamping = 0.0f;
            //PhysicsEntity.CollisionInformation.Events.InitialCollisionDetected += HandleCollision;
            ShipSpeedIndex = ShipSpeedIndexZero;
        }
    }
}
