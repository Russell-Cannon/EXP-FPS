using Godot;
using System;
using System.Collections.Generic;


public partial class DebugInfo : Node
{
	[Export] Label text;
	public static DebugInfo Instance {get; private set;}
	Dictionary<string, string> messages = new();
    public override void _Ready()
    {
		Instance = this;
    }

	void UpdateMessages() {
		text.Text = "";
		foreach (string m in messages.Values) {
			text.Text += m + "\n";			
		}
	}
    public void Post(string key, string message) {
        messages[key] = message;
		UpdateMessages();
	}
}
