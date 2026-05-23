using System;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000093 RID: 147
	[SaveableInterface(22001)]
	public interface IFaction
	{
		// Token: 0x170004E3 RID: 1251
		// (get) Token: 0x0600126E RID: 4718
		TextObject Name { get; }

		// Token: 0x170004E4 RID: 1252
		// (get) Token: 0x0600126F RID: 4719
		string StringId { get; }

		// Token: 0x170004E5 RID: 1253
		// (get) Token: 0x06001270 RID: 4720
		MBGUID Id { get; }

		// Token: 0x170004E6 RID: 1254
		// (get) Token: 0x06001271 RID: 4721
		TextObject InformalName { get; }

		// Token: 0x170004E7 RID: 1255
		// (get) Token: 0x06001272 RID: 4722
		string EncyclopediaLink { get; }

		// Token: 0x170004E8 RID: 1256
		// (get) Token: 0x06001273 RID: 4723
		TextObject EncyclopediaLinkWithName { get; }

		// Token: 0x170004E9 RID: 1257
		// (get) Token: 0x06001274 RID: 4724
		TextObject EncyclopediaText { get; }

		// Token: 0x170004EA RID: 1258
		// (get) Token: 0x06001275 RID: 4725
		CultureObject Culture { get; }

		// Token: 0x170004EB RID: 1259
		// (get) Token: 0x06001276 RID: 4726
		Settlement InitialHomeSettlement { get; }

		// Token: 0x170004EC RID: 1260
		// (get) Token: 0x06001277 RID: 4727
		uint Color { get; }

		// Token: 0x170004ED RID: 1261
		// (get) Token: 0x06001278 RID: 4728
		uint Color2 { get; }

		// Token: 0x170004EE RID: 1262
		// (get) Token: 0x06001279 RID: 4729
		CharacterObject BasicTroop { get; }

		// Token: 0x170004EF RID: 1263
		// (get) Token: 0x0600127A RID: 4730
		Hero Leader { get; }

		// Token: 0x170004F0 RID: 1264
		// (get) Token: 0x0600127B RID: 4731
		Banner Banner { get; }

		// Token: 0x170004F1 RID: 1265
		// (get) Token: 0x0600127C RID: 4732
		MBReadOnlyList<Settlement> Settlements { get; }

		// Token: 0x170004F2 RID: 1266
		// (get) Token: 0x0600127D RID: 4733
		MBReadOnlyList<Town> Fiefs { get; }

		// Token: 0x170004F3 RID: 1267
		// (get) Token: 0x0600127E RID: 4734
		MBReadOnlyList<Hero> AliveLords { get; }

		// Token: 0x170004F4 RID: 1268
		// (get) Token: 0x0600127F RID: 4735
		MBReadOnlyList<Hero> DeadLords { get; }

		// Token: 0x170004F5 RID: 1269
		// (get) Token: 0x06001280 RID: 4736
		MBReadOnlyList<Hero> Heroes { get; }

		// Token: 0x170004F6 RID: 1270
		// (get) Token: 0x06001281 RID: 4737
		MBReadOnlyList<WarPartyComponent> WarPartyComponents { get; }

		// Token: 0x170004F7 RID: 1271
		// (get) Token: 0x06001282 RID: 4738
		bool IsBanditFaction { get; }

		// Token: 0x170004F8 RID: 1272
		// (get) Token: 0x06001283 RID: 4739
		bool IsMinorFaction { get; }

		// Token: 0x170004F9 RID: 1273
		// (get) Token: 0x06001284 RID: 4740
		bool IsKingdomFaction { get; }

		// Token: 0x170004FA RID: 1274
		// (get) Token: 0x06001285 RID: 4741
		bool IsRebelClan { get; }

		// Token: 0x170004FB RID: 1275
		// (get) Token: 0x06001286 RID: 4742
		bool IsClan { get; }

		// Token: 0x170004FC RID: 1276
		// (get) Token: 0x06001287 RID: 4743
		bool IsOutlaw { get; }

		// Token: 0x170004FD RID: 1277
		// (get) Token: 0x06001288 RID: 4744
		bool IsMapFaction { get; }

		// Token: 0x170004FE RID: 1278
		// (get) Token: 0x06001289 RID: 4745
		bool HasNavalNavigationCapability { get; }

		// Token: 0x170004FF RID: 1279
		// (get) Token: 0x0600128A RID: 4746
		IFaction MapFaction { get; }

		// Token: 0x17000500 RID: 1280
		// (get) Token: 0x0600128B RID: 4747
		float CurrentTotalStrength { get; }

		// Token: 0x17000501 RID: 1281
		// (get) Token: 0x0600128C RID: 4748
		Settlement FactionMidSettlement { get; }

		// Token: 0x17000502 RID: 1282
		// (get) Token: 0x0600128D RID: 4749
		float DistanceToClosestNonAllyFortification { get; }

		// Token: 0x0600128E RID: 4750
		bool IsAtWarWith(IFaction other);

		// Token: 0x0600128F RID: 4751
		StanceLink GetStanceWith(IFaction other);

		// Token: 0x17000503 RID: 1283
		// (get) Token: 0x06001290 RID: 4752
		MBReadOnlyList<IFaction> FactionsAtWarWith { get; }

		// Token: 0x06001291 RID: 4753
		void UpdateFactionsAtWarWith();

		// Token: 0x17000504 RID: 1284
		// (get) Token: 0x06001292 RID: 4754
		// (set) Token: 0x06001293 RID: 4755
		int TributeWallet { get; set; }

		// Token: 0x17000505 RID: 1285
		// (get) Token: 0x06001294 RID: 4756
		// (set) Token: 0x06001295 RID: 4757
		float MainHeroCrimeRating { get; set; }

		// Token: 0x17000506 RID: 1286
		// (get) Token: 0x06001296 RID: 4758
		float DailyCrimeRatingChange { get; }

		// Token: 0x17000507 RID: 1287
		// (get) Token: 0x06001297 RID: 4759
		float Aggressiveness { get; }

		// Token: 0x17000508 RID: 1288
		// (get) Token: 0x06001298 RID: 4760
		bool IsEliminated { get; }

		// Token: 0x17000509 RID: 1289
		// (get) Token: 0x06001299 RID: 4761
		ExplainedNumber DailyCrimeRatingChangeExplained { get; }

		// Token: 0x1700050A RID: 1290
		// (get) Token: 0x0600129A RID: 4762
		// (set) Token: 0x0600129B RID: 4763
		CampaignTime NotAttackableByPlayerUntilTime { get; set; }
	}
}
