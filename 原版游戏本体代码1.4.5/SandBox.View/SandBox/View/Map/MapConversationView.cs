using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace SandBox.View.Map;

public class MapConversationView : MapView
{
	public class MapConversationMission : ICampaignMission
	{
		public struct ConversationPlayArgs
		{
			public readonly string IdleActionId;

			public readonly string IdleFaceAnimId;

			public readonly string ReactionId;

			public readonly string ReactionFaceAnimId;

			public readonly string SoundPath;

			public ConversationPlayArgs(string idleActionId, string idleFaceAnimId, string reactionId, string reactionFaceAnimId, string soundPath)
			{
				IdleActionId = idleActionId;
				IdleFaceAnimId = idleFaceAnimId;
				ReactionId = reactionId;
				ReactionFaceAnimId = reactionFaceAnimId;
				SoundPath = soundPath;
			}
		}

		private Queue<ConversationPlayArgs> _conversationPlayQueue;

		GameState ICampaignMission.State => GameStateManager.Current.ActiveState;

		IMissionTroopSupplier ICampaignMission.AgentSupplier => null;

		Location ICampaignMission.Location { get; set; }

		Alley ICampaignMission.LastVisitedAlley { get; set; }

		MissionMode ICampaignMission.Mode => MissionMode.Conversation;

		public MapConversationTableau ConversationTableau { get; private set; }

		public MapConversationMission()
		{
			CampaignMission.Current = this;
			_conversationPlayQueue = new Queue<ConversationPlayArgs>();
		}

		public void SetConversationTableau(MapConversationTableau tableau)
		{
			ConversationTableau = tableau;
			PlayCachedConversations();
		}

		public void Tick(float dt)
		{
			PlayCachedConversations();
		}

		public void OnFinalize()
		{
			ConversationTableau = null;
			_conversationPlayQueue = null;
			CampaignMission.Current = null;
		}

		private void PlayCachedConversations()
		{
			if (ConversationTableau != null)
			{
				while (_conversationPlayQueue.Count > 0)
				{
					ConversationPlayArgs conversationPlayArgs = _conversationPlayQueue.Dequeue();
					ConversationTableau.OnConversationPlay(conversationPlayArgs.IdleActionId, conversationPlayArgs.IdleFaceAnimId, conversationPlayArgs.ReactionId, conversationPlayArgs.ReactionFaceAnimId, conversationPlayArgs.SoundPath);
				}
			}
		}

		void ICampaignMission.OnConversationPlay(string idleActionId, string idleFaceAnimId, string reactionId, string reactionFaceAnimId, string soundPath)
		{
			if (ConversationTableau != null)
			{
				ConversationTableau.OnConversationPlay(idleActionId, idleFaceAnimId, reactionId, reactionFaceAnimId, soundPath);
			}
			else
			{
				_conversationPlayQueue.Enqueue(new ConversationPlayArgs(idleActionId, idleFaceAnimId, reactionId, reactionFaceAnimId, soundPath));
			}
		}

		void ICampaignMission.AddAgentFollowing(IAgent agent)
		{
		}

		bool ICampaignMission.AgentLookingAtAgent(IAgent agent1, IAgent agent2)
		{
			return false;
		}

		bool ICampaignMission.CheckIfAgentCanFollow(IAgent agent)
		{
			return false;
		}

		bool ICampaignMission.CheckIfAgentCanUnFollow(IAgent agent)
		{
			return false;
		}

		void ICampaignMission.EndMission()
		{
		}

		void ICampaignMission.OnCharacterLocationChanged(LocationCharacter locationCharacter, Location fromLocation, Location toLocation)
		{
		}

		void ICampaignMission.OnCloseEncounterMenu()
		{
		}

		void ICampaignMission.OnConversationContinue()
		{
		}

		void ICampaignMission.OnConversationEnd(IAgent agent)
		{
		}

		void ICampaignMission.OnConversationStart(IAgent agent, bool setActionsInstantly)
		{
		}

		void ICampaignMission.OnProcessSentence()
		{
		}

		void ICampaignMission.RemoveAgentFollowing(IAgent agent)
		{
		}

		void ICampaignMission.SetMissionMode(MissionMode newMode, bool atStart)
		{
		}

		void ICampaignMission.FadeOutCharacter(CharacterObject characterObject)
		{
		}

		void ICampaignMission.OnGameStateChanged()
		{
			ConversationTableau?.RemovePreviousAgentsSoundEvent();
			ConversationTableau?.StopConversationSoundEvent();
		}
	}

	public MapConversationMission ConversationMission;

	public bool IsConversationActive { get; protected set; }

	protected internal virtual void InitializeConversation(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
	{
	}

	protected internal override void OnFinalize()
	{
		base.OnFinalize();
		DestroyConversationMission();
	}

	protected internal virtual void FinalizeConversation()
	{
	}

	protected void CreateConversationMissionIfMissing()
	{
		if (CampaignMission.Current is MapConversationMission conversationMission)
		{
			ConversationMission = conversationMission;
			return;
		}
		ConversationMission = new MapConversationMission();
		CampaignMission.Current = ConversationMission;
	}

	protected void DestroyConversationMission()
	{
		ConversationMission?.OnFinalize();
		ConversationMission = null;
	}
}
