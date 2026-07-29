using System;
using Godot;
using Godot.Collections;

public partial class Rocket : Area3D
{
    [Export] RayCast3D rayCast;
    [Export] Node3D decalTransform;
    [Export] Decal decal;
    [Export] Curve KnockBackFallOff;
    [Export] Curve DamageFallOff;

    public ulong Author;
    Vector3 Direction;
    public const float Speed = 50f;
    public const float Radius = 0.25f;
    public const float KnockBack = 12.5f;
    public const int MaxDamage = 65;
    public override void _PhysicsProcess(double delta)
    {
        if (rayCast.IsColliding())
        {
            GlobalPosition = rayCast.GetCollisionPoint();
            //Spawn decal
            SpawnDecal();

            //Do damage
            SplashDamage();
            
            //Delete
            QueueFree();
        } else
        {
            GlobalPosition += Direction*Speed*(float)delta;
            UpdateRayCast((float)delta);
        }
    }

    public void SetDirection(Vector3 direction)
    {
        Direction = direction;
        UpdateRayCast(1);
    }
    void UpdateRayCast(float delta)
    {
        rayCast.GlobalPosition = GlobalPosition - Direction*Radius;
        rayCast.TargetPosition = Direction*(Speed*delta + Radius*2f);
    }
    void SpawnDecal()
    {
        decalTransform.Visible = true;
        decalTransform.GlobalPosition = GlobalPosition;
        if (rayCast.GetCollisionNormal() == Vector3.Up) 
            decalTransform.RotateX(Mathf.Pi/2f);
        else 
            decalTransform.LookAt(GlobalPosition + rayCast.GetCollisionNormal());
        decal.RotateZ(GD.Randf()*Mathf.Tau);
        decalTransform.Reparent(rayCast.GetCollider() as Node3D);
    }
    void SplashDamage()
    {
        if (!HasOverlappingBodies()) return;
        Array<Node3D> bodies = GetOverlappingBodies();
        foreach (Node3D n in bodies)
        {
            if (n is Character)
            {
                Character c = n as Character;
                float distance = GlobalPosition.DistanceTo(c.Collider.GlobalPosition);
                c.Velocity += KnockBack*GlobalPosition.DirectionTo(c.Collider.GlobalPosition)*KnockBackFallOff.SampleBaked(distance);
                if (Author == Profile.Instance.ID) //If locally owned: do damage
                    GameWarden.Instance?.DealDamage(c.ID, (int)((float)MaxDamage*DamageFallOff.SampleBaked(distance)));
            }
        }
    }
}
