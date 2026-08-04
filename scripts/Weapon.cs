using Godot;
using System;

public partial class Weapon : RayCast3D
{
    [Export] public Character owner;

    public virtual void Shoot(AmmoType type)
    {
        Vector3 dir = -GlobalBasis.Z;
        if (IsColliding())
            dir = GlobalPosition.DirectionTo(GetCollisionPoint());

        SpawnProjectile(dir, type);
        GameWarden.Instance?.TellSpawnProjectile(dir, type);
    }
    public virtual void SpawnProjectile(Vector3 direction, AmmoType type)
    {
        //Instance projectile
        Ammunition projectile = Constants.Instance.AMMO_TYPES[(int)type].Instantiate<Ammunition>();
        GameWarden.Instance?.AddChild(projectile);
        projectile.Author = owner.ID;

        //Set path
        projectile.GlobalPosition = GlobalPosition;
        projectile.Direction = direction;
    }
}
