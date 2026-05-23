using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Objects;

public class ShipVisual : ScriptComponentBehavior
{
	private float _health = 1f;

	public (uint sailColor1, uint sailColor2) SailColors = (sailColor1: Colors.White.ToUnsignedInteger(), sailColor2: Colors.White.ToUnsignedInteger());

	public int Seed { get; private set; }

	public string CustomSailPatternId { get; private set; }

	public List<ScriptComponentBehavior> SailVisuals { get; private set; } = new List<ScriptComponentBehavior>();

	public float Health
	{
		get
		{
			return _health;
		}
		set
		{
			_health = MathF.Clamp(value, 0f, 1f);
		}
	}

	public void Initialize(int seed, string customSailPatternId = "")
	{
		Seed = seed;
		CustomSailPatternId = customSailPatternId;
	}
}
