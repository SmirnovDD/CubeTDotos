using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Utils;

public struct CharacterControllerHorizontalCollisionsCollector : ICollector<ColliderCastHit>
{
    public bool EarlyOutOnFirstHit => false;
    public float MaxFraction => 1;
    public int NumHits => AllHits.Length;

    public NativeList<ColliderCastHit> AllHits;
    private readonly Translation _translation;
    private readonly float _colliderRadius;
    private readonly Entity _thisEntity;

    public CharacterControllerHorizontalCollisionsCollector(NativeList<ColliderCastHit> allHits, Entity thisEntity, in Translation translation, float colliderRadius)
    {
        AllHits = allHits;
        _thisEntity = thisEntity;
        _colliderRadius = colliderRadius;
        _translation = translation;
    }

    public bool AddHit(ColliderCastHit hit)
    {
        if (hit.Entity == _thisEntity)
            return false;
        
        var verticalCollisionPosition = hit.Position;
        var vectorFromPointToCenter2D = verticalCollisionPosition.ToFloat2() - _translation.Value.ToFloat2();
        if (vectorFromPointToCenter2D.SqrMagnitude() >= (_colliderRadius * _colliderRadius) - MathUtilities.Epsilon)
            return false;

        AllHits.Add(hit);
        return true;
    }
}
