using Godot;
using System;

public partial class Inventory : Node
{
    [Export] public Player player;
    public Weapon currentWeapon;
    public override void _Ready()
    {
        SetWeapon(WeaponType.BOW);
    }
    public void SetWeapon(WeaponType weaponType)
    {
        if (currentWeapon != null) 
            currentWeapon.QueueFree();
        
        Weapon weapon = Constants.Instance.WEAPON_TYPES[(int)weaponType].Instantiate<Weapon>();
        player.Camera.AddChild(weapon);
        currentWeapon = weapon;
        weapon.owner = player;
    } 
}
