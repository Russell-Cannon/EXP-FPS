using Godot;
using System;

public partial class Message : Label
{
    Timer timer;
    public Message(string message)
    {
        Text = message;
    }
    public override void _Ready()
    {
        SetSize(new Vector2(325, 31));
        timer = new Timer
        {
            Autostart = true,
            WaitTime = 30
        };
        timer.Timeout += QueueFree;
        AddChild(timer);
    }

}
