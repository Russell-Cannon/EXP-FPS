using Godot;
using System;

public partial class Constants : Node
{
	public static Constants Instance { get; private set; }
	[Export] public string VERSION = "ExperimentalFPSPre-Alpha";
  [Export] public PackedScene PLAYER_SCENE;
  [Export] public PackedScene ACTOR_SCENE;
  [Export] public PackedScene GYM_SCENE;
  [Export] public PackedScene ROCKET_SCENE;
  public override void _Ready()
  {
    Instance = this;
  }

}
