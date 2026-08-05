using Godot;
using System;

public partial class Console : Control
{
	public static Console Instance {get; private set;} 
	public bool DebugEnabled = false;
	LineEdit input;
	VBoxContainer chat;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		input = new();
		input.CaretBlink = true;
		input.Visible = false;
		input.TextSubmitted += Execute;
		AddChild(input);
		input.SetSize(new Vector2(325, 31));

		PanelContainer panel = new();
		panel.CustomMinimumSize = new Vector2(325, 0);
		panel.SetPosition(new Vector2(0, 31));
		AddChild(panel);
		
		chat = new();
		chat.Visible = true;
		panel.AddChild(chat);
	}

    public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("console") || Input.IsActionJustPressed("chat") && !input.Visible) {
			openConsole();
			if (Input.IsActionJustPressed("chat"))
				GetViewport().SetInputAsHandled();
		}
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			closeConsole();
		}
	}

	public void Execute(string command)
	{
		if (command.Length == 0) { }
		else if (command[0] == '/') {
			//Ignore starting character and case
			switch (command.ToLower()[1..])
			{
				// Networking
				case "findmatch":
					MatchMaker.Instance.FindMatch();
					break;
				case "host":
					MatchMaker.Instance.Host();
					break;
				case "leave":
					MatchMaker.Instance.LeaveMatch();
					break;
				case "lobby":
					if (MatchMaker.Instance.Lobby == null)
						Post("Not in a lobby");
					else {
						string lobbyInfo = MatchMaker.Instance.Lobby.ID + ": {";
						foreach (ulong member in MatchMaker.Instance.Lobby.Members) 
							lobbyInfo += member + " ";
						Post(lobbyInfo + "}");
					}
					break;
				case "kill":
					if (GetPlayer())
						GameWarden.Instance.LocalPlayer.ReSpawn();
					break;
				case "give bow":
					if (GetPlayer())
						GameWarden.Instance.LocalPlayer.Inventory.SetWeapon(WeaponType.BOW);
					break;
				case "give rocket":
					if (GetPlayer())
						GameWarden.Instance.LocalPlayer.Inventory.SetWeapon(WeaponType.ROCKET_LAUNCHER);
					break;
				case "version":
					Post(Constants.Instance.VERSION);
					break;
				// Cheats
				case "float 1":
					if (GetPlayer())
						GameWarden.Instance.LocalPlayer.Gravity = 0f;
					break;
				case "float 0":
					if (GetPlayer())
						GameWarden.Instance.LocalPlayer.Gravity = 20f;
					break;
				case "debug":
					DebugEnabled = true;
					break;
				default:
					Post("Unknown command: " + command);
					break;
			}
		} else {
			Network.Instance.SendMessage(command);
			Post(Profile.Instance.Name + ": " + command);
			if (MatchMaker.Instance.Lobby == null)
				Post("No one heard that.");
			else if (MatchMaker.Instance.Lobby.Members.Count == 1)
				Post("No one else is online.");
		}
		closeConsole();
	}
	public bool GetPlayer()
	{
		if (GameWarden.Instance == null)
		{
			Post("Not in a game");
			return false;
		}
		if (GameWarden.Instance.LocalPlayer == null)
		{
			Post("Not alive");
			return false;
		}
		return true;
	}

	public void Post(string message, bool DebugMessage = false)
	{
		if (DebugEnabled || !DebugMessage)
			chat.AddChild(new Message(message));
		GD.Print(message);
	}

	private void closeConsole()
	{
		input.ReleaseFocus();
		input.Visible = false;
		input.Text = "";
		if (Game.Instance.Playing) Game.Instance.HideMouse();
	}
	void openConsole()
	{
		input.Visible = true;
		input.GrabFocus();
		Game.Instance.FreeMouse();
	}
}
