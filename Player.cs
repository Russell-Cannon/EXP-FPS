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
	[Export] double gravity = 20, jumpForce = 7, roofRebound = 1f, friction = 6, slideForce = 12;
	[Export] Camera3D Camera;
	[Export] CollisionShape3D Shape;
	public Vector2 MoveInput;
	Vector3 groundSurfaceNormal = Vector3.Up;
	float airControl = 0.3f;
	MovementSettings groundSettings = new(7, 14, 10), airSettings = new(7, 2, 2), strafeSettings = new(1, 50, 50);
	Buffer jump = new(0.125f);
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

		MoveAndSlide();

		KinematicCollision3D col = GetLastSlideCollision();
		if (col != null) {
			Collide(col);
		}
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

		// If the player is ONLY strafing left or right
		if (MoveInput.Y == 0 && MoveInput.X != 0) {
			//Clamp desired speed to max speed
			if (wishspeed > strafeSettings.MaxSpeed) {
				wishspeed = strafeSettings.MaxSpeed;
			}
			Acceleration = strafeSettings.Acceleration;
		} else { // Otherwise moving forwards/backwards or diagonnaly
			//If moving forwards: accelerate. If moving backwards: decelerate
			if (Velocity.Dot(wishdir) > 0) {
				Acceleration = airSettings.Acceleration;
			} else {
				Acceleration = airSettings.Deceleration;
			}
		}

		//Accelerate 
		Accelerate(wishdir, wishspeed, Acceleration, delta);

		//Apply air control 
		AirControl(wishdir, wishspeed, delta);
	}
	private void AirControl(Vector3 targetDir, float wishspeed, double delta) {
		// Air strafe the player is in the air
		// Allows players to move turn mid-air (corner)

		// Only control air movement when moving forward or backward and actually moving
		if (MoveInput.Y == 0 || Mathf.Abs(wishspeed) < 0.001)
			return;

		//Store how much velocity is facing downwards and remove it
		Vector3 ySpeed = Velocity.Project(Vector3.Down); 
		Velocity -= Velocity.Project(Vector3.Down); 

		//Store the current speed then remove it from the player
		float speed = Velocity.Length();
		Velocity = Velocity.Normalized();

		//Get the distance between where the player is going and where they want to go
		float dot = Velocity.Dot(targetDir);
		double k = 32;
		k *= airControl * dot * dot * delta; //!!!

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

		//Restore speed
		Velocity *= speed;
		//Restore downwards velocity
		Velocity += ySpeed;
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
		Velocity *= (speed - speedLoss) / speed;
	}

	private void Accelerate(Vector3 targetDir, double targetSpeed, double accel, double delta) {
		// Calculates acceleration based on desired speed and direction.
		// Get how much of our velocity is towards were we want to go.
		double currentspeed = Velocity.Dot(targetDir);
		// Get how much speed we have left to gain.
		double addspeed = targetSpeed - currentspeed;
		// If going faster than we want: don't slow down
		if (addspeed <= 0)
			return;

		// Get how much speed should be added per second
		double accelspeed = accel * delta * targetSpeed;
		// Don't add enough speed to overpass the max speed
		if (accelspeed > addspeed)
			accelspeed = addspeed;

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
	void ClampHead() {
		//Stop the camera from looking too far up or down
		Camera.Rotation = new Vector3(Mathf.Clamp(Camera.Rotation.X, -GodotMath.HALF_PI, GodotMath.HALF_PI), Camera.Rotation.Y, Camera.Rotation.Z);
	}
}