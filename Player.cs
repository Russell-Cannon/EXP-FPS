using Godot;
using GodotSteam;
using System;
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
	[Export] double gravity = 20, jumpForce = 7, roofRebound = 1f, friction = 6, slideForce = 12, slideFriction = 0.125f, terminalVelocity = 50f, brakeAmount = 0.125f, wallJumpMult = 1.5f;
	[Export] Camera3D Camera;
	[Export] CollisionShape3D Shape;
	public Vector2 MoveInput;
	Vector3 groundSurfaceNormal = Vector3.Up;
	float airControl = 0.3f;
	MovementSettings groundSettings = new(7, 14, 10), airSettings = new(7, 2, 2), strafeSettings = new(1, 50, 50);
	Gated canShoot = new(0.4f), canReload = new(2.6f), canSlide = new(1.125f), canLash = new(0.125f);
	Buffer jump = new(0.125f);
	public bool Crouched = false;
	const float MASS = 70;


	//Godot calls
	public override void _Ready() {
		Game.Instance.HideMouse();
    }
	
    public override void _Input(InputEvent @event) {
		if (Input.IsActionJustPressed("move_jump"))
			jump.Set();

		if (Input.IsActionPressed("move_right")) {
			MoveInput.X = -1;
		} else if (Input.IsActionPressed("move_left")) {
			MoveInput.X = 1;
		} else {
			MoveInput.X = 0;
		}

		if (Input.IsActionPressed("move_up")) {
			MoveInput.Y = 1;
		} else if (Input.IsActionPressed("move_down")) {
			MoveInput.Y = -1;
		} else {
			MoveInput.Y = 0;
		}
		
		if (@event is InputEventMouseMotion motion) {
			Vector2 distance = new(-Mathf.DegToRad((float)(motion.Relative.X * 0.125f)), Mathf.DegToRad((float)(motion.Relative.Y * 0.125f)));
			RotateY(distance.X);
 			Camera.RotateX(distance.Y);
			ClampHead();
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
		Velocity += Vector3.Down * (float)(gravity * delta); //v = a*t //Apply gravity
		if (Velocity.Length() > terminalVelocity)
			Velocity = Velocity.Normalized() * (float)terminalVelocity;

		MoveAndSlide();

		KinematicCollision3D col = GetLastSlideCollision();
		if (col != null) {
			Collide(col);
		}
	}
    public override void _Process(double delta) {
		ClampHead();
    }

//physics
    void Collide(KinematicCollision3D collision) {
		Vector3 normal = collision.GetNormal();

		if (collision.GetCollider() is RigidBody3D rb)
			rb.ApplyCentralForce(MASS * Velocity.Project(normal));

		if (Vector3.Up.Dot(normal) < 0) { // Bounce off roof
			Addforce(normal * (float)roofRebound * Vector3.Down.Dot(normal));
		} else if (Vector3.Up.Dot(normal) > 0.5f) {
			groundSurfaceNormal = normal;
		}
	}
	private void AirMove(double delta)
	{
		float accel;

		var wishdir = (MoveInput.X * GlobalBasis.X) + (MoveInput.Y * GlobalBasis.Z);

		float wishspeed = wishdir.Length() * airSettings.MaxSpeed;
		wishdir = wishdir.Normalized();
		// CPM Air control
		if (Velocity.Dot(wishdir) < 0) {
			accel = airSettings.Deceleration;
		} else {
			accel = airSettings.Acceleration;
		}
		// If the player is ONLY strafing left or right
		if (MoveInput.Y == 0 && MoveInput.X != 0) {
			if (wishspeed > strafeSettings.MaxSpeed) {
				wishspeed = strafeSettings.MaxSpeed;
			}
			accel = strafeSettings.Acceleration;
		}
		Accelerate(wishdir, wishspeed, accel, delta);


		if (airControl > 0) {
			AirControl(wishdir, wishspeed, delta);
		}
	}
	private void AirControl(Vector3 targetDir, float wishspeed, double delta) {
		// Air control occurs when the player is in the air, it allows players to move side 
		// to side much faster rather than being 'sluggish' when it comes to cornering.

		// Only control air movement when moving forward or backward and actually moving
		if (Mathf.Abs(MoveInput.Y) < 0.001 || Mathf.Abs(wishspeed) < 0.001)
		{
			return;
		}
		Vector3 ySpeed = Velocity.Project(Vector3.Down); //record how much velocity is upward
		Velocity -= Velocity.Project(Vector3.Down); //remove all velocity downwards

		float speed = Velocity.Length();
		Velocity = Velocity.Normalized();

		float dot = Velocity.Dot(targetDir);
		double k = 32;
		k *= airControl * dot * dot * delta;

		Vector3 xSpeed = Velocity.Project(GlobalBasis.X);
		Vector3 zSpeed = Velocity.Project(GlobalBasis.Z); 

		// Change direction while slowing down.
		if (dot > 0) {
			Velocity = Vector3.Zero;

			xSpeed *= (float)(speed + targetDir.X * k);
			zSpeed *= (float)(speed + targetDir.Z * k);

			Velocity += xSpeed + zSpeed;

			Velocity = Velocity.Normalized();//change direction
		}

		Velocity *= speed;//restore magnitude
		Velocity += ySpeed; //preserves y velocity b/c the player could be falling
	}

	private void GroundMove(double delta) {
		// Handle ground movement.
		// Do not apply friction if the player is queueing up the next jump to maintain speed
		if (Crouched) {
			if (IsOnFloor()) //add force downhill
				Addforce((groundSurfaceNormal - Vector3.Down) * (float)gravity * MASS, delta);
			ApplyFriction(slideFriction, delta);
		}
		else
			ApplyFriction(1.0f, delta);

		Vector3 realDir = (MoveInput.X * GlobalBasis.X) + (MoveInput.Y * GlobalBasis.Z);
		realDir = GodotMath.AlignUpToNormal(groundSurfaceNormal, realDir).Z;
		var wishspeed = MoveInput.Length() * groundSettings.MaxSpeed;
		if (Crouched) realDir *= 0.125f;
		Accelerate(realDir, wishspeed, groundSettings.Acceleration, delta);
	}

	private void ApplyFriction(double t, double delta) {
		float speed = Velocity.Length();
		float control = speed < groundSettings.Deceleration ? groundSettings.Deceleration : speed;
		float newSpeed = speed - (float)(control * friction * delta * t);
		newSpeed = newSpeed < 0 ? 0 : newSpeed / speed;

		Velocity *= newSpeed;
	}

	private void Accelerate(Vector3 targetDir, double targetSpeed, double accel, double delta) {
		// Calculates acceleration based on desired speed and direction.
		// Because this is based on Quake, there is some an oversight causes airstrafing to behave the way it does
		double currentspeed = Velocity.Dot(targetDir); //This is where the oversight is. When we chose to move perpendicular to velocity, our speed is read as 0.
		double addspeed = targetSpeed - currentspeed;
		if (addspeed <= 0)
		{
			return;
		}

		double accelspeed = accel * delta * targetSpeed;
		if (accelspeed > addspeed)
		{
			accelspeed = addspeed;
		}
		Velocity += (float)accelspeed*targetDir;
	}

	public void Addforce(Vector3 force) {//impulse
		Addforce(force, 1);
	}
	public void Addforce(Vector3 force, double delta) {//real force
		/*	v(t) = |f(t)/m dt
			v(t) = F*t/m + C	*/
		Velocity += force*(float)delta/MASS;
	}
	private bool CanMoveInDirection(Vector3 direction)
	{
		if (!IsOnFloor() && !IsOnWall()) return true;

		return GetFloorNormal().Dot(direction) > -0.5f && GetWallNormal().Dot(direction) > -0.5f; // Allow movement if not directly against the normal
	}
	
//input
	public void Jump() {
		if (IsOnFloor()) {//floor jumps always bounce upward
			if (Velocity.Dot(Vector3.Down) > 0)
				Velocity += -Velocity.Project(Vector3.Down); // If already going down : cancel velocity
			Velocity += (float)jumpForce * Vector3.Up;
		} else if (IsOnWall()) { //wall jumps are towards normal
			Vector3 normal = GetWallNormal();
			if (Velocity.Dot(normal) > 0)
				Velocity += -Velocity.Project(normal);
			Velocity += (float)(jumpForce * wallJumpMult) * normal;
		}
		jump.Cancel();
	}
	void ClampHead() {
		Camera.Rotation = new Vector3(Mathf.Clamp(Camera.Rotation.X, -GodotMath.HALF_PI, GodotMath.HALF_PI), Camera.Rotation.Y, Camera.Rotation.Z);
	}
}