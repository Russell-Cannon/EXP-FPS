using Godot;
using System;
using System.Collections.Generic;

public partial class LineRenderer : MeshInstance3D
{
    [Export] StandardMaterial3D MeshMaterial;
    [Export] public float Thickness = 0.125f;
    public List<Vector3> Points = new();
    protected ImmediateMesh mesh = new();
    protected Camera3D cam = null;

    public override void _Ready()
    {
        Mesh = mesh;
        cam = GetViewport().GetCamera3D();
    }
    public override void _Process(double delta)
    {
        Draw();
    }
    public void Draw()
    {
        mesh.ClearSurfaces();
        if (Points.Count < 2)
            return;
        if (cam == null || !IsInstanceValid(cam)) {
            cam = GetViewport().GetCamera3D();
            return;
        }
        
        mesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip, MeshMaterial);
        for (int i = 0; i < Points.Count; i++)
        {
            // Get the point
            Vector3 point = Calculate(i);

            // Get the direction perpendicular to where to the line is pointing
            Vector3 dir;
            if (i < Points.Count - 1)
                dir = point.DirectionTo(cam.GlobalPosition).Cross(point.DirectionTo(Calculate(i + 1))).Normalized();
            else 
                dir = point.DirectionTo(cam.GlobalPosition).Cross(Calculate(i - 1).DirectionTo(point)).Normalized();

            mesh.SurfaceAddVertex(ToLocal(point - dir*Thickness/2f));
            mesh.SurfaceAddVertex(ToLocal(point + dir*Thickness/2f));
        }
        mesh.SurfaceEnd();
    }

    public virtual Vector3 Calculate(int index)
    {
        return Points[index];
    }
}
