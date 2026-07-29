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
			Console.Instance.Post($"Failed to initialize Steam (code: {steamInitExResult})");
			// GetTree().Quit();
			return;
		}
		if (!Steam.IsSteamRunning()) {
			Console.Instance.Post("Steam is not running");
			// GetTree().Quit();
			return;
		}

		ID = Steam.GetSteamID();
		Name = Steam.GetPersonaName();

		Console.Instance.Post($"Successfully initialized Steam. Hello {Name} ({ID})", true);
	}

}
