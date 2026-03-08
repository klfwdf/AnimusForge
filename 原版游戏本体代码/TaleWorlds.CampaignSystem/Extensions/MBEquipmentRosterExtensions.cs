using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Extensions
{
	// Token: 0x0200016F RID: 367
	public static class MBEquipmentRosterExtensions
	{
		// Token: 0x170006E7 RID: 1767
		// (get) Token: 0x06001AFC RID: 6908 RVA: 0x0008ACD6 File Offset: 0x00088ED6
		public static MBReadOnlyList<MBEquipmentRoster> All
		{
			get
			{
				return Campaign.Current.AllEquipmentRosters;
			}
		}

		// Token: 0x06001AFD RID: 6909 RVA: 0x0008ACE2 File Offset: 0x00088EE2
		public static IEnumerable<Equipment> GetCivilianEquipments(this MBEquipmentRoster instance)
		{
			return from x in instance.AllEquipments
				where x.IsCivilian
				select x;
		}

		// Token: 0x06001AFE RID: 6910 RVA: 0x0008AD0E File Offset: 0x00088F0E
		public static IEnumerable<Equipment> GetStealthEquipments(this MBEquipmentRoster instance)
		{
			return from x in instance.AllEquipments
				where x.IsStealth
				select x;
		}

		// Token: 0x06001AFF RID: 6911 RVA: 0x0008AD3A File Offset: 0x00088F3A
		public static IEnumerable<Equipment> GetBattleEquipments(this MBEquipmentRoster instance)
		{
			return from x in instance.AllEquipments
				where x.IsBattle
				select x;
		}

		// Token: 0x06001B00 RID: 6912 RVA: 0x0008AD66 File Offset: 0x00088F66
		public static Equipment GetRandomCivilianEquipment(this MBEquipmentRoster instance)
		{
			return instance.AllEquipments.GetRandomElementWithPredicate((Equipment x) => x.IsCivilian);
		}

		// Token: 0x06001B01 RID: 6913 RVA: 0x0008AD92 File Offset: 0x00088F92
		public static Equipment GetRandomStealthEquipment(this MBEquipmentRoster instance)
		{
			return instance.AllEquipments.GetRandomElementWithPredicate((Equipment x) => x.IsStealth);
		}
	}
}
