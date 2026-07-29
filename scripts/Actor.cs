using Godot;
using System;
using GodotSteam;

public partial class Actor : Character
{
    [Export] public Label nameTag;
    [Export] CapsuleMesh Mesh;
    [Export] CsgShape3D CSGShape;
    [Export] Node3D Neck;
    [Export] ColorRect healthBar;
    public Vector3 KnownPosition = Vector3.Zero;
    public Vector2 KnownRotation = Vector2.Zero;
    Buffer interpolating = new(0.025f);
    public override void SetID(ulong id)
    {
        nameTag.Text = Steam.GetFriendPersonaName(id);
        Health.OnUpdate += (h) => {healthBar.CustomMinimumSize = new Vector2(h*504/100, 25);};
        base.SetID(id);
    }
    public void _SetPosition(Vector3 _Position)
    {
        KnownPosition = _Position;
        interpolating.Set();
    }
    public void _SetVelocity(Vector3 _Velocity)
    {
        Velocity = _Velocity;
    }
    public void _SetRotation(Vector2 _Rotation)
    {
        KnownRotation = _Rotation;
    }
    public void SetState(PlayerStateMachine.State state)
    {
        State = state;
    }
    public override void _Process(double delta)
    {
        Rotation = new Vector3(Rotation.X, Lerp.LerpHalfLifeRadial(Rotation.Y, KnownRotation.X, (float)delta, 0.0125f), Rotation.Z);
        Neck.Rotation = new Vector3(Lerp.LerpHalfLifeRadial(Neck.Rotation.X, KnownRotation.Y, (float)delta, 0.0125f), Neck.Rotation.Y, Neck.Rotation.Z);

        if (interpolating.Active)
            GlobalPosition = Lerp.LerpHalfLife(GlobalPosition, KnownPosition, (float)delta, 0.0125f);
        else 
            MoveAndSlide();

        if (State == PlayerStateMachine.State.Sliding || State == PlayerStateMachine.State.KickWindUp)
            Crouch();
        else
            Stand();
    }
	public void Crouch() {
		Shape.Height = 1;
        Mesh.Height = 1;
		Collider.Position = new Vector3(0, 1.5f, 0);
		CSGShape.Position = new Vector3(0, 1.5f, 0);
    }
	public void Stand() {
		Shape.Height = 2;
        Mesh.Height = 2;
		Collider.Position = new Vector3(0, 1.0f, 0);
		CSGShape.Position = new Vector3(0, 1.0f, 0);
	}
}