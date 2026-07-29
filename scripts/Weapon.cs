using Godot;
using System;

public partial class Weapon : RayCast3D
{
    [Export] public Character owner;
    public Gated CoolDown = new(0.5f);
    public const float KickBack = 2f;
    public void Shoot()
    {
        Vector3 dir = -GlobalBasis.Z;
        if (IsColliding())
            dir = GlobalPosition.DirectionTo(GetCollisionPoint());

        SpawnProjectile(dir);
        GameWarden.Instance?.TellSpawnProjectile(dir);

        //Kick back the player
        owner.Velocity += GlobalBasis.Z*KickBack;
    }
    public Rocket SpawnProjectile(Vector3 direction)
    {
        //Instance projectile
        Rocket projectile = Constants.Instance.ROCKET_SCENE.Instantiate<Rocket>();
        GameWarden.Instance?.AddChild(projectile);
        projectile.Author = owner.ID;

        //Set path
        projectile.GlobalPosition = GlobalPosition;
        projectile.SetDirection(direction);
        
        return projectile;
    }

}
