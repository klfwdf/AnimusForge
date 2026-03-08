using System;
using SandBox.Tournaments.AgentControllers;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace SandBox.View.Missions.Tournaments
{
	// Token: 0x02000028 RID: 40
	public class MissionTournamentJoustingView : MissionView
	{
		// Token: 0x06000110 RID: 272 RVA: 0x0000CCE4 File Offset: 0x0000AEE4
		public override void AfterStart()
		{
			base.AfterStart();
			this._gameSystem = Game.Current;
			this._messageUIHandler = base.Mission.GetMissionBehavior<MissionMessageUIHandler>();
			this._scoreUIHandler = base.Mission.GetMissionBehavior<MissionScoreUIHandler>();
			this._tournamentJoustingMissionController = base.Mission.GetMissionBehavior<TournamentJoustingMissionController>();
			this._tournamentJoustingMissionController.VictoryAchieved += this.OnVictoryAchieved;
			this._tournamentJoustingMissionController.PointGanied += this.OnPointGanied;
			this._tournamentJoustingMissionController.Disqualified += this.OnDisqualified;
			this._tournamentJoustingMissionController.Unconscious += this.OnUnconscious;
			this._tournamentJoustingMissionController.AgentStateChanged += this.OnAgentStateChanged;
			int num = 0;
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent.IsHuman)
				{
					this._scoreUIHandler.SetName(agent.Name.ToString(), num);
					num++;
				}
			}
			this.SetJoustingBanners();
		}

		// Token: 0x06000111 RID: 273 RVA: 0x0000CE18 File Offset: 0x0000B018
		private void RefreshScoreBoard()
		{
			int num = 0;
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent.IsHuman)
				{
					JoustingAgentController controller = agent.GetController<JoustingAgentController>();
					this._scoreUIHandler.SaveScore(controller.Score, num);
					num++;
				}
			}
		}

		// Token: 0x06000112 RID: 274 RVA: 0x0000CE90 File Offset: 0x0000B090
		private void SetJoustingBanners()
		{
			GameEntity banner0 = base.Mission.Scene.FindEntityWithTag("banner_0");
			GameEntity banner1 = base.Mission.Scene.FindEntityWithTag("banner_1");
			Banner banner = Banner.CreateOneColoredEmptyBanner(6);
			Banner banner2 = Banner.CreateOneColoredEmptyBanner(8);
			if (banner0 != null)
			{
				Action<Texture> setAction = delegate(Texture tex)
				{
					Material material = Mesh.GetFromResource("banner_test").GetMaterial().CreateCopy();
					if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
					{
						material.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
					}
					banner0.SetMaterialForAllMeshes(material);
				};
				Banner banner3 = banner;
				BannerDebugInfo bannerDebugInfo = BannerDebugInfo.CreateManual(base.GetType().Name);
				banner3.GetTableauTextureLarge(bannerDebugInfo, setAction);
			}
			if (banner1 != null)
			{
				Action<Texture> setAction2 = delegate(Texture tex)
				{
					Material material = Mesh.GetFromResource("banner_test").GetMaterial().CreateCopy();
					if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
					{
						material.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
					}
					banner1.SetMaterialForAllMeshes(material);
				};
				Banner banner4 = banner2;
				BannerDebugInfo bannerDebugInfo = BannerDebugInfo.CreateManual(base.GetType().Name);
				banner4.GetTableauTextureLarge(bannerDebugInfo, setAction2);
			}
		}

		// Token: 0x06000113 RID: 275 RVA: 0x0000CF57 File Offset: 0x0000B157
		public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
		{
			this.RefreshScoreBoard();
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000CF5F File Offset: 0x0000B15F
		private void OnVictoryAchieved(Agent affectorAgent, Agent affectedAgent)
		{
			this.ShowMessage(affectorAgent, GameTexts.FindText("str_tournament_joust_player_victory", null).ToString(), 8f, true);
			this.ShowMessage(affectedAgent, GameTexts.FindText("str_tournament_joust_opponent_victory", null).ToString(), 8f, true);
		}

		// Token: 0x06000115 RID: 277 RVA: 0x0000CF9B File Offset: 0x0000B19B
		private void OnPointGanied(Agent affectorAgent, Agent affectedAgent)
		{
			this.ShowMessage(affectorAgent, GameTexts.FindText("str_tournament_joust_you_gain_point", null).ToString(), 5f, true);
			this.ShowMessage(affectedAgent, GameTexts.FindText("str_tournament_joust_opponent_gain_point", null).ToString(), 5f, true);
		}

		// Token: 0x06000116 RID: 278 RVA: 0x0000CFD7 File Offset: 0x0000B1D7
		private void OnDisqualified(Agent affectorAgent, Agent affectedAgent)
		{
			this.ShowMessage(affectedAgent, GameTexts.FindText("str_tournament_joust_opponent_disqualified", null).ToString(), 5f, true);
			this.ShowMessage(affectorAgent, GameTexts.FindText("str_tournament_joust_you_disqualified", null).ToString(), 5f, true);
		}

		// Token: 0x06000117 RID: 279 RVA: 0x0000D013 File Offset: 0x0000B213
		private void OnUnconscious(Agent affectorAgent, Agent affectedAgent)
		{
			this.ShowMessage(affectedAgent, GameTexts.FindText("str_tournament_joust_you_become_unconscious", null).ToString(), 5f, true);
			this.ShowMessage(affectorAgent, GameTexts.FindText("str_tournament_joust_opponent_become_unconscious", null).ToString(), 5f, true);
		}

		// Token: 0x06000118 RID: 280 RVA: 0x0000D04F File Offset: 0x0000B24F
		public void ShowMessage(string str, float duration, bool hasPriority = true)
		{
			this._messageUIHandler.ShowMessage(str, duration, hasPriority);
		}

		// Token: 0x06000119 RID: 281 RVA: 0x0000D05F File Offset: 0x0000B25F
		public void ShowMessage(Agent agent, string str, float duration, bool hasPriority = true)
		{
			if (agent.Character == this._gameSystem.PlayerTroop)
			{
				this.ShowMessage(str, duration, hasPriority);
			}
		}

		// Token: 0x0600011A RID: 282 RVA: 0x0000D07E File Offset: 0x0000B27E
		public void DeleteMessage(string str)
		{
			this._messageUIHandler.DeleteMessage(str);
		}

		// Token: 0x0600011B RID: 283 RVA: 0x0000D08C File Offset: 0x0000B28C
		public void DeleteMessage(Agent agent, string str)
		{
			this.DeleteMessage(str);
		}

		// Token: 0x0600011C RID: 284 RVA: 0x0000D098 File Offset: 0x0000B298
		private void OnAgentStateChanged(Agent agent, JoustingAgentController.JoustingAgentState state)
		{
			string text;
			switch (state)
			{
			case JoustingAgentController.JoustingAgentState.GoingToBackStart:
				text = "";
				break;
			case JoustingAgentController.JoustingAgentState.GoToStartPosition:
				text = "str_tournament_joust_go_to_starting_position";
				break;
			case JoustingAgentController.JoustingAgentState.WaitInStartPosition:
				text = "str_tournament_joust_wait_in_starting_position";
				break;
			case JoustingAgentController.JoustingAgentState.WaitingOpponent:
				text = "str_tournament_joust_wait_opponent_to_go_starting_position";
				break;
			case JoustingAgentController.JoustingAgentState.Ready:
				text = "str_tournament_joust_go";
				break;
			case JoustingAgentController.JoustingAgentState.StartRiding:
				text = "";
				break;
			case JoustingAgentController.JoustingAgentState.Riding:
				text = "";
				break;
			case JoustingAgentController.JoustingAgentState.RidingAtWrongSide:
				text = "str_tournament_joust_wrong_side";
				break;
			case JoustingAgentController.JoustingAgentState.SwordDuel:
				text = "";
				break;
			default:
				throw new ArgumentOutOfRangeException("value");
			}
			if (text == "")
			{
				this.ShowMessage(agent, "", 15f, true);
			}
			else
			{
				this.ShowMessage(agent, GameTexts.FindText(text, null).ToString(), float.PositiveInfinity, true);
			}
			if (state == JoustingAgentController.JoustingAgentState.SwordDuel)
			{
				this.ShowMessage(agent, GameTexts.FindText("str_tournament_joust_duel_on_foot", null).ToString(), 8f, true);
			}
		}

		// Token: 0x04000085 RID: 133
		private MissionScoreUIHandler _scoreUIHandler;

		// Token: 0x04000086 RID: 134
		private MissionMessageUIHandler _messageUIHandler;

		// Token: 0x04000087 RID: 135
		private TournamentJoustingMissionController _tournamentJoustingMissionController;

		// Token: 0x04000088 RID: 136
		private Game _gameSystem;
	}
}
