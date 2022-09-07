using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Utils;
[DisableAutoCreation]
public partial class TestSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        var collisionWorld = buildPhysicsWorld.PhysicsData.PhysicsWorld.CollisionWorld;
        
        Entities.WithAll<TestData>().ForEach((Entity entity, ref Translation translation, ref Rotation rotation, ref PhysicsCollider collider) =>
        {
            var horizontalCollisions = PhysicsUtilities.ColliderCastAll(collider, translation.Value, translation.Value + new float3(0,0,5),  collisionWorld, entity, CollisionFilters.DynamicWithPhysical, Allocator.Temp);
            for (int i = 0; i < horizontalCollisions.Length; i++)
            {
                Debug.Log($"{horizontalCollisions[i].Entity}");
            }
            horizontalCollisions.Dispose();
        }).WithoutBurst().Run();
    }
}
