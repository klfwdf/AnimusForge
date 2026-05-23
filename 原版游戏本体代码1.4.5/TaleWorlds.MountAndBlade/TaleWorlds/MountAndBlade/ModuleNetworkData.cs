using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public static class ModuleNetworkData
{
	public static EquipmentElement ReadItemReferenceFromPacket(MBObjectManager objectManager, ref bool bufferReadValid)
	{
		MBObjectBase mBObjectBase = GameNetworkMessage.ReadObjectReferenceFromPacket(objectManager, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
		bool num = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		MBObjectBase mBObjectBase2 = null;
		if (num)
		{
			mBObjectBase2 = GameNetworkMessage.ReadObjectReferenceFromPacket(objectManager, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
		}
		ItemObject item = mBObjectBase as ItemObject;
		return new EquipmentElement(item, null, mBObjectBase2 as ItemObject);
	}

	public static void WriteItemReferenceToPacket(EquipmentElement equipElement)
	{
		GameNetworkMessage.WriteObjectReferenceToPacket(equipElement.Item, CompressionBasic.GUIDCompressionInfo);
		if (equipElement.CosmeticItem != null)
		{
			GameNetworkMessage.WriteBoolToPacket(value: true);
			GameNetworkMessage.WriteObjectReferenceToPacket(equipElement.CosmeticItem, CompressionBasic.GUIDCompressionInfo);
		}
		else
		{
			GameNetworkMessage.WriteBoolToPacket(value: false);
		}
	}

	public static MissionWeapon ReadWeaponReferenceFromPacket(MBObjectManager objectManager, ref bool bufferReadValid)
	{
		if (GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid))
		{
			return MissionWeapon.Invalid;
		}
		MBObjectBase mBObjectBase = GameNetworkMessage.ReadObjectReferenceFromPacket(objectManager, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
		int num = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.ItemDataValueCompressionInfo, ref bufferReadValid);
		int num2 = GameNetworkMessage.ReadIntFromPacket(CompressionMission.WeaponReloadPhaseCompressionInfo, ref bufferReadValid);
		short currentUsageIndex = (short)GameNetworkMessage.ReadIntFromPacket(CompressionMission.WeaponUsageIndexCompressionInfo, ref bufferReadValid);
		bool num3 = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		Banner banner = null;
		if (num3)
		{
			string bannerKey = GameNetworkMessage.ReadBannerCodeFromPacket(ref bufferReadValid);
			if (bufferReadValid)
			{
				banner = new Banner(bannerKey);
			}
		}
		ItemObject primaryItem = mBObjectBase as ItemObject;
		bool flag = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		MissionWeapon? ammoWeapon = null;
		if (bufferReadValid && flag)
		{
			MBObjectBase mBObjectBase2 = GameNetworkMessage.ReadObjectReferenceFromPacket(objectManager, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
			int num4 = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.ItemDataValueCompressionInfo, ref bufferReadValid);
			ItemObject primaryItem2 = mBObjectBase2 as ItemObject;
			ammoWeapon = new MissionWeapon(primaryItem2, null, banner, (short)num4);
		}
		MissionWeapon result = new MissionWeapon(primaryItem, null, banner, (short)num, (short)num2, ammoWeapon);
		result.CurrentUsageIndex = currentUsageIndex;
		return result;
	}

	public static void WriteWeaponReferenceToPacket(MissionWeapon weapon)
	{
		GameNetworkMessage.WriteBoolToPacket(weapon.IsEmpty);
		if (!weapon.IsEmpty)
		{
			GameNetworkMessage.WriteObjectReferenceToPacket(weapon.Item, CompressionBasic.GUIDCompressionInfo);
			GameNetworkMessage.WriteIntToPacket(weapon.RawDataForNetwork, CompressionBasic.ItemDataValueCompressionInfo);
			GameNetworkMessage.WriteIntToPacket(weapon.ReloadPhase, CompressionMission.WeaponReloadPhaseCompressionInfo);
			GameNetworkMessage.WriteIntToPacket(weapon.CurrentUsageIndex, CompressionMission.WeaponUsageIndexCompressionInfo);
			bool num = weapon.Banner != null;
			GameNetworkMessage.WriteBoolToPacket(num);
			if (num)
			{
				GameNetworkMessage.WriteBannerCodeToPacket(weapon.Banner.BannerCode);
			}
			MissionWeapon ammoWeapon = weapon.AmmoWeapon;
			bool num2 = !ammoWeapon.IsEmpty;
			GameNetworkMessage.WriteBoolToPacket(num2);
			if (num2)
			{
				GameNetworkMessage.WriteObjectReferenceToPacket(ammoWeapon.Item, CompressionBasic.GUIDCompressionInfo);
				GameNetworkMessage.WriteIntToPacket(ammoWeapon.RawDataForNetwork, CompressionBasic.ItemDataValueCompressionInfo);
			}
		}
	}

	public static MissionWeapon ReadMissileWeaponReferenceFromPacket(MBObjectManager objectManager, ref bool bufferReadValid)
	{
		MBObjectBase mBObjectBase = GameNetworkMessage.ReadObjectReferenceFromPacket(objectManager, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
		short currentUsageIndex = (short)GameNetworkMessage.ReadIntFromPacket(CompressionMission.WeaponUsageIndexCompressionInfo, ref bufferReadValid);
		ItemObject primaryItem = mBObjectBase as ItemObject;
		MissionWeapon result = new MissionWeapon(primaryItem, null, null, 1);
		result.CurrentUsageIndex = currentUsageIndex;
		return result;
	}

	public static void WriteMissileWeaponReferenceToPacket(MissionWeapon weapon)
	{
		GameNetworkMessage.WriteObjectReferenceToPacket(weapon.Item, CompressionBasic.GUIDCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(weapon.CurrentUsageIndex, CompressionMission.WeaponUsageIndexCompressionInfo);
	}
}
