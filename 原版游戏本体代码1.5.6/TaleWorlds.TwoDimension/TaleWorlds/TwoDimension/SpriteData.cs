using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension;

public class SpriteData
{
	private struct SpriteDataLoadResult
	{
		public Dictionary<string, SpritePart> SpritePartNames;

		public Dictionary<string, Sprite> SpriteNames;

		public Dictionary<string, SpriteCategory> SpriteCategories;
	}

	public Dictionary<string, SpritePart> SpriteParts { get; private set; }

	public Dictionary<string, Sprite> Sprites { get; private set; }

	public Dictionary<string, SpriteCategory> SpriteCategories { get; private set; }

	public string Name { get; private set; }

	public SpriteData(string name)
	{
		Name = name;
		SpriteParts = new Dictionary<string, SpritePart>();
		Sprites = new Dictionary<string, Sprite>();
		SpriteCategories = new Dictionary<string, SpriteCategory>();
	}

	public Sprite GetSprite(string name)
	{
		if (Sprites.TryGetValue(name, out var value))
		{
			return value;
		}
		return null;
	}

	public bool SpriteExists(string spriteName)
	{
		return GetSprite(spriteName) != null;
	}

	private static SpriteDataLoadResult LoadFromDepot(ResourceDepot resourceDepot, string name)
	{
		XmlDocument spriteData = new XmlDocument();
		SpriteDataLoadResult loadResult = new SpriteDataLoadResult
		{
			SpriteCategories = new Dictionary<string, SpriteCategory>(),
			SpriteNames = new Dictionary<string, Sprite>(),
			SpritePartNames = new Dictionary<string, SpritePart>()
		};
		foreach (string item in resourceDepot.GetFilesEndingWith(name + ".xml"))
		{
			try
			{
				LoadSpriteDataFromFile(spriteData, item, ref loadResult);
			}
			catch (Exception)
			{
				Debug.FailedAssert("Failed to load sprite data from file: " + item, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.TwoDimension\\SpriteData.cs", "LoadFromDepot", 72);
			}
		}
		return loadResult;
	}

	private static SpriteDataLoadResult LoadSpriteDataFromFile(XmlDocument spriteData, string filePath, ref SpriteDataLoadResult loadResult)
	{
		StreamReader txtReader = new StreamReader(filePath);
		spriteData.Load(txtReader);
		XmlElement? xmlElement = spriteData["SpriteData"];
		XmlNode xmlNode = xmlElement["SpriteCategories"];
		XmlNode xmlNode2 = xmlElement["SpriteParts"];
		XmlNode xmlNode3 = xmlElement["Sprites"];
		foreach (XmlNode item in xmlNode)
		{
			string innerText = item["Name"].InnerText;
			int num = Convert.ToInt32(item["SpriteSheetCount"].InnerText);
			bool alwaysLoad = false;
			Vec2i[] array = new Vec2i[num];
			foreach (XmlNode childNode in item.ChildNodes)
			{
				if (childNode.Name == "SpriteSheetSize")
				{
					int num2 = Convert.ToInt32(childNode.Attributes["ID"].InnerText);
					int x = Convert.ToInt32(childNode.Attributes["Width"].InnerText);
					int y = Convert.ToInt32(childNode.Attributes["Height"].InnerText);
					array[num2 - 1] = new Vec2i(x, y);
				}
				else if (childNode.Name == "AlwaysLoad")
				{
					alwaysLoad = true;
				}
			}
			SpriteCategory spriteCategory = new SpriteCategory(innerText, num, alwaysLoad)
			{
				SheetSizes = array
			};
			loadResult.SpriteCategories[spriteCategory.Name] = spriteCategory;
		}
		foreach (XmlNode item2 in xmlNode2)
		{
			string innerText2 = item2["Name"].InnerText;
			int width = Convert.ToInt32(item2["Width"].InnerText);
			int height = Convert.ToInt32(item2["Height"].InnerText);
			string innerText3 = item2["CategoryName"].InnerText;
			SpriteCategory category = loadResult.SpriteCategories[innerText3];
			SpritePart spritePart = new SpritePart(innerText2, category, width, height)
			{
				SheetID = Convert.ToInt32(item2["SheetID"].InnerText),
				SheetX = Convert.ToInt32(item2["SheetX"].InnerText),
				SheetY = Convert.ToInt32(item2["SheetY"].InnerText)
			};
			loadResult.SpritePartNames[spritePart.Name] = spritePart;
			spritePart.UpdateInitValues();
		}
		foreach (XmlNode item3 in xmlNode3)
		{
			Sprite sprite = null;
			if (item3.Name == "GenericSprite")
			{
				string innerText4 = item3["Name"].InnerText;
				string innerText5 = item3["SpritePartName"].InnerText;
				SpritePart spritePart2 = loadResult.SpritePartNames[innerText5];
				sprite = new SpriteGeneric(innerText4, spritePart2, in SpriteNinePatchParameters.Empty);
			}
			else if (item3.Name == "NineRegionSprite")
			{
				string innerText6 = item3["Name"].InnerText;
				string innerText7 = item3["SpritePartName"].InnerText;
				int leftWidth = Convert.ToInt32(item3["LeftWidth"].InnerText);
				int rightWidth = Convert.ToInt32(item3["RightWidth"].InnerText);
				int topHeight = Convert.ToInt32(item3["TopHeight"].InnerText);
				int bottomHeight = Convert.ToInt32(item3["BottomHeight"].InnerText);
				SpriteNinePatchParameters ninePatchParameters = new SpriteNinePatchParameters(leftWidth, rightWidth, topHeight, bottomHeight);
				sprite = new SpriteGeneric(innerText6, loadResult.SpritePartNames[innerText7], in ninePatchParameters);
			}
			loadResult.SpriteNames[sprite.Name] = sprite;
		}
		return loadResult;
	}

	public void Load(ResourceDepot resourceDepot)
	{
		SpriteDataLoadResult spriteDataLoadResult = LoadFromDepot(resourceDepot, Name);
		SpriteCategories = spriteDataLoadResult.SpriteCategories;
		Sprites = spriteDataLoadResult.SpriteNames;
		SpriteParts = spriteDataLoadResult.SpritePartNames;
	}

	public void Reload(ResourceDepot resourceDepot, ITwoDimensionResourceContext resourceContext)
	{
		SpriteDataLoadResult spriteDataLoadResult = LoadFromDepot(resourceDepot, Name);
		Sprites = spriteDataLoadResult.SpriteNames;
		SpriteParts = spriteDataLoadResult.SpritePartNames;
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (KeyValuePair<string, SpriteCategory> spriteCategory2 in SpriteCategories)
		{
			bool flag = false;
			foreach (KeyValuePair<string, SpriteCategory> spriteCategory3 in spriteDataLoadResult.SpriteCategories)
			{
				if (spriteCategory3.Key == spriteCategory2.Key)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Add(spriteCategory2.Key);
			}
		}
		foreach (KeyValuePair<string, SpriteCategory> spriteCategory4 in spriteDataLoadResult.SpriteCategories)
		{
			bool flag2 = false;
			foreach (KeyValuePair<string, SpriteCategory> spriteCategory5 in SpriteCategories)
			{
				if (spriteCategory4.Key == spriteCategory5.Key)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				list2.Add(spriteCategory4.Key);
			}
		}
		foreach (string item in list)
		{
			SpriteCategories[item].Unload();
			SpriteCategories.Remove(item);
		}
		foreach (string item2 in list2)
		{
			SpriteCategory spriteCategory = spriteDataLoadResult.SpriteCategories[item2];
			SpriteCategories.Add(item2, spriteCategory);
			if (spriteCategory.AlwaysLoad)
			{
				spriteCategory.Load(resourceContext, resourceDepot);
			}
		}
		foreach (KeyValuePair<string, SpriteCategory> spriteCategory6 in SpriteCategories)
		{
			if (spriteDataLoadResult.SpriteCategories.TryGetValue(spriteCategory6.Key, out var value))
			{
				spriteCategory6.Value.Reload(resourceContext, resourceDepot, value);
			}
		}
		foreach (KeyValuePair<string, SpritePart> spritePart in SpriteParts)
		{
			if (SpriteCategories.TryGetValue(spritePart.Value.Category.Name, out var value2))
			{
				spritePart.Value.Category = value2;
			}
		}
	}
}
