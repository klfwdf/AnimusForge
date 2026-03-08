using System;
using TaleWorlds.CampaignSystem.Map;

namespace SandBox
{
	// Token: 0x0200001C RID: 28
	public class MapSceneCreator : IMapSceneCreator
	{
		// Token: 0x0600008D RID: 141 RVA: 0x00005029 File Offset: 0x00003229
		IMapScene IMapSceneCreator.CreateMapScene()
		{
			return new MapScene();
		}
	}
}
