using Godot;
using System;

public partial class Duration
{
    public Duration() {
        time = 1f;
    }
    public Duration(float time) {
        this.time = time;
    }

    private float time = 1f;
    private Timer timer = null;
    public event Action OnDone;
    public void Set() {
        if (timer == null) {
            timer = new() {
                OneShot = true,
                Autostart = true,
                WaitTime = time
            };
            timer.Timeout += () => {OnDone?.Invoke();};
            Game.Instance.AddChild(timer);
        }
        timer.Start();
    }
    public void Cancel()
    {   
        timer?.Stop();
    }
    public bool Expired {
        get {
            if (timer == null) return true;
            return timer.TimeLeft <= 0;
        }
        set {}
    }
    public float TimeLeft {
        get {
            if (timer == null) return 0;
            return (float)timer.TimeLeft;
        }
        set {}
    }
    public float PercentLeft {
        get {
            if (timer == null) return 0f;
            if (timer.TimeLeft == 0) return 0f;
            return (float)timer.TimeLeft / time;
        }
        set {}
    }
    public float PercentDone {
        get {
            if (timer == null) return 1f;
            if (timer.TimeLeft == 0) return 1f;
            return 1f - ((float)timer.TimeLeft / time);
        }
        set {}
    }
}
