using Godot;

public class GodotMath {
    public const float PI = 3.1415926536f, HALF_PI = 1.5707963268f, PI_64 = 0.0490873852f, PI_256 = 0.0122718463f;
	public static Vector3 AlignUpToNormal(Vector3 normal, Vector3 vector) {
		if (vector == Vector3.Zero) return Vector3.Zero;
		Basis b = new();
		b.Z = vector;
		b.Y = normal;
		b = b.Orthonormalized();
		return b.Z;
	}
    public static Basis AlignYToVector(Vector3 vector) {
		Basis b = new();
		b.Y = -vector;
		b.X = -vector.Cross(Vector3.Right);
        b.Z = b.X.Cross(b.Y);
		b = b.Orthonormalized();
		return b;
    }
    public static Vector3 XZ(Vector3 Vector, Vector3 Y)
    {
		Vector3 amountInY = Vector.Project(Y);
		return Vector - amountInY;
    }
    public static Vector3 XZ(Vector3 Vector)
    {
        return new Vector3(Vector.X, 0, Vector.Z);
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
}
