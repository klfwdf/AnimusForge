using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class FakePointLightSpec : ScriptComponentBehavior
{
	public float baseIntensity = 200f;

	public float noonIntensityMultiplier = 0.6f;

	public float dawnDuskIntensityMultiplier = 1f;

	public float nightIntensityMultiplier = 1.5f;

	private const string ManagedTag = "fake_spec";

	private const float PlateauHalfWidth = 2f;

	private readonly List<Light> _managedLights = new List<Light>();

	private List<WeakGameEntity> _descendantBuffer = new List<WeakGameEntity>();

	private void CacheManagedLights()
	{
		_managedLights.Clear();
		_descendantBuffer.Clear();
		base.GameEntity.GetChildrenRecursive(ref _descendantBuffer);
		for (int i = 0; i < _descendantBuffer.Count; i++)
		{
			WeakGameEntity weakGameEntity = _descendantBuffer[i];
			if (weakGameEntity.HasTag("fake_spec"))
			{
				Light light = weakGameEntity.GetLight();
				if (!(light == null) && light.IsValid)
				{
					light.SetShadowType(Light.ShadowType.NoShadow);
					_managedLights.Add(light);
				}
			}
		}
	}

	private float GetTimeOfDayMultiplier(float hour)
	{
		hour -= (float)MathF.Floor(hour / 24f) * 24f;
		float num = noonIntensityMultiplier;
		float num2 = dawnDuskIntensityMultiplier;
		float num3 = nightIntensityMultiplier;
		if (hour <= 6f)
		{
			return LerpBetweenPlateaus(hour, 0f, 6f, num3, num2);
		}
		if (hour <= 12f)
		{
			return LerpBetweenPlateaus(hour, 6f, 12f, num2, num);
		}
		if (hour <= 18f)
		{
			return LerpBetweenPlateaus(hour, 12f, 18f, num, num2);
		}
		return LerpBetweenPlateaus(hour, 18f, 24f, num2, num3);
	}

	private static float LerpBetweenPlateaus(float hour, float fromHour, float toHour, float fromValue, float toValue)
	{
		float num = fromHour + 2f;
		float num2 = toHour - 2f;
		if (hour <= num)
		{
			return fromValue;
		}
		if (hour >= num2)
		{
			return toValue;
		}
		float num3 = num2 - num;
		if (num3 <= 0f)
		{
			return (fromValue + toValue) * 0.5f;
		}
		float num4 = (hour - num) / num3;
		return fromValue + (toValue - fromValue) * num4;
	}

	private void ApplyIntensity(float intensity)
	{
		for (int i = 0; i < _managedLights.Count; i++)
		{
			_managedLights[i].Intensity = intensity;
		}
	}

	private void UpdateLights()
	{
		if (_managedLights.Count != 0)
		{
			float hour = 12f;
			Scene scene = base.GameEntity.Scene;
			if (scene != null)
			{
				hour = scene.TimeOfDay;
			}
			float intensity = baseIntensity * GetTimeOfDayMultiplier(hour);
			ApplyIntensity(intensity);
		}
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		CacheManagedLights();
		SetScriptComponentToTick(GetTickRequirement());
	}

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		CacheManagedLights();
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		UpdateLights();
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		UpdateLights();
	}

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		switch (variableName)
		{
		case "baseIntensity":
			baseIntensity = MathF.Max(0f, baseIntensity);
			break;
		case "noonIntensityMultiplier":
		case "dawnDuskIntensityMultiplier":
		case "nightIntensityMultiplier":
			noonIntensityMultiplier = MathF.Max(0f, noonIntensityMultiplier);
			dawnDuskIntensityMultiplier = MathF.Max(0f, dawnDuskIntensityMultiplier);
			nightIntensityMultiplier = MathF.Max(0f, nightIntensityMultiplier);
			break;
		}
	}
}
