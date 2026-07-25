using Godot;
using System;
using GodotSteam;

public partial class Actor : CharacterBody3D
{
    [Export] public Label3D nameTag;
    public ulong ID { get; private set;}
    public Vector3 KnownPosition;
    public void SetID(ulong id)
    {
        ID = id;
        nameTag.Text = Steam.GetFriendPersonaName(id);
    }
    public void MoveActor(Vector3 _Position)
    {
        KnownPosition = _Position;
    }
    public override void _Process(double delta)
    {
        GlobalPosition = Lerp.LerpHalfLife(GlobalPosition, KnownPosition, (float)delta, 0.0125f);
    }
}