using Godot;
using System;

public enum WeaponType
{
    ROCKET_LAUNCHER,
    BOW
};

public partial class Weapon : RayCast3D
{
    [Export] public Node3D SpawnPoint;
    public Character owner;

    public virtual void Shoot(AmmoType type)
    {
        Vector3 dir = -GlobalBasis.Z;
        if (IsColliding())
            dir = SpawnPoint.GlobalPosition.DirectionTo(GetCollisionPoint());

        SpawnProjectile(dir, type, -1);
        GameWarden.Instance?.TellSpawnProjectile(dir, type);
    }
    public virtual Ammunition SpawnProjectile(Vector3 direction, AmmoType type, float property)
    {
        //Instance projectile
        Ammunition projectile = Constants.Instance.AMMO_TYPES[(int)type].Instantiate<Ammunition>();
        GameWarden.Instance?.AddChild(projectile);
        projectile.Author = owner.ID;

        //Set path
        projectile.GlobalPosition = SpawnPoint.GlobalPosition;
        projectile.Direction = direction;

        //Set properties
        if (type == AmmoType.ARROW)
            (projectile as Arrow).Strength = property;

        return projectile;
    }
}
