using Unity.Entities;
using Unity.Transforms;

public partial class UnitMoveJobTestSystem : SystemBase
{
    private TargetsCollectionData _targetsCollectionData;
    private EntityQuery _movementQuery;


    protected override void OnStartRunning()
    {
        _targetsCollectionData = GetSingleton<TargetsCollectionData>();
        
        _movementQuery = EntityManager.CreateEntityQuery(
            ComponentType.ReadWrite<Translation>(),
            ComponentType.ReadOnly<MoveData>()
        );
    }

    protected override void OnUpdate()
    {
        var job = new MoveJob();

        var deltaTime = Time.DeltaTime;
        job.DeltaTime = deltaTime;

        var target = _targetsCollectionData.Target;
        var targetTranslation = GetComponentDataFromEntity<Translation>(true);
        job.TargetPos = targetTranslation[target].Value;
        
        Dependency = job.ScheduleParallel(_movementQuery, Dependency);
    }
}