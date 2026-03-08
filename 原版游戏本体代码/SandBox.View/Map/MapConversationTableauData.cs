using System;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace SandBox.View.Map
{
	// Token: 0x02000047 RID: 71
	public class MapConversationTableauData
	{
		// Token: 0x1700002F RID: 47
		// (get) Token: 0x06000267 RID: 615 RVA: 0x00017400 File Offset: 0x00015600
		// (set) Token: 0x06000268 RID: 616 RVA: 0x00017408 File Offset: 0x00015608
		public ConversationCharacterData PlayerCharacterData { get; private set; }

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x06000269 RID: 617 RVA: 0x00017411 File Offset: 0x00015611
		// (set) Token: 0x0600026A RID: 618 RVA: 0x00017419 File Offset: 0x00015619
		public ConversationCharacterData ConversationPartnerData { get; private set; }

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x0600026B RID: 619 RVA: 0x00017422 File Offset: 0x00015622
		// (set) Token: 0x0600026C RID: 620 RVA: 0x0001742A File Offset: 0x0001562A
		public TerrainType ConversationTerrainType { get; private set; }

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x0600026D RID: 621 RVA: 0x00017433 File Offset: 0x00015633
		// (set) Token: 0x0600026E RID: 622 RVA: 0x0001743B File Offset: 0x0001563B
		public float TimeOfDay { get; private set; }

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x0600026F RID: 623 RVA: 0x00017444 File Offset: 0x00015644
		// (set) Token: 0x06000270 RID: 624 RVA: 0x0001744C File Offset: 0x0001564C
		public bool IsCurrentTerrainUnderSnow { get; private set; }

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x06000271 RID: 625 RVA: 0x00017455 File Offset: 0x00015655
		// (set) Token: 0x06000272 RID: 626 RVA: 0x0001745D File Offset: 0x0001565D
		public Settlement Settlement { get; private set; }

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x06000273 RID: 627 RVA: 0x00017466 File Offset: 0x00015666
		// (set) Token: 0x06000274 RID: 628 RVA: 0x0001746E File Offset: 0x0001566E
		public string LocationId { get; private set; }

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x06000275 RID: 629 RVA: 0x00017477 File Offset: 0x00015677
		// (set) Token: 0x06000276 RID: 630 RVA: 0x0001747F File Offset: 0x0001567F
		public bool IsSnowing { get; private set; }

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x06000277 RID: 631 RVA: 0x00017488 File Offset: 0x00015688
		// (set) Token: 0x06000278 RID: 632 RVA: 0x00017490 File Offset: 0x00015690
		public bool IsRaining { get; private set; }

		// Token: 0x06000279 RID: 633 RVA: 0x00017499 File Offset: 0x00015699
		private MapConversationTableauData()
		{
		}

		// Token: 0x0600027A RID: 634 RVA: 0x000174A4 File Offset: 0x000156A4
		public static MapConversationTableauData CreateFrom(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData, TerrainType terrainType, float timeOfDay, bool isCurrentTerrainUnderSnow, Settlement settlement, string locationId, bool isRaining, bool isSnowing)
		{
			return new MapConversationTableauData
			{
				PlayerCharacterData = playerCharacterData,
				ConversationPartnerData = conversationPartnerData,
				ConversationTerrainType = terrainType,
				TimeOfDay = timeOfDay,
				IsCurrentTerrainUnderSnow = isCurrentTerrainUnderSnow,
				Settlement = settlement,
				LocationId = locationId,
				IsRaining = isRaining,
				IsSnowing = isSnowing
			};
		}
	}
}
