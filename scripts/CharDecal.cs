using Godot;
using System;

public partial class CharDecal : Decal
{
    public override void _Ready()
    {
        RotateZ(GD.Randf()*Mathf.Tau);
    }

}
