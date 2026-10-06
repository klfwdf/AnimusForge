using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace NavalDLC;

public interface INavalMapSceneWrapper
{
	List<(CampaignVec2, float)> GetSpawnPoints(string tag);

	Vec2 GetWindAtPosition(Vec2 position);

	void Tick(float dt);
}
