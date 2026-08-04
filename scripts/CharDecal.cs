using Godot;
using System;

public partial class CharDecal : Decal
{
    [Export] AnimationPlayer animationPlayer;
    public override void _Ready()
    {
        RotateZ(GD.Randf()*Mathf.Tau);
        animationPlayer.Play("CharDecal");
    }

}
