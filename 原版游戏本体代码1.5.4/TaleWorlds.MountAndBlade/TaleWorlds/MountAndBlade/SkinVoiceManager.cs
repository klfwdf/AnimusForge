using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public static class SkinVoiceManager
{
	public enum CombatVoiceNetworkPredictionType
	{
		Prediction,
		OwnerPrediction,
		NoPrediction
	}

	public struct SkinVoiceType
	{
		public string TypeID { get; private set; }

		public int Index { get; private set; }

		public SkinVoiceType(string typeID)
		{
			TypeID = typeID;
			Index = MBAPI.IMBVoiceManager.GetVoiceTypeIndex(typeID);
		}

		public TextObject GetName()
		{
			return GameTexts.FindText("str_taunt_name", TypeID);
		}
	}

	public static class VoiceType
	{
		public static readonly SkinVoiceType Grunt = new SkinVoiceType("Grunt");

		public static readonly SkinVoiceType Jump = new SkinVoiceType("Jump");

		public static readonly SkinVoiceType Yell = new SkinVoiceType("Yell");

		public static readonly SkinVoiceType Pain = new SkinVoiceType("Pain");

		public static readonly SkinVoiceType Death = new SkinVoiceType("Death");

		public static readonly SkinVoiceType Stun = new SkinVoiceType("Stun");

		public static readonly SkinVoiceType Fear = new SkinVoiceType("Fear");

		public static readonly SkinVoiceType Climb = new SkinVoiceType("Climb");

		public static readonly SkinVoiceType Focus = new SkinVoiceType("Focus");

		public static readonly SkinVoiceType Debacle = new SkinVoiceType("Debacle");

		public static readonly SkinVoiceType Victory = new SkinVoiceType("Victory");

		public static readonly SkinVoiceType HorseStop = new SkinVoiceType("HorseStop");

		public static readonly SkinVoiceType HorseRally = new SkinVoiceType("HorseRally");

		public static readonly SkinVoiceType Drown = new SkinVoiceType("Drown");

		public static readonly SkinVoiceType Infantry = new SkinVoiceType("Infantry");

		public static readonly SkinVoiceType Cavalry = new SkinVoiceType("Cavalry");

		public static readonly SkinVoiceType Archers = new SkinVoiceType("Archers");

		public static readonly SkinVoiceType HorseArchers = new SkinVoiceType("HorseArchers");

		public static readonly SkinVoiceType Everyone = new SkinVoiceType("Everyone");

		public static readonly SkinVoiceType MixedFormation = new SkinVoiceType("Mixed");

		public static readonly SkinVoiceType Move = new SkinVoiceType("Move");

		public static readonly SkinVoiceType Follow = new SkinVoiceType("Follow");

		public static readonly SkinVoiceType Charge = new SkinVoiceType("Charge");

		public static readonly SkinVoiceType Advance = new SkinVoiceType("Advance");

		public static readonly SkinVoiceType FallBack = new SkinVoiceType("FallBack");

		public static readonly SkinVoiceType Stop = new SkinVoiceType("Stop");

		public static readonly SkinVoiceType Retreat = new SkinVoiceType("Retreat");

		public static readonly SkinVoiceType Mount = new SkinVoiceType("Mount");

		public static readonly SkinVoiceType Dismount = new SkinVoiceType("Dismount");

		public static readonly SkinVoiceType FireAtWill = new SkinVoiceType("FireAtWill");

		public static readonly SkinVoiceType HoldFire = new SkinVoiceType("HoldFire");

		public static readonly SkinVoiceType PickSpears = new SkinVoiceType("PickSpears");

		public static readonly SkinVoiceType PickDefault = new SkinVoiceType("PickDefault");

		public static readonly SkinVoiceType FaceEnemy = new SkinVoiceType("FaceEnemy");

		public static readonly SkinVoiceType FaceDirection = new SkinVoiceType("FaceDirection");

		public static readonly SkinVoiceType UseSiegeWeapon = new SkinVoiceType("UseSiegeWeapon");

		public static readonly SkinVoiceType UseLadders = new SkinVoiceType("UseLadders");

		public static readonly SkinVoiceType AttackGate = new SkinVoiceType("AttackGate");

		public static readonly SkinVoiceType CommandDelegate = new SkinVoiceType("CommandDelegate");

		public static readonly SkinVoiceType CommandUndelegate = new SkinVoiceType("CommandUndelegate");

		public static readonly SkinVoiceType BoardAtWill = new SkinVoiceType("BoardAtWill");

		public static readonly SkinVoiceType AvoidBoarding = new SkinVoiceType("AvoidBoarding");

		public static readonly SkinVoiceType FormLine = new SkinVoiceType("FormLine");

		public static readonly SkinVoiceType FormShieldWall = new SkinVoiceType("FormShieldWall");

		public static readonly SkinVoiceType FormLoose = new SkinVoiceType("FormLoose");

		public static readonly SkinVoiceType FormCircle = new SkinVoiceType("FormCircle");

		public static readonly SkinVoiceType FormSquare = new SkinVoiceType("FormSquare");

		public static readonly SkinVoiceType FormSkein = new SkinVoiceType("FormSkein");

		public static readonly SkinVoiceType FormColumn = new SkinVoiceType("FormColumn");

		public static readonly SkinVoiceType FormScatter = new SkinVoiceType("FormScatter");

		public static readonly SkinVoiceType[] MpBarks = new SkinVoiceType[9]
		{
			new SkinVoiceType("MpDefend"),
			new SkinVoiceType("MpAttack"),
			new SkinVoiceType("MpHelp"),
			new SkinVoiceType("MpSpot"),
			new SkinVoiceType("MpThanks"),
			new SkinVoiceType("MpSorry"),
			new SkinVoiceType("MpAffirmative"),
			new SkinVoiceType("MpNegative"),
			new SkinVoiceType("MpRegroup")
		};

		public static readonly SkinVoiceType MpDefend = MpBarks[0];

		public static readonly SkinVoiceType MpAttack = MpBarks[1];

		public static readonly SkinVoiceType MpHelp = MpBarks[2];

		public static readonly SkinVoiceType MpSpot = MpBarks[3];

		public static readonly SkinVoiceType MpThanks = MpBarks[4];

		public static readonly SkinVoiceType MpSorry = MpBarks[5];

		public static readonly SkinVoiceType MpAffirmative = MpBarks[6];

		public static readonly SkinVoiceType MpNegative = MpBarks[7];

		public static readonly SkinVoiceType MpRegroup = MpBarks[8];

		public static readonly SkinVoiceType Idle = new SkinVoiceType("Idle");

		public static readonly SkinVoiceType Neigh = new SkinVoiceType("Neigh");

		public static readonly SkinVoiceType Collide = new SkinVoiceType("Collide");
	}

	public static int GetVoiceDefinitionCountWithMonsterSoundAndCollisionInfoClassName(string className)
	{
		return MBAPI.IMBVoiceManager.GetVoiceDefinitionCountWithMonsterSoundAndCollisionInfoClassName(className);
	}

	public static void GetVoiceDefinitionListWithMonsterSoundAndCollisionInfoClassName(string className, int[] definitionIndices)
	{
		MBAPI.IMBVoiceManager.GetVoiceDefinitionListWithMonsterSoundAndCollisionInfoClassName(className, definitionIndices);
	}
}
