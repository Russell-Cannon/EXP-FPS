using System;
using Godot;
using Godot.Collections;

public partial class Rocket : Ammunition
{
    public virtual float Speed {get;} = 100f;
    public virtual float Radius {get;} = 0.25f;
    public virtual float KnockBack {get;} = 12.5f;
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
            GlobalPosition += Direction*Speed*(float)delta;
            UpdateRayCast((float)delta);
        }
    }
    public void UpdateRayCast(float delta)
    {
        RayCast.GlobalPosition = GlobalPosition - Direction*Radius;
        RayCast.TargetPosition = Direction*(Speed*delta + Radius*2f);
    }
    public void SpawnDecal()
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
