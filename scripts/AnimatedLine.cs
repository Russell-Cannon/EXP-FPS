using Godot;
using System;

public partial class AnimatedLine : LineRenderer
{
    [Export] public Node3D TargetPosition;
    [Export] Curve InfluenceOverDistance;
    [Export] Curve InfluenceOverTime;
    float density = 10f;
    float amplitude = 5f;
    float elapsedTime = 0f;
    float timeScale = -30f;
    float distanceScale = 2f;
    public override void _Process(double delta)
    {
        if (!IsInstanceValid(TargetPosition))
        {
            QueueFree();
            return;
        }

        Points.Clear();

        //Add <density> points per meter between position and target
        for (float i = 0f; i < 1f; i += 1f/(GlobalPosition.DistanceTo(TargetPosition.GlobalPosition)*density))
        {
            Points.Add(GlobalPosition.Lerp(TargetPosition.GlobalPosition, i));
        }

        base._Process(delta);
        elapsedTime += (float)delta;
    }

    public override Vector3 Calculate(int index)
    {
        // Calculate base point
        Vector3 BasePoint = base.Calculate(index);

        // Calculate Offset
        float Distance = TargetPosition.GlobalPosition.DistanceTo(BasePoint);
        float Rotation = Mathf.PosMod(Distance*distanceScale + elapsedTime*timeScale, Mathf.Tau);
        Vector3 OffsetDirection = cam.GlobalPosition.DirectionTo(GlobalPosition);
        OffsetDirection = OffsetDirection.Cross(GlobalPosition.DirectionTo(BasePoint)).Rotated(OffsetDirection, Rotation);
        Vector3 Offset = OffsetDirection * amplitude;

        // Apply Offset
        return BasePoint + Offset * InfluenceOverDistance.SampleBaked(Distance/GlobalPosition.DistanceTo(TargetPosition.GlobalPosition)) * InfluenceOverTime.SampleBaked(elapsedTime);
    }

}
