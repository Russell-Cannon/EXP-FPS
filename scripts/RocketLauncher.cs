using Godot;
using System;

public partial class RocketLauncher : Weapon
{
    public const float KickBack = 2f;
    Gated FireCoolDown = new (0.5f);
    Gated AltFireCoolDown = new (0.5f);
    public override void _Process(double delta) {
        if (Game.Instance.IsMouseFree()) return;

        if (Input.IsActionPressed("fire") && FireCoolDown.Use())
            Shoot(AmmoType.EXPLODING_ROCKET);

        if (Input.IsActionPressed("alt_fire") && AltFireCoolDown.Use())
            Shoot(AmmoType.ROCKET);
    }

    public override void Shoot(AmmoType type)
    {
        base.Shoot(type);
        //Kick back the player
        owner.Velocity += GlobalBasis.Z*KickBack;
    }

}
