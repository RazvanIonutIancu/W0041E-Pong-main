using Godot;
using Godot.NativeInterop;
using System;
using System.Reflection.Metadata;

public partial class PongLogic : Node
{

    [Export]
    public Node3D kitchenAssets;

    [Export]
    public Node3D meat;

    [Export]
    public Node3D rightPan;

    [Export]
    public Node3D leftPan;






    [Export]
    public Node3D leftPaddle;

    [Export]
    public Node3D rightPaddle;



    [Export]
    public Timer leftPanTimer;

    [Export]
    public Timer rightPanTimer;

    [Export]
    public Timer cameraShakeTimer;



    [Export]
    public Node3D audioNode;

    [Export]
    public Camera3D camera;










    [Export]
    public Node3D centerLine;

    [Export]
    public Node3D ball;

    [Export]
    public Vector2 tableSize;

    private Vector3 ballVelocity = Vector3.Zero;

    [Export]
    private float ballSpeed = 5.0f; 

    [Export]
    private float paddleSpeed = 10.0f; 

    [Export]
    public float paddleLerpSpeed = 20;

    private Random random = new Random();

    public float leftStickMagnitude = 0;
    public float rightStickMagnitude = 0;
    public Vector2 leftStickInput = Vector2.Zero;
    public Vector2 rightStickInput = Vector2.Zero;

	public int sideCheck = 0;

    private float leftPaddleVerticalVelocity = 0;
    private float rightPaddleVerticalVelocity = 0;

    private float leftPanVerticalVelocity = 0;
    private float rightPanVerticalVelocity = 0;

    private float panMaxXYOffset = 2f;
    private float panMaxRotation = Mathf.DegToRad(90);
    private float panRotationSpeed = 7.0f;
    private float meatRotationAngle = 1440f;

    Vector3 leftPanBasePosition = new Vector3(0f, 0f, 0f);
    Vector3 rightPanBasePosition = new Vector3(0f, 0f, 0f);

    float panDefaultRotationY = Mathf.DegToRad(-180);

    public enum Stage
    {
        PONG,
        KITCHEN
    }

    public Stage currentStage = Stage.KITCHEN;

    private float meatMinHeight = 0.1f;
    private float meatMaxHeight = 0.5f;
    private bool meatIsMoving = true;

    private AudioStreamPlayer sizzling;
    private AudioStreamPlayer bong;
    private AudioStreamPlayer fwoosh;
    private AudioStreamPlayer bleft;
    private bool bleftPlayed = false;


    private float cameraRotation;
    private Vector3 cameraBaseRotation;
    private float cameraRotationRange = Mathf.DegToRad(20);

    private Vector3 cameraBasePosition;
    private float cameraZoom;
    private float cameraShakeAmplitude;
    private float cameraShakeFrequency = 50f;
    private float cameraShakeDecay = 0.95f;



    private SpeedParticles speedParticles;
    private SmokeParticles smokeParticles;



    //****************************
    //
    //
    // METHODS
    //
    //
    //****************************


    public override void _Ready()
    {
        leftPan.Position = leftPaddle.Position;
        rightPan.Position = rightPaddle.Position;
        cameraBaseRotation = camera.Rotation;
        cameraBasePosition = camera.Position;


        sizzling = audioNode.GetNode<AudioStreamPlayer>("Sizzling");
        bong = audioNode.GetNode<AudioStreamPlayer>("Bong");
        fwoosh = audioNode.GetNode<AudioStreamPlayer>("Fwoosh");
        bleft = audioNode.GetNode<AudioStreamPlayer>("Bleft");

        speedParticles = GetNode<SpeedParticles>("Particles/SpeedParticles");
        smokeParticles = GetNode<SmokeParticles>("Particles/SmokeParticles");

        InitMatch();

    }

    public override void _Process(double delta)
    {
        StageChange();

        PollInput((float)delta);


        PaddleMovement((float)delta);
        BallMovement((float)delta);
        CheckPaddleCollision();
        CheckForScore();

    }

