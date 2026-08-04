using Godot;
using System;

//A script that lets us have a bool that resets itself when it returns true
public class Gated
{
    public Gated() {
        time = 1f;
    }
    public Gated(float time) {
        this.time = time;
    }

    private float time = 1f;
    private Timer timer = null;
    public bool Ready = true;

    public bool Use() {
		if (Ready) {
			Ready = false;
			countDown();
			return true;
		}
		return false;
    }
    public void Reset()
    {
        Ready = true;
        timer?.Stop();
    }
    private void countDown() {
        if (timer == null) {
            timer = new() {
                OneShot = true,
                Autostart = true,
                WaitTime = time
            };
            timer.Timeout += () => Ready = true;
            Game.Instance.AddChild(timer);
        }
        timer.Start();
    }
}
