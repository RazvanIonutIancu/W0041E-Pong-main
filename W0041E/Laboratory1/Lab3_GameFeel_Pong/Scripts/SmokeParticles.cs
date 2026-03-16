using Godot;
using System;

public partial class SmokeParticles : GpuParticles3D
{

	public void EmitSmoke(Vector3 position)
	{
		GlobalPosition = position;
		Restart();
		Emitting = true;
	}
}
