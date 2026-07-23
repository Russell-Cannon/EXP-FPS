using Godot;
using System;

public partial class Console : Control
{
	public static Console Instance {get; private set;} 
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
			input.Visible = true;
			input.GrabFocus();
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
		if (command[0] == '/') {
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
				case "version":
					Post(MatchMaker.VERSION);
					break;
				// Cheats
				case "float 1":
					Player.Gravity = 0f;
					break;
				case "float 0":
					Player.Gravity = 20f;
					break;
				default:
					Post("Unknown command: " + command);
					break;
			}
		} else {
			Network.Instance.SendMessage(command, true);
			Post(Profile.Instance.Name + ": " + command);
		}
		closeConsole();
	}

	public void Post(string message)
	{
		chat.AddChild(new Message(message));
		GD.Print(message);
	}

	private void closeConsole()
	{
		input.ReleaseFocus();
		input.Visible = false;
		input.Text = "";
	}
}
