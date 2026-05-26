using Godot;
using GodotSteam;

public struct MovementSettings
{
	public float MaxSpeed, Acceleration, Deceleration;
	public MovementSettings(float maxSpeed, float accel, float decel)
	{
		MaxSpeed = maxSpeed;
		Acceleration = accel;
		Deceleration = decel;
	}
}

public partial class Player : CharacterBody3D
{
	[Export] double gravity = 20, jumpForce = 7, roofRebound = 1f, friction = 6, slideForce = 12;
	[Export] Camera3D Camera;
	[Export] CollisionShape3D Shape;
	public Vector2 MoveInput;
	Vector3 groundSurfaceNormal = Vector3.Up;
	float airControl = 0.3f;
	MovementSettings groundSettings = new(7, 14, 10), airSettings = new(7, 2, 2);
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
			if (jump.Active) { //Jump the second we hit the floor without applying friction
				Jump();
			} else {
				GroundMove(delta);
			}
		} else {
			AirMove(delta);
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
	private void AirMove(double delta)
	{
		float Acceleration;

		// Get the direction the player wants to go in place
		var wishdir = (MoveInput.X * GlobalBasis.X) + (MoveInput.Y * GlobalBasis.Z);
		// The players wants to go as fast as possible times whether or not they even want to move
		float wishspeed = wishdir.Length() * airSettings.MaxSpeed;

		if (Velocity.Dot(wishdir) > 0) {
			Acceleration = airSettings.Acceleration;
		} else {
			Acceleration = airSettings.Deceleration;
		}

		Accelerate(wishdir, wishspeed, Acceleration, delta);
	}

	private void GroundMove(double delta) {
		// Handle ground movement.
		ApplyFriction(delta);

		//Convert the players input to a vector pointing where the player wants to go
		Vector3 realDir = (MoveInput.X * GlobalBasis.X) + (MoveInput.Y * GlobalBasis.Z);
		//Align that vector such that walking forward on a slope is actually partially walking downwards
		realDir = GodotMath.AlignUpToNormal(groundSurfaceNormal, realDir).Z;
		
		//Accelerate the player towards where they want to go
		Accelerate(realDir, MoveInput.Length() * groundSettings.MaxSpeed, groundSettings.Acceleration, delta);
	}

	private void ApplyFriction(double delta) {
		//Get speed
		float speed = Velocity.Length();

		//Raise friction in the lower bound of speed to keep movement snappy
		float control = speed < groundSettings.Deceleration ? groundSettings.Deceleration : speed;
		//Get amount of speed to lose based on current speed * friction per second
		float speedLoss = (float)(control * friction * delta);

		//Clamp newspeed to avoid slowing down into the opposite direction.
		if (speedLoss > speed) 
			speedLoss = speed;

		//Lower velocity by the ratio of new speed to current speed
		if (speed != 0)
			Velocity *= (speed - speedLoss) / speed;
	}

	private void Accelerate(Vector3 targetDir, double targetSpeed, double accel, double delta) {
		// Accelerate towards the desired direction.
		// Get how much of our velocity is towards were we want to go.
		double currentspeed = Velocity.Dot(targetDir);
		// Get how much speed we have left to gain.
		double addspeed = targetSpeed - currentspeed;
		// Get how much speed should be added per second
		double accelspeed = accel * delta * targetSpeed;
		// Don't add enough speed to overpass the max speed
		if (accelspeed > addspeed)
			accelspeed = addspeed;

		// Subtract amount of speed we intend to gain
		if (GodotMath.XZ(Velocity).Length() > targetSpeed)
			Velocity -= (float)accelspeed * GodotMath.XZ(Velocity).Normalized();
		
		// Add velocity
		Velocity += (float)accelspeed*targetDir;
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
}