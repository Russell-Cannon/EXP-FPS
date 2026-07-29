using Godot;
using System;

public partial class Weapon : RayCast3D
{
    [Export] Player player;
    Gated CoolDown = new(0.5f);
    public const float KickBack = 2f;
    public override void _Input(InputEvent @event) {
        if (Input.IsActionPressed("fire") && CoolDown.Use())
            Shoot();

    }
    public void Shoot()
    {
        //Instance projectile
        Rocket projectile = Constants.Instance.ROCKET_SCENE.Instantiate<Rocket>();
        GameWarden.Instance?.AddChild(projectile);
        projectile.Author = Profile.Instance.ID;

        //Set path
        Vector3 dir = -GlobalBasis.Z;
        if (IsColliding())
            dir = GlobalPosition.DirectionTo(GetCollisionPoint());
        projectile.GlobalPosition = GlobalPosition;
        projectile.SetDirection(dir);

        //Kick back the player
        player.Velocity += GlobalBasis.Z*KickBack;
    }

}
