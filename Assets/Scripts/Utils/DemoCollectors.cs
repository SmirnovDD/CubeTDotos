using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Utils
{
    public static class DemoCollectors
    {
        public struct ObstacleAvoidanceCollector : ICollector<ColliderCastHit>
        {
            public bool EarlyOutOnFirstHit => false;
            public float MaxFraction { get; private set; }
            public int NumHits { get; private set; }

            private ColliderCastHit m_ClosestHit;
            private float3 _castDirection;
            private Entity _castingEntity;
            public ColliderCastHit ClosestHit => m_ClosestHit;
            public ObstacleAvoidanceCollector(float maxFraction, float3 castDirection, Entity castingEntity)
            {
                _castingEntity = castingEntity;
                _castDirection = castDirection;
                MaxFraction = maxFraction;
                m_ClosestHit = default;
                NumHits = 0;
            }

            public bool AddHit(ColliderCastHit hit)
            {
                if (_castingEntity == hit.Entity)
                    return false;
                
                if (hit.Fraction > MaxFraction)
                    return false;
                
                MaxFraction = hit.Fraction;
                m_ClosestHit = hit;
                NumHits++;
                return true;
            }
        }
    }
}