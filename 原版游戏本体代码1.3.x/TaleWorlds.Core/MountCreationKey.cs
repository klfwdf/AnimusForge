using System;
using System.Globalization;

namespace TaleWorlds.Core
{
	// Token: 0x020000C0 RID: 192
	public class MountCreationKey
	{
		// Token: 0x170003AC RID: 940
		// (get) Token: 0x06000AA0 RID: 2720 RVA: 0x0002246A File Offset: 0x0002066A
		// (set) Token: 0x06000AA1 RID: 2721 RVA: 0x00022472 File Offset: 0x00020672
		public byte _leftFrontLegColorIndex { get; private set; }

		// Token: 0x170003AD RID: 941
		// (get) Token: 0x06000AA2 RID: 2722 RVA: 0x0002247B File Offset: 0x0002067B
		// (set) Token: 0x06000AA3 RID: 2723 RVA: 0x00022483 File Offset: 0x00020683
		public byte _rightFrontLegColorIndex { get; private set; }

		// Token: 0x170003AE RID: 942
		// (get) Token: 0x06000AA4 RID: 2724 RVA: 0x0002248C File Offset: 0x0002068C
		// (set) Token: 0x06000AA5 RID: 2725 RVA: 0x00022494 File Offset: 0x00020694
		public byte _leftBackLegColorIndex { get; private set; }

		// Token: 0x170003AF RID: 943
		// (get) Token: 0x06000AA6 RID: 2726 RVA: 0x0002249D File Offset: 0x0002069D
		// (set) Token: 0x06000AA7 RID: 2727 RVA: 0x000224A5 File Offset: 0x000206A5
		public byte _rightBackLegColorIndex { get; private set; }

		// Token: 0x170003B0 RID: 944
		// (get) Token: 0x06000AA8 RID: 2728 RVA: 0x000224AE File Offset: 0x000206AE
		// (set) Token: 0x06000AA9 RID: 2729 RVA: 0x000224B6 File Offset: 0x000206B6
		public byte MaterialIndex { get; private set; }

		// Token: 0x170003B1 RID: 945
		// (get) Token: 0x06000AAA RID: 2730 RVA: 0x000224BF File Offset: 0x000206BF
		// (set) Token: 0x06000AAB RID: 2731 RVA: 0x000224C7 File Offset: 0x000206C7
		public byte MeshMultiplierIndex { get; private set; }

		// Token: 0x06000AAC RID: 2732 RVA: 0x000224D0 File Offset: 0x000206D0
		public MountCreationKey(byte leftFrontLegColorIndex, byte rightFrontLegColorIndex, byte leftBackLegColorIndex, byte rightBackLegColorIndex, byte materialIndex, byte meshMultiplierIndex)
		{
			if (leftFrontLegColorIndex == 3 || rightFrontLegColorIndex == 3)
			{
				leftFrontLegColorIndex = 3;
				rightFrontLegColorIndex = 3;
			}
			this._leftFrontLegColorIndex = leftFrontLegColorIndex;
			this._rightFrontLegColorIndex = rightFrontLegColorIndex;
			this._leftBackLegColorIndex = leftBackLegColorIndex;
			this._rightBackLegColorIndex = rightBackLegColorIndex;
			this.MaterialIndex = materialIndex;
			this.MeshMultiplierIndex = meshMultiplierIndex;
		}

		// Token: 0x06000AAD RID: 2733 RVA: 0x00022520 File Offset: 0x00020720
		public static MountCreationKey FromString(string str)
		{
			if (str != null)
			{
				uint numericKey = uint.Parse(str, NumberStyles.HexNumber);
				int bitsFromKey = MountCreationKey.GetBitsFromKey(numericKey, 0, 2);
				int bitsFromKey2 = MountCreationKey.GetBitsFromKey(numericKey, 2, 2);
				int bitsFromKey3 = MountCreationKey.GetBitsFromKey(numericKey, 4, 2);
				int bitsFromKey4 = MountCreationKey.GetBitsFromKey(numericKey, 6, 2);
				int bitsFromKey5 = MountCreationKey.GetBitsFromKey(numericKey, 8, 2);
				int bitsFromKey6 = MountCreationKey.GetBitsFromKey(numericKey, 10, 2);
				return new MountCreationKey((byte)bitsFromKey, (byte)bitsFromKey2, (byte)bitsFromKey3, (byte)bitsFromKey4, (byte)bitsFromKey5, (byte)bitsFromKey6);
			}
			return new MountCreationKey(0, 0, 0, 0, 0, 0);
		}

		// Token: 0x06000AAE RID: 2734 RVA: 0x00022594 File Offset: 0x00020794
		public override string ToString()
		{
			uint num = 0U;
			this.SetBits(ref num, (int)this._leftFrontLegColorIndex, 0);
			this.SetBits(ref num, (int)this._rightFrontLegColorIndex, 2);
			this.SetBits(ref num, (int)this._leftBackLegColorIndex, 4);
			this.SetBits(ref num, (int)this._rightBackLegColorIndex, 6);
			this.SetBits(ref num, (int)this.MaterialIndex, 8);
			this.SetBits(ref num, (int)this.MeshMultiplierIndex, 10);
			return num.ToString("X");
		}

		// Token: 0x06000AAF RID: 2735 RVA: 0x0002260C File Offset: 0x0002080C
		private static int GetBitsFromKey(uint numericKey, int startingBit, int numBits)
		{
			int num = (int)(numericKey >> startingBit);
			uint num2 = (uint)(numBits * numBits - 1);
			return num & (int)num2;
		}

		// Token: 0x06000AB0 RID: 2736 RVA: 0x00022628 File Offset: 0x00020828
		private void SetBits(ref uint numericKey, int value, int startingBit)
		{
			uint num = (uint)((uint)value << startingBit);
			numericKey |= num;
		}

		// Token: 0x06000AB1 RID: 2737 RVA: 0x00022644 File Offset: 0x00020844
		public static string GetRandomMountKeyString(ItemObject mountItem, int randomSeed)
		{
			return MountCreationKey.GetRandomMountKey(mountItem, randomSeed).ToString();
		}

		// Token: 0x06000AB2 RID: 2738 RVA: 0x00022654 File Offset: 0x00020854
		public static MountCreationKey GetRandomMountKey(ItemObject mountItem, int randomSeed)
		{
			MBFastRandom mbfastRandom = new MBFastRandom((uint)randomSeed);
			if (mountItem == null)
			{
				return new MountCreationKey((byte)mbfastRandom.Next(4), (byte)mbfastRandom.Next(4), (byte)mbfastRandom.Next(4), (byte)mbfastRandom.Next(4), 0, 0);
			}
			HorseComponent horseComponent = mountItem.HorseComponent;
			if (horseComponent.HorseMaterialNames != null && horseComponent.HorseMaterialNames.Count > 0)
			{
				int num = mbfastRandom.Next(horseComponent.HorseMaterialNames.Count);
				float num2 = mbfastRandom.NextFloat();
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
				return new MountCreationKey((byte)mbfastRandom.Next(4), (byte)mbfastRandom.Next(4), (byte)mbfastRandom.Next(4), (byte)mbfastRandom.Next(4), (byte)num, (byte)num3);
			}
			return new MountCreationKey((byte)mbfastRandom.Next(4), (byte)mbfastRandom.Next(4), (byte)mbfastRandom.Next(4), (byte)mbfastRandom.Next(4), 0, 0);
		}

		// Token: 0x040005EE RID: 1518
		private const int NumLegColors = 4;
	}
}
