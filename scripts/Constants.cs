using Godot;
using System;
using System.Collections.Generic;

public partial class Constants : Node
{
	public static Constants Instance { get; private set; }
	[Export] public string VERSION = "ExperimentalFPSPre-Alpha";
  [Export] public PackedScene PLAYER_SCENE;
  [Export] public PackedScene ACTOR_SCENE;
  [Export] public PackedScene GYM_SCENE;
  [Export] public PackedScene[] AMMO_TYPES;
  [Export] public PackedScene[] WEAPON_TYPES;
  public override void _Ready()
  {
    Instance = this;
  }

}
