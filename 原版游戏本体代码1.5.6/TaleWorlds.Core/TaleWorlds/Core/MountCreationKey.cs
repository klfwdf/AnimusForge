using System.Globalization;

namespace TaleWorlds.Core;

public class MountCreationKey
{
	private const int NumLegColors = 4;

	public byte _leftFrontLegColorIndex { get; private set; }

	public byte _rightFrontLegColorIndex { get; private set; }

	public byte _leftBackLegColorIndex { get; private set; }

	public byte _rightBackLegColorIndex { get; private set; }

	public byte MaterialIndex { get; private set; }

	public byte MeshMultiplierIndex { get; private set; }

	public MountCreationKey(byte leftFrontLegColorIndex, byte rightFrontLegColorIndex, byte leftBackLegColorIndex, byte rightBackLegColorIndex, byte materialIndex, byte meshMultiplierIndex)
	{
		if (leftFrontLegColorIndex == 3 || rightFrontLegColorIndex == 3)
		{
			leftFrontLegColorIndex = 3;
			rightFrontLegColorIndex = 3;
		}
		_leftFrontLegColorIndex = leftFrontLegColorIndex;
		_rightFrontLegColorIndex = rightFrontLegColorIndex;
		_leftBackLegColorIndex = leftBackLegColorIndex;
		_rightBackLegColorIndex = rightBackLegColorIndex;
		MaterialIndex = materialIndex;
		MeshMultiplierIndex = meshMultiplierIndex;
	}

	public static MountCreationKey FromString(string str)
	{
		if (str != null)
		{
			uint numericKey = uint.Parse(str, NumberStyles.HexNumber);
			int bitsFromKey = GetBitsFromKey(numericKey, 0, 2);
			int bitsFromKey2 = GetBitsFromKey(numericKey, 2, 2);
			int bitsFromKey3 = GetBitsFromKey(numericKey, 4, 2);
			int bitsFromKey4 = GetBitsFromKey(numericKey, 6, 2);
			int bitsFromKey5 = GetBitsFromKey(numericKey, 8, 2);
			int bitsFromKey6 = GetBitsFromKey(numericKey, 10, 2);
			return new MountCreationKey((byte)bitsFromKey, (byte)bitsFromKey2, (byte)bitsFromKey3, (byte)bitsFromKey4, (byte)bitsFromKey5, (byte)bitsFromKey6);
		}
		return new MountCreationKey(0, 0, 0, 0, 0, 0);
	}

	public override string ToString()
	{
		uint numericKey = 0u;
		SetBits(ref numericKey, _leftFrontLegColorIndex, 0);
		SetBits(ref numericKey, _rightFrontLegColorIndex, 2);
		SetBits(ref numericKey, _leftBackLegColorIndex, 4);
		SetBits(ref numericKey, _rightBackLegColorIndex, 6);
		SetBits(ref numericKey, MaterialIndex, 8);
		SetBits(ref numericKey, MeshMultiplierIndex, 10);
		return numericKey.ToString("X");
	}

	private static int GetBitsFromKey(uint numericKey, int startingBit, int numBits)
	{
		uint num = numericKey >> startingBit;
		uint num2 = (uint)(numBits * numBits - 1);
		return (int)(num & num2);
	}

	private void SetBits(ref uint numericKey, int value, int startingBit)
	{
		uint num = (uint)value;
		num <<= startingBit;
		numericKey |= num;
	}

	public static string GetRandomMountKeyString(ItemObject mountItem, int randomSeed)
	{
		return GetRandomMountKey(mountItem, randomSeed).ToString();
	}

	public static MountCreationKey GetRandomMountKey(ItemObject mountItem, int randomSeed)
	{
		MBFastRandom mBFastRandom = new MBFastRandom((uint)randomSeed);
		if (mountItem != null)
		{
			HorseComponent horseComponent = mountItem.HorseComponent;
			if (horseComponent.HorseMaterialNames != null && horseComponent.HorseMaterialNames.Count > 0)
			{
				int num = mBFastRandom.Next(horseComponent.HorseMaterialNames.Count);
				float num2 = mBFastRandom.NextFloat();
				int num3 = 0;
				float num4 = 0f;
				HorseComponent.MaterialProperty materialProperty = horseComponent.HorseMaterialNames[num];
				for (int i = 0; i < materialProperty.MeshMultiplier.Count; i++)
				{
					num4 += materialProperty.MeshMultiplier[i].Item2;
					if (num2 <= num4)
					{
						num3 = i;
						break;
					}
				}
				return new MountCreationKey((byte)mBFastRandom.Next(4), (byte)mBFastRandom.Next(4), (byte)mBFastRandom.Next(4), (byte)mBFastRandom.Next(4), (byte)num, (byte)num3);
			}
			return new MountCreationKey((byte)mBFastRandom.Next(4), (byte)mBFastRandom.Next(4), (byte)mBFastRandom.Next(4), (byte)mBFastRandom.Next(4), 0, 0);
		}
		return new MountCreationKey((byte)mBFastRandom.Next(4), (byte)mBFastRandom.Next(4), (byte)mBFastRandom.Next(4), (byte)mBFastRandom.Next(4), 0, 0);
	}
}
