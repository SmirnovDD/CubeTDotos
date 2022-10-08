using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace DEMO
{
    [UpdateInGroup(typeof (SimulationSystemGroup))]
    //[UpdateAfter()]
     public partial class EntityDestroySystem : SystemBase
     {
         private EntityQuery _query;
         private EntityQueryDesc _entityQueryDesc;
         private EntityManager _entityManager;
    
         protected override void OnCreate()
         {
             base.OnCreate();
             _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
             _entityQueryDesc = new EntityQueryDesc()
             {
                 All = new[]
                 {
                     ComponentType.ReadOnly<DestroyEntityTag>(),
                 }
             };
         }
    
         protected override void OnStartRunning()
         {
             base.OnStartRunning();
             _query = GetEntityQuery(_entityQueryDesc);
         }
         
         protected override void OnUpdate()
         {
             EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
             EntityCommandBuffer.ParallelWriter parallelEcb = ecb.AsParallelWriter();

             var job = new EntityDestroyJob()
             {
                 EntityCommandBuffer = parallelEcb
             };
    
             Dependency = job.ScheduleParallel(_query, Dependency);
             Dependency.Complete();
            
             ecb.Playback(EntityManager);
             ecb.Dispose();
         }
     }
    
     [BurstCompile]
     public partial struct EntityDestroyJob : IJobEntity
     {
         public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;
    
         public void Execute(Entity entity, [EntityInQueryIndex] int entityInQueryIndex)
         {
             EntityCommandBuffer.DestroyEntity(entityInQueryIndex, entity);
         }
    }
}