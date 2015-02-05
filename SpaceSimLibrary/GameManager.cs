using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics;
using Microsoft.Xna.Framework;
using BEPUphysics.BroadPhaseEntries;
using ConversionHelper;

namespace SpaceSimLibrary
{
    public class GameManager
    {
        Dictionary<EntityType, List<GameEntity>> GameEntityGroups = new Dictionary<EntityType, List<GameEntity>>();
        Dictionary<int, GameEntity> GameEntities = new Dictionary<int, GameEntity>();
        Dictionary<EntityType, GameModel> GameModels = new Dictionary<EntityType, GameModel>();
        List<EntityType> EntityTypes = new List<EntityType>();
        Space Space = null;

        public GameManager(bool physicsEnabled)
        {
            if (physicsEnabled)
            {
                Space = new BEPUphysics.Space();
                Space.ForceUpdater.Gravity = new BEPUutilities.Vector3(0, 0, 0);
            }
        }

        public GameEntity RegisterEntity(GameEntity gameEntity)
        {
            if (!GameEntities.ContainsKey(gameEntity.ID))
            {
                List<GameEntity> entityGroup = GameEntityGroups.ContainsKey(gameEntity.EntityType) ? GameEntityGroups[gameEntity.EntityType] : null;
                if (entityGroup == null) GameEntityGroups.Add(gameEntity.EntityType, entityGroup = new List<GameEntity>());
            
                entityGroup.Add(gameEntity);
                GameEntities.Add(gameEntity.ID, gameEntity);
                if (Space != null) Space.Add(gameEntity.PhysicsEntity);
            }
            return gameEntity;
        }

        public GameModel RegisterModel(EntityType entityType, GameModel gameModel)
        {
            if (GameModels.ContainsKey(entityType)) GameModels[entityType] = gameModel;
            else GameModels.Add(entityType, gameModel);

            if (!EntityTypes.Contains(entityType)) EntityTypes.Add(entityType);

            return gameModel;
        }

        public List<EntityType> GetEntityTypes()
        {
            return EntityTypes;
        }

        public GameModel GetModel(EntityType entityType)
        {
            if (GameModels.ContainsKey(entityType)) return GameModels[entityType];
            else return null;
        }

        public GameEntity GetEntity(int entityID)
        {
            if (GameEntities.ContainsKey(entityID)) return GameEntities[entityID];
            else return null;
        }

        public List<GameEntity> GetEntitiesByType(EntityType entityType)
        {
            if (GameEntityGroups.ContainsKey(entityType)) return GameEntityGroups[entityType];
            else return null;
        }

        public void RemoveEntity(int entityID)
        {
            if (GameEntities.ContainsKey(entityID))
            {
                GameEntity gameEntity = GetEntity(entityID);
                GameEntityGroups[gameEntity.EntityType].Remove(gameEntity);
                GameEntities.Remove(entityID);
            }
        }

        public Vector3? GetRandomNonCollidingPoint(float requestedPlacementRadius, Vector3 targetAreaCenter, int targetAreaRadius, int maxPlacementAttempts, Random r)
        {
            if (Space == null) return null;

            for (int j = 0; j < maxPlacementAttempts; j++)
            {
                IList<BroadPhaseEntry> overlaps = new List<BroadPhaseEntry>();
                BEPUutilities.Vector3 pos = new BEPUutilities.Vector3(targetAreaCenter.X + r.Next(-targetAreaRadius / 2, targetAreaRadius / 2), r.Next(-targetAreaRadius / 2, targetAreaRadius / 2), r.Next(-targetAreaRadius / 2, targetAreaRadius / 2));
                Space.BroadPhase.QueryAccelerator.GetEntries(new BEPUutilities.BoundingBox(new BEPUutilities.Vector3(pos.X - requestedPlacementRadius, pos.Y - requestedPlacementRadius, pos.Z - requestedPlacementRadius), new BEPUutilities.Vector3(pos.X + requestedPlacementRadius, pos.Y + requestedPlacementRadius, pos.Z + requestedPlacementRadius)), overlaps);
                //if (overlaps.Count == 0)
                return MathConverter.Convert(pos);
            }
            return null;
        }

        public void Update(GameTime gameTime)
        {
            if (Space != null) Space.Update();
        }
    }
}
