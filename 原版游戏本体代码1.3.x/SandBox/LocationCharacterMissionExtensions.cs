using System;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.MountAndBlade;

namespace SandBox
{
	// Token: 0x02000020 RID: 32
	public static class LocationCharacterMissionExtensions
	{
		// Token: 0x060000E7 RID: 231 RVA: 0x0000612E File Offset: 0x0000432E
		public static AgentBuildData GetAgentBuildData(this LocationCharacter locationCharacter)
		{
			return new AgentBuildData(locationCharacter.AgentData);
		}
	}
}
