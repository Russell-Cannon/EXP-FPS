using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerStateMachine
{
    public enum State {
		Walking,
        Airborne,
		WallRunning,
		Sliding,
        Stalling,
		Kicking,
        KickWindUp
	}
    public struct AnimatedTransition {
        public State StartState;
        public State EndState;
        public float Duration;
        public float TimeElapsed;
    }
    AnimatedTransition[] animatedTransitions = {
        new() {StartState = State.KickWindUp, EndState = State.Kicking, Duration = 0.4f, TimeElapsed = 0f},
        new() {StartState = State.Stalling, EndState = State.Airborne, Duration = 0.3f, TimeElapsed = 0f}
    };
    public event Action StartWallRun;
    public event Action StopWallRun;
    public event Action Land;
    public State CurrentState = State.Airborne;
    public void Set(State state) {
        // Restrict some transitions
        if (state == State.WallRunning && CurrentState != State.Airborne && CurrentState != State.Stalling) return; // Do not begin wall running if not airborne
        if (CurrentState == State.KickWindUp && state == State.Kicking && Input.IsActionPressed("move_kick")) return; // Do not advance from wind up if still holding

        // Activate animations for some transitions
        if ((CurrentState == State.Airborne || CurrentState == State.Stalling) && state == State.WallRunning) 
            StartWallRun?.Invoke();
        if (CurrentState == State.WallRunning && state != State.WallRunning) 
            StopWallRun?.Invoke();
        if (CurrentState == State.Airborne && state == State.Walking)
            Land?.Invoke();

        // Allow the state to transition
        CurrentState = state;
    }
    public void Tick(float delta) {
        for (int i = 0; i < animatedTransitions.Length; i++) {
            //Skip transitions that don't apply
            if (animatedTransitions[i].StartState != CurrentState) {
                animatedTransitions[i].TimeElapsed = 0;
                continue;
            }
            
            //Advance timer
            animatedTransitions[i].TimeElapsed += delta;

            //Enact any transition that is due
            if (animatedTransitions[i].Duration <= animatedTransitions[i].TimeElapsed) {
                Set(animatedTransitions[i].EndState);
            }
        }
    }
    public AnimatedTransition GetTransition(State StartState, State EndState) {
        for (int i = 0; i < animatedTransitions.Length; i++) {
            if (animatedTransitions[i].StartState == StartState && animatedTransitions[i].EndState == EndState)
                return animatedTransitions[i];
        }
        return new() {StartState = State.Airborne, EndState = State.Walking, Duration = 0f, TimeElapsed = 0f};
    }
}
