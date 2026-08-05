using Godot;
using System;

public partial class Bow : Weapon
{
    HeldInput input = new("fire", 0.75f);
    float strength = 0f;
    public override void _Ready()
    {
        AddChild(input);
        input.LongPress += () =>
        {
            strength = 1f;
            Shoot(AmmoType.ARROW);
        };
        input.EarlyRelease += (percentElapsed) =>
        {
            strength = percentElapsed;
            Shoot(AmmoType.ARROW);
        };
    }
    public override void Shoot(AmmoType type)
    {
        Vector3 dir = -GlobalBasis.Z;
        if (IsColliding())
            dir = SpawnPoint.GlobalPosition.DirectionTo(GetCollisionPoint());

        SpawnProjectile(dir, type, strength);
        GameWarden.Instance?.TellSpawnProjectile(dir, type, strength);
        DebugInfo.Instance.Post("bow_power", "Last Shot Power: "+strength);
    }
}
