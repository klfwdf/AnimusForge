using System;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x0200049C RID: 1180
	public class ChangePlayerCharacterAction
	{
		// Token: 0x06004971 RID: 18801 RVA: 0x00171C88 File Offset: 0x0016FE88
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
			bool flag;
			Campaign.Current.OnPlayerCharacterChanged(out flag);
			if (mainParty.Ships.Count > 0 && flag)
			{
				Ship ship;
				if (mainParty.MemberRoster.TotalManCount > 1 && isCurrentlyAtSea)
				{
					ship = mainParty.Ships.MinBy((Ship x) => x.HitPoints);
				}
				else
				{
					ship = null;
				}
				Ship ship2 = ship;
				for (int i = mainParty.Ships.Count - 1; i >= 0; i--)
				{
					if (mainParty.Ships[i] != ship2)
					{
						ChangeShipOwnerAction.ApplyByTransferring(PartyBase.MainParty, mainParty.Ships[i]);
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
			bool isPrisoner = Hero.MainHero.IsPrisoner;
			if (hero.IsPrisoner)
			{
				PlayerCaptivity.OnPlayerCharacterChanged();
			}
			CampaignEventDispatcher.Instance.OnPlayerCharacterChanged(mainHero, hero, MobileParty.MainParty, flag);
			PartyBase.MainParty.SetVisualAsDirty();
			mainParty.Party.SetVisualAsDirty();
			Campaign.Current.MainHeroIllDays = -1;
		}
	}
}
