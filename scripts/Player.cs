using Godot;
using GodotSteam;

public partial class Player : CharacterBody3D
{
	[Export] double gravity = 20, jumpForce = 7, roofRebound = 1f, friction = 6, slideForce = 12;
	[Export] Camera3D Camera;
	[Export] CollisionShape3D Shape;
	[Export] RayCast3D WallReader;
	public Vector2 MoveInput;
	public PlayerStateMachine StateMachine = new();
	Vector3 groundSurfaceNormal = Vector3.Up;
	Vector3 wallSurfaceNormal = Vector3.Right;
	public const float AirControlMultiplier = 0.3f;
	public const float MaxSpeed = 7;
	public const float Acceleration = 1;
	public const float MaxAcceleration = 1;
	public const float WallRunMinimumSpeed = 3;
	public const float Mass = 70;
	Buffer jump = new(0.125f);

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
		if (StateMachine.CurrentState == PlayerStateMachine.State.Walking) {
			GroundMove((float)delta);
		} else if (StateMachine.CurrentState == PlayerStateMachine.State.Airborne) {
			AirMove((float)delta);
			groundSurfaceNormal = Vector3.Up;
		} else if (StateMachine.CurrentState == PlayerStateMachine.State.WallRunning) {
			WallMove((float)delta);
		}

		if (StateMachine.CurrentState != PlayerStateMachine.State.WallRunning)
			Velocity += Vector3.Down * (float)(gravity * delta); //v = a*t //Apply gravity

		MoveAndSlide();

		KinematicCollision3D col = GetLastSlideCollision();
		if (col != null) {
			Collide(col);
		}
		GD.Print(StateMachine.CurrentState + ": " + GetSpeed());
	}

//physics
    void Collide(KinematicCollision3D collision) {
		//Get the normal of the current surface
		Vector3 normal = collision.GetNormal();

		//If possible: push rigidbodies back as they push on us
		if (collision.GetCollider() is RigidBody3D rb)
			rb.ApplyCentralForce(Mass * Velocity.Project(normal));

		if (Vector3.Up.Dot(normal) < -0.5f) {
			//If the normal is facing downwards: bound off the surface.
			AddForce(normal * (float)roofRebound * Vector3.Down.Dot(normal));
		} else if (Vector3.Up.Dot(normal) > 0.5f) {
			//If the normal is facing upwards, cancel the affects of gravity
			groundSurfaceNormal = normal;
			StateMachine.Set(PlayerStateMachine.State.Walking);
		} else {
			//If the normal is mostly flat, begin wall running.
			wallSurfaceNormal = normal;
			if (GetSpeed() > WallRunMinimumSpeed)
				StateMachine.Set(PlayerStateMachine.State.WallRunning);
		}
	}

	private Vector3 GetDesiredDirection()
	{
		return (MoveInput.X * GlobalBasis.X) + (MoveInput.Y * GlobalBasis.Z);
	}
	private float GetSpeed()
	{
		return GodotMath.XZ(Velocity).Length();
	}
	private void AirMove(float delta)
	{
		//Accelerate if not going as fast as possible
		if (GetSpeed() < MaxSpeed) {
			Accelerate(GetDesiredDirection(), Acceleration * AirControlMultiplier, delta);
		} else if (GetDesiredDirection().Dot(GodotMath.XZ(Velocity)) < 0) {
			//Decelerate if trying to
			Accelerate(GetDesiredDirection(), Acceleration * AirControlMultiplier, delta);
		}
	}

	private void GroundMove(float delta) {
		if (jump.Active) { //Jump the second we hit the floor without applying friction
			Jump();
		} else {
			Accelerate(GetDesiredDirection(), Acceleration, delta);
		}
	}
	private void WallMove(float delta) {
		//Check if we want to leave the wall
		// Check if we want to jump off the wall
		if (jump.Active) {
			Jump();
			StateMachine.Set(PlayerStateMachine.State.Airborne);
			return;
		}
		// Check if we are moving off the wall
		if (GetDesiredDirection().Dot(GodotMath.XZ(wallSurfaceNormal)) > 0.5f) {
			StateMachine.Set(PlayerStateMachine.State.Airborne);
			return;
		}
		// Check if we are moving too slow to continue
		if (GetSpeed() < WallRunMinimumSpeed) {
			StateMachine.Set(PlayerStateMachine.State.Airborne);
			return;			
		}

		//Check if we already left the wall
		WallReader.GlobalPosition = GlobalPosition;
		WallReader.TargetPosition = -wallSurfaceNormal;
		GD.Print(WallReader.TargetPosition);
		if (!WallReader.IsColliding()) {
			StateMachine.Set(PlayerStateMachine.State.Airborne);
			return;
		}

		//Accelerate along the wall
		Vector3 wallTangent = GodotMath.XZ(wallSurfaceNormal).Cross(Vector3.Up);
		Vector3 desiredDirection = GetDesiredDirection().Project(wallTangent);
		// Accelerate if not going as fast as possible
		if (GetSpeed() < MaxSpeed) {
			Accelerate(desiredDirection, Acceleration, delta);
		} else if (desiredDirection.Dot(GodotMath.XZ(Velocity)) < 0) {
			//Decelerate if trying to
			Accelerate(desiredDirection, Acceleration, delta);
		}

		//Reduce Y-velocity
		float Y = Lerp.LerpHalfLife(Velocity.Y, 0, delta, .1f/Acceleration);
		Velocity = new Vector3(Velocity.X, Y, Velocity.Z);
	}
	public void Accelerate(Vector3 direction, float acceleration, float delta)
	{
		// Calculate new velocity after lerping
		Vector3 flatVelocity = Lerp.LerpHalfLife(GodotMath.XZ(Velocity), direction * MaxSpeed, delta, .1f/acceleration);
		// Reduce change if over acceleration limit
		if ((GodotMath.XZ(Velocity) - flatVelocity).Length() > MaxAcceleration)
			flatVelocity = Lerp.MoveTowards(GodotMath.XZ(Velocity), direction * MaxSpeed, MaxAcceleration);
		// Apply change
		Velocity = new Vector3(flatVelocity.X, Velocity.Y, flatVelocity.Z); //preserve Y velocity
	}
	public void AddForce(Vector3 force) {//impulse
		AddForce(force, 1);
	}
	public void AddForce(Vector3 force, double delta) {//real force
		/*	v(t) = |f(t)/m dt
			v(t) = F*t/m + C	*/
		Velocity += force*(float)delta/Mass;
	}
	
//input
	public void Jump() {
		// If already going down : cancel velocity
		if (Velocity.Dot(Vector3.Down) > 0)
			Velocity += -Velocity.Project(Vector3.Down); 
		
		// Add jump force
		if (StateMachine.CurrentState == PlayerStateMachine.State.WallRunning) {
			Velocity += (float)jumpForce * (Vector3.Up + wallSurfaceNormal - GodotMath.XZ(Camera.GlobalBasis.Z)).Normalized();
		} else {
			Velocity += (float)jumpForce * Vector3.Up;
		}
		
		// Quit being grounded
		StateMachine.Set(PlayerStateMachine.State.Airborne);
		
		//Stop the jump buffer here
		jump.Cancel();
	}
	public void Kick() {
		Velocity += (float)jumpForce * -Camera.GlobalBasis.Z;
	}
}