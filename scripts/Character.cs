using Godot;
using System;

public partial class Character : CharacterBody3D
{
    public Health Health = new();
    public PlayerStateMachine.State State;
	[Export] public CollisionShape3D Collider;
	[Export] public CapsuleShape3D Shape;
    public ulong ID { get; private set;}
    public virtual void SetID(ulong id)
    {
        ID = id;
        Health.OnDie += () =>
        {
            GameWarden.Instance?.KillCharacter(id);
            GameWarden.Instance?.AddPlayer(id);
        };
    }
}
