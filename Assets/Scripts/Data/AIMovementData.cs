using Unity.Entities;

[GenerateAuthoringComponent]
public struct AIMovementData : IComponentData
{
    public float SquaredStoppingDistance;
    public float ObstacleAvoidanceDistance;
    public float RotationSpeed;
}