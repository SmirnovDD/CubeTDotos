using System;
using Unity.Physics;

namespace Utils
{
    public struct CollisionFilters
    {
        [Flags]
        private enum CollisionLayer
        {
            Solid = 1 << 0,
            Character = 1 << 1,
            Unit = 1 << 2,
            ObstacleAvoidanceCollider = 1 << 3,
            Terrain = 1 << 4,
            Dynamic = 1 << 5
        }

        public static readonly CollisionFilter Solid = new CollisionFilter()
        {
            BelongsTo = (uint) CollisionLayer.Solid,
            CollidesWith = (uint) (CollisionLayer.Character | CollisionLayer.Unit)
        };

        public static readonly CollisionFilter Character = new CollisionFilter()
        {
            BelongsTo = (uint) CollisionLayer.Character,
            CollidesWith = (uint) (CollisionLayer.Solid | CollisionLayer.Unit | CollisionLayer.Terrain)
        };

        public static readonly CollisionFilter DynamicWithPhysical = new CollisionFilter()
        {
            BelongsTo = (uint)CollisionLayer.Dynamic,
            CollidesWith = (uint) (CollisionLayer.Solid | CollisionLayer.Terrain | CollisionLayer.Dynamic)
        };
        
        public static readonly CollisionFilter DynamicWithPhysicalExcludingTerrain = new CollisionFilter()
        {
            BelongsTo = (uint)CollisionLayer.Dynamic,
            CollidesWith = (uint) (CollisionLayer.Solid | CollisionLayer.Dynamic)
        };
        
        public static readonly CollisionFilter OnlyWithStaticObjects = new CollisionFilter()
        {
            BelongsTo = (uint) (CollisionLayer.Unit | CollisionLayer.Character),
            CollidesWith = (uint) (CollisionLayer.Solid | CollisionLayer.Terrain)
        };
        
        public static readonly CollisionFilter ObstacleAvoidanceCollider = new CollisionFilter()
        {
            BelongsTo = (uint) (CollisionLayer.Unit | CollisionLayer.Dynamic),
            CollidesWith = (uint) (CollisionLayer.Solid | CollisionLayer.Dynamic)
        };
        
        public static readonly CollisionFilter TerrainCollider = new CollisionFilter()
        {
            BelongsTo = (uint) CollisionLayer.Terrain,
            CollidesWith = (uint) (CollisionLayer.Solid | CollisionLayer.Character | CollisionLayer.Unit)
        };
    }
}