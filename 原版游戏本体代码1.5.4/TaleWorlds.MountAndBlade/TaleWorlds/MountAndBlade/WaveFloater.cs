using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class WaveFloater : ScriptComponentBehavior
{
	public SimpleButton largeObject;

	public SimpleButton smallObject;

	public bool oscillateAtX;

	public bool oscillateAtY;

	public bool oscillateAtZ;

	public float oscillationFrequency = 1f;

	public float maxOscillationAngle = 10f;

	public bool bounceX;

	public float bounceXFrequency = 14f;

	public float maxBounceXDistance = 0.3f;

	public bool bounceY;

	public float bounceYFrequency = 14f;

	public float maxBounceYDistance = 0.3f;

	public bool bounceZ;

	public float bounceZFrequency = 14f;

	public float maxBounceZDistance = 0.3f;

	private Vec3 axis;

	private float oscillationSpeed = 1f;

	private float oscillationPercentage = 0.5f;

	private MatrixFrame resetMF;

	private MatrixFrame oscillationStart;

	private MatrixFrame oscillationEnd;

	private bool oscillate;

	private float bounceXSpeed = 1f;

	private float bounceXPercentage = 0.5f;

	private MatrixFrame bounceXStart;

	private MatrixFrame bounceXEnd;

	private float bounceYSpeed = 1f;

	private float bounceYPercentage = 0.5f;

	private MatrixFrame bounceYStart;

	private MatrixFrame bounceYEnd;

	private float bounceZSpeed = 1f;

	private float bounceZPercentage = 0.5f;

	private MatrixFrame bounceZStart;

	private MatrixFrame bounceZEnd;

	private bool bounce;

	private float et;

	private const float SPEED_MODIFIER = 1f;

	private float ConvertToRadians(float angle)
	{
		return System.MathF.PI / 180f * angle;
	}

	private void SetMatrix()
	{
		resetMF = base.GameEntity.GetGlobalFrame();
	}

	private void ResetMatrix()
	{
		base.GameEntity.SetGlobalFrame(in resetMF);
	}

	private void CalculateAxis()
	{
		axis = new Vec3(Convert.ToSingle(oscillateAtX), Convert.ToSingle(oscillateAtY), Convert.ToSingle(oscillateAtZ));
		base.GameEntity.GetGlobalFrame().TransformToParent(in axis);
		axis.Normalize();
	}

	private float CalculateSpeed(float fq, float maxVal, bool angular)
	{
		if (!angular)
		{
			return maxVal * 2f * fq * 1f;
		}
		return maxVal * System.MathF.PI / 90f * fq * 1f;
	}

	private void CalculateOscilations()
	{
		ResetMatrix();
		oscillationStart = base.GameEntity.GetGlobalFrame();
		oscillationEnd = base.GameEntity.GetGlobalFrame();
		oscillationStart.rotation.RotateAboutAnArbitraryVector(in axis, 0f - ConvertToRadians(maxOscillationAngle));
		oscillationEnd.rotation.RotateAboutAnArbitraryVector(in axis, ConvertToRadians(maxOscillationAngle));
	}

	private void Oscillate()
	{
		MatrixFrame frame = base.GameEntity.GetGlobalFrame();
		frame.rotation = Mat3.Lerp(in oscillationStart.rotation, in oscillationEnd.rotation, TaleWorlds.Library.MathF.Clamp(oscillationPercentage, 0f, 1f));
		base.GameEntity.SetGlobalFrame(in frame);
		oscillationPercentage = (1f + TaleWorlds.Library.MathF.Cos(oscillationSpeed * 1f * et)) / 2f;
	}

	private void CalculateBounces()
	{
		ResetMatrix();
		bounceXStart = base.GameEntity.GetGlobalFrame();
		bounceXEnd = base.GameEntity.GetGlobalFrame();
		bounceYStart = base.GameEntity.GetGlobalFrame();
		bounceYEnd = base.GameEntity.GetGlobalFrame();
		bounceZStart = base.GameEntity.GetGlobalFrame();
		bounceZEnd = base.GameEntity.GetGlobalFrame();
		bounceXStart.origin.x += maxBounceXDistance;
		bounceXEnd.origin.x -= maxBounceXDistance;
		bounceYStart.origin.y += maxBounceYDistance;
		bounceYEnd.origin.y -= maxBounceYDistance;
		bounceZStart.origin.z += maxBounceZDistance;
		bounceZEnd.origin.z -= maxBounceZDistance;
	}

	private void Bounce()
	{
		if (bounceX)
		{
			MatrixFrame frame = base.GameEntity.GetGlobalFrame();
			frame.origin.x = Vec3.Lerp(bounceXStart.origin, bounceXEnd.origin, TaleWorlds.Library.MathF.Clamp(bounceXPercentage, 0f, 1f)).x;
			base.GameEntity.SetGlobalFrame(in frame);
			bounceXPercentage = (1f + TaleWorlds.Library.MathF.Sin(bounceXSpeed * 1f * et)) / 2f;
		}
		if (bounceY)
		{
			MatrixFrame frame = base.GameEntity.GetGlobalFrame();
			frame.origin.y = Vec3.Lerp(bounceYStart.origin, bounceYEnd.origin, TaleWorlds.Library.MathF.Clamp(bounceYPercentage, 0f, 1f)).y;
			base.GameEntity.SetGlobalFrame(in frame);
			bounceYPercentage = (1f + TaleWorlds.Library.MathF.Cos(bounceYSpeed * 1f * et)) / 2f;
		}
		if (bounceZ)
		{
			MatrixFrame frame = base.GameEntity.GetGlobalFrame();
			frame.origin.z = Vec3.Lerp(bounceZStart.origin, bounceZEnd.origin, TaleWorlds.Library.MathF.Clamp(bounceZPercentage, 0f, 1f)).z;
			base.GameEntity.SetGlobalFrame(in frame);
			bounceZPercentage = (1f + TaleWorlds.Library.MathF.Cos(bounceZSpeed * 1f * et)) / 2f;
		}
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		SetMatrix();
		oscillate = oscillateAtX || oscillateAtY || oscillateAtZ;
		bounce = bounceX || bounceY || bounceZ;
		oscillationSpeed = CalculateSpeed(oscillationFrequency, maxOscillationAngle, angular: true);
		bounceXSpeed = CalculateSpeed(bounceXFrequency, maxBounceXDistance, angular: false);
		bounceYSpeed = CalculateSpeed(bounceYFrequency, maxBounceYDistance, angular: false);
		bounceZSpeed = CalculateSpeed(bounceZFrequency, maxBounceZDistance, angular: false);
		CalculateBounces();
		CalculateAxis();
		CalculateOscilations();
		SetScriptComponentToTick(GetTickRequirement());
	}

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		SetMatrix();
		oscillate = oscillateAtX || oscillateAtY || oscillateAtZ;
		bounce = bounceX || bounceY || bounceZ;
		oscillationSpeed = CalculateSpeed(oscillationFrequency, maxOscillationAngle, angular: true);
		bounceXSpeed = CalculateSpeed(bounceXFrequency, maxBounceXDistance, angular: false);
		bounceYSpeed = CalculateSpeed(bounceYFrequency, maxBounceYDistance, angular: false);
		bounceZSpeed = CalculateSpeed(bounceZFrequency, maxBounceZDistance, angular: false);
		CalculateBounces();
		CalculateAxis();
		CalculateOscilations();
	}

	protected internal override void OnSceneSave(string saveFolder)
	{
		base.OnSceneSave(saveFolder);
		ResetMatrix();
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		et += dt;
		if (oscillate)
		{
			Oscillate();
		}
		if (bounce)
		{
			Bounce();
		}
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		et += dt;
		if (oscillate)
		{
			Oscillate();
		}
		if (bounce)
		{
			Bounce();
		}
	}

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		switch (variableName)
		{
		case "largeObject":
			ResetMatrix();
			oscillateAtX = true;
			oscillateAtY = true;
			oscillationFrequency = 1.5f;
			maxOscillationAngle = 7.4f;
			bounceX = true;
			bounceXFrequency = 2f;
			maxBounceXDistance = 0.1f;
			bounceY = true;
			bounceYFrequency = 0.2f;
			maxBounceYDistance = 0.5f;
			bounceZ = true;
			bounceZFrequency = 0.6f;
			maxBounceZDistance = 0.22f;
			CalculateAxis();
			oscillationSpeed = CalculateSpeed(oscillationFrequency, maxOscillationAngle, angular: true);
			bounceXSpeed = CalculateSpeed(bounceXFrequency, maxBounceXDistance, angular: false);
			bounceYSpeed = CalculateSpeed(bounceYFrequency, maxBounceYDistance, angular: false);
			bounceZSpeed = CalculateSpeed(bounceZFrequency, maxBounceZDistance, angular: false);
			CalculateOscilations();
			CalculateOscilations();
			oscillate = true;
			bounce = true;
			break;
		case "smallObject":
			ResetMatrix();
			oscillateAtX = true;
			oscillateAtY = true;
			oscillateAtZ = true;
			oscillationFrequency = 1f;
			maxOscillationAngle = 11f;
			bounceX = true;
			bounceXFrequency = 1.5f;
			maxBounceXDistance = 0.3f;
			bounceY = true;
			bounceYFrequency = 1.5f;
			maxBounceYDistance = 0.2f;
			bounceZ = true;
			bounceZFrequency = 1f;
			maxBounceZDistance = 0.1f;
			CalculateAxis();
			oscillationSpeed = CalculateSpeed(oscillationFrequency, maxOscillationAngle, angular: true);
			bounceXSpeed = CalculateSpeed(bounceXFrequency, maxBounceXDistance, angular: false);
			bounceYSpeed = CalculateSpeed(bounceYFrequency, maxBounceYDistance, angular: false);
			bounceZSpeed = CalculateSpeed(bounceZFrequency, maxBounceZDistance, angular: false);
			CalculateOscilations();
			CalculateOscilations();
			oscillate = true;
			bounce = true;
			break;
		case "oscillateAtX":
		case "oscillateAtY":
		case "oscillateAtZ":
			if (oscillateAtX || oscillateAtY || oscillateAtZ)
			{
				if (!oscillate)
				{
					if (!bounce)
					{
						SetMatrix();
					}
					oscillate = true;
				}
			}
			else
			{
				oscillate = false;
				if (!bounce)
				{
					ResetMatrix();
				}
			}
			CalculateAxis();
			CalculateOscilations();
			break;
		case "oscillationFrequency":
			oscillationSpeed = CalculateSpeed(oscillationFrequency, maxOscillationAngle, angular: true);
			break;
		case "maxOscillationAngle":
			maxOscillationAngle = TaleWorlds.Library.MathF.Clamp(maxOscillationAngle, 0f, 90f);
			oscillationSpeed = CalculateSpeed(oscillationFrequency, maxOscillationAngle, angular: true);
			CalculateOscilations();
			break;
		case "bounceX":
		case "bounceY":
		case "bounceZ":
			if (bounceX || bounceY || bounceZ)
			{
				if (!bounce)
				{
					if (!oscillate)
					{
						SetMatrix();
					}
					bounce = true;
				}
			}
			else
			{
				bounce = false;
				if (!oscillate)
				{
					ResetMatrix();
				}
			}
			CalculateBounces();
			break;
		case "bounceXFrequency":
			bounceXSpeed = CalculateSpeed(bounceXFrequency, maxBounceXDistance, angular: false);
			break;
		case "bounceYFrequency":
			bounceYSpeed = CalculateSpeed(bounceYFrequency, maxBounceYDistance, angular: false);
			break;
		case "bounceZFrequency":
			bounceZSpeed = CalculateSpeed(bounceZFrequency, maxBounceZDistance, angular: false);
			break;
		case "maxBounceXDistance":
			bounceXSpeed = CalculateSpeed(bounceXFrequency, maxBounceXDistance, angular: false);
			CalculateBounces();
			break;
		case "maxBounceYDistance":
			bounceYSpeed = CalculateSpeed(bounceYFrequency, maxBounceYDistance, angular: false);
			CalculateBounces();
			break;
		case "maxBounceZDistance":
			bounceZSpeed = CalculateSpeed(bounceZFrequency, maxBounceZDistance, angular: false);
			CalculateBounces();
			break;
		}
	}
}
