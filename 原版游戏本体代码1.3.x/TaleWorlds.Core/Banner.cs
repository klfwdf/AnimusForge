using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.Core
{
	// Token: 0x0200000F RID: 15
	public class Banner
	{
		// Token: 0x1700002B RID: 43
		// (get) Token: 0x0600007E RID: 126 RVA: 0x00002E1C File Offset: 0x0000101C
		public string BannerCode
		{
			get
			{
				string result;
				if ((result = this._bannerCode) == null)
				{
					result = (this._bannerCode = this.Serialize());
				}
				return result;
			}
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x0600007F RID: 127 RVA: 0x00002E42 File Offset: 0x00001042
		public MBReadOnlyList<BannerData> BannerDataList
		{
			get
			{
				return this._bannerDataList;
			}
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x06000080 RID: 128 RVA: 0x00002E4C File Offset: 0x0000104C
		public IBannerVisual BannerVisual
		{
			get
			{
				IBannerVisual result;
				if ((result = this._bannerVisual) == null)
				{
					result = (this._bannerVisual = Game.Current.CreateBannerVisual(this));
				}
				return result;
			}
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00002E77 File Offset: 0x00001077
		public Banner()
		{
			this._bannerDataList = new MBList<BannerData>();
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00002E8C File Offset: 0x0000108C
		public Banner(Banner banner)
			: this()
		{
			this._bannerCode = banner._bannerCode;
			foreach (BannerData bannerData in banner._bannerDataList)
			{
				this._bannerDataList.Add(new BannerData(bannerData));
			}
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00002EFC File Offset: 0x000010FC
		public Banner(Banner banner, uint color1, uint color2)
			: this(banner)
		{
			this.ChangePrimaryColor(color1);
			this.ChangeIconColors(color2);
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00002F13 File Offset: 0x00001113
		public Banner(string bannerKey)
			: this()
		{
			if (string.IsNullOrEmpty(bannerKey))
			{
				Debug.FailedAssert("Banner key is empty!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Banner.cs", ".ctor", 73);
				return;
			}
			this.Deserialize(bannerKey);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00002F41 File Offset: 0x00001141
		public Banner(string bannerKey, uint color1, uint color2)
			: this(bannerKey)
		{
			this.ChangePrimaryColor(color1);
			this.ChangeIconColors(color2);
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00002F58 File Offset: 0x00001158
		public void SetBannerVisual(IBannerVisual visual)
		{
			this._bannerVisual = visual;
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00002F61 File Offset: 0x00001161
		public BannerData GetBannerDataAtIndex(int index)
		{
			this._bannerCode = null;
			if (this._bannerDataList.Count <= index)
			{
				return null;
			}
			return this._bannerDataList[index];
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00002F86 File Offset: 0x00001186
		public int GetBannerDataListCount()
		{
			return this._bannerDataList.Count;
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00002F93 File Offset: 0x00001193
		public bool IsBannerDataListEmpty()
		{
			return this._bannerDataList.IsEmpty<BannerData>();
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00002FA0 File Offset: 0x000011A0
		public int GetPrimaryColorId()
		{
			return this._bannerDataList[0].ColorId;
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00002FB3 File Offset: 0x000011B3
		public int GetSecondaryColorId()
		{
			return this._bannerDataList[0].ColorId2;
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00002FC6 File Offset: 0x000011C6
		public int GetIconColorId()
		{
			return this._bannerDataList[1].ColorId;
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00002FD9 File Offset: 0x000011D9
		public Vec2 GetIconSize()
		{
			return this._bannerDataList[1].Size;
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00002FEC File Offset: 0x000011EC
		public void SetPrimaryColorId(int colorId)
		{
			this._bannerCode = null;
			this._bannerDataList[0].ColorId = colorId;
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00003007 File Offset: 0x00001207
		public void SetSecondaryColorId(int colorId)
		{
			this._bannerCode = null;
			this._bannerDataList[0].ColorId2 = colorId;
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00003022 File Offset: 0x00001222
		public void SetIconColorId(int colorId)
		{
			this._bannerCode = null;
			this._bannerDataList[1].ColorId = colorId;
		}

		// Token: 0x06000091 RID: 145 RVA: 0x0000303D File Offset: 0x0000123D
		public void SetIconSize(int newSize)
		{
			this._bannerCode = null;
			this._bannerDataList[1].Size = new Vec2((float)newSize, (float)newSize);
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00003060 File Offset: 0x00001260
		public void ChangePrimaryColor(uint mainColor)
		{
			int colorId = BannerManager.GetColorId(mainColor);
			if (colorId < 0)
			{
				return;
			}
			this._bannerCode = null;
			this._bannerDataList[0].ColorId = colorId;
			this._bannerDataList[0].ColorId2 = colorId;
		}

		// Token: 0x06000093 RID: 147 RVA: 0x000030A4 File Offset: 0x000012A4
		public void ChangeBackgroundColor(uint primaryColor, uint secondaryColor)
		{
			int colorId = BannerManager.GetColorId(primaryColor);
			int colorId2 = BannerManager.GetColorId(secondaryColor);
			if (colorId < 0)
			{
				return;
			}
			if (colorId2 < 0)
			{
				return;
			}
			this._bannerCode = null;
			this._bannerDataList[0].ColorId = colorId;
			this._bannerDataList[0].ColorId2 = colorId2;
		}

		// Token: 0x06000094 RID: 148 RVA: 0x000030F4 File Offset: 0x000012F4
		public void ChangeIconColors(uint color)
		{
			int colorId = BannerManager.GetColorId(color);
			if (colorId < 0)
			{
				return;
			}
			this._bannerCode = null;
			for (int i = 1; i < this._bannerDataList.Count; i++)
			{
				this._bannerDataList[i].ColorId = colorId;
				this._bannerDataList[i].ColorId2 = colorId;
			}
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00003150 File Offset: 0x00001350
		public void RotateBackgroundToRight()
		{
			this._bannerCode = null;
			this._bannerDataList[0].RotationValue -= 0.0027777778f;
			this._bannerDataList[0].RotationValue = ((this._bannerDataList[0].RotationValue < 0f) ? (this._bannerDataList[0].RotationValue + 1f) : this._bannerDataList[0].RotationValue);
		}

		// Token: 0x06000096 RID: 150 RVA: 0x000031D4 File Offset: 0x000013D4
		public void RotateBackgroundToLeft()
		{
			this._bannerCode = null;
			this._bannerDataList[0].RotationValue += 0.0027777778f;
			this._bannerDataList[0].RotationValue = ((this._bannerDataList[0].RotationValue > 0f) ? (this._bannerDataList[0].RotationValue - 1f) : this._bannerDataList[0].RotationValue);
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00003258 File Offset: 0x00001458
		public int GetBackgroundMeshId()
		{
			return this._bannerDataList[0].MeshId;
		}

		// Token: 0x06000098 RID: 152 RVA: 0x0000326B File Offset: 0x0000146B
		public int GetIconMeshId()
		{
			return this._bannerDataList[1].MeshId;
		}

		// Token: 0x06000099 RID: 153 RVA: 0x0000327E File Offset: 0x0000147E
		public void SetBackgroundMeshId(int meshId)
		{
			this._bannerCode = null;
			this._bannerDataList[0].MeshId = meshId;
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00003299 File Offset: 0x00001499
		public void SetIconMeshId(int meshId)
		{
			this._bannerCode = null;
			this._bannerDataList[1].MeshId = meshId;
		}

		// Token: 0x0600009B RID: 155 RVA: 0x000032B4 File Offset: 0x000014B4
		public string Serialize()
		{
			return Banner.GetBannerCodeFromBannerDataList(this._bannerDataList);
		}

		// Token: 0x0600009C RID: 156 RVA: 0x000032C4 File Offset: 0x000014C4
		public void Deserialize(string message)
		{
			this._bannerCode = message;
			this._bannerVisual = null;
			this._bannerDataList.Clear();
			List<BannerData> collection;
			if (Banner.TryGetBannerDataFromCode(message, out collection))
			{
				this._bannerDataList.AddRange(collection);
			}
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00003300 File Offset: 0x00001500
		public void ClearAllIcons()
		{
			this._bannerCode = null;
			BannerData item = this._bannerDataList[0];
			this._bannerDataList.Clear();
			this._bannerDataList.Add(item);
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00003338 File Offset: 0x00001538
		public void AddIconData(BannerData iconData)
		{
			if (this._bannerDataList.Count < 33)
			{
				this._bannerCode = null;
				this._bannerDataList.Add(iconData);
			}
		}

		// Token: 0x0600009F RID: 159 RVA: 0x0000335C File Offset: 0x0000155C
		public void AddIconData(BannerData iconData, int index)
		{
			if (this._bannerDataList.Count < 33 && index > 0 && index <= this._bannerDataList.Count)
			{
				this._bannerDataList.Insert(index, iconData);
			}
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x0000338C File Offset: 0x0000158C
		public void RemoveIconDataAtIndex(int index)
		{
			if (index > 0 && index < this._bannerDataList.Count)
			{
				this._bannerDataList.RemoveAt(index);
			}
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x000033AC File Offset: 0x000015AC
		public static Banner CreateRandomClanBanner(int seed = -1)
		{
			return Banner.CreateRandomBannerInternal(seed, Banner.BannerIconOrientation.CentralPositionedOneIcon);
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x000033B5 File Offset: 0x000015B5
		public static Banner CreateRandomBanner()
		{
			return Banner.CreateRandomBannerInternal(-1, Banner.BannerIconOrientation.None);
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x000033C0 File Offset: 0x000015C0
		private static Banner CreateRandomBannerInternal(int seed = -1, Banner.BannerIconOrientation orientation = Banner.BannerIconOrientation.None)
		{
			Game game = Game.Current;
			MBFastRandom mbfastRandom = ((seed == -1) ? new MBFastRandom() : new MBFastRandom((uint)seed));
			Banner banner = new Banner();
			BannerData iconData = new BannerData(BannerManager.Instance.GetRandomBackgroundId(mbfastRandom), BannerManager.Instance.GetRandomColorId(mbfastRandom), BannerManager.Instance.GetRandomColorId(mbfastRandom), new Vec2(1528f, 1528f), new Vec2(764f, 764f), false, false, 0f);
			banner.AddIconData(iconData);
			switch ((orientation == Banner.BannerIconOrientation.None) ? mbfastRandom.Next(6) : ((int)orientation))
			{
			case 0:
				banner.CentralPositionedOneIcon(mbfastRandom);
				break;
			case 1:
				banner.CenteredTwoMirroredIcons(mbfastRandom);
				break;
			case 2:
				banner.DiagonalIcons(mbfastRandom);
				break;
			case 3:
				banner.HorizontalIcons(mbfastRandom);
				break;
			case 4:
				banner.VerticalIcons(mbfastRandom);
				break;
			case 5:
				banner.SquarePositionedFourIcons(mbfastRandom);
				break;
			}
			return banner;
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x000034A4 File Offset: 0x000016A4
		public static Banner CreateOneColoredEmptyBanner(int colorIndex)
		{
			Banner banner = new Banner();
			BannerData iconData = new BannerData(BannerManager.Instance.GetRandomBackgroundId(new MBFastRandom()), colorIndex, colorIndex, new Vec2(1528f, 1528f), new Vec2(764f, 764f), false, false, 0f);
			banner.AddIconData(iconData);
			return banner;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x000034FC File Offset: 0x000016FC
		public static Banner CreateOneColoredBannerWithOneIcon(uint backgroundColor, uint iconColor, int iconMeshId)
		{
			Banner banner = Banner.CreateOneColoredEmptyBanner(BannerManager.GetColorId(backgroundColor));
			if (iconMeshId == -1)
			{
				iconMeshId = BannerManager.Instance.GetRandomBannerIconId(new MBFastRandom());
			}
			banner.AddIconData(new BannerData(iconMeshId, BannerManager.GetColorId(iconColor), BannerManager.GetColorId(iconColor), new Vec2(512f, 512f), new Vec2(764f, 764f), false, false, 0f));
			return banner;
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00003568 File Offset: 0x00001768
		private void CentralPositionedOneIcon(MBFastRandom random)
		{
			int randomBannerIconId = BannerManager.Instance.GetRandomBannerIconId(random);
			int randomColorId = BannerManager.Instance.GetRandomColorId(random);
			bool flag = random.NextFloat() < 0.5f;
			int colorId = (flag ? BannerManager.Instance.GetRandomColorId(random) : BannerManager.Instance.ReadOnlyColorPalette.Last<KeyValuePair<int, BannerColor>>().Key);
			bool mirror = random.Next(2) == 0;
			float num = random.NextFloat();
			float rotationValue = 0f;
			if (num > 0.9f)
			{
				rotationValue = 0.25f;
			}
			else if (num > 0.8f)
			{
				rotationValue = 0.5f;
			}
			else if (num > 0.7f)
			{
				rotationValue = 0.75f;
			}
			BannerData iconData = new BannerData(randomBannerIconId, randomColorId, colorId, new Vec2(512f, 512f), new Vec2(764f, 764f), flag, mirror, rotationValue);
			this.AddIconData(iconData);
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00003644 File Offset: 0x00001844
		private void DiagonalIcons(MBFastRandom random)
		{
			int num = ((random.NextFloat() < 0.5f) ? 2 : 3);
			bool flag = random.NextFloat() < 0.5f;
			int num2 = (512 - 20 * (num + 1)) / num;
			int num3 = BannerManager.Instance.GetRandomBannerIconId(random);
			int num4 = BannerManager.Instance.GetRandomColorId(random);
			bool flag2 = random.NextFloat() < 0.5f;
			int colorId = (flag2 ? BannerManager.Instance.GetRandomColorId(random) : BannerManager.Instance.ReadOnlyColorPalette.Last<KeyValuePair<int, BannerColor>>().Key);
			int num5 = (512 - num * num2) / (num + 1);
			bool flag3 = random.NextFloat() < 0.3f;
			bool flag4 = flag3 || random.NextFloat() < 0.3f;
			for (int i = 0; i < num; i++)
			{
				num3 = (flag3 ? BannerManager.Instance.GetRandomBannerIconId(random) : num3);
				num4 = (flag4 ? BannerManager.Instance.GetRandomColorId(random) : num4);
				int num6 = i * (num2 + num5) + num5 + num2 / 2;
				int num7 = i * (num2 + num5) + num5 + num2 / 2;
				if (flag)
				{
					num7 = 512 - num7;
				}
				BannerData iconData = new BannerData(num3, num4, colorId, new Vec2((float)num2, (float)num2), new Vec2((float)(num6 + 508), (float)(num7 + 508)), flag2, false, 0f);
				this.AddIconData(iconData);
			}
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x000037B0 File Offset: 0x000019B0
		private void HorizontalIcons(MBFastRandom random)
		{
			int num = ((random.NextFloat() < 0.5f) ? 2 : 3);
			int num2 = (512 - 20 * (num + 1)) / num;
			int num3 = BannerManager.Instance.GetRandomBannerIconId(random);
			int num4 = BannerManager.Instance.GetRandomColorId(random);
			bool flag = random.NextFloat() < 0.5f;
			int colorId = (flag ? BannerManager.Instance.GetRandomColorId(random) : BannerManager.Instance.ReadOnlyColorPalette.Last<KeyValuePair<int, BannerColor>>().Key);
			int num5 = (512 - num * num2) / (num + 1);
			bool flag2 = random.NextFloat() < 0.3f;
			bool flag3 = flag2 || random.NextFloat() < 0.3f;
			for (int i = 0; i < num; i++)
			{
				num3 = (flag2 ? BannerManager.Instance.GetRandomBannerIconId(random) : num3);
				num4 = (flag3 ? BannerManager.Instance.GetRandomColorId(random) : num4);
				int num6 = i * (num2 + num5) + num5 + num2 / 2;
				BannerData iconData = new BannerData(num3, num4, colorId, new Vec2((float)num2, (float)num2), new Vec2((float)(num6 + 508), 764f), flag, false, 0f);
				this.AddIconData(iconData);
			}
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x000038E4 File Offset: 0x00001AE4
		private void VerticalIcons(MBFastRandom random)
		{
			int num = ((random.NextFloat() < 0.5f) ? 2 : 3);
			int num2 = (512 - 20 * (num + 1)) / num;
			int num3 = BannerManager.Instance.GetRandomBannerIconId(random);
			int num4 = BannerManager.Instance.GetRandomColorId(random);
			bool flag = random.NextFloat() < 0.5f;
			int colorId = (flag ? BannerManager.Instance.GetRandomColorId(random) : BannerManager.Instance.ReadOnlyColorPalette.Last<KeyValuePair<int, BannerColor>>().Key);
			int num5 = (512 - num * num2) / (num + 1);
			bool flag2 = random.NextFloat() < 0.3f;
			bool flag3 = flag2 || random.NextFloat() < 0.3f;
			for (int i = 0; i < num; i++)
			{
				num3 = (flag2 ? BannerManager.Instance.GetRandomBannerIconId(random) : num3);
				num4 = (flag3 ? BannerManager.Instance.GetRandomColorId(random) : num4);
				int num6 = i * (num2 + num5) + num5 + num2 / 2;
				BannerData iconData = new BannerData(num3, num4, colorId, new Vec2((float)num2, (float)num2), new Vec2(764f, (float)(num6 + 508)), flag, false, 0f);
				this.AddIconData(iconData);
			}
		}

		// Token: 0x060000AA RID: 170 RVA: 0x00003A18 File Offset: 0x00001C18
		private void SquarePositionedFourIcons(MBFastRandom random)
		{
			bool flag = random.NextFloat() < 0.5f;
			bool flag2 = !flag && random.NextFloat() < 0.5f;
			bool flag3 = flag2 || random.NextFloat() < 0.5f;
			bool flag4 = random.NextFloat() < 0.5f;
			int num = BannerManager.Instance.GetRandomBannerIconId(random);
			int colorId = (flag4 ? BannerManager.Instance.GetRandomColorId(random) : BannerManager.Instance.ReadOnlyColorPalette.Last<KeyValuePair<int, BannerColor>>().Key);
			int num2 = BannerManager.Instance.GetRandomColorId(random);
			BannerData iconData = new BannerData(num, num2, colorId, new Vec2(220f, 220f), new Vec2(654f, 654f), flag4, false, 0f);
			this.AddIconData(iconData);
			num = (flag2 ? BannerManager.Instance.GetRandomBannerIconId(random) : num);
			num2 = (flag3 ? BannerManager.Instance.GetRandomColorId(random) : num2);
			iconData = new BannerData(num, num2, colorId, new Vec2(220f, 220f), new Vec2(874f, 654f), flag4, flag, 0f);
			this.AddIconData(iconData);
			num = (flag2 ? BannerManager.Instance.GetRandomBannerIconId(random) : num);
			num2 = (flag3 ? BannerManager.Instance.GetRandomColorId(random) : num2);
			iconData = new BannerData(num, num2, colorId, new Vec2(220f, 220f), new Vec2(654f, 874f), flag4, flag, flag ? 0.5f : 0f);
			this.AddIconData(iconData);
			num = (flag2 ? BannerManager.Instance.GetRandomBannerIconId(random) : num);
			num2 = (flag3 ? BannerManager.Instance.GetRandomColorId(random) : num2);
			iconData = new BannerData(num, num2, colorId, new Vec2(220f, 220f), new Vec2(874f, 874f), flag4, false, flag ? 0.5f : 0f);
			this.AddIconData(iconData);
		}

		// Token: 0x060000AB RID: 171 RVA: 0x00003C14 File Offset: 0x00001E14
		private void CenteredTwoMirroredIcons(MBFastRandom random)
		{
			bool flag = random.NextFloat() < 0.5f;
			bool flag2 = random.NextFloat() < 0.5f;
			int randomBannerIconId = BannerManager.Instance.GetRandomBannerIconId(random);
			int colorId = (flag2 ? BannerManager.Instance.GetRandomColorId(random) : BannerManager.Instance.ReadOnlyColorPalette.Last<KeyValuePair<int, BannerColor>>().Key);
			int num = BannerManager.Instance.GetRandomColorId(random);
			BannerData iconData = new BannerData(randomBannerIconId, num, colorId, new Vec2(200f, 200f), new Vec2(664f, 764f), flag2, false, 0f);
			this.AddIconData(iconData);
			num = (flag ? BannerManager.Instance.GetRandomColorId(random) : num);
			iconData = new BannerData(randomBannerIconId, num, colorId, new Vec2(200f, 200f), new Vec2(864f, 764f), flag2, true, 0f);
			this.AddIconData(iconData);
		}

		// Token: 0x060000AC RID: 172 RVA: 0x00003CFC File Offset: 0x00001EFC
		public uint GetPrimaryColor()
		{
			if (this._bannerDataList.Count <= 0)
			{
				return uint.MaxValue;
			}
			return BannerManager.GetColor(this._bannerDataList[0].ColorId);
		}

		// Token: 0x060000AD RID: 173 RVA: 0x00003D24 File Offset: 0x00001F24
		public uint GetSecondaryColor()
		{
			if (this._bannerDataList.Count <= 0)
			{
				return uint.MaxValue;
			}
			return BannerManager.GetColor(this._bannerDataList[0].ColorId2);
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00003D4C File Offset: 0x00001F4C
		public uint GetFirstIconColor()
		{
			if (this._bannerDataList.Count <= 1)
			{
				return uint.MaxValue;
			}
			return BannerManager.GetColor(this._bannerDataList[1].ColorId);
		}

		// Token: 0x060000AF RID: 175 RVA: 0x00003D74 File Offset: 0x00001F74
		public int GetVersionNo()
		{
			int num = 0;
			for (int i = 0; i < this._bannerDataList.Count; i++)
			{
				num += this._bannerDataList[i].LocalVersion;
			}
			return num;
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00003DB0 File Offset: 0x00001FB0
		public static string GetBannerCodeFromBannerDataList(MBList<BannerData> bannerDataList)
		{
			MBStringBuilder mbstringBuilder = default(MBStringBuilder);
			mbstringBuilder.Initialize(16, "GetBannerCodeFromBannerDataList");
			bool flag = true;
			foreach (BannerData bannerData in bannerDataList)
			{
				if (!flag)
				{
					mbstringBuilder.Append('.');
				}
				flag = false;
				mbstringBuilder.Append(bannerData.MeshId);
				mbstringBuilder.Append('.');
				mbstringBuilder.Append(bannerData.ColorId);
				mbstringBuilder.Append('.');
				mbstringBuilder.Append(bannerData.ColorId2);
				mbstringBuilder.Append('.');
				mbstringBuilder.Append((int)bannerData.Size.x);
				mbstringBuilder.Append('.');
				mbstringBuilder.Append((int)bannerData.Size.y);
				mbstringBuilder.Append('.');
				mbstringBuilder.Append((int)bannerData.Position.x);
				mbstringBuilder.Append('.');
				mbstringBuilder.Append((int)bannerData.Position.y);
				mbstringBuilder.Append('.');
				mbstringBuilder.Append(bannerData.DrawStroke ? 1 : 0);
				mbstringBuilder.Append('.');
				mbstringBuilder.Append(bannerData.Mirror ? 1 : 0);
				mbstringBuilder.Append('.');
				mbstringBuilder.Append((int)(bannerData.RotationValue / 0.0027777778f));
			}
			return mbstringBuilder.ToStringAndRelease();
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x00003F48 File Offset: 0x00002148
		public static bool IsValidBannerCode(string bannerCode)
		{
			List<BannerData> list;
			return !string.IsNullOrEmpty(bannerCode) && Banner.TryGetBannerDataFromCode(bannerCode, out list);
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00003F68 File Offset: 0x00002168
		public static bool TryGetBannerDataFromCode(string bannerCode, out List<BannerData> bannerDataList)
		{
			bannerDataList = new List<BannerData>();
			string[] array = bannerCode.Split(new char[] { '.' });
			int num = 0;
			while (num + 10 <= array.Length)
			{
				int meshId;
				int colorId;
				int colorId2;
				int num2;
				int num3;
				int num4;
				int num5;
				int num6;
				int num7;
				int num8;
				if (!int.TryParse(array[num], out meshId) || !int.TryParse(array[num + 1], out colorId) || !int.TryParse(array[num + 2], out colorId2) || !int.TryParse(array[num + 3], out num2) || !int.TryParse(array[num + 4], out num3) || !int.TryParse(array[num + 5], out num4) || !int.TryParse(array[num + 6], out num5) || !int.TryParse(array[num + 7], out num6) || !int.TryParse(array[num + 8], out num7) || !int.TryParse(array[num + 9], out num8))
				{
					bannerDataList.Clear();
					return false;
				}
				BannerData item = new BannerData(meshId, colorId, colorId2, new Vec2((float)num2, (float)num3), new Vec2((float)num4, (float)num5), num6 == 1, num7 == 1, (float)num8 * 0.0027777778f);
				bannerDataList.Add(item);
				num += 10;
			}
			if (bannerDataList.Count > 32)
			{
				bannerDataList.RemoveRange(31, bannerDataList.Count - 32);
			}
			return true;
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x000040A7 File Offset: 0x000022A7
		internal static void AutoGeneratedStaticCollectObjectsBanner(object o, List<object> collectedObjects)
		{
			((Banner)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x000040B5 File Offset: 0x000022B5
		protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			collectedObjects.Add(this._bannerDataList);
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x000040C3 File Offset: 0x000022C3
		internal static object AutoGeneratedGetMemberValue_bannerDataList(object o)
		{
			return ((Banner)o)._bannerDataList;
		}

		// Token: 0x040000F9 RID: 249
		public const int MaxSize = 8000;

		// Token: 0x040000FA RID: 250
		public const int BannerFullSize = 1528;

		// Token: 0x040000FB RID: 251
		public const int BannerEditableAreaSize = 512;

		// Token: 0x040000FC RID: 252
		public const int MaxIconCount = 32;

		// Token: 0x040000FD RID: 253
		private const char Splitter = '.';

		// Token: 0x040000FE RID: 254
		public const int BackgroundDataIndex = 0;

		// Token: 0x040000FF RID: 255
		public const int BannerIconDataIndex = 1;

		// Token: 0x04000100 RID: 256
		[CachedData]
		private string _bannerCode;

		// Token: 0x04000101 RID: 257
		[SaveableField(1)]
		private readonly MBList<BannerData> _bannerDataList;

		// Token: 0x04000102 RID: 258
		[CachedData]
		private IBannerVisual _bannerVisual;

		// Token: 0x020000F2 RID: 242
		private enum BannerIconOrientation
		{
			// Token: 0x040006E3 RID: 1763
			None = -1,
			// Token: 0x040006E4 RID: 1764
			CentralPositionedOneIcon,
			// Token: 0x040006E5 RID: 1765
			CenteredTwoMirroredIcons,
			// Token: 0x040006E6 RID: 1766
			DiagonalIcons,
			// Token: 0x040006E7 RID: 1767
			HorizontalIcons,
			// Token: 0x040006E8 RID: 1768
			VerticalIcons,
			// Token: 0x040006E9 RID: 1769
			SquarePositionedFourIcons,
			// Token: 0x040006EA RID: 1770
			NumberOfOrientation
		}
	}
}
