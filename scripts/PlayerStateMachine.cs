using Godot;
using System;

public partial class PlayerStateMachine
{
    public enum State {
		Walking,
        Airborne,
		Sliding,
		Kicking,
        Stalling,
		WallRunning
	}
    public State CurrentState = State.Airborne;
    public void Set(State state)
    {
        if (state == State.WallRunning && CurrentState != State.Airborne) return;
        if (state == State.WallRunning) {
            if (CurrentState != State.Airborne) return;
        }
        CurrentState = state;
    }
}
