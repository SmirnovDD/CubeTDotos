using Unity.Entities;

[GenerateAuthoringComponent]
public struct TurretData : IComponentData
{
    public Entity Ammo;
    public Entity Turret;
    public float ShotCooldown;
    public float ReloadTime;
    public float SquaredShootDistance;
    public float ProjectileVelocity;
}