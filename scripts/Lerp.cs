using Godot;
using System;

//Holmer Framerate-Independant Lerp
//From `A`, to `B`, given delta time `dt`, with a half-life of `h`
public class Lerp
{
	public static Vector3 LerpHalfLife(Vector3 a, Vector3 b, float dt, float h) {
		return new Vector3(LerpHalfLife(a.X, b.X, dt, h), LerpHalfLife(a.Y, b.Y, dt, h), LerpHalfLife(a.Z, b.Z, dt, h));
	}
	public static float LerpHalfLife(float a, float b, float dt, float h) {
		return b + (a - b)*(float)Math.Pow(2, -dt/h);
	}
	public static float LerpOverTime(float a, float b, float dt, float s) {
		return b + (a - b)*(float)Math.Pow(2, -dt/(-s/Math.Log2(0.01)));
	}
	public static Vector3 MoveTowards(Vector3 a, Vector3 b, float step) {
    Vector3 toMove = a.DirectionTo(b) * step;
    //if we would overshoot: snap to the target
    if (toMove.Length() > a.DistanceTo(b)) 
        a = b;
    else //otherwise: continue moving
        a += toMove;
    return a;
}
}
