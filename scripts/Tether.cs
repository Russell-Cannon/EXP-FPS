using Godot;

public partial class Tether : Node3D
{
    [Export] public LineRenderer Cable;
    public Node3D Anchor;
    public Character Author;
    public float Distance;
    public override void _Ready()
    {
        Cable.Points.Add(Anchor.GlobalPosition);
        Cable.Points.Add(GlobalPosition);
        SetDistance();
    }
    public override void _Process(double delta)
    {
        if (!IsInstanceValid(Anchor))
        {
            QueueFree();
            return;
        }
        Cable.Points[0] = Anchor.GlobalPosition;
        Cable.Points[1] = GlobalPosition;
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!IsInstanceValid(Author)) {
            QueueFree();
            return;
        }
        
        // Handle end-of-rope 
        if (GlobalPosition.DistanceTo(Author.GlobalPosition) > Distance)
        {
            Vector3 dir = GlobalPosition.DirectionTo(Author.GlobalPosition);
            
            // Snap player to be within proper distance
            Author.GlobalPosition = dir*Distance + GlobalPosition;

            // Remove any velocity towards the end of the rope
            if (Author.Velocity.Normalized().Dot(dir) > 0)
                Author.Velocity -= Author.Velocity.Project(dir);
        }
    }
    public void SetDistance()
    {
        Distance = Author.GlobalPosition.DistanceTo(GlobalPosition);
    }
}
