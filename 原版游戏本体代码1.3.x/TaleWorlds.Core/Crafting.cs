using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core
{
	// Token: 0x02000045 RID: 69
	public class Crafting
	{
		// Token: 0x060005F2 RID: 1522 RVA: 0x000146DA File Offset: 0x000128DA
		public Crafting(CraftingTemplate craftingTemplate, BasicCultureObject culture, TextObject name)
		{
			this.CraftedWeaponName = name;
			this.CurrentCraftingTemplate = craftingTemplate;
			this.CurrentCulture = culture;
		}

		// Token: 0x170001FC RID: 508
		// (get) Token: 0x060005F3 RID: 1523 RVA: 0x000146F7 File Offset: 0x000128F7
		public BasicCultureObject CurrentCulture { get; }

		// Token: 0x170001FD RID: 509
		// (get) Token: 0x060005F4 RID: 1524 RVA: 0x000146FF File Offset: 0x000128FF
		public CraftingTemplate CurrentCraftingTemplate { get; }

		// Token: 0x170001FE RID: 510
		// (get) Token: 0x060005F5 RID: 1525 RVA: 0x00014707 File Offset: 0x00012907
		// (set) Token: 0x060005F6 RID: 1526 RVA: 0x0001470F File Offset: 0x0001290F
		public WeaponDesign CurrentWeaponDesign { get; private set; }

		// Token: 0x170001FF RID: 511
		// (get) Token: 0x060005F7 RID: 1527 RVA: 0x00014718 File Offset: 0x00012918
		// (set) Token: 0x060005F8 RID: 1528 RVA: 0x00014720 File Offset: 0x00012920
		public ItemModifierGroup CurrentItemModifierGroup { get; private set; }

		// Token: 0x17000200 RID: 512
		// (get) Token: 0x060005F9 RID: 1529 RVA: 0x00014729 File Offset: 0x00012929
		// (set) Token: 0x060005FA RID: 1530 RVA: 0x00014731 File Offset: 0x00012931
		public TextObject CraftedWeaponName { get; private set; }

		// Token: 0x060005FB RID: 1531 RVA: 0x0001473A File Offset: 0x0001293A
		public void SetCraftedWeaponName(TextObject weaponName)
		{
			if (!weaponName.Equals(this.CraftedWeaponName))
			{
				this.CraftedWeaponName = weaponName;
				this._craftedItemObject.SetCraftedWeaponName(this.CraftedWeaponName);
			}
		}

		// Token: 0x060005FC RID: 1532 RVA: 0x00014764 File Offset: 0x00012964
		public void Init()
		{
			this._history = new List<WeaponDesign>();
			this.UsablePiecesList = new List<WeaponDesignElement>[4];
			using (IEnumerator<CraftingPiece> enumerator = ((IEnumerable<CraftingPiece>)this.CurrentCraftingTemplate.Pieces).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					CraftingPiece craftingPiece = enumerator.Current;
					if (!this.CurrentCraftingTemplate.BuildOrders.All((PieceData x) => x.PieceType != craftingPiece.PieceType))
					{
						int pieceType = (int)craftingPiece.PieceType;
						if (this.UsablePiecesList[pieceType] == null)
						{
							this.UsablePiecesList[pieceType] = new List<WeaponDesignElement>();
						}
						this.UsablePiecesList[pieceType].Add(WeaponDesignElement.CreateUsablePiece(craftingPiece, 100));
					}
				}
			}
			WeaponDesignElement[] array = new WeaponDesignElement[4];
			for (int i = 0; i < array.Length; i++)
			{
				if (this.UsablePiecesList[i] != null)
				{
					array[i] = this.UsablePiecesList[i].First((WeaponDesignElement p) => !p.CraftingPiece.IsHiddenOnDesigner);
				}
				else
				{
					array[i] = WeaponDesignElement.GetInvalidPieceForType((CraftingPiece.PieceTypes)i);
				}
			}
			this.CurrentWeaponDesign = new WeaponDesign(this.CurrentCraftingTemplate, null, array, this.CurrentCraftingTemplate.StringId);
			this._history.Add(this.CurrentWeaponDesign);
		}

		// Token: 0x17000201 RID: 513
		// (get) Token: 0x060005FD RID: 1533 RVA: 0x000148C0 File Offset: 0x00012AC0
		// (set) Token: 0x060005FE RID: 1534 RVA: 0x000148C8 File Offset: 0x00012AC8
		public List<WeaponDesignElement>[] UsablePiecesList { get; private set; }

		// Token: 0x17000202 RID: 514
		// (get) Token: 0x060005FF RID: 1535 RVA: 0x000148D1 File Offset: 0x00012AD1
		public WeaponDesignElement[] SelectedPieces
		{
			get
			{
				return this.CurrentWeaponDesign.UsedPieces;
			}
		}

		// Token: 0x06000600 RID: 1536 RVA: 0x000148E0 File Offset: 0x00012AE0
		public WeaponDesignElement GetRandomPieceOfType(CraftingPiece.PieceTypes pieceType, bool randomScale)
		{
			if (!this.CurrentCraftingTemplate.IsPieceTypeUsable(pieceType))
			{
				return WeaponDesignElement.GetInvalidPieceForType(pieceType);
			}
			WeaponDesignElement copy = this.UsablePiecesList[(int)pieceType][MBRandom.RandomInt(this.UsablePiecesList[(int)pieceType].Count)].GetCopy();
			if (randomScale)
			{
				copy.SetScale((int)(90f + MBRandom.RandomFloat * 20f));
			}
			return copy;
		}

		// Token: 0x06000601 RID: 1537 RVA: 0x00014944 File Offset: 0x00012B44
		public void SwitchToCraftedItem(ItemObject item)
		{
			WeaponDesignElement[] usedPieces = item.WeaponDesign.UsedPieces;
			WeaponDesignElement[] array = new WeaponDesignElement[4];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = usedPieces[i].GetCopy();
			}
			this.CurrentWeaponDesign = new WeaponDesign(this.CurrentWeaponDesign.Template, this.CurrentWeaponDesign.WeaponName, array, null);
			this.ReIndex(false);
		}

		// Token: 0x06000602 RID: 1538 RVA: 0x000149A8 File Offset: 0x00012BA8
		public void Randomize()
		{
			WeaponDesignElement[] array = new WeaponDesignElement[4];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = this.GetRandomPieceOfType((CraftingPiece.PieceTypes)i, true);
			}
			this.CurrentWeaponDesign = new WeaponDesign(this.CurrentWeaponDesign.Template, this.CurrentWeaponDesign.WeaponName, array, null);
			this.ReIndex(false);
		}

		// Token: 0x06000603 RID: 1539 RVA: 0x00014A00 File Offset: 0x00012C00
		public void SwitchToPiece(WeaponDesignElement piece)
		{
			CraftingPiece.PieceTypes pieceType = piece.CraftingPiece.PieceType;
			WeaponDesignElement[] array = new WeaponDesignElement[4];
			for (int i = 0; i < array.Length; i++)
			{
				if (pieceType == (CraftingPiece.PieceTypes)i)
				{
					array[i] = piece.GetCopy();
					array[i].SetScale(100);
				}
				else
				{
					array[i] = this.CurrentWeaponDesign.UsedPieces[i].GetCopy();
					if (array[i].IsValid)
					{
						array[i].SetScale(this.CurrentWeaponDesign.UsedPieces[i].ScalePercentage);
					}
				}
			}
			this.CurrentWeaponDesign = new WeaponDesign(this.CurrentWeaponDesign.Template, this.CurrentWeaponDesign.WeaponName, array, null);
			this.ReIndex(false);
		}

		// Token: 0x06000604 RID: 1540 RVA: 0x00014AAC File Offset: 0x00012CAC
		public void ScaleThePiece(CraftingPiece.PieceTypes scalingPieceType, int percentage)
		{
			WeaponDesignElement[] array = new WeaponDesignElement[4];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = this.SelectedPieces[i].GetCopy();
				if (this.SelectedPieces[i].IsPieceScaled)
				{
					array[i].SetScale(this.SelectedPieces[i].ScalePercentage);
				}
			}
			array[(int)scalingPieceType].SetScale(percentage);
			this.CurrentWeaponDesign = new WeaponDesign(this.CurrentWeaponDesign.Template, this.CurrentWeaponDesign.WeaponName, array, null);
			this.ReIndex(false);
		}

		// Token: 0x06000605 RID: 1541 RVA: 0x00014B38 File Offset: 0x00012D38
		public void ReIndex(bool enforceReCreation = false)
		{
			if (!TextObject.IsNullOrEmpty(this.CurrentWeaponDesign.WeaponName) && !this.CurrentWeaponDesign.WeaponName.ToString().Equals(this.CraftedWeaponName.ToString()))
			{
				this.CraftedWeaponName = this.CurrentWeaponDesign.WeaponName.CopyTextObject();
			}
			if (enforceReCreation)
			{
				this.CurrentWeaponDesign = new WeaponDesign(this.CurrentWeaponDesign.Template, this.CurrentWeaponDesign.WeaponName, this.CurrentWeaponDesign.UsedPieces.ToArray<WeaponDesignElement>(), null);
			}
			this.SetItemObject(null, null);
		}

		// Token: 0x06000606 RID: 1542 RVA: 0x00014BCC File Offset: 0x00012DCC
		public bool Undo()
		{
			if (this._currentHistoryIndex <= 0)
			{
				return false;
			}
			this._currentHistoryIndex--;
			this.CurrentWeaponDesign = this._history[this._currentHistoryIndex];
			this.ReIndex(false);
			return true;
		}

		// Token: 0x06000607 RID: 1543 RVA: 0x00014C08 File Offset: 0x00012E08
		public bool Redo()
		{
			if (this._currentHistoryIndex + 1 >= this._history.Count)
			{
				return false;
			}
			this._currentHistoryIndex++;
			this.CurrentWeaponDesign = this._history[this._currentHistoryIndex];
			this.ReIndex(false);
			return true;
		}

		// Token: 0x06000608 RID: 1544 RVA: 0x00014C5C File Offset: 0x00012E5C
		public void UpdateHistory()
		{
			if (this._currentHistoryIndex < this._history.Count - 1)
			{
				this._history.RemoveRange(this._currentHistoryIndex + 1, this._history.Count - 1 - this._currentHistoryIndex);
			}
			WeaponDesignElement[] array = new WeaponDesignElement[this.CurrentWeaponDesign.UsedPieces.Length];
			for (int i = 0; i < this.CurrentWeaponDesign.UsedPieces.Length; i++)
			{
				array[i] = this.CurrentWeaponDesign.UsedPieces[i].GetCopy();
				if (array[i].IsValid)
				{
					array[i].SetScale(this.CurrentWeaponDesign.UsedPieces[i].ScalePercentage);
				}
			}
			this._history.Add(new WeaponDesign(this.CurrentWeaponDesign.Template, this.CurrentWeaponDesign.WeaponName, array, null));
			this._currentHistoryIndex = this._history.Count - 1;
		}

		// Token: 0x06000609 RID: 1545 RVA: 0x00014D44 File Offset: 0x00012F44
		public TextObject GetRandomCraftName()
		{
			return new TextObject("{=!}RANDOM_NAME", null);
		}

		// Token: 0x0600060A RID: 1546 RVA: 0x00014D54 File Offset: 0x00012F54
		public static void GenerateItem(WeaponDesign weaponDesignTemplate, TextObject name, BasicCultureObject culture, ItemModifierGroup itemModifierGroup, ref ItemObject itemObject, string customId = null)
		{
			if (itemObject == null)
			{
				itemObject = new ItemObject();
			}
			WeaponDesignElement[] array = new WeaponDesignElement[weaponDesignTemplate.UsedPieces.Length];
			for (int i = 0; i < weaponDesignTemplate.UsedPieces.Length; i++)
			{
				WeaponDesignElement weaponDesignElement = weaponDesignTemplate.UsedPieces[i];
				array[i] = WeaponDesignElement.CreateUsablePiece(weaponDesignElement.CraftingPiece, weaponDesignElement.ScalePercentage);
			}
			WeaponDesign weaponDesign = new WeaponDesign(weaponDesignTemplate.Template, name, array, customId);
			float weight = MathF.Round(weaponDesign.UsedPieces.Sum((WeaponDesignElement selectedUsablePiece) => selectedUsablePiece.ScaledWeight), 2);
			float appearance = (weaponDesign.UsedPieces[3].IsValid ? weaponDesign.UsedPieces[3].CraftingPiece.Appearance : weaponDesign.UsedPieces[0].CraftingPiece.Appearance);
			if (!string.IsNullOrEmpty(customId))
			{
				itemObject.StringId = customId;
			}
			else
			{
				itemObject.StringId = ((!string.IsNullOrEmpty(itemObject.StringId)) ? itemObject.StringId : weaponDesignTemplate.Template.StringId);
			}
			ItemObject.InitCraftedItemObject(ref itemObject, name, culture, Crafting.GetItemFlags(weaponDesign), weight, appearance, weaponDesign, weaponDesign.Template.ItemType);
			itemObject = Crafting.CraftedItemGenerationHelper.GenerateCraftedItem(itemObject, weaponDesign, itemModifierGroup);
			if (itemObject != null)
			{
				if (itemObject.IsCraftedByPlayer)
				{
					itemObject.IsReady = true;
				}
				itemObject.DetermineValue();
				itemObject.DetermineItemCategoryForItem();
			}
		}

		// Token: 0x0600060B RID: 1547 RVA: 0x00014EBF File Offset: 0x000130BF
		private static ItemFlags GetItemFlags(WeaponDesign weaponDesign)
		{
			return weaponDesign.UsedPieces[0].CraftingPiece.AdditionalItemFlags;
		}

		// Token: 0x0600060C RID: 1548 RVA: 0x00014ED3 File Offset: 0x000130D3
		private void SetItemObject(ItemObject itemObject = null, string customId = null)
		{
			if (itemObject == null)
			{
				itemObject = new ItemObject();
			}
			Crafting.GenerateItem(this.CurrentWeaponDesign, this.CraftedWeaponName, this.CurrentCulture, this.CurrentItemModifierGroup, ref itemObject, customId);
			this._craftedItemObject = itemObject;
		}

		// Token: 0x0600060D RID: 1549 RVA: 0x00014F06 File Offset: 0x00013106
		public ItemObject GetCurrentCraftedItemObject(bool forceReCreate = false, string customId = null)
		{
			if (forceReCreate)
			{
				this.SetItemObject(null, customId);
			}
			return this._craftedItemObject;
		}

		// Token: 0x0600060E RID: 1550 RVA: 0x00014F19 File Offset: 0x00013119
		public static IEnumerable<CraftingStatData> GetStatDatasFromTemplate(int usageIndex, ItemObject craftedItemObject, CraftingTemplate template)
		{
			WeaponComponentData weapon = craftedItemObject.GetWeaponWithUsageIndex(usageIndex);
			DamageTypes statDamageType = DamageTypes.Invalid;
			foreach (KeyValuePair<CraftingTemplate.CraftingStatTypes, float> keyValuePair in template.GetStatDatas(weapon.WeaponDescriptionId, weapon.ThrustDamageType, weapon.SwingDamageType))
			{
				TextObject textObject = GameTexts.FindText("str_crafting_stat", keyValuePair.Key.ToString());
				switch (keyValuePair.Key)
				{
				case CraftingTemplate.CraftingStatTypes.ThrustDamage:
					textObject.SetTextVariable("THRUST_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.ThrustDamageType).ToString()));
					statDamageType = weapon.ThrustDamageType;
					break;
				case CraftingTemplate.CraftingStatTypes.SwingDamage:
					textObject.SetTextVariable("SWING_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.SwingDamageType).ToString()));
					statDamageType = weapon.SwingDamageType;
					break;
				case CraftingTemplate.CraftingStatTypes.MissileDamage:
					if (weapon.ThrustDamageType != DamageTypes.Invalid)
					{
						textObject.SetTextVariable("THRUST_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.ThrustDamageType).ToString()));
						statDamageType = weapon.ThrustDamageType;
					}
					else if (weapon.SwingDamageType != DamageTypes.Invalid)
					{
						textObject.SetTextVariable("SWING_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.SwingDamageType).ToString()));
						statDamageType = weapon.SwingDamageType;
					}
					else
					{
						Debug.FailedAssert("Missile damage type is missing.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Crafting.cs", "GetStatDatasFromTemplate", 1192);
					}
					break;
				}
				float value = keyValuePair.Value;
				float num = Crafting.GetValueForCraftingStatForWeaponOfUsageIndex(keyValuePair.Key, craftedItemObject, weapon);
				num = MBMath.ClampFloat(num, 0f, value);
				yield return new CraftingStatData(textObject, num, value, keyValuePair.Key, statDamageType);
			}
			IEnumerator<KeyValuePair<CraftingTemplate.CraftingStatTypes, float>> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x0600060F RID: 1551 RVA: 0x00014F38 File Offset: 0x00013138
		private static float GetValueForCraftingStatForWeaponOfUsageIndex(CraftingTemplate.CraftingStatTypes craftingStatType, ItemObject item, WeaponComponentData weapon)
		{
			switch (craftingStatType)
			{
			case CraftingTemplate.CraftingStatTypes.Weight:
				return item.Weight;
			case CraftingTemplate.CraftingStatTypes.WeaponReach:
				return (float)weapon.WeaponLength;
			case CraftingTemplate.CraftingStatTypes.ThrustSpeed:
				return (float)weapon.ThrustSpeed;
			case CraftingTemplate.CraftingStatTypes.SwingSpeed:
				return (float)weapon.SwingSpeed;
			case CraftingTemplate.CraftingStatTypes.ThrustDamage:
				return (float)weapon.ThrustDamage;
			case CraftingTemplate.CraftingStatTypes.SwingDamage:
				return (float)weapon.SwingDamage;
			case CraftingTemplate.CraftingStatTypes.Handling:
				return (float)weapon.Handling;
			case CraftingTemplate.CraftingStatTypes.MissileDamage:
				return (float)weapon.MissileDamage;
			case CraftingTemplate.CraftingStatTypes.MissileSpeed:
				return (float)weapon.MissileSpeed;
			case CraftingTemplate.CraftingStatTypes.Accuracy:
				return (float)weapon.Accuracy;
			case CraftingTemplate.CraftingStatTypes.StackAmount:
				return (float)weapon.GetModifiedStackCount(null);
			default:
				throw new ArgumentOutOfRangeException("craftingStatType", craftingStatType, null);
			}
		}

		// Token: 0x06000610 RID: 1552 RVA: 0x00014FE2 File Offset: 0x000131E2
		public IEnumerable<CraftingStatData> GetStatDatas(int usageIndex)
		{
			WeaponComponentData weapon = this._craftedItemObject.GetWeaponWithUsageIndex(usageIndex);
			foreach (KeyValuePair<CraftingTemplate.CraftingStatTypes, float> keyValuePair in this.CurrentCraftingTemplate.GetStatDatas(weapon.WeaponDescriptionId, weapon.ThrustDamageType, weapon.SwingDamageType))
			{
				DamageTypes damageType = DamageTypes.Invalid;
				TextObject textObject = GameTexts.FindText("str_crafting_stat", keyValuePair.Key.ToString());
				switch (keyValuePair.Key)
				{
				case CraftingTemplate.CraftingStatTypes.ThrustDamage:
					textObject.SetTextVariable("THRUST_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.ThrustDamageType).ToString()));
					damageType = weapon.ThrustDamageType;
					break;
				case CraftingTemplate.CraftingStatTypes.SwingDamage:
					textObject.SetTextVariable("SWING_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.SwingDamageType).ToString()));
					damageType = weapon.SwingDamageType;
					break;
				case CraftingTemplate.CraftingStatTypes.MissileDamage:
					if (weapon.ThrustDamageType != DamageTypes.Invalid)
					{
						textObject.SetTextVariable("THRUST_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.ThrustDamageType).ToString()));
						damageType = weapon.ThrustDamageType;
					}
					else if (weapon.SwingDamageType != DamageTypes.Invalid)
					{
						textObject.SetTextVariable("SWING_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.SwingDamageType).ToString()));
						damageType = weapon.SwingDamageType;
					}
					else
					{
						Debug.FailedAssert("Missile damage type is missing.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Crafting.cs", "GetStatDatas", 1277);
					}
					break;
				}
				float valueForCraftingStatForWeaponOfUsageIndex = Crafting.GetValueForCraftingStatForWeaponOfUsageIndex(keyValuePair.Key, this._craftedItemObject, weapon);
				float value = keyValuePair.Value;
				yield return new CraftingStatData(textObject, valueForCraftingStatForWeaponOfUsageIndex, value, keyValuePair.Key, damageType);
			}
			IEnumerator<KeyValuePair<CraftingTemplate.CraftingStatTypes, float>> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000611 RID: 1553 RVA: 0x00014FFC File Offset: 0x000131FC
		public string GetXmlCodeForCurrentItem(ItemObject item)
		{
			string text = "";
			text = string.Concat(new object[]
			{
				text,
				"<CraftedItem id=\"",
				this.CurrentWeaponDesign.HashedCode,
				"\"\n\t\t\t\t\t\t\t name=\"",
				this.CraftedWeaponName,
				"\"\n\t\t\t\t\t\t\t crafting_template=\"",
				this.CurrentCraftingTemplate.StringId,
				"\">"
			});
			text += "\n";
			text += "<Pieces>";
			text += "\n";
			foreach (WeaponDesignElement weaponDesignElement in this.SelectedPieces)
			{
				if (weaponDesignElement.IsValid)
				{
					string text2 = "";
					if (weaponDesignElement.ScalePercentage != 100)
					{
						int scalePercentage = weaponDesignElement.ScalePercentage;
						text2 = "\n\t\t\t scale_factor=\"" + scalePercentage + "\"";
					}
					text = string.Concat(new object[]
					{
						text,
						"<Piece id=\"",
						weaponDesignElement.CraftingPiece.StringId,
						"\"\n\t\t\t Type=\"",
						weaponDesignElement.CraftingPiece.PieceType,
						"\"",
						text2,
						"/>"
					});
					text += "\n";
				}
			}
			text += "</Pieces>";
			text += "\n";
			text += "<!-- ";
			text = text + "Length: " + item.PrimaryWeapon.WeaponLength;
			text = text + " Weight: " + MathF.Round(item.Weight, 2);
			text += " -->";
			text += "\n";
			return text + "</CraftedItem>";
		}

		// Token: 0x06000612 RID: 1554 RVA: 0x000151C8 File Offset: 0x000133C8
		public bool TryGetWeaponPropertiesFromXmlCode(string xmlCode, out CraftingTemplate craftingTemplate, out ValueTuple<CraftingPiece, int>[] pieces)
		{
			bool result;
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(xmlCode);
				pieces = new ValueTuple<CraftingPiece, int>[4];
				XmlNode xmlNode = xmlDocument.SelectSingleNode("CraftedItem");
				string value = xmlNode.Attributes["crafting_template"].Value;
				craftingTemplate = CraftingTemplate.GetTemplateFromId(value);
				foreach (object obj in xmlNode.SelectSingleNode("Pieces").ChildNodes)
				{
					XmlNode xmlNode2 = (XmlNode)obj;
					CraftingPiece.PieceTypes pieceTypes = CraftingPiece.PieceTypes.Invalid;
					string pieceId = null;
					int item = 100;
					foreach (object obj2 in xmlNode2.Attributes)
					{
						XmlAttribute xmlAttribute = (XmlAttribute)obj2;
						if (xmlAttribute.Name == "Type")
						{
							pieceTypes = (CraftingPiece.PieceTypes)Enum.Parse(typeof(CraftingPiece.PieceTypes), xmlAttribute.Value);
						}
						else if (xmlAttribute.Name == "id")
						{
							pieceId = xmlAttribute.Value;
						}
						else if (xmlAttribute.Name == "scale_factor")
						{
							item = int.Parse(xmlAttribute.Value);
						}
					}
					if (pieceTypes != CraftingPiece.PieceTypes.Invalid && !string.IsNullOrEmpty(pieceId) && craftingTemplate.IsPieceTypeUsable(pieceTypes))
					{
						pieces[(int)pieceTypes] = new ValueTuple<CraftingPiece, int>(CraftingPiece.All.FirstOrDefault((CraftingPiece p) => p.StringId == pieceId), item);
					}
				}
				result = true;
			}
			catch (Exception)
			{
				craftingTemplate = null;
				pieces = null;
				result = false;
			}
			return result;
		}

		// Token: 0x06000613 RID: 1555 RVA: 0x000153C0 File Offset: 0x000135C0
		public static ItemObject CreatePreCraftedWeaponOnDeserialize(ItemObject itemObject, WeaponDesignElement[] usedPieces, string templateId, TextObject craftedWeaponName, ItemModifierGroup itemModifierGroup)
		{
			for (int i = 0; i < usedPieces.Length; i++)
			{
				if (usedPieces[i] == null)
				{
					usedPieces[i] = WeaponDesignElement.GetInvalidPieceForType((CraftingPiece.PieceTypes)i);
				}
			}
			if (TextObject.IsNullOrEmpty(craftedWeaponName))
			{
				Debug.Print("ItemObject with id = (" + itemObject.StringId + ") name is null from xml, make sure this is intended", 0, Debug.DebugColor.White, 17592186044416UL);
				craftedWeaponName = new TextObject("{=Uz1HHeKg}Crafted Random Weapon", null);
			}
			WeaponDesign weaponDesign = new WeaponDesign(CraftingTemplate.GetTemplateFromId(templateId), craftedWeaponName, usedPieces, itemObject.StringId);
			Crafting crafting = new Crafting(CraftingTemplate.GetTemplateFromId(templateId), null, craftedWeaponName);
			crafting.CurrentWeaponDesign = weaponDesign;
			crafting.CurrentItemModifierGroup = itemModifierGroup;
			crafting._history = new List<WeaponDesign> { weaponDesign };
			crafting.SetItemObject(itemObject, itemObject.StringId);
			return crafting._craftedItemObject;
		}

		// Token: 0x06000614 RID: 1556 RVA: 0x00015478 File Offset: 0x00013678
		public static ItemObject InitializePreCraftedWeaponOnLoad(ItemObject itemObject, WeaponDesign craftedData, TextObject itemName, BasicCultureObject culture)
		{
			Crafting crafting = new Crafting(craftedData.Template, culture, itemName);
			crafting.CurrentWeaponDesign = craftedData;
			crafting._history = new List<WeaponDesign> { craftedData };
			crafting.SetItemObject(itemObject, itemObject.StringId);
			return crafting._craftedItemObject;
		}

		// Token: 0x040002B6 RID: 694
		public const int WeightOfCrudeIron = 1;

		// Token: 0x040002B7 RID: 695
		public const int WeightOfIron = 2;

		// Token: 0x040002B8 RID: 696
		public const int WeightOfCompositeIron = 3;

		// Token: 0x040002B9 RID: 697
		public const int WeightOfSteel = 4;

		// Token: 0x040002BA RID: 698
		public const int WeightOfRefinedSteel = 5;

		// Token: 0x040002BB RID: 699
		public const int WeightOfCalradianSteel = 6;

		// Token: 0x040002C2 RID: 706
		private List<WeaponDesign> _history;

		// Token: 0x040002C3 RID: 707
		private int _currentHistoryIndex;

		// Token: 0x040002C4 RID: 708
		private ItemObject _craftedItemObject;

		// Token: 0x02000109 RID: 265
		public class RefiningFormula
		{
			// Token: 0x06000BB3 RID: 2995 RVA: 0x000254B4 File Offset: 0x000236B4
			public RefiningFormula(CraftingMaterials input1, int input1Count, CraftingMaterials input2, int input2Count, CraftingMaterials output, int outputCount = 1, CraftingMaterials output2 = CraftingMaterials.IronOre, int output2Count = 0)
			{
				this.Output = output;
				this.OutputCount = outputCount;
				this.Output2 = output2;
				this.Output2Count = output2Count;
				this.Input1 = input1;
				this.Input1Count = input1Count;
				this.Input2 = input2;
				this.Input2Count = input2Count;
			}

			// Token: 0x170003E9 RID: 1001
			// (get) Token: 0x06000BB4 RID: 2996 RVA: 0x00025504 File Offset: 0x00023704
			public CraftingMaterials Output { get; }

			// Token: 0x170003EA RID: 1002
			// (get) Token: 0x06000BB5 RID: 2997 RVA: 0x0002550C File Offset: 0x0002370C
			public int OutputCount { get; }

			// Token: 0x170003EB RID: 1003
			// (get) Token: 0x06000BB6 RID: 2998 RVA: 0x00025514 File Offset: 0x00023714
			public CraftingMaterials Output2 { get; }

			// Token: 0x170003EC RID: 1004
			// (get) Token: 0x06000BB7 RID: 2999 RVA: 0x0002551C File Offset: 0x0002371C
			public int Output2Count { get; }

			// Token: 0x170003ED RID: 1005
			// (get) Token: 0x06000BB8 RID: 3000 RVA: 0x00025524 File Offset: 0x00023724
			public CraftingMaterials Input1 { get; }

			// Token: 0x170003EE RID: 1006
			// (get) Token: 0x06000BB9 RID: 3001 RVA: 0x0002552C File Offset: 0x0002372C
			public int Input1Count { get; }

			// Token: 0x170003EF RID: 1007
			// (get) Token: 0x06000BBA RID: 3002 RVA: 0x00025534 File Offset: 0x00023734
			public CraftingMaterials Input2 { get; }

			// Token: 0x170003F0 RID: 1008
			// (get) Token: 0x06000BBB RID: 3003 RVA: 0x0002553C File Offset: 0x0002373C
			public int Input2Count { get; }
		}

		// Token: 0x0200010A RID: 266
		private static class CraftedItemGenerationHelper
		{
			// Token: 0x06000BBC RID: 3004 RVA: 0x00025544 File Offset: 0x00023744
			public static ItemObject GenerateCraftedItem(ItemObject item, WeaponDesign weaponDesign, ItemModifierGroup itemModifierGroup)
			{
				foreach (WeaponDesignElement weaponDesignElement in weaponDesign.UsedPieces)
				{
					if ((weaponDesignElement.IsValid && !weaponDesign.Template.Pieces.Contains(weaponDesignElement.CraftingPiece)) || (weaponDesignElement.CraftingPiece.IsInitialized && !weaponDesignElement.IsValid))
					{
						Debug.Print(weaponDesignElement.CraftingPiece.StringId + " is not a valid valid anymore.", 0, Debug.DebugColor.White, 17592186044416UL);
						return null;
					}
				}
				bool isAlternative = false;
				foreach (WeaponDescription weaponDescription in weaponDesign.Template.WeaponDescriptions)
				{
					int num = 4;
					for (int j = 0; j < weaponDesign.UsedPieces.Length; j++)
					{
						if (!weaponDesign.UsedPieces[j].IsValid)
						{
							num--;
						}
					}
					foreach (CraftingPiece craftingPiece in weaponDescription.AvailablePieces)
					{
						int pieceType = (int)craftingPiece.PieceType;
						if (weaponDesign.UsedPieces[pieceType].CraftingPiece == craftingPiece)
						{
							num--;
						}
						if (num == 0)
						{
							break;
						}
					}
					if (num <= 0)
					{
						WeaponFlags weaponFlags = weaponDescription.WeaponFlags | weaponDesign.WeaponFlags;
						WeaponComponentData weapon;
						Crafting.CraftedItemGenerationHelper.CraftingStats.FillWeapon(item, weaponDescription, weaponFlags, isAlternative, out weapon);
						item.AddWeapon(weapon, itemModifierGroup);
						isAlternative = true;
					}
				}
				return item;
			}

			// Token: 0x02000140 RID: 320
			private struct CraftingStats
			{
				// Token: 0x06000C41 RID: 3137 RVA: 0x00026CBC File Offset: 0x00024EBC
				public static void FillWeapon(ItemObject item, WeaponDescription weaponDescription, WeaponFlags weaponFlags, bool isAlternative, out WeaponComponentData filledWeapon)
				{
					filledWeapon = new WeaponComponentData(item, weaponDescription.WeaponClass, weaponFlags);
					Crafting.CraftedItemGenerationHelper.CraftingStats craftingStats = new Crafting.CraftedItemGenerationHelper.CraftingStats
					{
						_craftedData = item.WeaponDesign,
						_weaponDescription = weaponDescription
					};
					craftingStats.CalculateStats();
					craftingStats.SetWeaponData(filledWeapon, isAlternative);
				}

				// Token: 0x06000C42 RID: 3138 RVA: 0x00026D0C File Offset: 0x00024F0C
				private void CalculateStats()
				{
					WeaponDesign craftedData = this._craftedData;
					this._stoppingTorque = 10f;
					this._armInertia = 2.9f;
					if (this._weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.NotUsableWithOneHand))
					{
						this._stoppingTorque *= 1.5f;
						this._armInertia *= 1.4f;
					}
					if (this._weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.WideGrip))
					{
						this._stoppingTorque *= 1.5f;
						this._armInertia *= 1.4f;
					}
					this._currentWeaponWeight = 0f;
					this._currentWeaponReach = 0f;
					this._currentWeaponCenterOfMass = 0f;
					this._currentWeaponInertia = 0f;
					this._currentWeaponInertiaAroundShoulder = 0f;
					this._currentWeaponInertiaAroundGrip = 0f;
					this._currentWeaponSwingSpeed = 1f;
					this._currentWeaponThrustSpeed = 1f;
					this._currentWeaponSwingDamage = 0f;
					this._currentWeaponThrustDamage = 0f;
					this._currentWeaponHandling = 1f;
					this._currentWeaponTier = WeaponComponentData.WeaponTiers.Tier1;
					this._currentWeaponWeight = MathF.Round(craftedData.UsedPieces.Sum((WeaponDesignElement selectedUsablePiece) => selectedUsablePiece.ScaledWeight), 2);
					this._currentWeaponReach = MathF.Round(this._craftedData.CraftedWeaponLength, 2);
					this._currentWeaponCenterOfMass = this.CalculateCenterOfMass();
					this._currentWeaponInertia = this.CalculateWeaponInertia();
					this._currentWeaponInertiaAroundShoulder = Crafting.CraftedItemGenerationHelper.CraftingStats.ParallelAxis(this._currentWeaponInertia, this._currentWeaponWeight, 0.5f + this._currentWeaponCenterOfMass);
					this._currentWeaponInertiaAroundGrip = Crafting.CraftedItemGenerationHelper.CraftingStats.ParallelAxis(this._currentWeaponInertia, this._currentWeaponWeight, this._currentWeaponCenterOfMass);
					this._currentWeaponSwingSpeed = this.CalculateSwingSpeed();
					this._currentWeaponThrustSpeed = this.CalculateThrustSpeed();
					this._currentWeaponHandling = (float)this.CalculateAgility();
					this._currentWeaponTier = this.CalculateWeaponTier();
					this._swingDamageFactor = this._craftedData.UsedPieces[0].CraftingPiece.BladeData.SwingDamageFactor;
					this._thrustDamageFactor = this._craftedData.UsedPieces[0].CraftingPiece.BladeData.ThrustDamageFactor;
					if (this._weaponDescription.WeaponClass == WeaponClass.ThrowingAxe || this._weaponDescription.WeaponClass == WeaponClass.ThrowingKnife || this._weaponDescription.WeaponClass == WeaponClass.Javelin)
					{
						this._currentWeaponSwingDamage = 0f;
						this.CalculateMissileDamage(out this._currentWeaponThrustDamage);
					}
					else
					{
						this.CalculateSwingBaseDamage(out this._currentWeaponSwingDamage);
						this.CalculateThrustBaseDamage(out this._currentWeaponThrustDamage, false);
					}
					this._currentWeaponSweetSpot = this.CalculateSweetSpot();
				}

				// Token: 0x06000C43 RID: 3139 RVA: 0x00026FB0 File Offset: 0x000251B0
				private void SetWeaponData(WeaponComponentData weapon, bool isAlternative)
				{
					BladeData bladeData = this._craftedData.UsedPieces[0].CraftingPiece.BladeData;
					short maxDataValue = 0;
					string passBySoundCode = "";
					int accuracy = 0;
					int missileSpeed = 0;
					MatrixFrame stickingFrame = MatrixFrame.Identity;
					short reloadPhaseCount = 1;
					Vec3 vec;
					if (this._weaponDescription.WeaponClass == WeaponClass.Javelin || this._weaponDescription.WeaponClass == WeaponClass.ThrowingAxe || this._weaponDescription.WeaponClass == WeaponClass.ThrowingKnife)
					{
						short num = (isAlternative ? 1 : bladeData.StackAmount);
						switch (this._weaponDescription.WeaponClass)
						{
						case WeaponClass.ThrowingAxe:
							maxDataValue = num;
							accuracy = 93;
							passBySoundCode = "event:/mission/combat/throwing/passby";
							break;
						case WeaponClass.ThrowingKnife:
							maxDataValue = num;
							accuracy = 95;
							passBySoundCode = "event:/mission/combat/throwing/passby";
							break;
						case WeaponClass.Javelin:
							maxDataValue = num;
							accuracy = 92;
							passBySoundCode = "event:/mission/combat/missile/passby";
							break;
						}
						missileSpeed = MathF.Floor(this.CalculateMissileSpeed());
						Mat3 identity = Mat3.Identity;
						switch (this._weaponDescription.WeaponClass)
						{
						case WeaponClass.ThrowingAxe:
						{
							float bladeWidth = this._craftedData.UsedPieces[0].CraftingPiece.BladeData.BladeWidth;
							float num2 = this._craftedData.PiecePivotDistances[0];
							float scaledDistanceToNextPiece = this._craftedData.UsedPieces[0].ScaledDistanceToNextPiece;
							identity.RotateAboutUp(1.5707964f);
							identity.RotateAboutSide(-(15f + scaledDistanceToNextPiece * 3f / num2 * 25f) * 0.017453292f);
							vec = -identity.u * (num2 + scaledDistanceToNextPiece * 0.6f) - identity.f * bladeWidth * 0.8f;
							stickingFrame = new MatrixFrame(ref identity, ref vec);
							break;
						}
						case WeaponClass.ThrowingKnife:
							identity.RotateAboutForward(-1.5707964f);
							vec = Vec3.Side * this._currentWeaponReach;
							stickingFrame = new MatrixFrame(ref identity, ref vec);
							break;
						case WeaponClass.Javelin:
							identity.RotateAboutSide(1.5707964f);
							vec = -Vec3.Up * this._currentWeaponReach;
							stickingFrame = new MatrixFrame(ref identity, ref vec);
							break;
						}
					}
					if (this._weaponDescription.WeaponClass == WeaponClass.Arrow || this._weaponDescription.WeaponClass == WeaponClass.Bolt)
					{
						stickingFrame.rotation.RotateAboutSide(1.5707964f);
					}
					Vec3 zero = Vec3.Zero;
					if (this._weaponDescription.WeaponClass == WeaponClass.ThrowingAxe)
					{
						zero = new Vec3(0f, 18f, 0f, -1f);
					}
					else if (this._weaponDescription.WeaponClass == WeaponClass.ThrowingKnife)
					{
						zero = new Vec3(0f, 24f, 0f, -1f);
					}
					weapon.Init(this._weaponDescription.StringId, bladeData.PhysicsMaterial, this.GetItemUsage(), bladeData.ThrustDamageType, bladeData.SwingDamageType, this.GetWeaponHandArmorBonus(), (int)(this._currentWeaponReach * 100f), MathF.Round(this.GetWeaponBalance(), 2), this._currentWeaponInertia, this._currentWeaponCenterOfMass, MathF.Floor(this._currentWeaponHandling), MathF.Round(this._swingDamageFactor, 2), MathF.Round(this._thrustDamageFactor, 2), maxDataValue, passBySoundCode, accuracy, missileSpeed, stickingFrame, this.GetAmmoClass(), this._currentWeaponSweetSpot, MathF.Floor(this._currentWeaponSwingSpeed * 4.5454545f), MathF.Round(this._currentWeaponSwingDamage), MathF.Floor(this._currentWeaponThrustSpeed * 11.764706f), MathF.Round(this._currentWeaponThrustDamage), zero, this._currentWeaponTier, reloadPhaseCount);
					Mat3 identity2 = Mat3.Identity;
					Vec3 vec2 = Vec3.Zero;
					if (this._weaponDescription.RotatedInHand)
					{
						identity2.RotateAboutSide(3.1415927f);
					}
					if (this._weaponDescription.UseCenterOfMassAsHandBase)
					{
						vec2 = -Vec3.Up * this._currentWeaponCenterOfMass;
					}
					vec = identity2.TransformToParent(vec2);
					weapon.SetFrame(new MatrixFrame(ref identity2, ref vec));
				}

				// Token: 0x06000C44 RID: 3140 RVA: 0x00027388 File Offset: 0x00025588
				private float CalculateSweetSpot()
				{
					float num = -1f;
					float result = -1f;
					for (int i = 0; i < 100; i++)
					{
						float num2 = 0.01f * (float)i;
						float num3 = CombatStatCalculator.CalculateStrikeMagnitudeForSwing(this._currentWeaponSwingSpeed, num2, this._currentWeaponWeight, this._currentWeaponReach, this._currentWeaponInertia, this._currentWeaponCenterOfMass, 0f);
						if (num < num3)
						{
							num = num3;
							result = num2;
						}
					}
					return result;
				}

				// Token: 0x06000C45 RID: 3141 RVA: 0x000273F0 File Offset: 0x000255F0
				private float CalculateCenterOfMass()
				{
					float num = 0f;
					float num2 = 0f;
					float num3 = 0f;
					foreach (PieceData pieceData in this._craftedData.Template.BuildOrders)
					{
						CraftingPiece.PieceTypes pieceType = pieceData.PieceType;
						WeaponDesignElement weaponDesignElement = this._craftedData.UsedPieces[(int)pieceType];
						if (weaponDesignElement.IsValid)
						{
							float scaledWeight = weaponDesignElement.ScaledWeight;
							float num4 = 0f;
							if (pieceData.Order < 0)
							{
								num4 -= (num3 + (weaponDesignElement.ScaledLength - weaponDesignElement.ScaledCenterOfMass)) * scaledWeight;
								num3 += weaponDesignElement.ScaledLength;
							}
							else
							{
								num4 += (num2 + weaponDesignElement.ScaledCenterOfMass) * scaledWeight;
								num2 += weaponDesignElement.ScaledLength;
							}
							num += num4;
						}
					}
					return num / this._currentWeaponWeight - (this._craftedData.UsedPieces[2].ScaledDistanceToPreviousPiece - this._craftedData.UsedPieces[2].ScaledPieceOffset);
				}

				// Token: 0x06000C46 RID: 3142 RVA: 0x000274F4 File Offset: 0x000256F4
				private float CalculateWeaponInertia()
				{
					float num = -this._currentWeaponCenterOfMass;
					float num2 = 0f;
					foreach (PieceData pieceData in this._craftedData.Template.BuildOrders)
					{
						WeaponDesignElement weaponDesignElement = this._craftedData.UsedPieces[(int)pieceData.PieceType];
						if (weaponDesignElement.IsValid)
						{
							float weightMultiplier = 1f;
							num2 += Crafting.CraftedItemGenerationHelper.CraftingStats.ParallelAxis(weaponDesignElement, num, weightMultiplier);
							num += weaponDesignElement.ScaledLength;
						}
					}
					return num2;
				}

				// Token: 0x06000C47 RID: 3143 RVA: 0x00027574 File Offset: 0x00025774
				private float CalculateSwingSpeed()
				{
					double num = 1.0 * (double)this._currentWeaponInertiaAroundShoulder + 0.9;
					double num2 = 170.0;
					double num3 = 90.0;
					double num4 = 27.0;
					double num5 = 15.0;
					double num6 = 7.0;
					if (this._weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.NotUsableWithOneHand))
					{
						if (this._weaponDescription.WeaponFlags.HasAnyFlag(WeaponFlags.WideGrip))
						{
							num += 1.5;
							num6 *= 4.0;
							num5 *= 1.7;
							num3 *= 1.3;
							num2 *= 1.15;
						}
						else
						{
							num += 1.0;
							num6 *= 2.4;
							num5 *= 1.3;
							num3 *= 1.35;
							num2 *= 1.15;
						}
					}
					num4 = MathF.Max(1.0, num4 - num);
					num5 = MathF.Max(1.0, num5 - num);
					num6 = MathF.Max(1.0, num6 - num);
					double num7;
					double num8;
					this.SimulateSwingLayer(1.5, 200.0, num4, 2.0 + num, out num7, out num8);
					double num9;
					double num10;
					this.SimulateSwingLayer(1.5, num2, num5, 1.0 + num, out num9, out num10);
					double num11;
					double num12;
					this.SimulateSwingLayer(1.5, num3, num6, 0.5 + num, out num11, out num12);
					double num13 = 0.33 * (num8 + num10 + num12);
					return (float)(20.8 / num13);
				}

				// Token: 0x06000C48 RID: 3144 RVA: 0x00027748 File Offset: 0x00025948
				private float CalculateThrustSpeed()
				{
					double num = 1.8 + (double)this._currentWeaponWeight + (double)this._currentWeaponInertiaAroundGrip * 0.2;
					double num2 = 170.0;
					double num3 = 90.0;
					double num4 = 24.0;
					double num5 = 15.0;
					if (this._weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.NotUsableWithOneHand) && !this._weaponDescription.WeaponFlags.HasAnyFlag(WeaponFlags.WideGrip))
					{
						num += 0.6;
						num5 *= 1.9;
						num4 *= 1.1;
						num3 *= 1.2;
						num2 *= 1.05;
					}
					else if (this._weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.NotUsableWithOneHand | WeaponFlags.WideGrip))
					{
						num += 0.9;
						num5 *= 2.1;
						num4 *= 1.2;
						num3 *= 1.2;
						num2 *= 1.05;
					}
					double num6;
					double num7;
					this.SimulateThrustLayer(0.6, 250.0, 48.0, 4.0 + num, out num6, out num7);
					double num8;
					double num9;
					this.SimulateThrustLayer(0.6, num2, num4, 2.0 + num, out num8, out num9);
					double num10;
					double num11;
					this.SimulateThrustLayer(0.6, num3, num5, 0.5 + num, out num10, out num11);
					double num12 = 0.33 * (num7 + num9 + num11);
					return (float)(3.8500000000000005 / num12);
				}

				// Token: 0x06000C49 RID: 3145 RVA: 0x000278F4 File Offset: 0x00025AF4
				private void CalculateSwingBaseDamage(out float damage)
				{
					float num = 0f;
					for (float num2 = 0.93f; num2 > 0.5f; num2 -= 0.05f)
					{
						float num3 = CombatStatCalculator.CalculateBaseBlowMagnitudeForSwing(this._currentWeaponSwingSpeed, this._currentWeaponReach, this._currentWeaponWeight, this._currentWeaponInertia, this._currentWeaponCenterOfMass, num2, 0f);
						if (num3 > num)
						{
							num = num3;
						}
					}
					damage = num * this._swingDamageFactor;
				}

				// Token: 0x06000C4A RID: 3146 RVA: 0x0002795C File Offset: 0x00025B5C
				private void CalculateThrustBaseDamage(out float damage, bool isThrown = false)
				{
					float num = CombatStatCalculator.CalculateStrikeMagnitudeForThrust(this._currentWeaponThrustSpeed, this._currentWeaponWeight, 0f, isThrown);
					damage = num * this._thrustDamageFactor;
				}

				// Token: 0x06000C4B RID: 3147 RVA: 0x0002798C File Offset: 0x00025B8C
				private void CalculateMissileDamage(out float damage)
				{
					switch (this._weaponDescription.WeaponClass)
					{
					case WeaponClass.ThrowingAxe:
						this.CalculateSwingBaseDamage(out damage);
						damage *= 2f;
						return;
					case WeaponClass.ThrowingKnife:
						this.CalculateThrustBaseDamage(out damage, true);
						damage *= 3.3f;
						return;
					case WeaponClass.Javelin:
						this.CalculateThrustBaseDamage(out damage, true);
						damage *= 9f;
						return;
					default:
						damage = 0f;
						Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Crafting.cs", "CalculateMissileDamage", 508);
						return;
					}
				}

				// Token: 0x06000C4C RID: 3148 RVA: 0x00027A14 File Offset: 0x00025C14
				private WeaponComponentData.WeaponTiers CalculateWeaponTier()
				{
					int num = 0;
					int num2 = 0;
					foreach (WeaponDesignElement weaponDesignElement in from ucp in this._craftedData.UsedPieces
						where ucp.IsValid
						select ucp)
					{
						num += weaponDesignElement.CraftingPiece.PieceTier;
						num2++;
					}
					WeaponComponentData.WeaponTiers result;
					if (Enum.TryParse<WeaponComponentData.WeaponTiers>(((int)((float)num / (float)num2)).ToString(), out result))
					{
						return result;
					}
					Debug.FailedAssert("Couldn't calculate weapon tier", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Crafting.cs", "CalculateWeaponTier", 529);
					return WeaponComponentData.WeaponTiers.Tier1;
				}

				// Token: 0x06000C4D RID: 3149 RVA: 0x00027AD4 File Offset: 0x00025CD4
				private string GetItemUsage()
				{
					List<string> list = this._weaponDescription.ItemUsageFeatures.Split(new char[] { ':' }).ToList<string>();
					foreach (WeaponDesignElement weaponDesignElement in from ucp in this._craftedData.UsedPieces
						where ucp.IsValid
						select ucp)
					{
						foreach (string text in weaponDesignElement.CraftingPiece.ItemUsageFeaturesToExclude.Split(new char[] { ':' }))
						{
							if (!string.IsNullOrEmpty(text))
							{
								list.Remove(text);
							}
						}
					}
					string text2 = "";
					for (int j = 0; j < list.Count; j++)
					{
						text2 += list[j];
						if (j < list.Count - 1)
						{
							text2 += "_";
						}
					}
					return text2;
				}

				// Token: 0x06000C4E RID: 3150 RVA: 0x00027BEC File Offset: 0x00025DEC
				private float CalculateMissileSpeed()
				{
					if (this._weaponDescription.WeaponClass == WeaponClass.ThrowingAxe)
					{
						return this._currentWeaponThrustSpeed * 3.2f;
					}
					if (this._weaponDescription.WeaponClass == WeaponClass.ThrowingKnife)
					{
						return this._currentWeaponThrustSpeed * 3.9f;
					}
					if (this._weaponDescription.WeaponClass == WeaponClass.Javelin)
					{
						return this._currentWeaponThrustSpeed * 3.6f;
					}
					Debug.FailedAssert("Weapon is not a missile.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Crafting.cs", "CalculateMissileSpeed", 580);
					return 10f;
				}

				// Token: 0x06000C4F RID: 3151 RVA: 0x00027C6C File Offset: 0x00025E6C
				private int CalculateAgility()
				{
					float num = this._currentWeaponInertiaAroundGrip;
					if (this._weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.NotUsableWithOneHand))
					{
						num *= 0.5f;
						num += 0.9f;
					}
					else if (this._weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.WideGrip))
					{
						num *= 0.4f;
						num += 1f;
					}
					else
					{
						num += 0.7f;
					}
					float num2 = MathF.Pow(1f / num, 0.55f);
					num2 *= 1f;
					return MathF.Round(100f * num2);
				}

				// Token: 0x06000C50 RID: 3152 RVA: 0x00027CFC File Offset: 0x00025EFC
				private float GetWeaponBalance()
				{
					return MBMath.ClampFloat((this._currentWeaponSwingSpeed * 4.5454545f - 70f) / 30f, 0f, 1f);
				}

				// Token: 0x06000C51 RID: 3153 RVA: 0x00027D25 File Offset: 0x00025F25
				private int GetWeaponHandArmorBonus()
				{
					WeaponDesignElement weaponDesignElement = this._craftedData.UsedPieces[1];
					if (weaponDesignElement == null)
					{
						return 0;
					}
					return weaponDesignElement.CraftingPiece.ArmorBonus;
				}

				// Token: 0x06000C52 RID: 3154 RVA: 0x00027D44 File Offset: 0x00025F44
				private WeaponClass GetAmmoClass()
				{
					if (this._weaponDescription.WeaponClass != WeaponClass.ThrowingKnife && this._weaponDescription.WeaponClass != WeaponClass.ThrowingAxe && this._weaponDescription.WeaponClass != WeaponClass.Javelin)
					{
						return WeaponClass.Undefined;
					}
					return this._weaponDescription.WeaponClass;
				}

				// Token: 0x06000C53 RID: 3155 RVA: 0x00027D80 File Offset: 0x00025F80
				private static float ParallelAxis(WeaponDesignElement selectedPiece, float offset, float weightMultiplier)
				{
					float inertia = selectedPiece.CraftingPiece.Inertia;
					float offsetFromCm = offset + selectedPiece.CraftingPiece.CenterOfMass;
					float mass = selectedPiece.ScaledWeight * weightMultiplier;
					return Crafting.CraftedItemGenerationHelper.CraftingStats.ParallelAxis(inertia, mass, offsetFromCm);
				}

				// Token: 0x06000C54 RID: 3156 RVA: 0x00027DB6 File Offset: 0x00025FB6
				private static float ParallelAxis(float inertiaAroundCm, float mass, float offsetFromCm)
				{
					return inertiaAroundCm + mass * offsetFromCm * offsetFromCm;
				}

				// Token: 0x06000C55 RID: 3157 RVA: 0x00027DC0 File Offset: 0x00025FC0
				private void SimulateSwingLayer(double angleSpan, double usablePower, double maxUsableTorque, double inertia, out double finalSpeed, out double finalTime)
				{
					double num = 0.0;
					double num2 = 0.01;
					double num3 = 0.0;
					double num4 = 3.9 * (double)this._currentWeaponReach * (this._weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.WideGrip) ? 1.0 : 0.3);
					while (num < angleSpan)
					{
						double num5 = usablePower / num2;
						if (num5 > maxUsableTorque)
						{
							num5 = maxUsableTorque;
						}
						num5 -= num2 * num4;
						double num6 = 0.009999999776482582 * num5 / inertia;
						num2 += num6;
						num += num2 * 0.009999999776482582;
						num3 += 0.009999999776482582;
					}
					finalSpeed = num2;
					finalTime = num3;
				}

				// Token: 0x06000C56 RID: 3158 RVA: 0x00027E7C File Offset: 0x0002607C
				private void SimulateThrustLayer(double distance, double usablePower, double maxUsableForce, double mass, out double finalSpeed, out double finalTime)
				{
					double num = 0.0;
					double num2 = 0.01;
					double num3 = 0.0;
					while (num < distance)
					{
						double num4 = usablePower / num2;
						if (num4 > maxUsableForce)
						{
							num4 = maxUsableForce;
						}
						double num5 = 0.01 * num4 / mass;
						num2 += num5;
						num += num2 * 0.01;
						num3 += 0.01;
					}
					finalSpeed = num2;
					finalTime = num3;
				}

				// Token: 0x04000823 RID: 2083
				private WeaponDesign _craftedData;

				// Token: 0x04000824 RID: 2084
				private WeaponDescription _weaponDescription;

				// Token: 0x04000825 RID: 2085
				private float _stoppingTorque;

				// Token: 0x04000826 RID: 2086
				private float _armInertia;

				// Token: 0x04000827 RID: 2087
				private float _swingDamageFactor;

				// Token: 0x04000828 RID: 2088
				private float _thrustDamageFactor;

				// Token: 0x04000829 RID: 2089
				private float _currentWeaponWeight;

				// Token: 0x0400082A RID: 2090
				private float _currentWeaponReach;

				// Token: 0x0400082B RID: 2091
				private float _currentWeaponSweetSpot;

				// Token: 0x0400082C RID: 2092
				private float _currentWeaponCenterOfMass;

				// Token: 0x0400082D RID: 2093
				private float _currentWeaponInertia;

				// Token: 0x0400082E RID: 2094
				private float _currentWeaponInertiaAroundShoulder;

				// Token: 0x0400082F RID: 2095
				private float _currentWeaponInertiaAroundGrip;

				// Token: 0x04000830 RID: 2096
				private float _currentWeaponSwingSpeed;

				// Token: 0x04000831 RID: 2097
				private float _currentWeaponThrustSpeed;

				// Token: 0x04000832 RID: 2098
				private float _currentWeaponHandling;

				// Token: 0x04000833 RID: 2099
				private float _currentWeaponSwingDamage;

				// Token: 0x04000834 RID: 2100
				private float _currentWeaponThrustDamage;

				// Token: 0x04000835 RID: 2101
				private WeaponComponentData.WeaponTiers _currentWeaponTier;
			}
		}
	}
}
