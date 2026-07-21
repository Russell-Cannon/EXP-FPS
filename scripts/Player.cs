using Godot;
using GodotSteam;

public partial class Player : CharacterBody3D
{
	[Export] double gravity = 20, jumpForce = 7, roofRebound = 1f, friction = 6, slideForce = 12;
	[Export] Camera3D Camera;
	[Export] CollisionShape3D Shape;
	public Vector2 MoveInput;
	Vector3 groundSurfaceNormal = Vector3.Up;
	float airControl = 0.3f;
	float maxSpeed = 7;
	float maxAcceleration = 1;
	Buffer jump = new(0.125f);
	const float MASS = 70;
	bool applyGravity = true;

	//Godot calls
	public override void _Ready() {
		Game.Instance.HideMouse();
    }
	
    public override void _Input(InputEvent @event) {
		if (Input.IsActionJustPressed("move_jump"))
			jump.Set();

		if (Input.IsActionPressed("move_right")) {
			MoveInput.X = 1;
		} else if (Input.IsActionPressed("move_left")) {
			MoveInput.X = -1;
		} else {
			MoveInput.X = 0;
		}

		if (Input.IsActionPressed("move_up")) {
			MoveInput.Y = -1;
		} else if (Input.IsActionPressed("move_down")) {
			MoveInput.Y = 1;
		} else {
			MoveInput.Y = 0;
		}

		MoveInput = MoveInput.Normalized();

		if (Input.IsActionJustPressed("move_kick"))
			Kick();

		if (@event is InputEventMouseMotion motion) {
			Vector2 distance = new(-Mathf.DegToRad((float)(motion.Relative.X * 0.125f)), Mathf.DegToRad((float)(motion.Relative.Y * 0.125f)));
			RotateY(distance.X);
			float rotation = Mathf.Clamp(Camera.Rotation.X - distance.Y, -GodotMath.HALF_PI, GodotMath.HALF_PI) - Camera.Rotation.X;
			Camera.RotateX(rotation);
		}
	}
	public override void _PhysicsProcess(double delta) {
		// Set Move		
		if (IsOnFloor()) {
			GroundMove((float)delta);
		} else {
			AirMove((float)delta);
			groundSurfaceNormal = Vector3.Up;
		}

		if (applyGravity)
			Velocity += Vector3.Down * (float)(gravity * delta); //v = a*t //Apply gravity

		MoveAndSlide();

		KinematicCollision3D col = GetLastSlideCollision();
		if (col != null) {
			Collide(col);
		}
		GD.Print(GodotMath.XZ(Velocity).Length());
	}

//physics
    void Collide(KinematicCollision3D collision) {
		//Get the normal of the current surface
		Vector3 normal = collision.GetNormal();

		//If possible: push rigidbodies back as they push on us
		if (collision.GetCollider() is RigidBody3D rb)
			rb.ApplyCentralForce(MASS * Velocity.Project(normal));

		//If the normal is facing downwards: bound off the surface.
		if (Vector3.Up.Dot(normal) < 0) {
			AddForce(normal * (float)roofRebound * Vector3.Down.Dot(normal));
		} else if (Vector3.Up.Dot(normal) > 0.5f) {
			//If the normal is facing mostly upwards, cancel the affects of gravity
			groundSurfaceNormal = normal;
		}
	}

	private Vector3 getWishDirection()
	{
		return (MoveInput.X * GlobalBasis.X) + (MoveInput.Y * GlobalBasis.Z);
	}
	private void AirMove(float delta)
	{
		//Accelerate if not going as fast as possible
		if (GodotMath.XZ(Velocity).Length() < maxSpeed)
			Accelerate(delta);
		//Decelerate if trying to
		else if (getWishDirection().Dot(GodotMath.XZ(Velocity)) < 0)
			Accelerate(delta);
	}

	private void GroundMove(float delta) {
		if (jump.Active) { //Jump the second we hit the floor without applying friction
			Jump();
		} else {
			Accelerate(delta);
		}
	}
	public void Accelerate(float delta)
	{
		// Calculate new velocity after lerping
		Vector3 flatVelocity = Lerp.LerpHalfLife(GodotMath.XZ(Velocity), getWishDirection() * maxSpeed, delta, 0.1f);
		// Reduce change if over acceleration limit
		if ((Velocity - flatVelocity).Length() > maxAcceleration)
			flatVelocity = Lerp.MoveTowards(GodotMath.XZ(Velocity), getWishDirection() * maxSpeed, maxAcceleration);
		// Apply change
		Velocity = new Vector3(flatVelocity.X, Velocity.Y, flatVelocity.Z); //preserve Y velocity
	}
	public void AddForce(Vector3 force) {//impulse
		AddForce(force, 1);
	}
	public void AddForce(Vector3 force, double delta) {//real force
		/*	v(t) = |f(t)/m dt
			v(t) = F*t/m + C	*/
		Velocity += force*(float)delta/MASS;
	}
	
//input
	public void Jump() {
		if (IsOnFloor()) {
			// If already going down : cancel velocity
			if (Velocity.Dot(Vector3.Down) > 0)
				Velocity += -Velocity.Project(Vector3.Down); 
			// Add jump force upwards
			Velocity += (float)jumpForce * Vector3.Up;
		}
		//Stop the jump buffer here
		jump.Cancel();
	}
	public void Kick() {
		Velocity += (float)jumpForce * -Camera.GlobalBasis.Z;
	}
}