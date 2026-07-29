using Godot;
using System;
public enum AmmoType
{
    EXPLODING_ROCKET,
    ROCKET,
};

public partial class Ammunition : Node3D
{
    [Export] public RayCast3D RayCast;
    [Export] public PackedScene Debris;
    public ulong Author;
    public Vector3 Direction;
    public virtual int Damage {get;} = 65;
}