    // Change stage from kitchen to Pong
    public void StageChange()
    {


        if (Input.IsActionJustPressed("pongStage"))
        {
            currentStage = Stage.PONG;
        }

        if (Input.IsActionJustPressed("kitchenStage"))
        {
            currentStage = Stage.KITCHEN;
        }





        if (currentStage == Stage.PONG)
        {
            //leftPan.Visible = false;
            //rightPan.Visible = false;

            kitchenAssets.Visible = false;

            ball.Visible = true;
            centerLine.Visible = true;
            leftPaddle.Visible = true;
            rightPaddle.Visible = true;
            smokeParticles.Hide();


            // Mute sounds
            AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), -80.0f);
        }

        if (currentStage == Stage.KITCHEN)
        {
            //leftPan.Visible = true;
            //rightPan.Visible = true;

            kitchenAssets.Visible = true;

            ball.Visible = false;
            centerLine.Visible = false;
            leftPaddle.Visible = false;
            rightPaddle.Visible = false;
            smokeParticles.Show();


            // Unmute sounds
            AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), 0.0f);
        }
    }





    // Ball movement with speed adjustments
    public void BallMovement(float delta)
    {
        ball.Translate(ballVelocity * delta);
        bool outOfBoundsTop = ball.Position.Z > tableSize.Y / 2.0f;
        bool outOfBoundsBottom = ball.Position.Z < -tableSize.Y / 2.0f;
        if (outOfBoundsTop && ballVelocity.Z > 0.0f || outOfBoundsBottom && ballVelocity.Z < 0.0f)
        {
            ballVelocity.Z *= -1;
        }

        MeatWave(delta);
    }




    // Calculating meat Y position
    private void MeatWave(double delta)
    {
        float distanceFromLeftPaddle = ball.GlobalPosition.X - leftPaddle.GlobalPosition.X;
        float distanceBetweenPaddles = rightPaddle.GlobalPosition.X - leftPaddle.GlobalPosition.X;

        float t = distanceFromLeftPaddle / distanceBetweenPaddles;

        if (t <= 1.0f && t >= 0.0f)
        {
            float meatAirPosition = meatMaxHeight * Mathf.Sin(t * Mathf.Pi);

            meat.GlobalPosition = new Vector3(ball.GlobalPosition.X, meatAirPosition + meatMinHeight, ball.GlobalPosition.Z);
            meatIsMoving = true;

            float meatAngle = t * Mathf.DegToRad(meatRotationAngle);
            meat.Rotation = new Vector3(0f, 0f, -meatAngle);

            if (!fwoosh.Playing)
            {
                fwoosh.Play();
            }
        }
        else
        {
            if (!bleftPlayed)
            {
                bleft.Play();
                bleftPlayed = true;
            }

            meatIsMoving = false;
            fwoosh.Stop();
            speedParticles.StopTrail();


        }

        CameraMovement(t, delta);

    }


    // Camera movement
    private void CameraMovement(float multiplier, double delta)
    {

        if (currentStage == Stage.PONG)
        {
            camera.Rotation = cameraBaseRotation;
            camera.Position = cameraBasePosition;
        }
        else if (currentStage == Stage.KITCHEN && meatIsMoving)
        {
            // Follow
            float rotationOffset = (multiplier * cameraRotationRange) - (cameraRotationRange / 2);

            cameraRotation = Mathf.Lerp(camera.Rotation.Z, rotationOffset, 5.0f * (float)delta);

            camera.Rotation = new Vector3(cameraBaseRotation.X, -cameraRotation, cameraRotation);



            // Zooming
            float cameraTargetZoom = (float)Mathf.Cos(rotationOffset * 4) + 1f;

            cameraZoom = Mathf.Lerp(camera.Position.Y, cameraTargetZoom,  4.0f * (float)delta);


            if(cameraShakeTimer.IsStopped())
            {
                camera.Position = new Vector3(cameraBasePosition.X, cameraZoom, cameraBasePosition.Z);
            }
            else
            {
                float shakeOffset = cameraShakeAmplitude * Mathf.Sin(Mathf.Pi * multiplier * cameraShakeFrequency);

                camera.Position = new Vector3(cameraBasePosition.X + shakeOffset, cameraZoom, cameraBasePosition.Z);

                cameraShakeAmplitude *= cameraShakeDecay;
            }
        }

    }


    private void StartCameraShakeTimer()
    {
        cameraShakeAmplitude = 0.1f;
        cameraShakeTimer.Start();
    }










    // Animating paddles
    private void AnimatePaddle(double delta)
    {
        if (!leftPanTimer.IsStopped())
        {

            float xOffset = Mathf.Lerp(leftPan.Position.X, leftPanBasePosition.X, 1.0f * (float)delta);
            float yOffset = Mathf.Lerp(leftPan.Position.Y, leftPanBasePosition.Y + panMaxXYOffset, 1.0f * (float)delta);

            leftPan.Position = new Vector3(xOffset, yOffset, leftPanBasePosition.Z);



            float newRotation = Mathf.LerpAngle(leftPan.Rotation.Z, panMaxRotation, panRotationSpeed * (float)delta);

            leftPan.Rotation = new Vector3(0f, panDefaultRotationY, newRotation);

        }
        else
        {
            float xOffset = Mathf.Lerp(leftPan.Position.X, leftPanBasePosition.X, 1.0f * (float)delta);
            float yOffset = Mathf.Lerp(leftPan.Position.Y, leftPanBasePosition.Y, 1.0f * (float)delta);

            leftPan.Position = new Vector3(xOffset, yOffset, leftPanBasePosition.Z);


            float newRotation = Mathf.LerpAngle(leftPan.Rotation.Z, Mathf.DegToRad(0f), panRotationSpeed * (float)delta);

            leftPan.Rotation = new Vector3(0f, panDefaultRotationY, newRotation);
        }



        if (!rightPanTimer.IsStopped())
        {
            float xOffset = Mathf.Lerp(rightPan.Position.X, rightPanBasePosition.X, 1.0f * (float)delta);
            float yOffset = Mathf.Lerp(rightPan.Position.Y, rightPanBasePosition.Y + panMaxXYOffset, 1.0f * (float)delta);

            rightPan.Position = new Vector3(xOffset, yOffset, rightPanBasePosition.Z);



            float newRotation = Mathf.LerpAngle(rightPan.Rotation.Z, -panMaxRotation, panRotationSpeed * (float)delta);

            rightPan.Rotation = new Vector3(0f, panDefaultRotationY, newRotation);

        }
        else
        {
            float xOffset = Mathf.Lerp(rightPan.Position.X, rightPanBasePosition.X, 1.0f * (float)delta);
            float yOffset = Mathf.Lerp(rightPan.Position.Y, rightPanBasePosition.Y, 1.0f * (float)delta);

            rightPan.Position = new Vector3(xOffset, yOffset, rightPanBasePosition.Z);


            float newRotation = Mathf.LerpAngle(rightPan.Rotation.Z, Mathf.DegToRad(0f), panRotationSpeed * (float)delta);

            rightPan.Rotation = new Vector3(0f, panDefaultRotationY, newRotation);
        }

    }





























    // Paddle movement
    public void PaddleMovement(float delta)
    {

        if (meatIsMoving)
        {
            Vector3 leftPaddlePosition = leftPaddle.Position;
            leftPaddlePosition.Z += leftStickInput.Y * paddleSpeed * leftStickMagnitude * delta;
            leftPaddlePosition.Z = Mathf.Clamp(leftPaddlePosition.Z, (-tableSize.Y + leftPaddle.Scale.Z) / 2, (tableSize.Y - leftPaddle.Scale.Z) / 2);
            leftPaddleVerticalVelocity = (leftPaddlePosition - leftPaddle.Position).Length();
            leftPaddle.Position = leftPaddlePosition;
            leftPanBasePosition = leftPaddlePosition;

            Vector3 rightPaddlePosition = rightPaddle.Position;
            rightPaddlePosition.Z += rightStickInput.Y * paddleSpeed * rightStickMagnitude * delta;
            rightPaddlePosition.Z = Mathf.Clamp(rightPaddlePosition.Z, (-tableSize.Y + rightPaddle.Scale.Z) / 2, (tableSize.Y - rightPaddle.Scale.Z) / 2);
            rightPaddleVerticalVelocity = (rightPaddlePosition - rightPaddle.Position).Length();
            rightPaddle.Position = rightPaddlePosition;
            rightPanBasePosition = rightPaddlePosition;


            // Pan animation
            AnimatePaddle(delta);
        }



    }


    public Vector3 GetBallVelocity()
    {
        return ballVelocity;
    }




    // Initialize match and set ball starting velocity
    public void InitMatch()
    {
        ball.GlobalPosition = Vector3.Zero;
        float angle = Mathf.DegToRad(random.Next(-45, 45));
        int horizontalDirection = random.Next(0, 2) == 0 ? 1 : -1;
        float velocityX = horizontalDirection * Mathf.Cos(angle);
        float velocityZ = Mathf.Sin(angle);
        ballVelocity = new Vector3(velocityX, 0, velocityZ) * ballSpeed;


        bleftPlayed = false;
    }

    // Restart match
    public void LooseMatch()
    {
        InitMatch();
    }

    // Handle joystick input for paddles (Same joystick)
    public void PollInput(float delta)
    {
        float leftX = Input.GetJoyAxis(0, JoyAxis.LeftX);
        float leftY = Input.GetJoyAxis(0, JoyAxis.LeftY);
        leftStickMagnitude = new Vector2(leftX, leftY).Length();
        leftStickInput = new Vector2(leftX, leftY);
        if (leftStickMagnitude < 0.04f) // Fuzzy joystick setting..
        {
            leftStickInput = Vector2.Zero;
        }

        float rightX = Input.GetJoyAxis(0, JoyAxis.RightX);
        float rightY = Input.GetJoyAxis(0, JoyAxis.RightY);
        rightStickMagnitude = new Vector2(rightX, rightY).Length();
        rightStickInput = new Vector2(rightX, rightY);

        if (rightStickMagnitude < 0.04f) // Fuzzy joystick setting..
        {
            rightStickInput = Vector2.Zero;
        }
    }

 // Check paddle collision with the ball
