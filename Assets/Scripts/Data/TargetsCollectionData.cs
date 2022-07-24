using Unity.Entities;
using Unity.Transforms;

[GenerateAuthoringComponent]
public struct TargetsCollectionData : IComponentData
{
    public Entity Target;
}