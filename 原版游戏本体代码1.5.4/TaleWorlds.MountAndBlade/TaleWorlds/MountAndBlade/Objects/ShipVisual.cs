using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Objects;

public class ShipVisual : ScriptComponentBehavior
{
	private float _health;

	public int Seed { get; private set; }

	public string CustomSailPatternId { get; private set; }

	public List<ScriptComponentBehavior> SailVisuals { get; private set; }

	public float Health
	{
		get
		{
			return _health;
		}
		private set
		{
			_health = MathF.Clamp(value, 0f, 1f);
		}
	}

	public (uint sailColor1, uint sailColor2) SailColors { get; private set; }

	public float FloatingForceMultiplier { get; private set; }

	public void Initialize(int seed, string customSailPatternId = "", float? health = null, (uint sailColor1, uint sailColor2)? sailColors = null, float? floatingForceMultiplier = null)
	{
		Seed = seed;
		CustomSailPatternId = customSailPatternId;
		SailVisuals = new List<ScriptComponentBehavior>();
		Health = (health.HasValue ? health.Value : 1f);
		SailColors = (sailColors.HasValue ? sailColors.Value : (sailColor1: Colors.White.ToUnsignedInteger(), sailColor2: Colors.White.ToUnsignedInteger()));
		FloatingForceMultiplier = (floatingForceMultiplier.HasValue ? floatingForceMultiplier.Value : 1f);
	}

	public void UpdateParameters(float? health = null, (uint sailColor1, uint sailColor2)? sailColors = null, float? floatingForceMultiplier = null)
	{
		if (health.HasValue)
		{
			Health = health.Value;
		}
		if (sailColors.HasValue)
		{
			SailColors = sailColors.Value;
		}
		if (floatingForceMultiplier.HasValue)
		{
			FloatingForceMultiplier = floatingForceMultiplier.Value;
		}
	}
}