private void CheckPaddleCollision()
{
    Node3D targetPaddle = ballVelocity.X < 0 ? leftPaddle : rightPaddle;
    float paddleHalfSizeZ = targetPaddle.Scale.Z / 2.0f;
    float paddleCenterZ = targetPaddle.GlobalPosition.Z;
    float paddleMinZ = paddleCenterZ - paddleHalfSizeZ;
    float paddleMaxZ = paddleCenterZ + paddleHalfSizeZ;

    if (Mathf.Abs(ball.GlobalPosition.X - targetPaddle.GlobalPosition.X) < targetPaddle.Scale.X / 2.0f)
    {
        if (ball.GlobalPosition.Z >= paddleMinZ && ball.GlobalPosition.Z <= paddleMaxZ)
        {
            ballVelocity.X *= -1;

            // Animate the paddle
            if (targetPaddle == leftPaddle)
            {
                leftPanTimer.Start();
                Vector3 smokePosition = new Vector3(meat.GlobalPosition.X + 0.3f, meat.GlobalPosition.Y + 0.3f, meat.GlobalPosition.Z);
                smokeParticles.EmitSmoke(smokePosition);
            }
            else
            {
                rightPanTimer.Start();
                Vector3 smokePosition = new Vector3(meat.GlobalPosition.X - 0.3f, meat.GlobalPosition.Y + 0.3f, meat.GlobalPosition.Z);
                smokeParticles.EmitSmoke(smokePosition);
            }

            // Sound
            bong.Play();
            sizzling.Play();

            // Camera Shake
            StartCameraShakeTimer();
            speedParticles.StopTrail();








            float distanceFromCenter = ball.GlobalPosition.Z - paddleCenterZ;
            float maxAngle = 75.0f;  
            float angle = Mathf.DegToRad(maxAngle * (distanceFromCenter / paddleHalfSizeZ));

            ballVelocity.Z = Mathf.Sin(angle) * ballSpeed;
            ballVelocity = ballVelocity.Normalized() * ballSpeed;

			if(leftPaddleVerticalVelocity > 0.07f && targetPaddle == leftPaddle)
            {
				ballVelocity = ballVelocity * 2.0f;
                speedParticles.EmitTrail();
			}
            if(rightPaddleVerticalVelocity > 0.07f && targetPaddle == rightPaddle)
            {
                ballVelocity = ballVelocity * 2.0f;
                speedParticles.EmitTrail();
            }

            if (ball.GlobalPosition.X < targetPaddle.GlobalPosition.X)
            {
                ball.GlobalPosition = new Vector3(targetPaddle.GlobalPosition.X - targetPaddle.Scale.X / 2, ball.GlobalPosition.Y, ball.GlobalPosition.Z);
            }
            else
            {
                ball.GlobalPosition = new Vector3(targetPaddle.GlobalPosition.X + targetPaddle.Scale.X / 2, ball.GlobalPosition.Y, ball.GlobalPosition.Z);
            }
        }
    }
}

    // Check if the ball goes out of bounds for scoring
    private void CheckForScore()
    {
        float padding = 2f;
        if (ball.GlobalPosition.X < -tableSize.X / 2 - padding || ball.GlobalPosition.X > tableSize.X / 2 + padding)
        {
            LooseMatch();
        }
    }

}
