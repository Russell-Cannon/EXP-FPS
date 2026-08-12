using Godot;
using System;

public partial class TetheredArrow : Arrow
{
    public Node3D Anchor;
    [Export] public AnimatedLine Cable;
    public override void _EnterTree()
    {
        Weapon w;
        GD.Print(Author + " == " + Profile.Instance.ID + " " + (Author == Profile.Instance.ID));
        if (Author == Profile.Instance.ID)
            w = GameWarden.Instance.LocalPlayer.Inventory.currentWeapon;
        else w = GameWarden.Instance.GetActor(Author).weapon;
        Anchor = w.SpawnPoint;
        Cable.TargetPosition = w.SpawnPoint;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }
    public override void SpawnDecal()
    {
        // Do not let the player tether themselves.
        if (RayCast.GetCollider() is Player) return;

        Tether tether = Debris.Instantiate<Tether>();
        tether.Anchor = Anchor;
        tether.Author = GameWarden.Instance.GetCharacter(Author);
        AddChild(tether);
        tether.GlobalPosition = Model.GlobalPosition;
        tether.GlobalRotation = Model.GlobalRotation;
        tether.Reparent(RayCast.GetCollider() as Node3D);


        if (Author == Profile.Instance.ID)
        {
            if (!IsInstanceValid(GameWarden.Instance.LocalPlayer)) return;
            Player p = GameWarden.Instance.LocalPlayer;
            if (p.Inventory.currentWeapon is Bow)
                (p.Inventory.currentWeapon as Bow).CurrentTether = tether;
        }
    }
}
