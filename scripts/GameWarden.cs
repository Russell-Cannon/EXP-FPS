using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class GameWarden : Node
{
	public static GameWarden Instance { get; private set; }
    public Player LocalPlayer;
    public List<Actor> Actors = new();
	public Node3D Map;
    public override void _Ready()
	{
        Map = Constants.Instance.GYM_SCENE.Instantiate<Node3D>();
        AddChild(Map);
    }
    public override void _EnterTree()
    {
		Instance = this;
    }

    public override void _ExitTree()
    {
        Instance = null;
    }
    // Manage players
    public void AddPlayer(ulong ID) {
        if (ID == Profile.Instance.ID) {
            Console.Instance.Post("Player created: " + ID);
            LocalPlayer = Constants.Instance.PLAYER_SCENE.Instantiate<Player>();
            Map.AddChild(LocalPlayer);
        } else {
            Console.Instance.Post("Actor created: " + ID);
            Actor a = Constants.Instance.ACTOR_SCENE.Instantiate<Actor>();
            a.SetID(ID);
            Map.AddChild(a);
            Actors.Add(a);
        }
    }
    public void RemovePlayer(ulong ID) {
        for (int i = Actors.Count - 1; i >= 0; i--) {
            if (Actors[i].ID == ID) {
                Actors[i].QueueFree();
                Actors.RemoveAt(i);
            }
        }
    }
    public void KillCharacter(ulong ID)
    {
        GetCharacter(ID).QueueFree();
    }
    public Character GetCharacter(ulong ID)
    {
        if (ID == Profile.Instance.ID)
            return LocalPlayer;
        return GetActor(ID);
    }
    public Actor GetActor(ulong ID)
    {
        for (int i = Actors.Count - 1; i >= 0; i--) {
            if (Actors[i].ID == ID)
            {
                return Actors[i];
            }
        }
        GD.PrintErr("No actor exists for " + ID);
        return null;
    }
    // RPC
    public void DealDamage(ulong TargetID, int Damage)
    {
        if (Damage == 0) return;

        //Tell everyone else to make this update
        Network.Instance.SendPacketToAll(new Dictionary() {
            {"type", "damage"}, 
            {"damage", Damage},
            {"target", TargetID}
        }, true);

        TakeDamage(TargetID, Damage);
    }
    public void TakeDamage(ulong TargetID, int Damage)
    {
        //Do damage to the person in question
        GetCharacter(TargetID)?.Health.TakeDamage(Damage);

        //If to the local player: tell everyone our new health
        if (TargetID == Profile.Instance.ID)
        {
            Network.Instance.SendPacketToAll(new Dictionary()
            {
                {"type", "update"},
                {"health", LocalPlayer.Health.Points}
            }, true);
        }
    }

    // Networking
    public void Report(Vector3 position, Vector3 velocity, Vector2 rotation, PlayerStateMachine.State state)
    {
        Network.Instance.SendPacketToAll(new Dictionary() {
            {"type", "update"}, 
            {"position", position},
            {"velocity", velocity},
            {"rotation", rotation},
            {"state", (int)state}
        }, false);
    }
    public void Parse(Dictionary dict, ulong author)
    {
        if (dict.ContainsKey("position")) {
            GetActor(author)._SetPosition((Vector3)dict["position"]);
        }
        if (dict.ContainsKey("velocity")) {
            GetActor(author)._SetVelocity((Vector3)dict["velocity"]);
        }
        if (dict.ContainsKey("rotation")) {
            GetActor(author)._SetRotation((Vector2)dict["rotation"]);
        }
        if (dict.ContainsKey("state")) {
            GetActor(author).SetState((PlayerStateMachine.State)(int)dict["state"]);
        }
        if (dict.ContainsKey("health")) {
            GetActor(author).SetState((PlayerStateMachine.State)(int)dict["state"]);
        }
    }
}
