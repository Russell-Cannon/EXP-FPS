using System;
using Godot;
using GodotSteam;

public partial class Player : CharacterBody3D
{
	[Export] Camera3D Camera;
	[Export] CollisionShape3D Collider;
	[Export] CapsuleShape3D Shape;
	[Export] RayCast3D KickRayCast;
	[Export] RayCast3D WallReader;
	public Vector2 MoveInput;
	public PlayerStateMachine StateMachine = new();
	Vector3 groundSurfaceNormal = Vector3.Up;
	Vector3 wallSurfaceNormal = Vector3.Right;
	public const float AirControlMultiplier = 0.3f;
	public const float MaxSpeed = 7;
	public const float Acceleration = 1;
	public const float SlideFriction = 0.05f;
	public const float MaxAcceleration = 1;
	public const float WallRunMinimumSpeed = 3;
	public const float Mass = 70;
	public const float Gravity = 20;
	public const float JumpForce = 7f;
	public const float RoofRebound = 0.02f;
	public const float KickForce = 10f;
	public const float SlideForce = 5f;
	Buffer jump = new(0.125f);
	Gated kickCoolDown = new(1f);
	Gated slideCoolDown = new(1f);

	//Godot calls
	public override void _Ready() {
		Game.Instance.HideMouse();
    }
	
    public override void _Input(InputEvent @event) {
		if (Input.IsActionJustPressed("move_jump"))
			jump.Set();

		if (Input.IsActionJustPressed("move_kick"))
			AttemptKick();

		if (Input.IsActionJustPressed("move_slide"))
			AttemptSlide();

		if (Input.IsActionJustReleased("move_slide"))
			StateMachine.Set(PlayerStateMachine.State.Airborne, true);

		if (Input.IsActionJustPressed("console"))
			Velocity += 20f * -Camera.GlobalBasis.Z;

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
		// Update States
		StateMachine.Tick((float)delta);
		if (StateMachine.CurrentState == PlayerStateMachine.State.Kicking) {
			Kick();
		}

		// Update collider
		if (StateMachine.CurrentState == PlayerStateMachine.State.Sliding || StateMachine.CurrentState == PlayerStateMachine.State.Stalling || StateMachine.CurrentState == PlayerStateMachine.State.Kicking || StateMachine.CurrentState == PlayerStateMachine.State.KickWindUp) {
			Shape.Height = 1;
			Collider.Position = new Vector3(0, 1.5f, 0);
		} else {
			Shape.Height = 2;
			Collider.Position = new Vector3(0, 1.0f, 0);
		}

		// Move
		if (StateMachine.CurrentState == PlayerStateMachine.State.Walking) {
			GroundMove((float)delta);
		} else if (StateMachine.CurrentState == PlayerStateMachine.State.Airborne) {
			AirMove((float)delta);
			groundSurfaceNormal = Vector3.Up;
		} else if (StateMachine.CurrentState == PlayerStateMachine.State.WallRunning) {
			WallMove((float)delta);
		} else if (StateMachine.CurrentState == PlayerStateMachine.State.Sliding) {
			SlideMove((float)delta);
		} else if (StateMachine.CurrentState == PlayerStateMachine.State.Stalling) {
			AirStall((float)delta);
		}

		//Apply gravity
		if (StateMachine.CurrentState != PlayerStateMachine.State.WallRunning && StateMachine.CurrentState != PlayerStateMachine.State.Stalling)
			Velocity += Vector3.Down * (float)(Gravity * delta); //v = a*t 

		MoveAndSlide();

		KinematicCollision3D col = GetLastSlideCollision();
		if (col != null) {
			Collide(col);
		} else if (StateMachine.CurrentState == PlayerStateMachine.State.Walking) {
			//If we are not colliding with anything and we think we are walking
			StateMachine.Set(PlayerStateMachine.State.Airborne);
		}
		GD.Print(StateMachine.CurrentState + ": " + GetSpeed());
		if (KickRayCast.IsColliding())
			DebugLine.I.DrawLine(KickRayCast.GetCollisionPoint(), KickRayCast.GetCollisionPoint() + KickRayCast.GetCollisionNormal()/10);
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
			Velocity += normal * RoofRebound * Vector3.Down.Dot(normal);
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
			Accelerate(GetDesiredDirection(), delta, Acceleration * AirControlMultiplier);
		} else if (GetDesiredDirection().Dot(GodotMath.XZ(Velocity)) < 0) {
			//Decelerate if trying to
			Accelerate(GetDesiredDirection(), delta, Acceleration * AirControlMultiplier);
		}
	}

	private void GroundMove(float delta) {
		if (jump.Active) { //Jump the second we hit the floor without applying friction
			Jump();
		} else {
			Accelerate(GetDesiredDirection(), delta);
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
			Accelerate(desiredDirection, delta);
		} else if (desiredDirection.Dot(GodotMath.XZ(Velocity)) < 0) {
			//Decelerate if trying to
			Accelerate(desiredDirection, delta);
		}

		//Reduce Y-velocity
		float Y = Lerp.LerpHalfLife(Velocity.Y, 0, delta, .1f/Acceleration);
		Velocity = new Vector3(Velocity.X, Y, Velocity.Z);
	}
	public void SlideMove(float delta) {
		if (IsOnFloor()) {
			if (jump.Active) {
				Jump();
			} else {
				//Apply friction
				Accelerate(-Velocity.Normalized(), delta, SlideFriction, MaxAcceleration/10f);
			}
		} else {
			AirMove(delta);
		}

	}
	public void AirStall(float delta) {
		AirMove(delta);
		
		//Reduce Y-velocity
		float Y = Lerp.LerpHalfLife(Velocity.Y, 0, delta, .1f/Acceleration);
		Velocity = new Vector3(Velocity.X, Y, Velocity.Z);
	}
	public void Accelerate(Vector3 direction, float delta, float acceleration = Acceleration, float maxAcceleration = MaxAcceleration)
	{
		// Calculate new velocity after lerping
		Vector3 flatVelocity = Lerp.LerpHalfLife(GodotMath.XZ(Velocity), direction * MaxSpeed, delta, .1f/acceleration);
		// Reduce change if over acceleration limit
		if ((GodotMath.XZ(Velocity) - flatVelocity).Length() > maxAcceleration)
			flatVelocity = Lerp.MoveTowards(GodotMath.XZ(Velocity), direction * MaxSpeed, maxAcceleration);
		// Apply change
		Velocity = new Vector3(flatVelocity.X, Velocity.Y, flatVelocity.Z); //preserve Y velocity
	}
	public void RedirectMomentum() {
		// Don't redirect if the player doesn't want to
		if (MoveInput == Vector2.Zero) return;

		// Don't redirect if almost stopped
		float speed = GodotMath.XZ(Velocity).Length();
		if (speed < 0.125f) return;
		
		// Reduce speed by penalty for changing direction
		float difference = GodotMath.XZ(Velocity).Normalized().Dot(GetDesiredDirection()) - 1f;
		float penalty = Mathf.Cos(difference * Mathf.Pi / 4f);

		// Redirect speed
		Velocity = new Vector3(GetDesiredDirection().X * speed * penalty, Velocity.Y, GetDesiredDirection().Z * speed * penalty);
	}	
