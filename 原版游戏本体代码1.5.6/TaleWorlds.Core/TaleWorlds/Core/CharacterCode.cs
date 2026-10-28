using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core;

public class CharacterCode
{
	public const string SpecialCodeSeparator = "@---@";

	public const int SpecialCodeSeparatorLength = 5;

	public BodyProperties BodyProperties;

	public bool IsEmpty => string.IsNullOrEmpty(Code);

	public string EquipmentCode { get; private set; }

	public string Code { get; private set; }

	public bool IsFemale { get; private set; }

	public bool IsHero { get; private set; }

	public float FaceDirtAmount => 0f;

	public Banner Banner => null;

	public FormationClass FormationClass { get; set; }

	public uint Color1 { get; set; } = Color.White.ToUnsignedInteger();

	public uint Color2 { get; set; } = Color.White.ToUnsignedInteger();

	public int Race { get; private set; }

	public Equipment CalculateEquipment()
	{
		if (EquipmentCode == null)
		{
			return null;
		}
		return Equipment.CreateFromEquipmentCode(EquipmentCode);
	}

	private CharacterCode()
	{
	}

	public static CharacterCode CreateFrom(BasicCharacterObject character)
	{
		return CreateFrom(character, character.Equipment);
	}

	public static CharacterCode CreateFrom(BasicCharacterObject character, Equipment equipment)
	{
		CharacterCode characterCode = new CharacterCode();
		Equipment equipment2 = equipment ?? character.Equipment;
		string value = (characterCode.EquipmentCode = equipment2?.CalculateEquipmentCode());
		characterCode.BodyProperties = character.GetBodyProperties(equipment2);
		characterCode.IsFemale = character.IsFemale;
		characterCode.IsHero = character.IsHero;
		characterCode.FormationClass = character.DefaultFormationClass;
		characterCode.Race = character.Race;
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "CreateFrom");
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(value);
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(characterCode.BodyProperties.ToString());
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(characterCode.IsFemale ? "1" : "0");
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(characterCode.IsHero ? "1" : "0");
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(((int)characterCode.FormationClass).ToString());
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(characterCode.Color1.ToString());
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(characterCode.Color2.ToString());
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(characterCode.Race.ToString());
		mBStringBuilder.Append("@---@");
		characterCode.Code = mBStringBuilder.ToStringAndRelease();
		return characterCode;
	}

	public static CharacterCode CreateFrom(string equipmentCode, BodyProperties bodyProperties, bool isFemale, bool isHero, uint color1, uint color2, FormationClass formationClass, int race)
	{
		CharacterCode characterCode = new CharacterCode();
		characterCode.EquipmentCode = equipmentCode;
		characterCode.BodyProperties = bodyProperties;
		characterCode.IsFemale = isFemale;
		characterCode.IsHero = isHero;
		characterCode.Color1 = color1;
		characterCode.Color2 = color2;
		characterCode.FormationClass = formationClass;
		characterCode.Race = race;
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "CreateFrom");
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(equipmentCode);
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(characterCode.BodyProperties.ToString());
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(characterCode.IsFemale ? "1" : "0");
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(characterCode.IsHero ? "1" : "0");
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(((int)characterCode.FormationClass).ToString());
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(characterCode.Color1.ToString());
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(characterCode.Color2.ToString());
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(characterCode.Race.ToString());
		mBStringBuilder.Append("@---@");
		characterCode.Code = mBStringBuilder.ToStringAndRelease();
		return characterCode;
	}

	public string CreateNewCodeString()
	{
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "CreateNewCodeString");
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(EquipmentCode);
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(BodyProperties.ToString());
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(IsFemale ? "1" : "0");
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(IsHero ? "1" : "0");
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(((int)FormationClass).ToString());
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(Color1.ToString());
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(Color2.ToString());
		mBStringBuilder.Append("@---@");
		mBStringBuilder.Append(Race.ToString());
		mBStringBuilder.Append("@---@");
		return mBStringBuilder.ToStringAndRelease();
	}

	public static CharacterCode CreateEmpty()
	{
		return new CharacterCode();
	}

	public static CharacterCode CreateFrom(string code)
	{
		CharacterCode characterCode = new CharacterCode();
		int num = 0;
		int num2;
		for (num2 = code.IndexOf("@---@", StringComparison.InvariantCulture); num2 == num; num2 = code.IndexOf("@---@", num, StringComparison.InvariantCulture))
		{
			num = num2 + 5;
		}
		string equipmentCode = code.Substring(num, num2 - num);
		do
		{
			num = num2 + 5;
			num2 = code.IndexOf("@---@", num, StringComparison.InvariantCulture);
		}
		while (num2 == num);
		string keyValue = code.Substring(num, num2 - num);
		do
		{
			num = num2 + 5;
			num2 = code.IndexOf("@---@", num, StringComparison.InvariantCulture);
		}
		while (num2 == num);
		string value = code.Substring(num, num2 - num);
		do
		{
			num = num2 + 5;
			num2 = code.IndexOf("@---@", num, StringComparison.InvariantCulture);
		}
		while (num2 == num);
		string value2 = code.Substring(num, num2 - num);
		do
		{
			num = num2 + 5;
			num2 = code.IndexOf("@---@", num, StringComparison.InvariantCulture);
		}
		while (num2 == num);
		string value3 = code.Substring(num, num2 - num);
		do
		{
			num = num2 + 5;
			num2 = code.IndexOf("@---@", num, StringComparison.InvariantCulture);
		}
		while (num2 == num);
		string value4 = code.Substring(num, num2 - num);
		do
		{
			num = num2 + 5;
			num2 = code.IndexOf("@---@", num, StringComparison.InvariantCulture);
		}
		while (num2 == num);
		string value5 = code.Substring(num, num2 - num);
		num = num2 + 5;
		num2 = code.IndexOf("@---@", num, StringComparison.InvariantCulture);
		string value6 = ((num2 >= 0) ? code.Substring(num, num2 - num) : code.Substring(num));
		characterCode.EquipmentCode = equipmentCode;
		if (BodyProperties.FromString(keyValue, out var bodyProperties))
		{
			characterCode.BodyProperties = bodyProperties;
		}
		else
		{
			Debug.FailedAssert("Cannot read the character code body property", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\CharacterCode.cs", "CreateFrom", 241);
		}
		characterCode.IsFemale = Convert.ToInt32(value) == 1;
		characterCode.IsHero = Convert.ToInt32(value2) == 1;
		characterCode.FormationClass = (FormationClass)Convert.ToInt32(value3);
		characterCode.Color1 = Convert.ToUInt32(value4);
		characterCode.Color2 = Convert.ToUInt32(value5);
		characterCode.Race = Convert.ToInt32(value6);
		characterCode.Code = code;
		return characterCode;
	}
}
