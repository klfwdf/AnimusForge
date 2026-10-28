using SandBox.GauntletUI.Map;
using SandBox.View.Map;
using StoryMode.GameComponents.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace StoryMode.GauntletUI.Map;

[OverrideView(typeof(MapCheatsView))]
internal class GauntletStoryModeMapCheatsView : GauntletMapCheatsView
{
	protected override void CreateLayout()
	{
		base.CreateLayout();
		AchievementsCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<AchievementsCampaignBehavior>();
		if (campaignBehavior == null || !campaignBehavior.CheckAchievementSystemActivity(out var _))
		{
			EnableCheatMenu();
			return;
		}
		_layerAsGauntletLayer.UIContext.ContextAlpha = 0f;
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=4Ygn4OGE}Enable Cheats").ToString(), new TextObject("{=YkbOfPRU}Enabling cheats will disable the achievements this game. Do you want to proceed?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), EnableCheatMenu, RemoveCheatMenu));
	}

	private void EnableCheatMenu()
	{
		_layerAsGauntletLayer.UIContext.ContextAlpha = 1f;
		AchievementsCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<AchievementsCampaignBehavior>();
		if (campaignBehavior != null && campaignBehavior.CheckAchievementSystemActivity(out var _))
		{
			campaignBehavior?.DeactivateAchievements(new TextObject("{=sO8Zh3ZH}Achievements are disabled due to cheat usage."));
		}
	}

	private void RemoveCheatMenu()
	{
		base.MapScreen.CloseGameplayCheats();
	}

	protected override void OnMapConversationStart()
	{
		base.OnMapConversationStart();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: true);
		}
	}

	protected override void OnMapConversationOver()
	{
		base.OnMapConversationOver();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: false);
		}
	}
}
