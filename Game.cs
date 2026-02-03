using Godot;
using System;

public partial class Game : Node
{
    public static Game Instance {get; private set;}
	public Viewport Root;
	public Node CurrentScene;

    public override void _Ready() {
		Root = GetTree().Root;
		CurrentScene = Root.GetChild(Root.GetChildCount() - 1);
        Instance = this;
		FreeMouse();
    }

	public void LoadScene(string scene) {
		var pack = GD.Load<PackedScene>(scene);
		var instance = pack.Instantiate();
		Root.AddChild(instance);
		CurrentScene = instance;
	}
	public void DeleteScene() {
		Root.GetChild(Root.GetChildCount() - 1).QueueFree();
		CurrentScene = null;
	}
	public void SwapScene(string scene) {
		DeleteScene();
		LoadScene(scene);
	}
	public void HideMouse() {
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}
	public void FreeMouse() {
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}
	public bool IsMouseFree() {
		return Input.MouseMode == Input.MouseModeEnum.Visible;
	}
}
