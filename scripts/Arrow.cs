using Godot;
using System;

public partial class Arrow : Rocket
{
    public float Strength = 1f;
    public override int Damage {get;} = 105;
    float AppliedGravity = 0;
    public override void _Process(double delta)
    {
        if (GetVelocity().LengthSquared() < 0.01f) //don't update
            return;

        if (GetVelocity().Normalized().Dot(Vector3.Up) > 0.99)
            Model.Rotation = new Vector3(Mathf.Pi/2f, 0, 0);
        else if (GetVelocity().Normalized().Dot(Vector3.Down) > 0.99)
            Model.Rotation = new Vector3(-Mathf.Pi/2f, 0, 0);
        else 
            Model.LookAt(GlobalPosition + GetVelocity());
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        AppliedGravity += 20f*(float)delta;
    }
    public override void SpawnDecal()
    {
        Model.GlobalPosition = RayCast.GetCollisionPoint() - GetVelocity().Normalized()*0.25f;
        Model.Reparent(RayCast.GetCollider() as Node3D);
    }

    public override Vector3 GetVelocity()
    {
        return Direction*Speed*Strength + AppliedGravity*Vector3.Down;
    }

    public override void DealDamage()
    {
        if (RayCast.GetCollider() is Character)
        {
            if (Author == Profile.Instance.ID) //If locally owned: do damage
                GameWarden.Instance?.DealDamage((RayCast.GetCollider() as Character).ID, (int)(Damage*Strength));
        }
    }

}
