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

namespace SandBox.View.Missions.Tournaments;

public class MissionTournamentJoustingView : MissionView
{
	private MissionScoreUIHandler _scoreUIHandler;

	private MissionMessageUIHandler _messageUIHandler;

	private TournamentJoustingMissionController _tournamentJoustingMissionController;

	private Game _gameSystem;

	public override void AfterStart()
	{
		base.AfterStart();
		_gameSystem = Game.Current;
		_messageUIHandler = base.Mission.GetMissionBehavior<MissionMessageUIHandler>();
		_scoreUIHandler = base.Mission.GetMissionBehavior<MissionScoreUIHandler>();
		_tournamentJoustingMissionController = base.Mission.GetMissionBehavior<TournamentJoustingMissionController>();
		_tournamentJoustingMissionController.VictoryAchieved += OnVictoryAchieved;
		_tournamentJoustingMissionController.PointGanied += OnPointGanied;
		_tournamentJoustingMissionController.Disqualified += OnDisqualified;
		_tournamentJoustingMissionController.Unconscious += OnUnconscious;
		_tournamentJoustingMissionController.AgentStateChanged += OnAgentStateChanged;
		int num = 0;
		foreach (Agent agent in base.Mission.Agents)
		{
			if (agent.IsHuman)
			{
				_scoreUIHandler.SetName(agent.Name.ToString(), num);
				num++;
			}
		}
		SetJoustingBanners();
	}

	private void RefreshScoreBoard()
	{
		int num = 0;
		foreach (Agent agent in base.Mission.Agents)
		{
			if (agent.IsHuman)
			{
				JoustingAgentController controller = agent.GetController<JoustingAgentController>();
				_scoreUIHandler.SaveScore(controller.Score, num);
				num++;
			}
		}
	}

	private void SetJoustingBanners()
	{
		GameEntity banner0 = base.Mission.Scene.FindEntityWithTag("banner_0");
		GameEntity banner1 = base.Mission.Scene.FindEntityWithTag("banner_1");
		Banner banner2 = Banner.CreateOneColoredEmptyBanner(6);
		Banner banner3 = Banner.CreateOneColoredEmptyBanner(8);
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
			banner2.GetTableauTextureLarge(BannerDebugInfo.CreateManual(GetType().Name), setAction);
		}
		if (!(banner1 != null))
		{
			return;
		}
		Action<Texture> setAction2 = delegate(Texture tex)
		{
			Material material = Mesh.GetFromResource("banner_test").GetMaterial().CreateCopy();
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
			{
				material.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
			}
			banner1.SetMaterialForAllMeshes(material);
		};
		banner3.GetTableauTextureLarge(BannerDebugInfo.CreateManual(GetType().Name), setAction2);
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		RefreshScoreBoard();
	}

	private void OnVictoryAchieved(Agent affectorAgent, Agent affectedAgent)
	{
		ShowMessage(affectorAgent, GameTexts.FindText("str_tournament_joust_player_victory").ToString(), 8f);
		ShowMessage(affectedAgent, GameTexts.FindText("str_tournament_joust_opponent_victory").ToString(), 8f);
	}

	private void OnPointGanied(Agent affectorAgent, Agent affectedAgent)
	{
		ShowMessage(affectorAgent, GameTexts.FindText("str_tournament_joust_you_gain_point").ToString(), 5f);
		ShowMessage(affectedAgent, GameTexts.FindText("str_tournament_joust_opponent_gain_point").ToString(), 5f);
	}

	private void OnDisqualified(Agent affectorAgent, Agent affectedAgent)
	{
		ShowMessage(affectedAgent, GameTexts.FindText("str_tournament_joust_opponent_disqualified").ToString(), 5f);
		ShowMessage(affectorAgent, GameTexts.FindText("str_tournament_joust_you_disqualified").ToString(), 5f);
	}

	private void OnUnconscious(Agent affectorAgent, Agent affectedAgent)
	{
		ShowMessage(affectedAgent, GameTexts.FindText("str_tournament_joust_you_become_unconscious").ToString(), 5f);
		ShowMessage(affectorAgent, GameTexts.FindText("str_tournament_joust_opponent_become_unconscious").ToString(), 5f);
	}

	public void ShowMessage(string str, float duration, bool hasPriority = true)
	{
		_messageUIHandler.ShowMessage(str, duration, hasPriority);
	}

	public void ShowMessage(Agent agent, string str, float duration, bool hasPriority = true)
	{
		if (agent.Character == _gameSystem.PlayerTroop)
		{
			ShowMessage(str, duration, hasPriority);
		}
	}

	public void DeleteMessage(string str)
	{
		_messageUIHandler.DeleteMessage(str);
	}

	public void DeleteMessage(Agent agent, string str)
	{
		DeleteMessage(str);
	}

	private void OnAgentStateChanged(Agent agent, JoustingAgentController.JoustingAgentState state)
	{
		string text = "";
		text = state switch
		{
			JoustingAgentController.JoustingAgentState.GoingToBackStart => "", 
			JoustingAgentController.JoustingAgentState.GoToStartPosition => "str_tournament_joust_go_to_starting_position", 
			JoustingAgentController.JoustingAgentState.WaitInStartPosition => "str_tournament_joust_wait_in_starting_position", 
			JoustingAgentController.JoustingAgentState.WaitingOpponent => "str_tournament_joust_wait_opponent_to_go_starting_position", 
			JoustingAgentController.JoustingAgentState.Ready => "str_tournament_joust_go", 
			JoustingAgentController.JoustingAgentState.StartRiding => "", 
			JoustingAgentController.JoustingAgentState.Riding => "", 
			JoustingAgentController.JoustingAgentState.RidingAtWrongSide => "str_tournament_joust_wrong_side", 
			JoustingAgentController.JoustingAgentState.SwordDuel => "", 
			_ => throw new ArgumentOutOfRangeException("value"), 
		};
		if (text == "")
		{
			ShowMessage(agent, "", 15f);
		}
		else
		{
			ShowMessage(agent, GameTexts.FindText(text).ToString(), float.PositiveInfinity);
		}
		if (state == JoustingAgentController.JoustingAgentState.SwordDuel)
		{
			ShowMessage(agent, GameTexts.FindText("str_tournament_joust_duel_on_foot").ToString(), 8f);
		}
	}
}
