using Godot;
using System;
using GodotSteam;

public partial class Actor : CharacterBody3D
{
    [Export] public Label3D nameTag;
    public ulong ID { get; private set;}
    public void SetID(ulong id)
    {
        ID = id;
        nameTag.Text = Steam.GetFriendPersonaName(id);
    }
    public void MoveActor(Vector3 position)
    {
        GlobalPosition = position;
    }
}
