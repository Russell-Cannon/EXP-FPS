using Godot;
using System;

public class Health
{
    public event Action OnDie;
    public event Action<int> OnUpdate;
    public int Points {get; private set;} = 100;
    public void TakeDamage(int damage)
    {
        if (Points - damage <= 0) {
            Set(0);
            OnDie?.Invoke();
        } else {
            Set(Points - damage);
        }
    }
    public void Set(int points)
    {
        Points = points;
        OnUpdate?.Invoke(Points);
    }
}
