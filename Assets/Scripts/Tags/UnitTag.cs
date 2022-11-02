using Unity.Entities;

[GenerateAuthoringComponent]
public struct UnitTag : IComponentData
{
    public Entity Unit;
}