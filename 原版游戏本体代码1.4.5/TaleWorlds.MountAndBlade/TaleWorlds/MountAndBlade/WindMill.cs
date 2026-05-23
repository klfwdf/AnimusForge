using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class WindMill : ScriptComponentBehavior
{
	public float rotationSpeed = 100f;

	public float waterSplashPhaseOffset;

	public float waterSplashIntervalMultiplier = 1f;

	public MetaMesh testMesh;

	public Texture testTexture;

	public GameEntity testEntity;

	public bool isWaterMill;

	private float currentRotation;

	protected internal override void OnInit()
	{
		SetScriptComponentToTick(GetTickRequirement());
	}

	private void Rotate(float dt)
	{
		float num = rotationSpeed * 0.001f * dt;
		MatrixFrame frame = base.GameEntity.GetFrame();
		frame.rotation.RotateAboutForward(num);
		base.GameEntity.SetLocalFrame(ref frame, isTeleportation: false);
		base.GameEntity.UpdateTriadFrameForEditorForAllChildren();
		currentRotation += num;
	}

	private static bool IsRotationPhaseInsidePhaseBoundaries(float currentPhase, float startPhase, float endPhase)
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
			if (IsRotationPhaseInsidePhaseBoundaries(currentPhase, num3, endPhase))
			{
				item.ResumeParticleSystem(doChildren: true);
			}
			else
			{
				item.PauseParticleSystem(doChildren: true);
			}
		}
	}

	public override TickRequirement GetTickRequirement()
	{
		return base.GetTickRequirement() | TickRequirement.TickParallel;
	}

	protected internal override void OnTickParallel(float dt)
	{
		Rotate(dt);
		if (isWaterMill)
		{
			DoWaterMillCalculation();
		}
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		Rotate(dt);
		if (isWaterMill)
		{
			DoWaterMillCalculation();
		}
	}

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		switch (variableName)
		{
		case "testMesh":
			if (testMesh != null)
			{
				base.GameEntity.AddMultiMesh(testMesh);
			}
			break;
		case "testTexture":
			if (testTexture != null)
			{
				Material material = base.GameEntity.GetFirstMesh().GetMaterial().CreateCopy();
				material.SetTexture(Material.MBTextureType.DiffuseMap, testTexture);
				base.GameEntity.SetMaterialForAllMeshes(material);
			}
			break;
		case "testEntity":
			_ = testEntity != null;
			break;
		case "testButton":
			rotationSpeed *= 2f;
			break;
		}
	}
}
