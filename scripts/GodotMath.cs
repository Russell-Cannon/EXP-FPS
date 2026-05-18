using Godot;

public class GodotMath {
    public const float PI = 3.1415926536f, HALF_PI = 1.5707963268f, PI_64 = 0.0490873852f, PI_256 = 0.0122718463f;
	public static Basis AlignUpToNormal(Vector3 normal, Vector3 vector) {
		Basis b = new();
		b.Z = vector;
		b.Y = normal;
		b = b.Orthonormalized();
		return b;
	}
    public static Basis AlignYToVector(Vector3 vector) {
		Basis b = new();
		b.Y = -vector;
		b.X = -vector.Cross(Vector3.Right);
        b.Z = b.X.Cross(b.Y);
		b = b.Orthonormalized();
		return b;
    }

	public static bool AboutEqual(Vector3 a, Vector3 b, float epsilon) {
		return AboutEqual(a.X, b.X, epsilon) && AboutEqual(a.Y, b.Y, epsilon) && AboutEqual(a.Z, b.Z, epsilon);
	}
	public static bool AboutEqual(float x, float y, float epsilon) {//1,2,1
		return Mathf.Abs(x - y) < epsilon;//1-2 = 1 < 1
	}

	public static Vector3 ClipVector(Vector3 vector) {
		return ClipVector(vector, 0.01f);
	}
	public static Vector3 ClipVector(Vector3 vector, float epsilon) {
		vector.X = Mathf.Abs(vector.X) < epsilon ? 0 : vector.X;
		vector.Y = Mathf.Abs(vector.Y) < epsilon ? 0 : vector.Y;
		vector.Z = Mathf.Abs(vector.Z) < epsilon ? 0 : vector.Z;
		return vector;
	}

    //The following is a collection of functions to smooth lerp towards a value, without being affected by frame rate.
    //This is based on Freya Holmier's blog post

    //Spherical lerp
    public static Quaternion SlerpSmooth(Quaternion from, Quaternion to, float deltatime, float halflife) {
        from = from.Normalized();
        to = to.Normalized();
        return to.Slerp(from, Mathf.Pow(2, -deltatime/halflife));
    }
    public static Vector3 SlerpSmooth(Vector3 from, Vector3 to, float deltatime, float halflife) {
        from = from.Normalized();
        to = to.Normalized();
        return to.Slerp(from, Mathf.Pow(2, -deltatime/halflife));
    }
    public static Basis SlerpSmooth(Basis from, Basis to, float deltatime, float halflife) {
        return to.Slerp(from, Mathf.Pow(2f, -deltatime/halflife)); //Causes head jerks
    }

    //Lerp
    public static Vector3 LerpSmooth(Vector3 from, Vector3 to, float deltatime, float halflife, bool radial) {
        return new Vector3(LerpSmooth(from.X, to.X, deltatime, halflife, radial), LerpSmooth(from.Y, to.Y, deltatime, halflife, radial), LerpSmooth(from.Z, to.Z, deltatime, halflife, radial));
    }
    public static Vector2 LerpSmooth(Vector2 from, Vector2 to, float deltatime, float halflife, bool radial) {
        return new Vector2(LerpSmooth(from.X, to.X, deltatime, halflife, radial), LerpSmooth(from.Y, to.Y, deltatime, halflife, radial));
    }
    public static Vector3 LerpSmooth(Vector3 from, Vector3 to, float deltatime, float halflife) {
        return new Vector3(LerpSmooth(from.X, to.X, deltatime, halflife), LerpSmooth(from.Y, to.Y, deltatime, halflife), LerpSmooth(from.Z, to.Z, deltatime, halflife));
    }
    public static Vector2 LerpSmooth(Vector2 from, Vector2 to, float deltatime, float halflife) {
        return new Vector2(LerpSmooth(from.X, to.X, deltatime, halflife), LerpSmooth(from.Y, to.Y, deltatime, halflife));
    }
    public static float LerpSmooth(float from, float to, float deltatime, float halflife) {
        return LerpSmooth(from, to, deltatime, halflife, false);
    }
    public static float LerpSmooth(float from, float to, float deltatime, float halflife, bool radial) {
        if (radial)
            to = Mathf.LerpAngle(from, to, 1);//fix over tau interp breaking
        return to+(from-to)*Mathf.Pow(2f, -deltatime/halflife);
    }
}
