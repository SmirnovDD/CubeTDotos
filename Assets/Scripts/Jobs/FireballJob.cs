using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Utils;
using Material = Unity.Physics.Material;
using MathUtilities = Utils.MathUtilities;
using PhysicsUtilities = Utils.PhysicsUtilities;
using SphereCollider = Unity.Physics.SphereCollider;

namespace DEMO
{
    [BurstCompile]
    public partial struct FireballJob : IJobEntity
    {
        [ReadOnly] public CollisionWorld CollisionWorld;
        [ReadOnly] public ComponentDataFromEntity<DontDestroyTag> DontDestroyObjectsDataFromEntity;
        public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;

        public float DeltaTime;
        public float Impulse;
        public float Gravity;
    
        public void Execute(Entity entity, [EntityInQueryIndex] int entityInQueryIndex, ref Translation translation, ref Rotation rotation, in PhysicsCollider collider)
        {
            float3 forwardVector = MathUtilities.GetForwardVectorFromRotation(rotation.Value);
            float3 curPos = translation.Value;
            float3 newPos = curPos + (forwardVector * (Impulse * DeltaTime)) + math.up() * (Gravity * (DeltaTime * DeltaTime));
            float3 newForwardVector = newPos - curPos;

            unsafe
            {
                var sphereCollider = (SphereCollider*) collider.ColliderPtr;
                var colliderForCheck = SphereCollider.Create(sphereCollider->Geometry, CollisionFilters.Fireball, Material.Default);
                var allHits = PhysicsUtilities.ColliderCastAllWithoutFilter(in colliderForCheck, in curPos, in newPos, in CollisionWorld, Allocator.Temp);
                foreach (var colliderCastHit in allHits)
                {
                   if (!DontDestroyObjectsDataFromEntity.HasComponent(colliderCastHit.Entity))
                        EntityCommandBuffer.AddComponent<DestroyEntityTag>(0, colliderCastHit.Entity);
                }
                if (allHits.Length > 0)
                    EntityCommandBuffer.AddComponent<DestroyEntityTag>(entityInQueryIndex, entity);
            }

            translation.Value = newPos;
            rotation.Value = quaternion.LookRotationSafe(newForwardVector, math.up());
        }
    }
}