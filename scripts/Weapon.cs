using Godot;
using System;

public partial class Weapon : RayCast3D
{
    [Export] public Character owner;
    public Gated[] CoolDown = {new(0.5f), new(0.5f)};
    public const float KickBack = 2f;
    public void Shoot(AmmoType type)
    {
        if (!CoolDown[(int)type].Use()) return;

        Vector3 dir = -GlobalBasis.Z;
        if (IsColliding())
            dir = GlobalPosition.DirectionTo(GetCollisionPoint());

        SpawnProjectile(dir, type);
        GameWarden.Instance?.TellSpawnProjectile(dir, type);

        //Kick back the player
        owner.Velocity += GlobalBasis.Z*KickBack;
    }
    public void SpawnProjectile(Vector3 direction, AmmoType type)
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