//input
	public void Jump() {
		// If already going down : cancel velocity
		if (Velocity.Dot(Vector3.Down) > 0)
			Velocity += -Velocity.Project(Vector3.Down); 
		
		// Add jump force
		if (StateMachine.CurrentState == PlayerStateMachine.State.WallRunning) {
			Velocity += JumpForce * (Vector3.Up + wallSurfaceNormal - GodotMath.XZ(Camera.GlobalBasis.Z)).Normalized();
		} else {
			Velocity += JumpForce * Vector3.Up;
		}
		
		// Quit being grounded
		StateMachine.Set(PlayerStateMachine.State.Airborne);
		
		//Stop the jump buffer here
		jump.Cancel();
	}
	public void AttemptKick() {
		if (kickCoolDown.Ready) {
			// If close enough to hit: attempt to hit
			if (KickRayCast.IsColliding()) {
				StateMachine.Set(PlayerStateMachine.State.KickWindUp);
				kickCoolDown.Use();
			} else if (StateMachine.CurrentState == PlayerStateMachine.State.Airborne) {
				StateMachine.Set(PlayerStateMachine.State.Stalling);
				RedirectMomentum();
				kickCoolDown.Use();
			}
		}
	}
	public void Kick() {
		// If hit: kick back
		if (KickRayCast.IsColliding()) {
			if (KickRayCast.GetCollider() is RigidBody3D rb)
				rb.ApplyCentralForce(Mass * KickForce * Velocity.Project(KickRayCast.GetCollisionNormal()));
			
			Vector3 knockBack = (KickRayCast.GetCollisionPoint().DirectionTo(Camera.GlobalPosition) + KickRayCast.GetCollisionNormal()).Normalized();
			Velocity += KickForce * knockBack;
			StateMachine.Set(PlayerStateMachine.State.Airborne);
		}
	}
	public void AttemptSlide() {
		if (StateMachine.CurrentState == PlayerStateMachine.State.Walking) {
			Slide();
		}
		StateMachine.Set(PlayerStateMachine.State.Sliding);
	}
	public void Slide() {
		if (slideCoolDown.Use()) 
			Velocity += GodotMath.XZ(-Camera.GlobalBasis.Z) * SlideForce;
		GlobalPosition -= new Vector3(0, 1, 0);
	}
}