using Godot;
using System;

//A script that lets us have a bool turns itself off after a time
public class Buffer
{
    public Buffer() {
        time = 1f;
    }
    public Buffer(float time) {
        this.time = time;
    }

    private float time = 1f;
    private Timer timer = null;
    public bool Active = false;

    public void Set() {
		Active = true;
		countDown();
    }
    public void Cancel() {
		Active = false;
		timer?.Stop();
    }
    private void countDown() {
        if (timer == null) {
            timer = new() {
                OneShot = true,
                Autostart = true,
                WaitTime = time
            };
            timer.Timeout += () => Active = false;
            Game.Instance.AddChild(timer);
        }
        timer.Start();
    }
}
