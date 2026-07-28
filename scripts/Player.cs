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
	[Export] RayCast3D GroundReader;
	[Export] RayCast3D StepUpReader;
	[Export] RayCast3D HeadSpaceReader;
	public Vector2 MoveInput;
	public PlayerStateMachine StateMachine = new();
	Vector3 groundSurfaceNormal = Vector3.Up;
	Vector3 wallSurfaceNormal = Vector3.Right;
	Vector3 lastSurfaceTouched = Vector3.Up;
	public const float AirControlMultiplier = 0.3f;
	public const float MaxSpeed = 7;
	public const float Acceleration = 1;
	public const float SlideFriction = 0.05f;
	public const float MaxAcceleration = 1;
	public const float WallRunMinimumSpeed = 3;
	public const float WallRunMaximumSpeed = 15;
	public const float Mass = 70;
	public const float JumpForce = 7f;
	public const float RoofRebound = 0.02f;
	public const float KickForce = 10f;
	public const float SlideForce = 5f;
	public const float SlopeForce = 10f;
	public const float WallDirectionInfluence = 0.4f;
	public static float Gravity = 20;
	Buffer jump = new(0.125f);
	Buffer coyoteTime = new(0.125f);
	Gated jumpCoolDown = new(0.125f);
	Gated kickCoolDown = new(1f);
	Gated slideCoolDown = new(1f);
	HeldInput kickInput = new ("move_kick", 0.1f);

	//Godot calls
	public override void _Ready() {
		Game.Instance.HideMouse();
		AddChild(kickInput);
		kickInput.HeldLong += () =>
		{
			if (kickCoolDown.Ready) {
				StateMachine.Set(PlayerStateMachine.State.KickWindUp);
				kickCoolDown.Use();
			}
		};
		kickInput.ShortPress += () =>
		{
			if (kickCoolDown.Ready && !GroundReader.IsColliding()) {
				StateMachine.Set(PlayerStateMachine.State.Stalling);
				RedirectMomentum();
				kickCoolDown.Use();
			}
		};
    }
	
    public override void _Input(InputEvent @event) {
		if (Input.IsActionJustPressed("move_jump"))
			jump.Set();

		if (Input.IsActionJustPressed("move_slide"))
			AttemptSlide();

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
		// Check if we wanted to leave the slide
		if (StateMachine.CurrentState == PlayerStateMachine.State.Sliding && !Input.IsActionPressed("move_slide")) {
			if (HeadSpaceReader.IsColliding() && GroundReader.IsColliding()) {
				//Grounded and no room to stand: give up for now
			} else {
				StateMachine.CurrentState = PlayerStateMachine.State.Airborne;
				if (GroundReader.IsColliding())
					GlobalPosition += Vector3.Up;
			}
		}

		// Update collider
		if (StateMachine.CurrentState == PlayerStateMachine.State.Sliding || StateMachine.CurrentState == PlayerStateMachine.State.Stalling || StateMachine.CurrentState == PlayerStateMachine.State.Kicking || StateMachine.CurrentState == PlayerStateMachine.State.KickWindUp) {
			Crouch();
		} else {
			Stand();
		}

		// Move
		if (StateMachine.CurrentState == PlayerStateMachine.State.Walking) {
			GroundMove((float)delta);
		} else if (StateMachine.CurrentState == PlayerStateMachine.State.Airborne) {
			groundSurfaceNormal = Vector3.Up;
			AirMove((float)delta);
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

		//Apply velocities
		MoveAndSlide();

		//Collide
		KinematicCollision3D col = GetLastSlideCollision();
		if (col != null) {
			Collide(col);
		}

		//Update grounded state
		if (GroundReader.IsColliding() && Vector3.Up.Dot(GroundReader.GetCollisionNormal()) > 0.5f) {
			groundSurfaceNormal = GroundReader.GetCollisionNormal();
			StateMachine.Set(PlayerStateMachine.State.Walking);
		} else if (StateMachine.CurrentState == PlayerStateMachine.State.Walking) {
			//If we are not colliding with anything and we think we are walking
			StateMachine.Set(PlayerStateMachine.State.Airborne);
		}
		GD.Print(StateMachine.CurrentState + ": " + GodotMath.XZ(Velocity).Length());
		GameWarden.Instance?.Report(GlobalPosition, Velocity, new Vector2(Rotation.Y, Camera.Rotation.X), StateMachine.CurrentState);
	}

//physics
    void Collide(KinematicCollision3D collision) {
		//Get the normal of the current surface
		Vector3 normal = collision.GetNormal();

		//If possible: push rigidbodies back as they push on us
		if (collision.GetCollider() is RigidBody3D rb)
			rb.ApplyCentralForce(Mass * Velocity.Project(normal));

		if (Vector3.Up.Dot(normal) < -0.5f) { //roof
			//If the normal is facing downwards: bound off the surface.
			Velocity += normal * RoofRebound * Vector3.Down.Dot(normal);
		} else if (Vector3.Up.Dot(normal) > 0.5f) { //floor
			groundSurfaceNormal = normal;
			StateMachine.Set(PlayerStateMachine.State.Walking);
		} else { //wall
			// Attempt to wall running
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
		return GodotMath.XZ(Velocity, groundSurfaceNormal).Length();
	}
	private void AirMove(float delta)
	{
		// Jump if we missed our chance while grounded
		if (coyoteTime.Active && jump.Active)
			Jump();

		//Accelerate if not going as fast as possible
		if (GetSpeed() < MaxSpeed) {
			Accelerate(GetDesiredDirection(), delta, Acceleration * AirControlMultiplier);
		} else if (GetDesiredDirection().Dot(GodotMath.XZ(Velocity)) < 0) {
			//Decelerate if trying to
			Accelerate(GetDesiredDirection(), delta, Acceleration * AirControlMultiplier);
		}
	}

	private void GroundMove(float delta) {
		lastSurfaceTouched = groundSurfaceNormal;
		if (jump.Active) { //Jump the second we hit the floor without applying friction
			Jump();
		} else {
			if (jumpCoolDown.Ready)
				coyoteTime.Set();

			StepUp(delta);
			Accelerate(GodotMath.AlignUpToNormal(groundSurfaceNormal, GetDesiredDirection()), delta);
		}
	}
	private void WallMove(float delta) {
		lastSurfaceTouched = wallSurfaceNormal;
		if (jumpCoolDown.Ready)
			coyoteTime.Set();
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
		if (!WallReader.IsColliding()) {
			StateMachine.Set(PlayerStateMachine.State.Airborne);
			return;
		}

		//Accelerate along the wall
		Vector3 wallTangent = GodotMath.XZ(wallSurfaceNormal).Cross(Vector3.Up);
		Vector3 desiredDirection = GetDesiredDirection().Project(wallTangent).Normalized();
		desiredDirection = desiredDirection.Lerp(-wallSurfaceNormal, WallDirectionInfluence);
		// Accelerate if not going as fast as possible
		if (GetSpeed() < WallRunMaximumSpeed) {
			Accelerate(desiredDirection, delta, maxSpeed: WallRunMaximumSpeed);
		} else if (desiredDirection.Dot(GodotMath.XZ(Velocity)) < 0) {
			//Decelerate if trying to
			Accelerate(desiredDirection, delta, maxSpeed: WallRunMaximumSpeed);
		}

		//Reduce Y-velocity
		float Y = Lerp.LerpHalfLife(Velocity.Y, 0, delta, .1f/Acceleration);
		Velocity = new Vector3(Velocity.X, Y, Velocity.Z);
	}
	public void SlideMove(float delta) {
		if (GroundReader.IsColliding()) {
			groundSurfaceNormal = GroundReader.GetCollisionNormal();
			lastSurfaceTouched = groundSurfaceNormal;
			if (jump.Active) {
				Jump();
			} else {
				// Jump if we missed our chance while grounded
				if (coyoteTime.Active && jump.Active)
					Jump();
				//Slide the player down the slope
				if (groundSurfaceNormal != Vector3.Up)
					Velocity += SlopeForce * (groundSurfaceNormal - Vector3.Up).Normalized() * delta;
				//Apply friction
				Accelerate(-Velocity.Normalized(), delta, SlideFriction, MaxAcceleration/10f);
			}
		} else {
			groundSurfaceNormal = Vector3.Up;
			AirMove(delta);
		}

	}
	public void AirStall(float delta) {
		AirMove(delta);
		
		//Reduce Y-velocity
		float Y = Lerp.LerpHalfLife(Velocity.Y, 0, delta, .1f/Acceleration);
		Velocity = new Vector3(Velocity.X, Y, Velocity.Z);
	}
	public void Accelerate(Vector3 direction, float delta, float acceleration = Acceleration, float maxAcceleration = MaxAcceleration, float maxSpeed = MaxSpeed)
	{
		// Calculate new velocity after lerping
		Vector3 flatVelocity = Lerp.LerpHalfLife(GodotMath.XZ(Velocity, groundSurfaceNormal), direction * maxSpeed, delta, .1f/acceleration);
		// Reduce change if over acceleration limit
		if ((GodotMath.XZ(Velocity, groundSurfaceNormal) - flatVelocity).Length() > maxAcceleration)
			flatVelocity = Lerp.MoveTowards(GodotMath.XZ(Velocity, groundSurfaceNormal), direction * maxSpeed, maxAcceleration);
		// Apply change
		Velocity = flatVelocity + Velocity.Project(groundSurfaceNormal); //preserve Y velocity
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
	public void StepUp(float delta)
	{
		//Set stepup reader's position
		StepUpReader.GlobalPosition = GodotMath.AlignUpToNormal(groundSurfaceNormal, GetDesiredDirection())*0.7f + groundSurfaceNormal*0.5f + GlobalPosition;
		StepUpReader.TargetPosition = -groundSurfaceNormal*0.4f;

		//Check if its possible
		if (!StepUpReader.IsColliding()) return; //if not colliding
		if (StepUpReader.GetCollisionPoint().Y <= GlobalPosition.Y) return; //if point is lower than we are
		if (StepUpReader.GetCollisionNormal().Dot(groundSurfaceNormal) < 0.25f) return; //if surface is too perpendicular to our current spot
		if (GodotMath.XZ(Velocity * delta * 3, groundSurfaceNormal).Project((StepUpReader.GetCollisionPoint() - GlobalPosition).Normalized()).Length() < 0.7f - 0.5f) return; //if are not going fast enough to land on the surface after stepping up

		//move body up
		GlobalPosition = new Vector3(GlobalPosition.X, StepUpReader.GetCollisionPoint().Y, GlobalPosition.Z);
	}
//input
	public void Jump() {
		// If we jumped too recently: quit
		if (!jumpCoolDown.Use()) return;

		// If already going down : cancel velocity
		if (Velocity.Dot(Vector3.Down) > 0)
			Velocity += -Velocity.Project(Vector3.Down); 
		
		// Add jump force
		if (lastSurfaceTouched == wallSurfaceNormal) {
			Velocity += JumpForce * (Vector3.Up + (lastSurfaceTouched - GodotMath.XZ(Camera.GlobalBasis.Z)).Normalized());
		} else {
			Velocity += JumpForce * Vector3.Up;
		}
		
		// Quit being grounded
		StateMachine.Set(PlayerStateMachine.State.Airborne);
		
		//Stop the jump buffer here
		jump.Cancel();
		coyoteTime.Cancel();
	}
	public void Kick() {
		// If hit: kick back
		if (KickRayCast.IsColliding()) {
			if (KickRayCast.GetCollider() is RigidBody3D rb)
				rb.ApplyCentralForce(Mass * KickForce * Velocity.Project(KickRayCast.GetCollisionNormal()));
			
			Vector3 knockBack = (KickRayCast.GetCollisionPoint().DirectionTo(Camera.GlobalPosition) + KickRayCast.GetCollisionNormal()).Normalized();
			Velocity += KickForce * knockBack;
		}
		StateMachine.Set(PlayerStateMachine.State.Airborne);
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
	// State
	public void Crouch() {
		Shape.Height = 1;
		Collider.Position = new Vector3(0, 1.5f, 0);
		GroundReader.Position = new Vector3(0, 1.0f, 0);
	}
	public void Stand() {
		Shape.Height = 2;
		Collider.Position = new Vector3(0, 1.0f, 0);
		GroundReader.Position = Vector3.Zero;
	}
}