using Godot;
using System;

public partial class Console : Node
{
	public static Console Instance {get; private set;} 
	LineEdit input;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;

		input = new();
		input.CaretBlink = true;
		input.Visible = false;
		input.SetSize(new Vector2(325, 31));
		input.TextSubmitted += Execute;
		AddChild(input);
	}

    public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("console"))
		{
			if (input.Visible)
			{
				closeConsole();
			} else {
				input.Visible = true;
				input.GrabFocus();
			}
		}
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			closeConsole();
		}
	}

	public void Execute(string command)
	{
		// do not execute regular messages
		if (command[0] != '/') return;
		//Ignore starting character and case
		switch (command.ToLower()[1..])
		{
			case "findmatch":
				MatchMaker.Instance.FindMatch();
				break;
			case "host":
				MatchMaker.Instance.Host();
				break;
			case "float 1":
				Player.Gravity = 0f;
				break;
			case "float 0":
				Player.Gravity = 20f;
				break;
			default:
				GD.Print("Unknown command: " + command);
				break;
		}
		closeConsole();
	}

	private void closeConsole()
	{
		input.ReleaseFocus();
		input.Visible = false;
		input.Text = "";
	}
}
