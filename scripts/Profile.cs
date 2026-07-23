using Godot;
using System;
using GodotSteam;

public partial class Profile : Node
{
	public static Profile Instance { get; private set; }
	public ulong ID { get; private set; }
	new public string Name { get; private set; }
	public override void _Ready()
	{
		Instance = this;

		var steamInitExResult = Steam.SteamInitEx(true, 480).Status;
		if(steamInitExResult > 0) {
			GD.Print($"Failed to initialize Steam (code: {steamInitExResult}), shutting down...");
			GetTree().Quit();
			return;
		}
		if (!Steam.IsSteamRunning()) {
			GD.Print("Steam is not running. Shutting down...");
			GetTree().Quit();
			return;
		}

		ID = Steam.GetSteamID();
		Name = Steam.GetPersonaName();

		GD.Print($"Successfully initialized Steam. {Name} ({ID})");
	}

}
