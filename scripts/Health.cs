using Godot;
using System;

public class Health
{
    public event Action OnDie;
    public event Action<int> OnUpdate;
    public int Points {get; private set;} = 100;
    public void TakeDamage(int damage)
    {
        Points -= damage;
        OnUpdate?.Invoke(Points);
        if (Points <= 0) OnDie?.Invoke();
    }
    public void Set(int points)
    {
        Points = points;
        OnUpdate?.Invoke(Points);
    }
}
