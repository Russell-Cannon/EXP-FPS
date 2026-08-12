using Godot;
using System;

public partial class Bow : Weapon
{
    HeldInput primaryFire = new("fire", 0.75f);
    Gated altFireCooldown = new(2);
    public Tether CurrentTether = null;
    public bool AltFire = false;
    public float ReelForce = 25f;
    public float ReelSpeed = 2f;
    float strength = 0f;
    public override void _Ready()
    {
        AddChild(primaryFire);
        primaryFire.LongPress += () =>
        {
            strength = 1f;
            Shoot(AltFire && altFireCooldown.Use() ? AmmoType.TETHERED_ARROW : AmmoType.ARROW);
        };
        primaryFire.EarlyRelease += (percentElapsed) =>
        {
            strength = percentElapsed;
            Shoot(AltFire && altFireCooldown.Use() ? AmmoType.TETHERED_ARROW : AmmoType.ARROW);
        };
    }
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Game.Instance.IsMouseFree()) return;

        // Reel into the tether
        if (Input.IsActionPressed("alt_fire") && IsInstanceValid(CurrentTether))
        {
            Reel(owner.GlobalPosition.DirectionTo(CurrentTether.GlobalPosition), (float)delta);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Game.Instance.IsMouseFree()) return;
        
        // If not tethered: toggle the alt fire state
        if (Input.IsActionJustPressed("alt_fire") && CurrentTether == null)
            AltFire = !AltFire;

        // Break the tether if attempting to
        if (Input.IsActionJustPressed("move_slide") && IsInstanceValid(CurrentTether))
            BreakTether();
    }

    public override void Shoot(AmmoType type)
    {
        AltFire = false;
        Vector3 dir = -GlobalBasis.Z;
        if (IsColliding())
            dir = SpawnPoint.GlobalPosition.DirectionTo(GetCollisionPoint());

        SpawnProjectile(dir, type, strength);
        GameWarden.Instance?.TellSpawnProjectile(dir, type, strength);
        DebugInfo.Instance.Post("bow_power", "Last Shot Power: "+strength);
    }
    public void Reel(Vector3 direction, float delta)
    {
        if (CurrentTether == null) return;
        owner.Velocity += direction * ReelForce * delta;
        owner.GlobalPosition += direction * ReelSpeed * delta;
        CurrentTether.SetDistance();
    }
    public void BreakTether()
    {
        CurrentTether.QueueFree();
        CurrentTether = null;
    }
}
