using System;
using Godot;
using Godot.Collections;

public partial class Rocket : Ammunition
{
    public virtual float Radius {get;} = 0.25f;
    public override void _PhysicsProcess(double delta)
    {
        if (RayCast.IsColliding())
        {
            GlobalPosition = RayCast.GetCollisionPoint();
            //Spawn decal
            SpawnDecal();

            //Do damage
            DealDamage();
            
            //Delete
            QueueFree();
        } else
        {
            GlobalPosition += GetVelocity()*(float)delta;
            UpdateRayCast((float)delta);
        }
    }
    public virtual Vector3 GetVelocity()
    {
        return Direction*Speed;
    }
    public virtual void UpdateRayCast(float delta)
    {
        Vector3 dir = GetVelocity().Normalized();
        RayCast.GlobalPosition = GlobalPosition - dir*Radius;
        RayCast.TargetPosition = GetVelocity()*delta + dir*Radius*2f;
    }
    public virtual void SpawnDecal()
    {
        Node3D Decal = Debris.Instantiate<Node3D>();
        AddChild(Decal);
        Decal.GlobalPosition = GlobalPosition;
        if (RayCast.GetCollisionNormal() == Vector3.Up) 
            Decal.RotateX(Mathf.Pi/2f);
        else 
            Decal.LookAt(GlobalPosition + RayCast.GetCollisionNormal());
        Decal.Reparent(RayCast.GetCollider() as Node3D);
    }
    public virtual void DealDamage()
    {
        if (RayCast.GetCollider() is Character)
        {
            if (Author == Profile.Instance.ID) //If locally owned: do damage
                GameWarden.Instance?.DealDamage((RayCast.GetCollider() as Character).ID, Damage);
        }
    }
}
