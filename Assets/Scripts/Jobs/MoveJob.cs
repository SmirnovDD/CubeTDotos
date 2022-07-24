using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct MoveJob : IJobEntity
{
    public float DeltaTime;
    public float3 TargetPos;
    
    public void Execute(ref Translation translation, in LocalToWorld localToWorld, in MoveData moveData)
    {
        var movementVector =  TargetPos - localToWorld.Position;
        var normalizedMovementVector = math.normalize(movementVector);
        translation.Value += normalizedMovementVector * moveData.Speed * DeltaTime;
    }
}