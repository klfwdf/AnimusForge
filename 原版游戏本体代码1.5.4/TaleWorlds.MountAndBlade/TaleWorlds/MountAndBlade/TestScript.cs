using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TestScript : ScriptComponentBehavior
{
	public string testString;

	public float rotationSpeed;

	public float waterSplashPhaseOffset;

	public float waterSplashIntervalMultiplier = 1f;

	public bool isWaterMill;

	private float currentRotation;

	public float MoveAxisX = 1f;

	public float MoveAxisY;

	public float MoveAxisZ;

	public float MoveSpeed = 0.0001f;

	public float MoveDistance = 10f;

	protected float MoveDirection = 1f;

	protected float CurrentDistance;

	public GameEntity sideRotatingEntity;

	public GameEntity forwardRotatingEntity;

	private void Move(float dt)
	{
		if (!(TaleWorlds.Library.MathF.Abs(MoveDistance) < 1E-05f))
		{
			Vec3 vec = new Vec3(MoveAxisX, MoveAxisY, MoveAxisZ);
			vec.Normalize();
			Vec3 vec2 = vec * MoveSpeed * MoveDirection;
			float num = vec2.Length * MoveDirection;
			if (CurrentDistance + num <= 0f - MoveDistance)
			{
				MoveDirection = 1f;
				num *= -1f;
				vec2 *= -1f;
			}
			else if (CurrentDistance + num >= MoveDistance)
			{
				MoveDirection = -1f;
				num *= -1f;
				vec2 *= -1f;
			}
			CurrentDistance += num;
			MatrixFrame frame = base.GameEntity.GetFrame();
			frame.origin += vec2;
			base.GameEntity.SetFrame(ref frame);
		}
	}

	private void Rotate(float dt)
	{
		MatrixFrame frame = base.GameEntity.GetFrame();
		frame.rotation.RotateAboutUp(rotationSpeed * 0.001f * dt);
		base.GameEntity.SetFrame(ref frame);
		currentRotation += rotationSpeed * 0.001f * dt;
	}

	private bool isRotationPhaseInsidePhaseBoundries(float currentPhase, float startPhase, float endPhase)
	{
		if (endPhase <= startPhase)
		{
			return currentPhase > startPhase;
		}
		if (currentPhase > startPhase)
		{
			return currentPhase < endPhase;
		}
		return false;
	}

	public static int GetIntegerFromStringEnd(string str)
	{
		string text = "";
		for (int num = str.Length - 1; num > -1; num--)
		{
			char c = str[num];
			if (c < '0' || c > '9')
			{
				break;
			}
			text = c + text;
		}
		return Convert.ToInt32(text);
	}

	private void DoWaterMillCalculation()
	{
		float num = base.GameEntity.ChildCount;
		if (!(num > 0f))
		{
			return;
		}
		IEnumerable<WeakGameEntity> children = base.GameEntity.GetChildren();
		float num2 = 6.28f / num;
		foreach (WeakGameEntity item in children)
		{
			int integerFromStringEnd = GetIntegerFromStringEnd(item.Name);
			float currentPhase = currentRotation % 6.28f;
			float num3 = (num2 * (float)integerFromStringEnd + waterSplashPhaseOffset) % 6.28f;
			float endPhase = (num3 + num2 * waterSplashIntervalMultiplier) % 6.28f;
			if (isRotationPhaseInsidePhaseBoundries(currentPhase, num3, endPhase))
			{
				item.ResumeParticleSystem(doChildren: true);
			}
			else
			{
				item.PauseParticleSystem(doChildren: true);
			}
		}
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		Rotate(dt);
		Move(dt);
		if (isWaterMill)
		{
			DoWaterMillCalculation();
		}
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		Rotate(dt);
		Move(dt);
		if (isWaterMill)
		{
			DoWaterMillCalculation();
		}
		if (sideRotatingEntity != null)
		{
			MatrixFrame frame = sideRotatingEntity.GetFrame();
			frame.rotation.RotateAboutSide(rotationSpeed * 0.01f * dt);
			sideRotatingEntity.SetFrame(ref frame);
		}
		if (forwardRotatingEntity != null)
		{
			MatrixFrame frame2 = forwardRotatingEntity.GetFrame();
			frame2.rotation.RotateAboutSide(rotationSpeed * 0.005f * dt);
			forwardRotatingEntity.SetFrame(ref frame2);
		}
	}
}
