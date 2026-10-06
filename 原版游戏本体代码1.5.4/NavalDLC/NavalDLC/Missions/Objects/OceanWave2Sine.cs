using System;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.Missions.Objects;

public class OceanWave2Sine : ScriptComponentBehavior
{
	[EditableScriptComponentVariable(true, "Enabled In Editor")]
	public bool enabledInEditor = true;

	[EditableScriptComponentVariable(true, "Wave 1 Frequency (rad/s)")]
	public float wave1Frequency = 1f;

	[EditableScriptComponentVariable(true, "Wave 1 Amplitude")]
	public float wave1Amplitude = 0.3f;

	[EditableScriptComponentVariable(true, "Wave 1 Wavelength")]
	public float wave1Wavelength = 8f;

	[EditableScriptComponentVariable(true, "Wave 2 Frequency (rad/s)")]
	public float wave2Frequency = 1.7f;

	[EditableScriptComponentVariable(true, "Wave 2 Amplitude")]
	public float wave2Amplitude = 0.15f;

	[EditableScriptComponentVariable(true, "Wave 2 Wavelength")]
	public float wave2Wavelength = 5f;

	[EditableScriptComponentVariable(true, "Angle Between Waves (degrees)")]
	public float phaseOffsetDegrees = 45f;

	[EditableScriptComponentVariable(true, "LOD Distance")]
	public float lodDistance = 80f;

	[EditableScriptComponentVariable(true, "Align Tagged To Wave Normal")]
	public bool alignToWaveNormal;

	[EditableScriptComponentVariable(true, "Tilt Sample Epsilon")]
	public float tiltSampleEpsilon = 0.25f;

	[EditableScriptComponentVariable(true, "Hint: tag children 'ow2s_align' to enable tilt")]
	public SimpleButton _helperTag = new SimpleButton();

	private const string AlignTag = "ow2s_align";

	private float et;

	private MatrixFrame[] childRestFrames;

	private bool[] childAlignEligible;

	private int cachedChildCount;

	private float lodDistanceSq;

	private bool atRest;

	private const float MinWavelength = 0.01f;

	private void CacheRestFrames()
	{
		cachedChildCount = base.GameEntity.ChildCount;
		childRestFrames = new MatrixFrame[cachedChildCount];
		childAlignEligible = new bool[cachedChildCount];
		for (int i = 0; i < cachedChildCount; i++)
		{
			childRestFrames[i] = base.GameEntity.GetChild(i).GetLocalFrame();
			childAlignEligible[i] = base.GameEntity.GetChild(i).HasTag("ow2s_align");
		}
		atRest = true;
	}

	private void ResetChildrenToRest()
	{
		if (childRestFrames != null && !atRest)
		{
			int num = TaleWorlds.Library.MathF.Min(cachedChildCount, base.GameEntity.ChildCount);
			for (int i = 0; i < num; i++)
			{
				MatrixFrame frame = childRestFrames[i];
				base.GameEntity.GetChild(i).SetLocalFrame(ref frame, isTeleportation: false);
			}
			atRest = true;
		}
	}

	private bool IsBeyondLod()
	{
		if (lodDistance <= 0f)
		{
			return false;
		}
		Vec3 lastFinalRenderCameraPositionOfScene = base.GameEntity.GetLastFinalRenderCameraPositionOfScene();
		return base.GameEntity.GetGlobalFrame().origin.DistanceSquared(lastFinalRenderCameraPositionOfScene) > lodDistanceSq;
	}

