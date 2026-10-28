using System;
using System.Globalization;
using System.IO;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade;

public static class MusicParameters
{
	private enum MusicParametersEnum
	{
		SmallBattleTreshold,
		MediumBattleTreshold,
		LargeBattleTreshold,
		SmallBattleDistanceTreshold,
		MediumBattleDistanceTreshold,
		LargeBattleDistanceTreshold,
		MaxBattleDistanceTreshold,
		MinIntensity,
		DefaultStartIntensity,
		PlayerChargeEffectMultiplierOnIntensity,
		BattleSizeEffectOnStartIntensity,
		RandomEffectMultiplierOnStartIntensity,
		FriendlyTroopDeadEffectOnIntensity,
		EnemyTroopDeadEffectOnIntensity,
		PlayerTroopDeadEffectMultiplierOnIntensity,
		BattleRatioTresholdOnIntensity,
		BattleTurnsOneSideCooldown,
		CampaignDarkModeThreshold,
		Count
	}

	private static float[] _parameters;

	public const float ZeroIntensity = 0f;

	public static int SmallBattleTreshold => (int)_parameters[0];

	public static int MediumBattleTreshold => (int)_parameters[1];

	public static int LargeBattleTreshold => (int)_parameters[2];

	public static float SmallBattleDistanceTreshold => _parameters[3];

	public static float MediumBattleDistanceTreshold => _parameters[4];

	public static float LargeBattleDistanceTreshold => _parameters[5];

	public static float MaxBattleDistanceTreshold => _parameters[6];

	public static float MinIntensity => _parameters[7];

	public static float DefaultStartIntensity => _parameters[8];

	public static float PlayerChargeEffectMultiplierOnIntensity => _parameters[9];

	public static float BattleSizeEffectOnStartIntensity => _parameters[10];

	public static float RandomEffectMultiplierOnStartIntensity => _parameters[11];

	public static float FriendlyTroopDeadEffectOnIntensity => _parameters[12];

	public static float EnemyTroopDeadEffectOnIntensity => _parameters[13];

	public static float PlayerTroopDeadEffectMultiplierOnIntensity => _parameters[14];

	public static float BattleRatioTresholdOnIntensity => _parameters[15];

	public static float BattleTurnsOneSideCooldown => _parameters[16];

	public static float CampaignDarkModeThreshold => _parameters[17];

	public static void LoadFromXml()
	{
		_parameters = new float[18];
		string path = ModuleHelper.GetModuleFullPath("Native") + "ModuleData/music_parameters.xml";
		XmlDocument xmlDocument = new XmlDocument();
		StreamReader streamReader = new StreamReader(path);
		string xml = streamReader.ReadToEnd();
		xmlDocument.LoadXml(xml);
		streamReader.Close();
		foreach (XmlNode childNode in xmlDocument.ChildNodes)
		{
			if (childNode.NodeType != XmlNodeType.Element || !(childNode.Name == "music_parameters"))
			{
				continue;
			}
			foreach (XmlNode childNode2 in childNode.ChildNodes)
			{
				if (childNode2.NodeType == XmlNodeType.Element)
				{
					MusicParametersEnum musicParametersEnum = (MusicParametersEnum)Enum.Parse(typeof(MusicParametersEnum), childNode2.Attributes["id"].Value);
					float num = float.Parse(childNode2.Attributes["value"].Value, CultureInfo.InvariantCulture);
					_parameters[(int)musicParametersEnum] = num;
				}
			}
			break;
		}
		Debug.Print("MusicParameters have been resetted.", 0, Debug.DebugColor.Green, 281474976710656uL);
	}
}
