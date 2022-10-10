using Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace DEMO
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(OrbitCameraSystem))]
    public partial class FireballSystem : SystemBase
    {
        private EntityQuery _entityQuery;
        private EntityQueryDesc _entityQueryDesc;
        private BuildPhysicsWorld _buildPhysicsWorld;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            _buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();

            _entityQueryDesc = new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<Translation>(),
                    ComponentType.ReadWrite<Rotation>(),
                    ComponentType.ReadOnly<PhysicsCollider>(),
                    ComponentType.ReadOnly<FireballTag>(),
                }
            };
        }
        
        protected override void OnStartRunning()
        {
            _entityQuery = GetEntityQuery(_entityQueryDesc);
            this.RegisterPhysicsRuntimeSystemReadOnly();
        }
        
        protected override void OnUpdate()
        {// get component from entity ignore destruction    
            var dontDestroyObjects = GetComponentDataFromEntity<DontDestroyTag>(true);
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            EntityCommandBuffer.ParallelWriter parallelEcb = ecb.AsParallelWriter();
            
            var job = new FireballJob()
            {
                CollisionWorld = _buildPhysicsWorld.PhysicsWorld.CollisionWorld,
                EntityCommandBuffer = parallelEcb,
                DontDestroyObjectsDataFromEntity = dontDestroyObjects,
                DeltaTime = Time.DeltaTime,
                Impulse = 42f,
                Gravity = -30f
            };
            
            Dependency = job.ScheduleParallel(_entityQuery, Dependency);
            Dependency.Complete();
            
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}