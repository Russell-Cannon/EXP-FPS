using System;
using Godot;
using Godot.Collections;

public partial class ExplodingRocket : Rocket
{
    [Export] public Area3D AreaOfEffect;
    [Export] public Curve KnockBackFallOff;
    [Export] public Curve DamageFallOff;
    public override float Speed {get;} = 50f;

    public override void DealDamage()
    {
        if (!AreaOfEffect.HasOverlappingBodies()) return;
        Array<Node3D> bodies = AreaOfEffect.GetOverlappingBodies();
        foreach (Node3D n in bodies)
        {
            if (n is Character)
            {
                Character c = n as Character;
                float distance = GlobalPosition.DistanceTo(c.Collider.GlobalPosition);
                c.Velocity += KnockBack*GlobalPosition.DirectionTo(c.Collider.GlobalPosition)*KnockBackFallOff.SampleBaked(distance);
                if (Author == Profile.Instance.ID) //If locally owned: do damage
                    GameWarden.Instance?.DealDamage(c.ID, (int)((float)Damage*DamageFallOff.SampleBaked(distance)));
            }
        }
    }
}
