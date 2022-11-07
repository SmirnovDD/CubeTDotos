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

    public TurretData(TurretData turretData) : this()
    {
        Ammo = turretData.Ammo;
        Turret = turretData.Turret;
        ShotCooldown = turretData.ShotCooldown;
        ReloadTime = turretData.ReloadTime;
        SquaredShootDistance = turretData.SquaredShootDistance;
        ProjectileVelocity = turretData.ProjectileVelocity;
    }
}