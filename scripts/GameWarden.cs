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
		Instance = this;
    }
    public override void _ExitTree()
    {
        Instance = null;
    }
    // Manage players
    public void AddPlayer(ulong ID, bool Local) {
        if (Local) {
            GD.Print("Player created: " + ID);
            LocalPlayer = Constants.Instance.PLAYER_SCENE.Instantiate<Player>();
            Map.AddChild(LocalPlayer);
        } else {
            GD.Print("Actor created: " + ID);
            Actor a = Constants.Instance.ACTOR_SCENE.Instantiate<Actor>();
            a.SetID(ID);
            Map.AddChild(a);
            Actors.Add(a);
        }
    }
    public void RemovePlayer(ulong ID) {
        for (int i = Actors.Count - 1; i >= 0; i--) {
            if (Actors[i].ID == ID) {
                KillPlayer(Actors[i]);
                Actors.RemoveAt(i);
            }
        }
    }
    public void KillPlayer(Actor actor)
    {
        actor.QueueFree();
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
    public void ReportPosition(Vector3 position)
    {
        Network.Instance.SendPacketToAll(new Dictionary() {
            {"type", "update"}, 
            {"position", position}
        }, false);
    }
    public void Parse(Dictionary dict, ulong author)
    {
        if (dict.ContainsKey("position")) {
            GetActor(author).MoveActor((Vector3)dict["position"]);
        }
    }
}
