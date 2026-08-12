using Godot;
using System;
public enum AmmoType
{
    EXPLODING_ROCKET,
    ROCKET,
    ARROW,
    TETHERED_ARROW
};

public partial class Ammunition : Node3D
{
    [Export] public RayCast3D RayCast;
    [Export] public PackedScene Debris;
    [Export] public Node3D Model;
    public ulong Author;
    public Vector3 Direction;
    public virtual int Damage {get;} = 65;
    public virtual float Speed {get;} = 100f;
}
