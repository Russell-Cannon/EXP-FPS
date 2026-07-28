using Godot;
using System;

public partial class CameraEffects : Camera3D
{
    [Export] public Player player;
    //Accessibility settings
    public static float RotationStrength = 1f;
    public static float TranslationStrength = 1f;
    //Tracked values
    Vector3 BasePosition;
    Vector3 LeanDirection = Vector3.Up;
    float LeanAmount = 0;
    Vector3 Velocity = Vector3.Zero;
    float Acceleration = 1f;
    float Friction = 1f;
    public override void _Ready()
    {
        BasePosition = Position;
    }

    public override void _Process(double delta)
    {
        //Apply rotations
        LeanAmount = Lerp.LerpHalfLife(LeanAmount, player.WallRunTime.PercentLeft, (float)delta, 0.05f);
        Rotation = new Vector3(Rotation.X, Rotation.Y, Mathf.Pi * 0.125f * RotationStrength * LeanAmount * GlobalBasis.X.Dot(LeanDirection));
        
        //Apply translations
        Position += Velocity * (float)delta;
        Velocity = Lerp.LerpHalfLife(Velocity, Vector3.Zero, (float)delta, .1f/Acceleration);

        //Reduce position
        Position = Lerp.LerpHalfLife(Position, BasePosition, (float)delta, .1f/Friction);

        //Clip position
        if (Position.DistanceTo(BasePosition) > 0.5f * TranslationStrength)
            Position = BasePosition.DirectionTo(Position).Normalized() * 0.5f * TranslationStrength + BasePosition;
    }
    public void Land(float force)
    {
        AddForce(Vector3.Up * force);
    }
    public void Jump() {}
    public void Lean(Vector3 WallNormal)
    {
        LeanDirection = -WallNormal;
    }
    public void Vault(Vector3 Point)
    {
        GlobalPosition = player.GlobalPosition + BasePosition - player.GlobalPosition.DirectionTo(Point) * player.GlobalPosition.DistanceTo(Point);
    }
    public void AddForce(Vector3 Force)
    {
        Velocity += Force/3f;
    }

}
