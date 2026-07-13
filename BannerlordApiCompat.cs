using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Centralizes Bannerlord minor-version API differences observed between 1.3.x,
/// early/local 1.4.x DLLs, and 1.4.5 reference source.
/// Keep feature logic in callers; this file only adapts method/property signatures.
/// </summary>
internal static class BannerlordApiCompat
{
	internal static bool HasTradeAgreement(ITradeAgreementsCampaignBehavior tradeBehavior, Kingdom kingdom, Kingdom other)
	{
		if (tradeBehavior == null || kingdom == null || other == null)
		{
			return false;
		}
		try
		{
			MethodInfo methodWithOut = FindMethod(tradeBehavior.GetType(), "HasTradeAgreement", 3);
			if (methodWithOut != null)
			{
				object[] args = { kingdom, other, null };
				return Convert.ToBoolean(methodWithOut.Invoke(tradeBehavior, args));
			}

			MethodInfo methodWithoutOut = FindMethod(tradeBehavior.GetType(), "HasTradeAgreement", 2);
			if (methodWithoutOut != null)
			{
				return Convert.ToBoolean(methodWithoutOut.Invoke(tradeBehavior, new object[] { kingdom, other }));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BannerlordApiCompat", "HasTradeAgreement failed: " + ex.Message);
		}
		return false;
	}

	internal static bool IsPlayerEncounterRestartedForRaid(PlayerEncounter encounter)
	{
		if (encounter == null)
		{
			return false;
		}
		try
		{
			PropertyInfo property = encounter.GetType().GetProperty("IsPlayerEncounterRestartedForRaid", BindingFlags.Instance | BindingFlags.Public);
			return property != null && Convert.ToBoolean(property.GetValue(encounter));
		}
		catch (Exception ex)
		{
			Logger.Log("BannerlordApiCompat", "IsPlayerEncounterRestartedForRaid failed: " + ex.Message);
			return false;
		}
	}

	internal static bool TryOverrideMapEventSettlementForRaidToFieldBattleSwitch(MapEvent mapEvent, Settlement settlement)
	{
		if (mapEvent == null || settlement == null)
		{
			return false;
		}
		try
		{
			MethodInfo method = mapEvent.GetType().GetMethod("OverrideMapEventSettlementForRaidToFieldBattleSwitch", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Settlement) }, null);
			if (method != null)
			{
				method.Invoke(mapEvent, new object[] { settlement });
				return true;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BannerlordApiCompat", "OverrideMapEventSettlementForRaidToFieldBattleSwitch failed: " + ex.Message);
		}
		try
		{
			PropertyInfo property = mapEvent.GetType().GetProperty("MapEventSettlement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				property.SetValue(mapEvent, settlement);
				return true;
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("BannerlordApiCompat", "MapEventSettlement reflection set failed: " + ex2.Message);
		}
		return false;
	}

	internal static bool TrySetMapEventWasEverInLootingPhase(MapEvent mapEvent, bool value)
	{
		if (mapEvent == null)
		{
			return false;
		}
		try
		{
			PropertyInfo property = mapEvent.GetType().GetProperty("WasEverInLootingPhase", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null && property.CanWrite)
			{
				property.SetValue(mapEvent, value);
				return true;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BannerlordApiCompat", "WasEverInLootingPhase reflection set failed: " + ex.Message);
		}
		return false;
	}

	internal static float GetNeededMaximumDistanceForEncounteringMobileParty(MobileParty party)
	{
		try
		{
			object model = Campaign.Current?.Models?.EncounterModel;
			if (model == null)
			{
				return 0.5f;
			}

			string versionedProperty = party?.IsCurrentlyAtSea == true
				? "NeededMaximumNavalDistanceForEncounteringMobileParty"
				: "NeededMaximumLandDistanceForEncounteringMobileParty";
			if (TryGetFloatProperty(model, versionedProperty, out float versionedDistance))
			{
				return versionedDistance;
			}
			if (TryGetFloatProperty(model, "NeededMaximumDistanceForEncounteringMobileParty", out float legacyDistance))
			{
				return legacyDistance;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BannerlordApiCompat", "GetNeededMaximumDistanceForEncounteringMobileParty failed: " + ex.Message);
		}
		return 0.5f;
	}

	internal static void GetActionForRaidingSettlement(MobileParty party, Settlement settlement)
	{
		try
		{
			MethodInfo method = typeof(SetPartyAiAction)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Where(x => x.Name == "GetActionForRaidingSettlement")
				.OrderByDescending(x => x.GetParameters().Length)
				.FirstOrDefault();
			if (method == null)
			{
				return;
			}

			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length >= 5)
			{
				method.Invoke(null, new object[] { party, settlement, MobileParty.NavigationType.Default, false, false });
				return;
			}
			if (parameters.Length == 4)
			{
				method.Invoke(null, new object[] { party, settlement, MobileParty.NavigationType.Default, false });
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BannerlordApiCompat", "GetActionForRaidingSettlement failed: " + ex.Message);
		}
	}

	internal static Agent SpawnPrisonerInspectionTroop(
		Mission mission,
		IAgentOriginBase origin,
		int formationTroopCount,
		int formationTroopIndex,
		FormationClass formationClass)
	{
		if (mission == null || origin == null)
		{
			return null;
		}
		try
		{
			MethodInfo method = typeof(Mission)
				.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.Where(x => x.Name == "SpawnTroop")
				.OrderByDescending(x => x.GetParameters().Length)
				.FirstOrDefault();
			if (method == null)
			{
				return null;
			}

			ParameterInfo[] parameters = method.GetParameters();
			object[] args = parameters.Select(parameter => BuildPrisonerSpawnTroopArgument(
				parameter,
				origin,
				formationTroopCount,
				formationTroopIndex,
				formationClass,
				null,
				null)).ToArray();
			return method.Invoke(mission, args) as Agent;
		}
		catch (Exception ex)
		{
			Logger.Log("BannerlordApiCompat", "SpawnPrisonerInspectionTroop failed: " + ex.Message);
			return null;
		}
	}

	private static object BuildPrisonerSpawnTroopArgument(
		ParameterInfo parameter,
		IAgentOriginBase origin,
		int formationTroopCount,
		int formationTroopIndex,
		FormationClass formationClass,
		Vec3? initialPosition,
		Vec2? initialDirection)
	{
		string name = parameter?.Name ?? string.Empty;
		switch (name.ToLowerInvariant())
		{
			case "trooporigin": return origin;
			case "isplayerside": return true;
			case "hasformation": return true;
			case "spawnwithhorse": return false;
			case "isreinforcement": return false;
			case "formationtroopcount": return formationTroopCount;
			case "formationtroopindex": return formationTroopIndex;
			case "isalarmed": return false;
			case "wieldinitialweapons": return false;
			case "forcedismounted": return true;
			case "initialposition": return initialPosition;
			case "initialdirection": return initialDirection;
			case "specialactionsetsuffix": return null;
			case "banneritem": return null;
			case "formationindex": return formationClass;
			case "usetroopclassforspawn": return false;
		}

		if (parameter.HasDefaultValue)
		{
			return parameter.DefaultValue;
		}
		if (parameter.ParameterType == typeof(bool))
		{
			return false;
		}
		if (parameter.ParameterType == typeof(int))
		{
			return 0;
		}
		return null;
	}

	internal static TerrainType ResolveTerrainTypeForParty(MobileParty party, TerrainType fallback = TerrainType.Plain, bool allowNavigationFaceFallback = false)
	{
		try
		{
			IMapScene mapSceneWrapper = Campaign.Current?.MapSceneWrapper;
			if (party == null || mapSceneWrapper == null)
			{
				return fallback;
			}
			if (party.Position.IsValid())
			{
				try
				{
					CampaignVec2 position = party.Position;
					return mapSceneWrapper.GetTerrainTypeAtPosition(in position);
				}
				catch
				{
				}
				try
				{
					mapSceneWrapper.GetEnvironmentTerrainTypesCount(party.Position, out TerrainType environmentTerrainType);
					return environmentTerrainType;
				}
				catch
				{
				}
			}
			if (allowNavigationFaceFallback)
			{
				try
				{
					return mapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return fallback;
	}

	private static MethodInfo FindMethod(Type type, string name, int parameterCount)
	{
		return type?.GetMethods(BindingFlags.Instance | BindingFlags.Public)
			.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.Ordinal) && x.GetParameters().Length == parameterCount);
	}

	private static bool TryGetFloatProperty(object instance, string propertyName, out float value)
	{
		value = 0f;
		PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
		if (property == null)
		{
			return false;
		}
		value = Convert.ToSingle(property.GetValue(instance));
		return true;
	}
}