	private void Animate(float dt)
	{
		et += dt;
		if (base.GameEntity.ChildCount != cachedChildCount || childRestFrames == null)
		{
			CacheRestFrames();
		}
		float x = System.MathF.PI / 180f * phaseOffsetDegrees;
		float d1x = 1f;
		float d1y = 0f;
		float d2x = TaleWorlds.Library.MathF.Cos(x);
		float d2y = TaleWorlds.Library.MathF.Sin(x);
		float num = TaleWorlds.Library.MathF.Max(0.01f, wave1Wavelength);
		float num2 = TaleWorlds.Library.MathF.Max(0.01f, wave2Wavelength);
		float k = System.MathF.PI * 2f / num;
		float k2 = System.MathF.PI * 2f / num2;
		float t = et * wave1Frequency;
		float t2 = et * wave2Frequency;
		float num3 = TaleWorlds.Library.MathF.Max(0.001f, tiltSampleEpsilon);
		float num4 = 1f / num3;
		for (int i = 0; i < cachedChildCount; i++)
		{
			MatrixFrame frame = childRestFrames[i];
			float x2 = frame.origin.x;
			float y = frame.origin.y;
			float num5 = SampleHeight(x2, y, d1x, d1y, k, t, d2x, d2y, k2, t2);
			frame.origin.z += num5;
			if (alignToWaveNormal && childAlignEligible[i])
			{
				float num6 = SampleHeight(x2 + num3, y, d1x, d1y, k, t, d2x, d2y, k2, t2);
				float num7 = SampleHeight(x2, y + num3, d1x, d1y, k, t, d2x, d2y, k2, t2);
				float x3 = (num6 - num5) * num4;
				float x4 = (num7 - num5) * num4;
				float a = TaleWorlds.Library.MathF.Atan(x3);
				float a2 = 0f - TaleWorlds.Library.MathF.Atan(x4);
				frame.rotation.RotateAboutSide(a2);
				frame.rotation.RotateAboutForward(a);
			}
			base.GameEntity.GetChild(i).SetLocalFrame(ref frame, isTeleportation: false);
		}
		atRest = false;
	}

	private float SampleHeight(float x, float y, float d1x, float d1y, float k1, float t1, float d2x, float d2y, float k2, float t2)
	{
		float num = x * d1x + y * d1y;
		float num2 = x * d2x + y * d2y;
		float num3 = TaleWorlds.Library.MathF.Sin(k1 * num - t1) * wave1Amplitude;
		float num4 = TaleWorlds.Library.MathF.Sin(k2 * num2 - t2) * wave2Amplitude;
		return num3 + num4;
	}

	private void RefreshLodDistanceSq()
	{
		lodDistanceSq = lodDistance * lodDistance;
	}

	protected override void OnInit()
	{
		base.OnInit();
		CacheRestFrames();
		RefreshLodDistanceSq();
		SetScriptComponentToTick(GetTickRequirement());
	}

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		CacheRestFrames();
		RefreshLodDistanceSq();
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected override void OnTick(float dt)
	{
		if (IsBeyondLod())
		{
			ResetChildrenToRest();
		}
		else
		{
			Animate(dt);
		}
	}

	protected override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		if (!enabledInEditor || IsBeyondLod())
		{
			ResetChildrenToRest();
		}
		else
		{
			Animate(dt);
		}
	}

	protected override void OnSceneSave(string saveFolder)
	{
		base.OnSceneSave(saveFolder);
		ResetChildrenToRest();
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		switch (variableName)
		{
		case "wave1Frequency":
		case "wave2Frequency":
			wave1Frequency = TaleWorlds.Library.MathF.Max(0f, wave1Frequency);
			wave2Frequency = TaleWorlds.Library.MathF.Max(0f, wave2Frequency);
			break;
		case "wave1Amplitude":
		case "wave2Amplitude":
			wave1Amplitude = TaleWorlds.Library.MathF.Max(0f, wave1Amplitude);
			wave2Amplitude = TaleWorlds.Library.MathF.Max(0f, wave2Amplitude);
			break;
		case "wave1Wavelength":
		case "wave2Wavelength":
			wave1Wavelength = TaleWorlds.Library.MathF.Max(0.01f, wave1Wavelength);
			wave2Wavelength = TaleWorlds.Library.MathF.Max(0.01f, wave2Wavelength);
			break;
		case "lodDistance":
			lodDistance = TaleWorlds.Library.MathF.Max(0f, lodDistance);
			RefreshLodDistanceSq();
			break;
		case "tiltSampleEpsilon":
			tiltSampleEpsilon = TaleWorlds.Library.MathF.Max(0.001f, tiltSampleEpsilon);
			break;
		case "enabledInEditor":
			if (!enabledInEditor)
			{
				ResetChildrenToRest();
			}
			break;
		case "alignToWaveNormal":
			CacheRestFrames();
			break;
		}
	}
}
