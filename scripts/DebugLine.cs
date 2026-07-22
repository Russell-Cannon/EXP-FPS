using Godot;
using System;

public partial class DebugLine : MeshInstance3D
{
	public static DebugLine I;
    public int lastingTime = 0;
    ImmediateMesh mesh = new();
    StandardMaterial3D meshMaterial = new() {
        DisableReceiveShadows = true,
        NoDepthTest = true,
        AlbedoColor = new Color(0x00000011)
    };
    Camera3D cam = null;
    int lastDrewFrame;
    public override void _Ready() {
        I = this;
        Mesh = mesh;
        cam = GetViewport().GetCamera3D();
    }
    public override void _Process(double delta) {
        if (Engine.GetFramesDrawn() > lastDrewFrame + lastingTime)
            mesh.ClearSurfaces();
    }
    public void DrawLine(Vector3 start, Vector3 end, float LineThickness = 0.00625f, bool flat = true) {
        if (cam == null) {
            cam = GetViewport().GetCamera3D();
            return;
        }

        lastDrewFrame = Engine.GetFramesDrawn();

        GlobalPosition = start;
        mesh.ClearSurfaces();

        mesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip, meshMaterial);
        
        for (int i = 0; i < 2; i++) {
            Vector3 position = start.Lerp(end, i);
            Vector3 dir = start.DirectionTo(cam.GlobalPosition).Cross(start.DirectionTo(end)).Normalized() * LineThickness * (flat ? cam.GlobalPosition.DistanceTo(position) : 1f)/2f;//Fix sizes decreasing over distance, if flat
            for (int j = 0; j < 2; j++) {
                mesh.SurfaceAddVertex(ToLocal(start.Lerp(end, i) + dir*(j*2f - 1)));
            }
        }

        mesh.SurfaceEnd();
    }
}
