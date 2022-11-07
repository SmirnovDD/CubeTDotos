using System.Linq;
using Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Utils;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
[DisableAutoCreation]
public partial class TestSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var buildPhysicsWorld = World.GetExistingSystem<BuildPhysicsWorld>();
        var collisionWorld = buildPhysicsWorld.PhysicsData.PhysicsWorld.CollisionWorld;
        
        // Dependency = Entities.WithAll<AIMovementData>().WithReadOnly(collisionWorld).ForEach((Entity entity, ref Translation translation, ref Rotation rotation, ref PhysicsCollider collider) =>
        // {
        //     var checkForStepCollision = PhysicsUtilities.ColliderCastAll(collider, float3.zero, new float3(0,1f,1),  collisionWorld, entity, CollisionFilters.DynamicWithPhysicalExcludingTerrain, Allocator.Temp);
        //     checkForStepCollision.Dispose();
        //     
        //     NativeList<ColliderCastHit> allHits = new NativeList<ColliderCastHit>(Allocator.Temp);
        //     var collector = new CharacterControllerHorizontalCollisionsCollector(allHits, entity, translation, 1);
        //     
        //     var verticalCollisions = PhysicsUtilities.ColliderCastAll(collider, translation.Value, translation.Value - new float3(0, 0.0f, 0),  collisionWorld, CollisionFilters.DynamicWithPhysical, collector);
        //     if (verticalCollisions.Length == 0)
        //         Debug.Log($"{verticalCollisions.Length}");
        //    
        //     verticalCollisions.Dispose();
        // }).WithBurst().ScheduleParallel(Dependency);
        Dependency = Entities.WithAll<FireballTag>().ForEach((Entity entity, ref LocalToWorld localToWorld, ref Translation translation, ref Rotation rotation) =>
        {
            translation.Value = new float3(0, 5, 0);
            rotation.Value = quaternion.LookRotation(new float3(1, 0, 1), math.up());
            Debug.DrawRay(translation.Value, localToWorld.Forward);
        }).WithBurst().ScheduleParallel(Dependency);
        Dependency.Complete();
    }
}
