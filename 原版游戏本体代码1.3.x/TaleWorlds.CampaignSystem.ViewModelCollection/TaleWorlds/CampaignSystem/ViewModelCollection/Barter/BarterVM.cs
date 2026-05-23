using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Helpers;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Barter
{
	// Token: 0x0200015A RID: 346
	public class BarterVM : ViewModel
	{
		// Token: 0x06002075 RID: 8309 RVA: 0x00075ED8 File Offset: 0x000740D8
		public BarterVM(BarterData args)
		{
			this._barterData = args;
			if (this._barterData.OtherHero == Hero.MainHero)
			{
				this._otherParty = this._barterData.OffererParty;
				this._otherCharacter = this._barterData.OffererHero.CharacterObject ?? CampaignUIHelper.GetVisualPartyLeader(this._otherParty);
			}
			else if (this._barterData.OtherHero != null)
			{
				this._otherCharacter = this._barterData.OtherHero.CharacterObject;
				this.LeftMaxGold = this._otherCharacter.HeroObject.Gold;
			}
			else
			{
				this._otherParty = this._barterData.OtherParty;
				this._otherCharacter = CampaignUIHelper.GetVisualPartyLeader(this._otherParty);
				this.LeftMaxGold = this._otherParty.MobileParty.PartyTradeGold;
			}
			this._barter = Campaign.Current.BarterManager;
			this._isPlayerOfferer = this._barterData.OffererHero == Hero.MainHero;
			this.AutoBalanceHint = new HintViewModel();
			this.LeftFiefList = new MBBindingList<BarterItemVM>();
			this.RightFiefList = new MBBindingList<BarterItemVM>();
			this.LeftPrisonerList = new MBBindingList<BarterItemVM>();
			this.RightPrisonerList = new MBBindingList<BarterItemVM>();
			this.LeftItemList = new MBBindingList<BarterItemVM>();
			this.RightItemList = new MBBindingList<BarterItemVM>();
			this.LeftOtherList = new MBBindingList<BarterItemVM>();
			this.RightOtherList = new MBBindingList<BarterItemVM>();
			this.LeftDiplomaticList = new MBBindingList<BarterItemVM>();
			this.RightDiplomaticList = new MBBindingList<BarterItemVM>();
			this.LeftGoldList = new MBBindingList<BarterItemVM>();
			this.RightGoldList = new MBBindingList<BarterItemVM>();
			this._leftList = new Dictionary<BarterGroup, MBBindingList<BarterItemVM>>();
			this._rightList = new Dictionary<BarterGroup, MBBindingList<BarterItemVM>>();
			this._barterList = new List<Dictionary<BarterGroup, MBBindingList<BarterItemVM>>>();
			this._offerList = new List<MBBindingList<BarterItemVM>>();
			this.LeftOfferList = new MBBindingList<BarterItemVM>();
			this.RightOfferList = new MBBindingList<BarterItemVM>();
			this.InitBarterList(this._barterData);
			this.OnInitialized();
			this.RightMaxGold = Hero.MainHero.Gold;
			this.LeftHero = new HeroVM(this._otherCharacter.HeroObject, false);
			this.RightHero = new HeroVM(Hero.MainHero, false);
			this.SendOffer();
			this.InitializationIsOver = true;
			this.RefreshValues();
		}

		// Token: 0x06002076 RID: 8310 RVA: 0x00076114 File Offset: 0x00074314
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.InitializeStaticContent();
			this.LeftNameLbl = this._otherCharacter.Name.ToString();
			this.RightNameLbl = Hero.MainHero.Name.ToString();
			this.LeftFiefList.ApplyActionOnAllItems(delegate(BarterItemVM x)
			{
				x.RefreshValues();
			});
			this.RightFiefList.ApplyActionOnAllItems(delegate(BarterItemVM x)
			{
				x.RefreshValues();
			});
			this.LeftPrisonerList.ApplyActionOnAllItems(delegate(BarterItemVM x)
			{
				x.RefreshValues();
			});
			this.RightPrisonerList.ApplyActionOnAllItems(delegate(BarterItemVM x)
			{
				x.RefreshValues();
			});
			this.LeftItemList.ApplyActionOnAllItems(delegate(BarterItemVM x)
			{
				x.RefreshValues();
			});
			this.RightItemList.ApplyActionOnAllItems(delegate(BarterItemVM x)
			{
				x.RefreshValues();
			});
			this.LeftOtherList.ApplyActionOnAllItems(delegate(BarterItemVM x)
			{
				x.RefreshValues();
			});
			this.RightOtherList.ApplyActionOnAllItems(delegate(BarterItemVM x)
			{
				x.RefreshValues();
			});
			this.LeftDiplomaticList.ApplyActionOnAllItems(delegate(BarterItemVM x)
			{
				x.RefreshValues();
			});
			this.RightDiplomaticList.ApplyActionOnAllItems(delegate(BarterItemVM x)
			{
				x.RefreshValues();
			});
			this.LeftGoldList.ApplyActionOnAllItems(delegate(BarterItemVM x)
			{
				x.RefreshValues();
			});
			this.RightGoldList.ApplyActionOnAllItems(delegate(BarterItemVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x06002077 RID: 8311 RVA: 0x00076350 File Offset: 0x00074550
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.DoneInputKey.OnFinalize();
			this.CancelInputKey.OnFinalize();
			this.ResetInputKey.OnFinalize();
		}

		// Token: 0x06002078 RID: 8312 RVA: 0x0007637C File Offset: 0x0007457C
		private void InitBarterList(BarterData args)
		{
			this._leftList.Add(args.GetBarterGroup<FiefBarterGroup>(), this.LeftFiefList);
			this._leftList.Add(args.GetBarterGroup<PrisonerBarterGroup>(), this.LeftPrisonerList);
			this._leftList.Add(args.GetBarterGroup<ItemBarterGroup>(), this.LeftItemList);
			this._leftList.Add(args.GetBarterGroup<OtherBarterGroup>(), this.LeftOtherList);
			this._leftList.Add(args.GetBarterGroup<GoldBarterGroup>(), this.LeftGoldList);
			this._rightList.Add(args.GetBarterGroup<FiefBarterGroup>(), this.RightFiefList);
			this._rightList.Add(args.GetBarterGroup<PrisonerBarterGroup>(), this.RightPrisonerList);
			this._rightList.Add(args.GetBarterGroup<ItemBarterGroup>(), this.RightItemList);
			this._rightList.Add(args.GetBarterGroup<OtherBarterGroup>(), this.RightOtherList);
			this._rightList.Add(args.GetBarterGroup<GoldBarterGroup>(), this.RightGoldList);
			this._barterList.Add(this._leftList);
			this._barterList.Add(this._rightList);
			this._offerList.Add(this.LeftOfferList);
			this._offerList.Add(this.RightOfferList);
			if (this._barterData.ContextInitializer != null)
			{
				foreach (Barterable barterable in this._barterData.GetBarterables())
				{
					if (barterable.IsContextDependent && this._barterData.ContextInitializer(barterable, this._barterData, null))
					{
						this.ChangeBarterableIsOffered(barterable, true);
					}
				}
			}
			foreach (Barterable barterable2 in args.GetBarterables())
			{
				if (!barterable2.IsOffered && !barterable2.IsContextDependent)
				{
					this._barterList[(barterable2.OriginalOwner == Hero.MainHero) ? 1 : 0][barterable2.Group].Add(new BarterItemVM(barterable2, new BarterItemVM.BarterTransferEventDelegate(this.TransferItem), new Action(this.OnOfferedAmountChange), false));
				}
				else
				{
					BarterItemVM barterItemVM = new BarterItemVM(barterable2, new BarterItemVM.BarterTransferEventDelegate(this.TransferItem), new Action(this.OnOfferedAmountChange), barterable2.IsContextDependent);
					this._offerList[(barterable2.OriginalOwner == Hero.MainHero) ? 1 : 0].Add(barterItemVM);
					this.RefreshCompatibility(barterItemVM, true);
				}
			}
			this._barterData.GetBarterables().Find((Barterable t) => t.Group.GetType() == typeof(GoldBarterGroup) && t.OriginalOwner == Hero.MainHero);
			this._barterData.GetBarterables().Find((Barterable t) => (t.Group.GetType() == typeof(GoldBarterGroup) && this._barterData.OffererHero == Hero.MainHero && t.OriginalOwner == this._barterData.OtherHero) || (this._barterData.OtherHero == Hero.MainHero && t.OriginalOwner == this._barterData.OffererHero));
			this.RefreshOfferLabel();
		}

		// Token: 0x06002079 RID: 8313 RVA: 0x00076670 File Offset: 0x00074870
		private void ChangeBarterableIsOffered(Barterable barterable, bool newState)
		{
			if (barterable.IsOffered != newState)
			{
				barterable.SetIsOffered(newState);
				this.OnTransferItem(barterable, true);
				foreach (Barterable barter in barterable.LinkedBarterables)
				{
					this.OnTransferItem(barter, true);
				}
			}
		}

		// Token: 0x0600207A RID: 8314 RVA: 0x000766DC File Offset: 0x000748DC
		public void OnInitialized()
		{
			BarterManager barterManager = Campaign.Current.BarterManager;
			barterManager.Closed = (BarterManager.BarterCloseEventDelegate)Delegate.Combine(barterManager.Closed, new BarterManager.BarterCloseEventDelegate(this.OnClosed));
		}

		// Token: 0x0600207B RID: 8315 RVA: 0x00076709 File Offset: 0x00074909
		private void OnClosed()
		{
			BarterManager barterManager = Campaign.Current.BarterManager;
			barterManager.Closed = (BarterManager.BarterCloseEventDelegate)Delegate.Remove(barterManager.Closed, new BarterManager.BarterCloseEventDelegate(this.OnClosed));
		}

		// Token: 0x0600207C RID: 8316 RVA: 0x00076736 File Offset: 0x00074936
		public void ExecuteTransferAllLeftFief()
		{
			this.ExecuteTransferAll(this._otherCharacter, this._barterData.GetBarterGroup<FiefBarterGroup>());
		}

		// Token: 0x0600207D RID: 8317 RVA: 0x0007674F File Offset: 0x0007494F
		public void ExecuteAutoBalance()
		{
			this.AutoBalanceAdd();
			this.AutoBalanceRemove();
			this.AutoBalanceAdd();
		}

		// Token: 0x0600207E RID: 8318 RVA: 0x00076764 File Offset: 0x00074964
		private void AutoBalanceRemove()
		{
			if ((int)Campaign.Current.BarterManager.GetOfferValue(this._otherCharacter.HeroObject, this._otherParty, this._barterData.OffererParty, this._barterData.GetOfferedBarterables()) > 0)
			{
				List<ValueTuple<Barterable, int>> newBarterables = BarterHelper.GetAutoBalanceBarterablesToRemove(this._barterData, this.OtherFaction, Clan.PlayerClan.MapFaction, Hero.MainHero).ToList<ValueTuple<Barterable, int>>();
				List<ValueTuple<BarterItemVM, int>> list = new List<ValueTuple<BarterItemVM, int>>();
				this.GetBarterItems(this.RightGoldList, newBarterables, list);
				this.GetBarterItems(this.RightItemList, newBarterables, list);
				this.GetBarterItems(this.RightPrisonerList, newBarterables, list);
				this.GetBarterItems(this.RightFiefList, newBarterables, list);
				foreach (ValueTuple<BarterItemVM, int> valueTuple in list)
				{
					BarterItemVM item = valueTuple.Item1;
					int item2 = valueTuple.Item2;
					this.OfferItemRemove(item, item2);
				}
			}
		}

		// Token: 0x0600207F RID: 8319 RVA: 0x00076864 File Offset: 0x00074A64
		private void AutoBalanceAdd()
		{
			if ((int)Campaign.Current.BarterManager.GetOfferValue(this._otherCharacter.HeroObject, this._otherParty, this._barterData.OffererParty, this._barterData.GetOfferedBarterables()) < 0)
			{
				List<ValueTuple<Barterable, int>> newBarterables = BarterHelper.GetAutoBalanceBarterablesAdd(this._barterData, this.OtherFaction, Clan.PlayerClan.MapFaction, Hero.MainHero, 1f).ToList<ValueTuple<Barterable, int>>();
				List<ValueTuple<BarterItemVM, int>> list = new List<ValueTuple<BarterItemVM, int>>();
				this.GetBarterItems(this.RightGoldList, newBarterables, list);
				this.GetBarterItems(this.RightItemList, newBarterables, list);
				this.GetBarterItems(this.RightPrisonerList, newBarterables, list);
				this.GetBarterItems(this.RightFiefList, newBarterables, list);
				foreach (ValueTuple<BarterItemVM, int> valueTuple in list)
				{
					BarterItemVM item = valueTuple.Item1;
					int item2 = valueTuple.Item2;
					if (item2 > 0)
					{
						this.OfferItemAdd(item, item2);
					}
				}
			}
		}

		// Token: 0x06002080 RID: 8320 RVA: 0x0007696C File Offset: 0x00074B6C
		private void GetBarterItems(MBBindingList<BarterItemVM> itemList, [TupleElementNames(new string[] { "barterable", "count" })] List<ValueTuple<Barterable, int>> newBarterables, List<ValueTuple<BarterItemVM, int>> barterItems)
		{
			foreach (BarterItemVM barterItemVM in itemList)
			{
				foreach (ValueTuple<Barterable, int> valueTuple in newBarterables)
				{
					Barterable item = valueTuple.Item1;
					int item2 = valueTuple.Item2;
					if (item == barterItemVM.Barterable)
					{
						barterItems.Add(new ValueTuple<BarterItemVM, int>(barterItemVM, item2));
					}
				}
			}
		}

		// Token: 0x06002081 RID: 8321 RVA: 0x00076A08 File Offset: 0x00074C08
		public void ExecuteTransferAllLeftItem()
		{
			this.ExecuteTransferAll(this._otherCharacter, this._barterData.GetBarterGroup<ItemBarterGroup>());
		}

		// Token: 0x06002082 RID: 8322 RVA: 0x00076A21 File Offset: 0x00074C21
		public void ExecuteTransferAllLeftPrisoner()
		{
			this.ExecuteTransferAll(this._otherCharacter, this._barterData.GetBarterGroup<PrisonerBarterGroup>());
		}

		// Token: 0x06002083 RID: 8323 RVA: 0x00076A3A File Offset: 0x00074C3A
		public void ExecuteTransferAllLeftOther()
		{
			this.ExecuteTransferAll(this._otherCharacter, this._barterData.GetBarterGroup<OtherBarterGroup>());
		}

		// Token: 0x06002084 RID: 8324 RVA: 0x00076A53 File Offset: 0x00074C53
		public void ExecuteTransferAllRightFief()
		{
			this.ExecuteTransferAll(CharacterObject.PlayerCharacter, this._barterData.GetBarterGroup<FiefBarterGroup>());
		}

		// Token: 0x06002085 RID: 8325 RVA: 0x00076A6B File Offset: 0x00074C6B
		public void ExecuteTransferAllRightItem()
		{
			this.ExecuteTransferAll(CharacterObject.PlayerCharacter, this._barterData.GetBarterGroup<ItemBarterGroup>());
		}

		// Token: 0x06002086 RID: 8326 RVA: 0x00076A83 File Offset: 0x00074C83
		public void ExecuteTransferAllRightPrisoner()
		{
			this.ExecuteTransferAll(CharacterObject.PlayerCharacter, this._barterData.GetBarterGroup<PrisonerBarterGroup>());
		}

		// Token: 0x06002087 RID: 8327 RVA: 0x00076A9B File Offset: 0x00074C9B
		public void ExecuteTransferAllRightOther()
		{
			this.ExecuteTransferAll(CharacterObject.PlayerCharacter, this._barterData.GetBarterGroup<OtherBarterGroup>());
		}

		// Token: 0x06002088 RID: 8328 RVA: 0x00076AB4 File Offset: 0x00074CB4
		private void ExecuteTransferAll(CharacterObject fromCharacter, BarterGroup barterGroup)
		{
			if (barterGroup != null)
			{
				foreach (BarterItemVM item in new List<BarterItemVM>(from barterItem in this._barterList[(fromCharacter == CharacterObject.PlayerCharacter) ? 1 : 0][barterGroup]
					where !barterItem.Barterable.IsOffered
					select barterItem))
				{
					this.TransferItem(item, true);
				}
				foreach (BarterItemVM barterItemVM in this._barterList[(fromCharacter == CharacterObject.PlayerCharacter) ? 1 : 0][barterGroup])
				{
					barterItemVM.CurrentOfferedAmount = barterItemVM.TotalItemCount;
				}
			}
		}

		// Token: 0x06002089 RID: 8329 RVA: 0x00076BA4 File Offset: 0x00074DA4
		private void SendOffer()
		{
			this.IsOfferDisabled = !this.IsCurrentOfferAcceptable() || (this.LeftOfferList.Count == 0 && this.RightOfferList.Count == 0);
			this.RefreshResultBar();
		}

		// Token: 0x0600208A RID: 8330 RVA: 0x00076BDB File Offset: 0x00074DDB
		private bool IsCurrentOfferAcceptable()
		{
			return Campaign.Current.BarterManager.IsOfferAcceptable(this._barterData, this._otherCharacter.HeroObject, this._otherParty);
		}

		// Token: 0x17000B0E RID: 2830
		// (get) Token: 0x0600208B RID: 8331 RVA: 0x00076C04 File Offset: 0x00074E04
		private IFaction OtherFaction
		{
			get
			{
				if (!this._otherCharacter.IsHero)
				{
					return this._otherParty.MapFaction;
				}
				return this._otherCharacter.HeroObject.Clan;
			}
		}

		// Token: 0x0600208C RID: 8332 RVA: 0x00076C3C File Offset: 0x00074E3C
		private void RefreshResultBar()
		{
			long num = 0L;
			long num2 = 0L;
			IFaction otherFaction = this.OtherFaction;
			foreach (BarterItemVM barterItemVM in this.LeftOfferList)
			{
				int valueForFaction = barterItemVM.Barterable.GetValueForFaction(otherFaction);
				if (valueForFaction < 0)
				{
					num2 += (long)valueForFaction;
				}
				else
				{
					num += (long)valueForFaction;
				}
			}
			foreach (BarterItemVM barterItemVM2 in this.RightOfferList)
			{
				int valueForFaction2 = barterItemVM2.Barterable.GetValueForFaction(otherFaction);
				if (valueForFaction2 < 0)
				{
					num2 += (long)valueForFaction2;
				}
				else
				{
					num += (long)valueForFaction2;
				}
			}
			double num3 = (double)MathF.Max(0f, (float)num);
			double num4 = (double)MathF.Max(1f, (float)(-(float)num2));
			this.ResultBarOtherPercentage = MathF.Round(num3 / num4 * 100.0);
		}

		// Token: 0x0600208D RID: 8333 RVA: 0x00076D44 File Offset: 0x00074F44
		private void ExecuteTransferAllGoldLeft()
		{
		}

		// Token: 0x0600208E RID: 8334 RVA: 0x00076D46 File Offset: 0x00074F46
		private void ExecuteTransferAllGoldRight()
		{
		}

		// Token: 0x0600208F RID: 8335 RVA: 0x00076D48 File Offset: 0x00074F48
		public void ExecuteOffer()
		{
			Campaign.Current.BarterManager.ApplyAndFinalizePlayerBarter(this._barterData.OffererHero, this._barterData.OtherHero, this._barterData);
		}

		// Token: 0x06002090 RID: 8336 RVA: 0x00076D75 File Offset: 0x00074F75
		public void ExecuteCancel()
		{
			Campaign.Current.BarterManager.CancelAndFinalizePlayerBarter(this._barterData.OffererHero, this._barterData.OtherHero, this._barterData);
		}

		// Token: 0x06002091 RID: 8337 RVA: 0x00076DA4 File Offset: 0x00074FA4
		public void ExecuteReset()
		{
			this.LeftFiefList.Clear();
			this.RightFiefList.Clear();
			this.LeftPrisonerList.Clear();
			this.RightPrisonerList.Clear();
			this.LeftItemList.Clear();
			this.RightItemList.Clear();
			this.LeftOtherList.Clear();
			this.RightOtherList.Clear();
			this.LeftDiplomaticList.Clear();
			this.RightDiplomaticList.Clear();
			this.LeftGoldList.Clear();
			this.RightGoldList.Clear();
			this._leftList.Clear();
			this._rightList.Clear();
			this._barterList.Clear();
			this.LeftOfferList.Clear();
			this.RightOfferList.Clear();
			this._offerList.Clear();
			foreach (Barterable barterable in this._barterData.GetBarterables())
			{
				if (barterable.IsOffered)
				{
					this.ChangeBarterableIsOffered(barterable, false);
				}
			}
			this.InitBarterList(this._barterData);
			this.SendOffer();
			this.InitializationIsOver = true;
			this.RefreshValues();
		}

		// Token: 0x06002092 RID: 8338 RVA: 0x00076EEC File Offset: 0x000750EC
		private void TransferItem(BarterItemVM item, bool offerAll)
		{
			this.ChangeBarterableIsOffered(item.Barterable, !item.IsOffered);
			if (offerAll)
			{
				item.CurrentOfferedAmount = item.TotalItemCount;
			}
			this.SendOffer();
			this.RefreshOfferLabel();
			this.RefreshCompatibility(item, item.IsOffered);
		}

		// Token: 0x06002093 RID: 8339 RVA: 0x00076F2C File Offset: 0x0007512C
		private void OfferItemAdd(BarterItemVM barterItemVM, int count)
		{
			this.ChangeBarterableIsOffered(barterItemVM.Barterable, true);
			barterItemVM.CurrentOfferedAmount = (int)MathF.Clamp((float)(barterItemVM.CurrentOfferedAmount + count), 0f, (float)barterItemVM.TotalItemCount);
			this.SendOffer();
			this.RefreshOfferLabel();
			this.RefreshCompatibility(barterItemVM, barterItemVM.IsOffered);
		}

		// Token: 0x06002094 RID: 8340 RVA: 0x00076F80 File Offset: 0x00075180
		private void OfferItemRemove(BarterItemVM barterItemVM, int count)
		{
			if (barterItemVM.CurrentOfferedAmount <= count)
			{
				this.ChangeBarterableIsOffered(barterItemVM.Barterable, false);
			}
			else
			{
				barterItemVM.CurrentOfferedAmount = (int)MathF.Clamp((float)(barterItemVM.CurrentOfferedAmount - count), 0f, (float)barterItemVM.TotalItemCount);
			}
			this.SendOffer();
			this.RefreshOfferLabel();
			this.RefreshCompatibility(barterItemVM, barterItemVM.IsOffered);
		}

		// Token: 0x06002095 RID: 8341 RVA: 0x00076FE0 File Offset: 0x000751E0
		public void OnTransferItem(Barterable barter, bool isTransferrable)
		{
			int index = ((barter.OriginalOwner == Hero.MainHero) ? 1 : 0);
			if (!this._barterList.IsEmpty<Dictionary<BarterGroup, MBBindingList<BarterItemVM>>>())
			{
				BarterItemVM barterItemVM = this._barterList[index][barter.Group].FirstOrDefault((BarterItemVM i) => i.Barterable == barter);
				if (barterItemVM == null && !this._offerList.IsEmpty<MBBindingList<BarterItemVM>>())
				{
					barterItemVM = this._offerList[index].FirstOrDefault((BarterItemVM i) => i.Barterable == barter);
				}
				if (barterItemVM != null)
				{
					barterItemVM.IsOffered = barter.IsOffered;
					barterItemVM.IsItemTransferrable = isTransferrable;
					if (barterItemVM.IsOffered)
					{
						this._offerList[index].Add(barterItemVM);
						if (barterItemVM.IsMultiple)
						{
							barterItemVM.CurrentOfferedAmount = 1;
							return;
						}
					}
					else
					{
						this._offerList[index].Remove(barterItemVM);
						if (barterItemVM.IsMultiple)
						{
							barterItemVM.CurrentOfferedAmount = 1;
						}
					}
				}
			}
		}

		// Token: 0x06002096 RID: 8342 RVA: 0x000770E4 File Offset: 0x000752E4
		private void OnOfferedAmountChange()
		{
			this.SendOffer();
		}

		// Token: 0x06002097 RID: 8343 RVA: 0x000770EC File Offset: 0x000752EC
		private void RefreshOfferLabel()
		{
			if (this.LeftOfferList.Any((BarterItemVM x) => x.Barterable.GetValueForFaction(this.OtherFaction) < 0) || this.RightOfferList.Any((BarterItemVM x) => x.Barterable.GetValueForFaction(this.OtherFaction) < 0))
			{
				this.OfferLbl = GameTexts.FindText("str_offer", null).ToString();
				return;
			}
			this.OfferLbl = GameTexts.FindText("str_gift", null).ToString();
		}

		// Token: 0x06002098 RID: 8344 RVA: 0x00077158 File Offset: 0x00075358
		private void RefreshCompatibility(BarterItemVM lastTransferredItem, bool gotOffered)
		{
			Action<BarterItemVM> <>9__0;
			foreach (MBBindingList<BarterItemVM> source in this._leftList.Values)
			{
				List<BarterItemVM> list = source.ToList<BarterItemVM>();
				Action<BarterItemVM> action;
				if ((action = <>9__0) == null)
				{
					action = (<>9__0 = delegate(BarterItemVM b)
					{
						b.RefreshCompabilityWithItem(lastTransferredItem, gotOffered);
					});
				}
				list.ForEach(action);
			}
			Action<BarterItemVM> <>9__1;
			foreach (MBBindingList<BarterItemVM> source2 in this._rightList.Values)
			{
				List<BarterItemVM> list2 = source2.ToList<BarterItemVM>();
				Action<BarterItemVM> action2;
				if ((action2 = <>9__1) == null)
				{
					action2 = (<>9__1 = delegate(BarterItemVM b)
					{
						b.RefreshCompabilityWithItem(lastTransferredItem, gotOffered);
					});
				}
				list2.ForEach(action2);
			}
		}

		// Token: 0x17000B0F RID: 2831
		// (get) Token: 0x06002099 RID: 8345 RVA: 0x00077250 File Offset: 0x00075450
		// (set) Token: 0x0600209A RID: 8346 RVA: 0x00077258 File Offset: 0x00075458
		[DataSourceProperty]
		public string FiefLbl
		{
			get
			{
				return this._fiefLbl;
			}
			set
			{
				if (value != this._fiefLbl)
				{
					this._fiefLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "FiefLbl");
				}
			}
		}

		// Token: 0x17000B10 RID: 2832
		// (get) Token: 0x0600209B RID: 8347 RVA: 0x0007727B File Offset: 0x0007547B
		// (set) Token: 0x0600209C RID: 8348 RVA: 0x00077283 File Offset: 0x00075483
		[DataSourceProperty]
		public string PrisonerLbl
		{
			get
			{
				return this._prisonerLbl;
			}
			set
			{
				if (value != this._prisonerLbl)
				{
					this._prisonerLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "PrisonerLbl");
				}
			}
		}

		// Token: 0x17000B11 RID: 2833
		// (get) Token: 0x0600209D RID: 8349 RVA: 0x000772A6 File Offset: 0x000754A6
		// (set) Token: 0x0600209E RID: 8350 RVA: 0x000772AE File Offset: 0x000754AE
		[DataSourceProperty]
		public string ItemLbl
		{
			get
			{
				return this._itemLbl;
			}
			set
			{
				if (value != this._itemLbl)
				{
					this._itemLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "ItemLbl");
				}
			}
		}

		// Token: 0x17000B12 RID: 2834
		// (get) Token: 0x0600209F RID: 8351 RVA: 0x000772D1 File Offset: 0x000754D1
		// (set) Token: 0x060020A0 RID: 8352 RVA: 0x000772D9 File Offset: 0x000754D9
		[DataSourceProperty]
		public string OtherLbl
		{
			get
			{
				return this._otherLbl;
			}
			set
			{
				if (value != this._otherLbl)
				{
					this._otherLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "OtherLbl");
				}
			}
		}

		// Token: 0x17000B13 RID: 2835
		// (get) Token: 0x060020A1 RID: 8353 RVA: 0x000772FC File Offset: 0x000754FC
		// (set) Token: 0x060020A2 RID: 8354 RVA: 0x00077304 File Offset: 0x00075504
		[DataSourceProperty]
		public string CancelLbl
		{
			get
			{
				return this._cancelLbl;
			}
			set
			{
				if (value != this._cancelLbl)
				{
					this._cancelLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "CancelLbl");
				}
			}
		}

		// Token: 0x17000B14 RID: 2836
		// (get) Token: 0x060020A3 RID: 8355 RVA: 0x00077327 File Offset: 0x00075527
		// (set) Token: 0x060020A4 RID: 8356 RVA: 0x0007732F File Offset: 0x0007552F
		[DataSourceProperty]
		public string ResetLbl
		{
			get
			{
				return this._resetLbl;
			}
			set
			{
				if (value != this._resetLbl)
				{
					this._resetLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "ResetLbl");
				}
			}
		}

		// Token: 0x17000B15 RID: 2837
		// (get) Token: 0x060020A5 RID: 8357 RVA: 0x00077352 File Offset: 0x00075552
		// (set) Token: 0x060020A6 RID: 8358 RVA: 0x0007735A File Offset: 0x0007555A
		[DataSourceProperty]
		public string OfferLbl
		{
			get
			{
				return this._offerLbl;
			}
			set
			{
				if (value != this._offerLbl)
				{
					this._offerLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "OfferLbl");
				}
			}
		}

		// Token: 0x17000B16 RID: 2838
		// (get) Token: 0x060020A7 RID: 8359 RVA: 0x0007737D File Offset: 0x0007557D
		// (set) Token: 0x060020A8 RID: 8360 RVA: 0x00077385 File Offset: 0x00075585
		[DataSourceProperty]
		public string DiplomaticLbl
		{
			get
			{
				return this._diplomaticLbl;
			}
			set
			{
				if (value != this._diplomaticLbl)
				{
					this._diplomaticLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "DiplomaticLbl");
				}
			}
		}

		// Token: 0x17000B17 RID: 2839
		// (get) Token: 0x060020A9 RID: 8361 RVA: 0x000773A8 File Offset: 0x000755A8
		// (set) Token: 0x060020AA RID: 8362 RVA: 0x000773B0 File Offset: 0x000755B0
		[DataSourceProperty]
		public HintViewModel AutoBalanceHint
		{
			get
			{
				return this._autoBalanceHint;
			}
			set
			{
				if (value != this._autoBalanceHint)
				{
					this._autoBalanceHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "AutoBalanceHint");
				}
			}
		}

		// Token: 0x17000B18 RID: 2840
		// (get) Token: 0x060020AB RID: 8363 RVA: 0x000773CE File Offset: 0x000755CE
		// (set) Token: 0x060020AC RID: 8364 RVA: 0x000773D6 File Offset: 0x000755D6
		[DataSourceProperty]
		public HeroVM LeftHero
		{
			get
			{
				return this._leftHero;
			}
			set
			{
				if (value != this._leftHero)
				{
					this._leftHero = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "LeftHero");
				}
			}
		}

		// Token: 0x17000B19 RID: 2841
		// (get) Token: 0x060020AD RID: 8365 RVA: 0x000773F4 File Offset: 0x000755F4
		// (set) Token: 0x060020AE RID: 8366 RVA: 0x000773FC File Offset: 0x000755FC
		[DataSourceProperty]
		public HeroVM RightHero
		{
			get
			{
				return this._rightHero;
			}
			set
			{
				if (value != this._rightHero)
				{
					this._rightHero = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "RightHero");
				}
			}
		}

		// Token: 0x17000B1A RID: 2842
		// (get) Token: 0x060020AF RID: 8367 RVA: 0x0007741A File Offset: 0x0007561A
		// (set) Token: 0x060020B0 RID: 8368 RVA: 0x00077422 File Offset: 0x00075622
		[DataSourceProperty]
		public bool IsOfferDisabled
		{
			get
			{
				return this._isOfferDisabled;
			}
			set
			{
				if (value != this._isOfferDisabled)
				{
					this._isOfferDisabled = value;
					base.OnPropertyChangedWithValue(value, "IsOfferDisabled");
				}
			}
		}

		// Token: 0x17000B1B RID: 2843
		// (get) Token: 0x060020B1 RID: 8369 RVA: 0x00077440 File Offset: 0x00075640
		// (set) Token: 0x060020B2 RID: 8370 RVA: 0x00077448 File Offset: 0x00075648
		[DataSourceProperty]
		public int LeftMaxGold
		{
			get
			{
				return this._leftMaxGold;
			}
			set
			{
				if (value != this._leftMaxGold)
				{
					this._leftMaxGold = value;
					base.OnPropertyChangedWithValue(value, "LeftMaxGold");
				}
			}
		}

		// Token: 0x17000B1C RID: 2844
		// (get) Token: 0x060020B3 RID: 8371 RVA: 0x00077466 File Offset: 0x00075666
		// (set) Token: 0x060020B4 RID: 8372 RVA: 0x0007746E File Offset: 0x0007566E
		[DataSourceProperty]
		public int RightMaxGold
		{
			get
			{
				return this._rightMaxGold;
			}
			set
			{
				if (value != this._rightMaxGold)
				{
					this._rightMaxGold = value;
					base.OnPropertyChangedWithValue(value, "RightMaxGold");
				}
			}
		}

		// Token: 0x17000B1D RID: 2845
		// (get) Token: 0x060020B5 RID: 8373 RVA: 0x0007748C File Offset: 0x0007568C
		// (set) Token: 0x060020B6 RID: 8374 RVA: 0x00077494 File Offset: 0x00075694
		[DataSourceProperty]
		public string LeftNameLbl
		{
			get
			{
				return this._leftNameLbl;
			}
			set
			{
				if (value != this._leftNameLbl)
				{
					this._leftNameLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "LeftNameLbl");
				}
			}
		}

		// Token: 0x17000B1E RID: 2846
		// (get) Token: 0x060020B7 RID: 8375 RVA: 0x000774B7 File Offset: 0x000756B7
		// (set) Token: 0x060020B8 RID: 8376 RVA: 0x000774BF File Offset: 0x000756BF
		[DataSourceProperty]
		public string RightNameLbl
		{
			get
			{
				return this._rightNameLbl;
			}
			set
			{
				if (value != this._rightNameLbl)
				{
					this._rightNameLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "RightNameLbl");
				}
			}
		}

		// Token: 0x17000B1F RID: 2847
		// (get) Token: 0x060020B9 RID: 8377 RVA: 0x000774E2 File Offset: 0x000756E2
		// (set) Token: 0x060020BA RID: 8378 RVA: 0x000774EA File Offset: 0x000756EA
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> LeftFiefList
		{
			get
			{
				return this._leftFiefList;
			}
			set
			{
				if (value != this._leftFiefList)
				{
					this._leftFiefList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "LeftFiefList");
				}
			}
		}

		// Token: 0x17000B20 RID: 2848
		// (get) Token: 0x060020BB RID: 8379 RVA: 0x00077508 File Offset: 0x00075708
		// (set) Token: 0x060020BC RID: 8380 RVA: 0x00077510 File Offset: 0x00075710
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> RightFiefList
		{
			get
			{
				return this._rightFiefList;
			}
			set
			{
				if (value != this._rightFiefList)
				{
					this._rightFiefList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "RightFiefList");
				}
			}
		}

		// Token: 0x17000B21 RID: 2849
		// (get) Token: 0x060020BD RID: 8381 RVA: 0x0007752E File Offset: 0x0007572E
		// (set) Token: 0x060020BE RID: 8382 RVA: 0x00077536 File Offset: 0x00075736
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> LeftPrisonerList
		{
			get
			{
				return this._leftPrisonerList;
			}
			set
			{
				if (value != this._leftPrisonerList)
				{
					this._leftPrisonerList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "LeftPrisonerList");
				}
			}
		}

		// Token: 0x17000B22 RID: 2850
		// (get) Token: 0x060020BF RID: 8383 RVA: 0x00077554 File Offset: 0x00075754
		// (set) Token: 0x060020C0 RID: 8384 RVA: 0x0007755C File Offset: 0x0007575C
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> RightPrisonerList
		{
			get
			{
				return this._rightPrisonerList;
			}
			set
			{
				if (value != this._rightPrisonerList)
				{
					this._rightPrisonerList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "RightPrisonerList");
				}
			}
		}

		// Token: 0x17000B23 RID: 2851
		// (get) Token: 0x060020C1 RID: 8385 RVA: 0x0007757A File Offset: 0x0007577A
		// (set) Token: 0x060020C2 RID: 8386 RVA: 0x00077582 File Offset: 0x00075782
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> LeftItemList
		{
			get
			{
				return this._leftItemList;
			}
			set
			{
				if (value != this._leftItemList)
				{
					this._leftItemList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "LeftItemList");
				}
			}
		}

		// Token: 0x17000B24 RID: 2852
		// (get) Token: 0x060020C3 RID: 8387 RVA: 0x000775A0 File Offset: 0x000757A0
		// (set) Token: 0x060020C4 RID: 8388 RVA: 0x000775A8 File Offset: 0x000757A8
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> RightItemList
		{
			get
			{
				return this._rightItemList;
			}
			set
			{
				if (value != this._rightItemList)
				{
					this._rightItemList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "RightItemList");
				}
			}
		}

		// Token: 0x17000B25 RID: 2853
		// (get) Token: 0x060020C5 RID: 8389 RVA: 0x000775C6 File Offset: 0x000757C6
		// (set) Token: 0x060020C6 RID: 8390 RVA: 0x000775CE File Offset: 0x000757CE
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> LeftOtherList
		{
			get
			{
				return this._leftOtherList;
			}
			set
			{
				if (value != this._leftOtherList)
				{
					this._leftOtherList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "LeftOtherList");
				}
			}
		}

		// Token: 0x17000B26 RID: 2854
		// (get) Token: 0x060020C7 RID: 8391 RVA: 0x000775EC File Offset: 0x000757EC
		// (set) Token: 0x060020C8 RID: 8392 RVA: 0x000775F4 File Offset: 0x000757F4
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> RightOtherList
		{
			get
			{
				return this._rightOtherList;
			}
			set
			{
				if (value != this._rightOtherList)
				{
					this._rightOtherList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "RightOtherList");
				}
			}
		}

		// Token: 0x17000B27 RID: 2855
		// (get) Token: 0x060020C9 RID: 8393 RVA: 0x00077612 File Offset: 0x00075812
		// (set) Token: 0x060020CA RID: 8394 RVA: 0x0007761A File Offset: 0x0007581A
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> LeftDiplomaticList
		{
			get
			{
				return this._leftDiplomaticList;
			}
			set
			{
				if (value != this._leftDiplomaticList)
				{
					this._leftDiplomaticList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "LeftDiplomaticList");
				}
			}
		}

		// Token: 0x17000B28 RID: 2856
		// (get) Token: 0x060020CB RID: 8395 RVA: 0x00077638 File Offset: 0x00075838
		// (set) Token: 0x060020CC RID: 8396 RVA: 0x00077640 File Offset: 0x00075840
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> RightDiplomaticList
		{
			get
			{
				return this._rightDiplomaticList;
			}
			set
			{
				if (value != this._rightDiplomaticList)
				{
					this._rightDiplomaticList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "RightDiplomaticList");
				}
			}
		}

		// Token: 0x17000B29 RID: 2857
		// (get) Token: 0x060020CD RID: 8397 RVA: 0x0007765E File Offset: 0x0007585E
		// (set) Token: 0x060020CE RID: 8398 RVA: 0x00077666 File Offset: 0x00075866
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> LeftOfferList
		{
			get
			{
				return this._leftOfferList;
			}
			set
			{
				if (value != this._leftOfferList)
				{
					this._leftOfferList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "LeftOfferList");
				}
			}
		}

		// Token: 0x17000B2A RID: 2858
		// (get) Token: 0x060020CF RID: 8399 RVA: 0x00077684 File Offset: 0x00075884
		// (set) Token: 0x060020D0 RID: 8400 RVA: 0x0007768C File Offset: 0x0007588C
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> RightOfferList
		{
			get
			{
				return this._rightOfferList;
			}
			set
			{
				if (value != this._rightOfferList)
				{
					this._rightOfferList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "RightOfferList");
				}
			}
		}

		// Token: 0x17000B2B RID: 2859
		// (get) Token: 0x060020D1 RID: 8401 RVA: 0x000776AA File Offset: 0x000758AA
		// (set) Token: 0x060020D2 RID: 8402 RVA: 0x000776B2 File Offset: 0x000758B2
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> RightGoldList
		{
			get
			{
				return this._rightGoldList;
			}
			set
			{
				if (value != this._rightGoldList)
				{
					this._rightGoldList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "RightGoldList");
				}
			}
		}

		// Token: 0x17000B2C RID: 2860
		// (get) Token: 0x060020D3 RID: 8403 RVA: 0x000776D0 File Offset: 0x000758D0
		// (set) Token: 0x060020D4 RID: 8404 RVA: 0x000776D8 File Offset: 0x000758D8
		[DataSourceProperty]
		public MBBindingList<BarterItemVM> LeftGoldList
		{
			get
			{
				return this._leftGoldList;
			}
			set
			{
				if (value != this._leftGoldList)
				{
					this._leftGoldList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BarterItemVM>>(value, "LeftGoldList");
				}
			}
		}

		// Token: 0x17000B2D RID: 2861
		// (get) Token: 0x060020D5 RID: 8405 RVA: 0x000776F6 File Offset: 0x000758F6
		// (set) Token: 0x060020D6 RID: 8406 RVA: 0x000776FE File Offset: 0x000758FE
		[DataSourceProperty]
		public bool InitializationIsOver
		{
			get
			{
				return this._initializationIsOver;
			}
			set
			{
				this._initializationIsOver = value;
				base.OnPropertyChangedWithValue(value, "InitializationIsOver");
			}
		}

		// Token: 0x17000B2E RID: 2862
		// (get) Token: 0x060020D7 RID: 8407 RVA: 0x00077713 File Offset: 0x00075913
		// (set) Token: 0x060020D8 RID: 8408 RVA: 0x0007771B File Offset: 0x0007591B
		[DataSourceProperty]
		public int ResultBarOtherPercentage
		{
			get
			{
				return this._resultBarOtherPercentage;
			}
			set
			{
				this._resultBarOtherPercentage = value;
				base.OnPropertyChangedWithValue(value, "ResultBarOtherPercentage");
			}
		}

		// Token: 0x17000B2F RID: 2863
		// (get) Token: 0x060020D9 RID: 8409 RVA: 0x00077730 File Offset: 0x00075930
		// (set) Token: 0x060020DA RID: 8410 RVA: 0x00077738 File Offset: 0x00075938
		[DataSourceProperty]
		public int ResultBarOffererPercentage
		{
			get
			{
				return this._resultBarOffererPercentage;
			}
			set
			{
				this._resultBarOffererPercentage = value;
				base.OnPropertyChangedWithValue(value, "ResultBarOffererPercentage");
			}
		}

		// Token: 0x060020DB RID: 8411 RVA: 0x0007774D File Offset: 0x0007594D
		public void SetResetInputKey(HotKey hotkey)
		{
			this.ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x060020DC RID: 8412 RVA: 0x0007775C File Offset: 0x0007595C
		public void SetDoneInputKey(HotKey hotkey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x060020DD RID: 8413 RVA: 0x0007776B File Offset: 0x0007596B
		public void SetCancelInputKey(HotKey hotkey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x17000B30 RID: 2864
		// (get) Token: 0x060020DE RID: 8414 RVA: 0x0007777A File Offset: 0x0007597A
		// (set) Token: 0x060020DF RID: 8415 RVA: 0x00077782 File Offset: 0x00075982
		[DataSourceProperty]
		public InputKeyItemVM ResetInputKey
		{
			get
			{
				return this._resetInputKey;
			}
			set
			{
				if (value != this._resetInputKey)
				{
					this._resetInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "ResetInputKey");
				}
			}
		}

		// Token: 0x17000B31 RID: 2865
		// (get) Token: 0x060020E0 RID: 8416 RVA: 0x000777A0 File Offset: 0x000759A0
		// (set) Token: 0x060020E1 RID: 8417 RVA: 0x000777A8 File Offset: 0x000759A8
		[DataSourceProperty]
		public InputKeyItemVM DoneInputKey
		{
			get
			{
				return this._doneInputKey;
			}
			set
			{
				if (value != this._doneInputKey)
				{
					this._doneInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
				}
			}
		}

		// Token: 0x17000B32 RID: 2866
		// (get) Token: 0x060020E2 RID: 8418 RVA: 0x000777C6 File Offset: 0x000759C6
		// (set) Token: 0x060020E3 RID: 8419 RVA: 0x000777CE File Offset: 0x000759CE
		[DataSourceProperty]
		public InputKeyItemVM CancelInputKey
		{
			get
			{
				return this._cancelInputKey;
			}
			set
			{
				if (value != this._cancelInputKey)
				{
					this._cancelInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "CancelInputKey");
				}
			}
		}

		// Token: 0x060020E4 RID: 8420 RVA: 0x000777EC File Offset: 0x000759EC
		public void InitializeStaticContent()
		{
			this.FiefLbl = GameTexts.FindText("str_fiefs", null).ToString();
			this.PrisonerLbl = GameTexts.FindText("str_prisoner_tag_name", null).ToString();
			this.ItemLbl = GameTexts.FindText("str_item_tag_name", null).ToString();
			this.OtherLbl = GameTexts.FindText("str_other", null).ToString();
			this.CancelLbl = GameTexts.FindText("str_cancel", null).ToString();
			this.ResetLbl = GameTexts.FindText("str_reset", null).ToString();
			this.DiplomaticLbl = GameTexts.FindText("str_diplomatic_group", null).ToString();
			this.AutoBalanceHint.HintText = new TextObject("{=Ve5jkJqf}Auto Offer", null);
		}

		// Token: 0x04000F1C RID: 3868
		private readonly List<Dictionary<BarterGroup, MBBindingList<BarterItemVM>>> _barterList;

		// Token: 0x04000F1D RID: 3869
		private readonly List<MBBindingList<BarterItemVM>> _offerList;

		// Token: 0x04000F1E RID: 3870
		private readonly Dictionary<BarterGroup, MBBindingList<BarterItemVM>> _leftList;

		// Token: 0x04000F1F RID: 3871
		private readonly Dictionary<BarterGroup, MBBindingList<BarterItemVM>> _rightList;

		// Token: 0x04000F20 RID: 3872
		private readonly bool _isPlayerOfferer;

		// Token: 0x04000F21 RID: 3873
		private readonly BarterManager _barter;

		// Token: 0x04000F22 RID: 3874
		private readonly CharacterObject _otherCharacter;

		// Token: 0x04000F23 RID: 3875
		private readonly PartyBase _otherParty;

		// Token: 0x04000F24 RID: 3876
		private readonly BarterData _barterData;

		// Token: 0x04000F25 RID: 3877
		private string _fiefLbl;

		// Token: 0x04000F26 RID: 3878
		private string _prisonerLbl;

		// Token: 0x04000F27 RID: 3879
		private string _itemLbl;

		// Token: 0x04000F28 RID: 3880
		private string _otherLbl;

		// Token: 0x04000F29 RID: 3881
		private string _cancelLbl;

		// Token: 0x04000F2A RID: 3882
		private string _resetLbl;

		// Token: 0x04000F2B RID: 3883
		private string _offerLbl;

		// Token: 0x04000F2C RID: 3884
		private string _diplomaticLbl;

		// Token: 0x04000F2D RID: 3885
		private HintViewModel _autoBalanceHint;

		// Token: 0x04000F2E RID: 3886
		private HeroVM _leftHero;

		// Token: 0x04000F2F RID: 3887
		private HeroVM _rightHero;

		// Token: 0x04000F30 RID: 3888
		private string _leftNameLbl;

		// Token: 0x04000F31 RID: 3889
		private string _rightNameLbl;

		// Token: 0x04000F32 RID: 3890
		private MBBindingList<BarterItemVM> _leftFiefList;

		// Token: 0x04000F33 RID: 3891
		private MBBindingList<BarterItemVM> _rightFiefList;

		// Token: 0x04000F34 RID: 3892
		private MBBindingList<BarterItemVM> _leftPrisonerList;

		// Token: 0x04000F35 RID: 3893
		private MBBindingList<BarterItemVM> _rightPrisonerList;

		// Token: 0x04000F36 RID: 3894
		private MBBindingList<BarterItemVM> _leftItemList;

		// Token: 0x04000F37 RID: 3895
		private MBBindingList<BarterItemVM> _rightItemList;

		// Token: 0x04000F38 RID: 3896
		private MBBindingList<BarterItemVM> _leftOtherList;

		// Token: 0x04000F39 RID: 3897
		private MBBindingList<BarterItemVM> _rightOtherList;

		// Token: 0x04000F3A RID: 3898
		private MBBindingList<BarterItemVM> _leftDiplomaticList;

		// Token: 0x04000F3B RID: 3899
		private MBBindingList<BarterItemVM> _rightDiplomaticList;

		// Token: 0x04000F3C RID: 3900
		private MBBindingList<BarterItemVM> _leftGoldList;

		// Token: 0x04000F3D RID: 3901
		private MBBindingList<BarterItemVM> _rightGoldList;

		// Token: 0x04000F3E RID: 3902
		private MBBindingList<BarterItemVM> _leftOfferList;

		// Token: 0x04000F3F RID: 3903
		private MBBindingList<BarterItemVM> _rightOfferList;

		// Token: 0x04000F40 RID: 3904
		private int _leftMaxGold;

		// Token: 0x04000F41 RID: 3905
		private int _rightMaxGold;

		// Token: 0x04000F42 RID: 3906
		private bool _initializationIsOver;

		// Token: 0x04000F43 RID: 3907
		private bool _isOfferDisabled;

		// Token: 0x04000F44 RID: 3908
		private int _resultBarOffererPercentage = -1;

		// Token: 0x04000F45 RID: 3909
		private int _resultBarOtherPercentage = -1;

		// Token: 0x04000F46 RID: 3910
		private InputKeyItemVM _resetInputKey;

		// Token: 0x04000F47 RID: 3911
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000F48 RID: 3912
		private InputKeyItemVM _cancelInputKey;
	}
}
