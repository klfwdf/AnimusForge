using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Actions;

public class ChangePlayerCharacterAction
{
	public static void Apply(Hero hero)
	{
		Hero mainHero = Hero.MainHero;
		MobileParty mainParty = MobileParty.MainParty;
		CampaignVec2 position = MobileParty.MainParty.Anchor.Position;
		CampaignVec2 lastUsedDisembarkPosition = MobileParty.MainParty.Anchor.GetLastUsedDisembarkPosition();
		bool isCurrentlyAtSea = MobileParty.MainParty.IsCurrentlyAtSea;
		Game.Current.PlayerTroop = hero.CharacterObject;
		if (MobileParty.MainParty.Anchor.IsMovingToPoint)
		{
			MobileParty.MainParty.Anchor.ResetMoveTarget();
		}
		CampaignEventDispatcher.Instance.OnBeforePlayerCharacterChanged(mainHero, hero);
		Campaign.Current.OnPlayerCharacterChanged(out var isMainPartyChanged);
		if (mainParty.Ships.Count > 0 && isMainPartyChanged)
		{
			Ship ship = ((mainParty.MemberRoster.TotalManCount <= 1 || !isCurrentlyAtSea) ? null : mainParty.Ships.MinBy((Ship x) => x.HitPoints));
			for (int num = mainParty.Ships.Count - 1; num >= 0; num--)
			{
				if (mainParty.Ships[num] != ship)
				{
					ChangeShipOwnerAction.ApplyByTransferring(PartyBase.MainParty, mainParty.Ships[num]);
				}
			}
		}
		if (mainParty.IsTransitionInProgress)
		{
			mainParty.CancelNavigationTransition();
		}
		if (MobileParty.MainParty.Ships.Count > 0 && position.IsValid() && !MobileParty.MainParty.Anchor.IsValid && !MobileParty.MainParty.IsCurrentlyAtSea)
		{
			MobileParty.MainParty.Anchor.SetPosition(position);
			MobileParty.MainParty.Anchor.SetLastUsedDisembarkPosition(lastUsedDisembarkPosition);
		}
		if (mainParty != MobileParty.MainParty && mainParty.IsActive)
		{
			if (mainParty.MemberRoster.TotalManCount == 0)
			{
				DestroyPartyAction.Apply(null, mainParty);
			}
			else
			{
				mainParty.LordPartyComponent.ChangePartyOwner(Hero.MainHero);
			}
		}
		_ = Hero.MainHero.IsPrisoner;
		if (hero.IsPrisoner)
		{
			PlayerCaptivity.OnPlayerCharacterChanged();
		}
		CampaignEventDispatcher.Instance.OnPlayerCharacterChanged(mainHero, hero, MobileParty.MainParty, isMainPartyChanged);
		PartyBase.MainParty.SetVisualAsDirty();
		mainParty.Party.SetVisualAsDirty();
		Campaign.Current.MainHeroIllDays = -1;
		for (int num2 = MobileParty.MainParty.Ships.Count - 1; num2 >= 0; num2--)
		{
			MobileParty.MainParty.Ships[num2].OnPlayerCharacterChanged();
		}
	}
}
