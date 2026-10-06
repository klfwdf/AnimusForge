using SandBox.Objects.Usables;
using TaleWorlds.Library;

namespace SandBox.Objects;

public class GroupSpawnPoint : UsablePlace
{
	public float Delay = -1f;

	public int SpawnCount = 1;

	public bool IsInstant
	{
		get
		{
			if (!(Delay < 0f))
			{
				return Delay.ApproximatelyEqualsTo(0f);
			}
			return true;
		}
	}
}
