using Godot;
using System;

public class Health
{
    public event Action OnDie;
    public int Points {get; private set;} = 100;
    public void TakeDamage(int damage)
    {
        Points -= damage;
        if (Points <= 0) OnDie?.Invoke();
    }
    public void Reset()
    {
        Points = 100;
    }
}
