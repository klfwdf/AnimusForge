using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace SandBox.View.Map
{
	// Token: 0x0200004A RID: 74
	public class MapConversationView : MapView
	{
		// Token: 0x17000038 RID: 56
		// (get) Token: 0x0600027F RID: 639 RVA: 0x0001772F File Offset: 0x0001592F
		// (set) Token: 0x06000280 RID: 640 RVA: 0x00017737 File Offset: 0x00015937
		public bool IsConversationActive { get; protected set; }

		// Token: 0x06000281 RID: 641 RVA: 0x00017740 File Offset: 0x00015940
		protected internal virtual void InitializeConversation(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
		{
		}

		// Token: 0x06000282 RID: 642 RVA: 0x00017742 File Offset: 0x00015942
		protected internal override void OnFinalize()
		{
			base.OnFinalize();
			this.DestroyConversationMission();
		}

		// Token: 0x06000283 RID: 643 RVA: 0x00017750 File Offset: 0x00015950
		protected internal virtual void FinalizeConversation()
		{
		}

		// Token: 0x06000284 RID: 644 RVA: 0x00017754 File Offset: 0x00015954
		protected void CreateConversationMissionIfMissing()
		{
			MapConversationView.MapConversationMission conversationMission;
			if ((conversationMission = CampaignMission.Current as MapConversationView.MapConversationMission) != null)
			{
				this.ConversationMission = conversationMission;
				return;
			}
			this.ConversationMission = new MapConversationView.MapConversationMission();
			CampaignMission.Current = this.ConversationMission;
		}

		// Token: 0x06000285 RID: 645 RVA: 0x0001778D File Offset: 0x0001598D
		protected void DestroyConversationMission()
		{
			MapConversationView.MapConversationMission conversationMission = this.ConversationMission;
			if (conversationMission != null)
			{
				conversationMission.OnFinalize();
			}
			this.ConversationMission = null;
		}

		// Token: 0x04000166 RID: 358
		public MapConversationView.MapConversationMission ConversationMission;

		// Token: 0x020000A4 RID: 164
		public class MapConversationMission : ICampaignMission
		{
			// Token: 0x170000B3 RID: 179
			// (get) Token: 0x060005AC RID: 1452 RVA: 0x00028EBD File Offset: 0x000270BD
			GameState ICampaignMission.State
			{
				get
				{
					return GameStateManager.Current.ActiveState;
				}
			}

			// Token: 0x170000B4 RID: 180
			// (get) Token: 0x060005AD RID: 1453 RVA: 0x00028EC9 File Offset: 0x000270C9
			IMissionTroopSupplier ICampaignMission.AgentSupplier
			{
				get
				{
					return null;
				}
			}

			// Token: 0x170000B5 RID: 181
			// (get) Token: 0x060005AE RID: 1454 RVA: 0x00028ECC File Offset: 0x000270CC
			// (set) Token: 0x060005AF RID: 1455 RVA: 0x00028ED4 File Offset: 0x000270D4
			Location ICampaignMission.Location { get; set; }

			// Token: 0x170000B6 RID: 182
			// (get) Token: 0x060005B0 RID: 1456 RVA: 0x00028EDD File Offset: 0x000270DD
			// (set) Token: 0x060005B1 RID: 1457 RVA: 0x00028EE5 File Offset: 0x000270E5
			Alley ICampaignMission.LastVisitedAlley { get; set; }

			// Token: 0x170000B7 RID: 183
			// (get) Token: 0x060005B2 RID: 1458 RVA: 0x00028EEE File Offset: 0x000270EE
			MissionMode ICampaignMission.Mode
			{
				get
				{
					return MissionMode.Conversation;
				}
			}

			// Token: 0x170000B8 RID: 184
			// (get) Token: 0x060005B3 RID: 1459 RVA: 0x00028EF1 File Offset: 0x000270F1
			// (set) Token: 0x060005B4 RID: 1460 RVA: 0x00028EF9 File Offset: 0x000270F9
			public MapConversationTableau ConversationTableau { get; private set; }

			// Token: 0x060005B5 RID: 1461 RVA: 0x00028F02 File Offset: 0x00027102
			public MapConversationMission()
			{
				CampaignMission.Current = this;
				this._conversationPlayQueue = new Queue<MapConversationView.MapConversationMission.ConversationPlayArgs>();
			}

			// Token: 0x060005B6 RID: 1462 RVA: 0x00028F1B File Offset: 0x0002711B
			public void SetConversationTableau(MapConversationTableau tableau)
			{
				this.ConversationTableau = tableau;
				this.PlayCachedConversations();
			}

			// Token: 0x060005B7 RID: 1463 RVA: 0x00028F2A File Offset: 0x0002712A
			public void Tick(float dt)
			{
				this.PlayCachedConversations();
			}

			// Token: 0x060005B8 RID: 1464 RVA: 0x00028F32 File Offset: 0x00027132
			public void OnFinalize()
			{
				this.ConversationTableau = null;
				this._conversationPlayQueue = null;
				CampaignMission.Current = null;
			}

			// Token: 0x060005B9 RID: 1465 RVA: 0x00028F48 File Offset: 0x00027148
			private void PlayCachedConversations()
			{
				if (this.ConversationTableau != null)
				{
					while (this._conversationPlayQueue.Count > 0)
					{
						MapConversationView.MapConversationMission.ConversationPlayArgs conversationPlayArgs = this._conversationPlayQueue.Dequeue();
						this.ConversationTableau.OnConversationPlay(conversationPlayArgs.IdleActionId, conversationPlayArgs.IdleFaceAnimId, conversationPlayArgs.ReactionId, conversationPlayArgs.ReactionFaceAnimId, conversationPlayArgs.SoundPath);
					}
				}
			}

			// Token: 0x060005BA RID: 1466 RVA: 0x00028FA2 File Offset: 0x000271A2
			void ICampaignMission.OnConversationPlay(string idleActionId, string idleFaceAnimId, string reactionId, string reactionFaceAnimId, string soundPath)
			{
				if (this.ConversationTableau != null)
				{
					this.ConversationTableau.OnConversationPlay(idleActionId, idleFaceAnimId, reactionId, reactionFaceAnimId, soundPath);
					return;
				}
				this._conversationPlayQueue.Enqueue(new MapConversationView.MapConversationMission.ConversationPlayArgs(idleActionId, idleFaceAnimId, reactionId, reactionFaceAnimId, soundPath));
			}

			// Token: 0x060005BB RID: 1467 RVA: 0x00028FD6 File Offset: 0x000271D6
			void ICampaignMission.AddAgentFollowing(IAgent agent)
			{
			}

			// Token: 0x060005BC RID: 1468 RVA: 0x00028FD8 File Offset: 0x000271D8
			bool ICampaignMission.AgentLookingAtAgent(IAgent agent1, IAgent agent2)
			{
				return false;
			}

			// Token: 0x060005BD RID: 1469 RVA: 0x00028FDB File Offset: 0x000271DB
			bool ICampaignMission.CheckIfAgentCanFollow(IAgent agent)
			{
				return false;
			}

			// Token: 0x060005BE RID: 1470 RVA: 0x00028FDE File Offset: 0x000271DE
			bool ICampaignMission.CheckIfAgentCanUnFollow(IAgent agent)
			{
				return false;
			}

			// Token: 0x060005BF RID: 1471 RVA: 0x00028FE1 File Offset: 0x000271E1
			void ICampaignMission.EndMission()
			{
			}

			// Token: 0x060005C0 RID: 1472 RVA: 0x00028FE3 File Offset: 0x000271E3
			void ICampaignMission.OnCharacterLocationChanged(LocationCharacter locationCharacter, Location fromLocation, Location toLocation)
			{
			}

			// Token: 0x060005C1 RID: 1473 RVA: 0x00028FE5 File Offset: 0x000271E5
			void ICampaignMission.OnCloseEncounterMenu()
			{
			}

			// Token: 0x060005C2 RID: 1474 RVA: 0x00028FE7 File Offset: 0x000271E7
			void ICampaignMission.OnConversationContinue()
			{
			}

			// Token: 0x060005C3 RID: 1475 RVA: 0x00028FE9 File Offset: 0x000271E9
			void ICampaignMission.OnConversationEnd(IAgent agent)
			{
			}

			// Token: 0x060005C4 RID: 1476 RVA: 0x00028FEB File Offset: 0x000271EB
			void ICampaignMission.OnConversationStart(IAgent agent, bool setActionsInstantly)
			{
			}

			// Token: 0x060005C5 RID: 1477 RVA: 0x00028FED File Offset: 0x000271ED
			void ICampaignMission.OnProcessSentence()
			{
			}

			// Token: 0x060005C6 RID: 1478 RVA: 0x00028FEF File Offset: 0x000271EF
			void ICampaignMission.RemoveAgentFollowing(IAgent agent)
			{
			}

			// Token: 0x060005C7 RID: 1479 RVA: 0x00028FF1 File Offset: 0x000271F1
			void ICampaignMission.SetMissionMode(MissionMode newMode, bool atStart)
			{
			}

			// Token: 0x060005C8 RID: 1480 RVA: 0x00028FF3 File Offset: 0x000271F3
			void ICampaignMission.FadeOutCharacter(CharacterObject characterObject)
			{
			}

			// Token: 0x060005C9 RID: 1481 RVA: 0x00028FF5 File Offset: 0x000271F5
			void ICampaignMission.OnGameStateChanged()
			{
				MapConversationTableau conversationTableau = this.ConversationTableau;
				if (conversationTableau != null)
				{
					conversationTableau.RemovePreviousAgentsSoundEvent();
				}
				MapConversationTableau conversationTableau2 = this.ConversationTableau;
				if (conversationTableau2 == null)
				{
					return;
				}
				conversationTableau2.StopConversationSoundEvent();
			}

			// Token: 0x0400034F RID: 847
			private Queue<MapConversationView.MapConversationMission.ConversationPlayArgs> _conversationPlayQueue;

			// Token: 0x020000CF RID: 207
			public struct ConversationPlayArgs
			{
				// Token: 0x06000678 RID: 1656 RVA: 0x0002A3E7 File Offset: 0x000285E7
				public ConversationPlayArgs(string idleActionId, string idleFaceAnimId, string reactionId, string reactionFaceAnimId, string soundPath)
				{
					this.IdleActionId = idleActionId;
					this.IdleFaceAnimId = idleFaceAnimId;
					this.ReactionId = reactionId;
					this.ReactionFaceAnimId = reactionFaceAnimId;
					this.SoundPath = soundPath;
				}

				// Token: 0x040003EE RID: 1006
				public readonly string IdleActionId;

				// Token: 0x040003EF RID: 1007
				public readonly string IdleFaceAnimId;

				// Token: 0x040003F0 RID: 1008
				public readonly string ReactionId;

				// Token: 0x040003F1 RID: 1009
				public readonly string ReactionFaceAnimId;

				// Token: 0x040003F2 RID: 1010
				public readonly string SoundPath;
			}
		}
	}
}
