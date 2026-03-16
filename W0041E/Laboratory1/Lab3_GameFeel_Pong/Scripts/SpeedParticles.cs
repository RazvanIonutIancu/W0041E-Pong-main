using Godot;
using System;

public partial class SpeedParticles : GpuParticles3D
{

	private bool isEmitting = true;

	private Vector3 direction;

	private PongLogic pongBall;
	private ParticleProcessMaterial material;


    public override void _Ready()
    {
		pongBall = GetNode<PongLogic>("/root/Lab31/PaddleController");
        material = ProcessMaterial as ParticleProcessMaterial;
    }


	public override void _Process(double delta)
	{

		if(isEmitting)
		{
			if(pongBall.currentStage == PongLogic.Stage.KITCHEN)
            {
				this.Show();
            }
			else
			{
				this.Hide();
			}


            GlobalPosition = pongBall.meat.GlobalPosition;


            if (material != null)
            {
                direction = new Vector3(-pongBall.GetBallVelocity().X, 0f, -pongBall.GetBallVelocity().Z);
                material.Gravity = direction;
            }

        }
	}













	public void EmitTrail()
	{
		isEmitting = true;
		Restart();
		Emitting = true;
	}

	public void StopTrail()
	{
		isEmitting = false;
		Emitting = false;
	}







}
