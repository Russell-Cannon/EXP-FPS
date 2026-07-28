using Godot;
using System;

public partial class HeldInput : Node
{
    public string InputName;
    public float Window;
    public bool HeldLongEnough = false;
    private Timer timer = null;
    public event Action ShortPress;
    public event Action HeldLong;
    public event Action LongPress;
    public HeldInput(string inputName, float window)
    {
        InputName = inputName;
        Window = window;
    }
    public override void _Ready()
    {
        timer = new() {
            OneShot = true,
            Autostart = false,
            WaitTime = Window
        };
        timer.Timeout += () => {
            HeldLongEnough = true;
            HeldLong?.Invoke();
        };
        Game.Instance.AddChild(timer);
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed(InputName)) {
            //start timer
            HeldLongEnough = false;
            timer.Start();
        }
        if (Input.IsActionJustReleased(InputName)) {
            if (HeldLongEnough) {
                //call long
                LongPress?.Invoke();
            } else {
                //call short
                ShortPress?.Invoke();
                timer?.Stop();
            }
        }

    }

}
