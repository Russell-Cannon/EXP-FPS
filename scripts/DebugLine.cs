using Godot;
using System;
using System.Collections.Generic;

public partial class DebugLine : MeshInstance3D
{
	public static DebugLine Instance;
    ImmediateMesh mesh = new();
    StandardMaterial3D meshMaterial = new() {
        DisableReceiveShadows = true,
        NoDepthTest = true,
        AlbedoColor = new Color(0x00000011),
    };
    Camera3D cam = null;
    List<Vector3[]> Lines = new();

    public override void _Ready() 
    {
        Instance = this;
        Mesh = mesh;
        cam = GetViewport().GetCamera3D();
    }
    public override void _Process(double delta) 
    {
        mesh.ClearSurfaces();
        if (Lines.Count == 0)
            return;
        if (cam == null || !IsInstanceValid(cam)) {
            cam = GetViewport().GetCamera3D();
            return;
        }
        
        GlobalPosition = Vector3.Zero;

        mesh.ClearSurfaces();

        foreach (Vector3[] Line in Lines)
        {
            mesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip, meshMaterial);
            for (int i = 0; i < 2; i++) 
            {
                Vector3 position = Line[0].Lerp(Line[1], i);
                Vector3 dir = Line[0].DirectionTo(cam.GlobalPosition).Cross(Line[0].DirectionTo(Line[1])).Normalized() * 0.0065f * cam.GlobalPosition.DistanceTo(position)/2f;
                for (int j = 0; j < 2; j++) {
                    mesh.SurfaceAddVertex(ToLocal(Line[0].Lerp(Line[1], i) + dir*(j*2f - 1)));
                }
            }
            mesh.SurfaceEnd();
        }

        Lines.Clear();
    }
    public void DrawTriad(Vector3 point, float length = 1f) {
        DrawLine(point - Vector3.Right * length, point + Vector3.Right * length);
        DrawLine(point - Vector3.Up * length, point + Vector3.Up * length);
        DrawLine(point - Vector3.Forward * length, point + Vector3.Forward * length);
    }
    public void DrawLine(Vector3 start, Vector3 end) {
        Lines.Add([start, end]);
    }
}
