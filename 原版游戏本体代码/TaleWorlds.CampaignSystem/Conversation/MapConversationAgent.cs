using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Conversation
{
	// Token: 0x0200022E RID: 558
	public class MapConversationAgent : IAgent
	{
		// Token: 0x060021DA RID: 8666 RVA: 0x00094C65 File Offset: 0x00092E65
		public MapConversationAgent(CharacterObject characterObject)
		{
			this._characterObject = characterObject;
		}

		// Token: 0x1700085F RID: 2143
		// (get) Token: 0x060021DB RID: 8667 RVA: 0x00094C74 File Offset: 0x00092E74
		public BasicCharacterObject Character
		{
			get
			{
				return this._characterObject;
			}
		}

		// Token: 0x060021DC RID: 8668 RVA: 0x00094C7C File Offset: 0x00092E7C
		public bool IsEnemyOf(IAgent agent)
		{
			return false;
		}

		// Token: 0x060021DD RID: 8669 RVA: 0x00094C7F File Offset: 0x00092E7F
		public bool IsFriendOf(IAgent agent)
		{
			return true;
		}

		// Token: 0x17000860 RID: 2144
		// (get) Token: 0x060021DE RID: 8670 RVA: 0x00094C82 File Offset: 0x00092E82
		public AgentState State
		{
			get
			{
				return AgentState.Active;
			}
		}

		// Token: 0x17000861 RID: 2145
		// (get) Token: 0x060021DF RID: 8671 RVA: 0x00094C85 File Offset: 0x00092E85
		public IMissionTeam Team
		{
			get
			{
				return null;
			}
		}

		// Token: 0x17000862 RID: 2146
		// (get) Token: 0x060021E0 RID: 8672 RVA: 0x00094C88 File Offset: 0x00092E88
		public IAgentOriginBase Origin
		{
			get
			{
				return null;
			}
		}

		// Token: 0x17000863 RID: 2147
		// (get) Token: 0x060021E1 RID: 8673 RVA: 0x00094C8B File Offset: 0x00092E8B
		public float Age
		{
			get
			{
				return this.Character.Age;
			}
		}

		// Token: 0x060021E2 RID: 8674 RVA: 0x00094C98 File Offset: 0x00092E98
		public bool IsActive()
		{
			return true;
		}

		// Token: 0x060021E3 RID: 8675 RVA: 0x00094C9B File Offset: 0x00092E9B
		public void SetAsConversationAgent(bool set)
		{
		}

		// Token: 0x040009DC RID: 2524
		private CharacterObject _characterObject;

		// Token: 0x040009DD RID: 2525
		public bool DeliveredLine;
	}
}
