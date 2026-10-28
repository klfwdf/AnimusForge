using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension;

public class Font
{
	private int _realSize;

	public string Name { get; private set; }

	public int Size { get; private set; }

	public int LineHeight { get; private set; }

	public int Base { get; private set; }

	public int CharacterCount { get; private set; }

	public float SmoothingConstant { get; private set; }

	public float CustomScale { get; private set; } = 1f;

	public bool Smooth { get; private set; }

	public SpritePart FontSprite { get; private set; }

	public Dictionary<int, BitmapFontCharacter> Characters { get; private set; }

	public Font(string name)
	{
		Name = name;
		Characters = new Dictionary<int, BitmapFontCharacter>();
	}

	public bool TryLoadFontFromPath(string path, SpriteData spriteData)
	{
		Debug.Print("Loading " + Name + " font, at: " + path);
		try
		{
			LoadFromPathAux(path, spriteData);
			return true;
		}
		catch (Exception arg)
		{
			Debug.FailedAssert("Failed to load font:" + Name + " at path: " + path, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.TwoDimension\\BitmapFont\\Font.cs", "TryLoadFontFromPath", 54);
			Debug.Print($"Failed to load font:{Name} at path: {path}. Error:{arg}");
			return false;
		}
	}

	private void LoadFromPathAux(string path, SpriteData spriteData)
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(path);
		XmlElement? xmlElement = xmlDocument["font"];
		XmlElement xmlElement2 = xmlElement["info"];
		_realSize = Math.Abs(Convert.ToInt32(xmlElement2.Attributes["size"].Value));
		Smooth = true;
		if (xmlElement2.Attributes["smooth"] != null)
		{
			Smooth = Convert.ToBoolean(Convert.ToInt32(xmlElement2.Attributes["smooth"].Value));
		}
		SmoothingConstant = 0.47f;
		if (xmlElement2.Attributes["smoothingConstant"] != null)
		{
			SmoothingConstant = Convert.ToSingle(xmlElement2.Attributes["smoothingConstant"].Value, CultureInfo.InvariantCulture);
		}
		if (xmlElement2.Attributes["customScale"] != null)
		{
			CustomScale = Convert.ToSingle(xmlElement2.Attributes["customScale"].Value, CultureInfo.InvariantCulture);
		}
		XmlElement xmlElement3 = xmlElement["common"];
		LineHeight = Convert.ToInt32(xmlElement3.Attributes["lineHeight"].Value);
		Base = Convert.ToInt32(xmlElement3.Attributes["base"].Value);
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(xmlElement["pages"].ChildNodes[0].Attributes["file"].Value);
		XmlElement xmlElement4 = xmlElement["chars"];
		CharacterCount = Convert.ToInt32(xmlElement4.Attributes["count"].Value);
		string path2 = Path.ChangeExtension(path, ".bfnt");
		if (File.Exists(path2))
		{
			using System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(File.Open(path2, FileMode.Open, FileAccess.Read));
			for (int i = 0; i < CharacterCount; i++)
			{
				GCHandle gCHandle = GCHandle.Alloc(binaryReader.ReadBytes(Marshal.SizeOf(typeof(BitmapFontCharacter))), GCHandleType.Pinned);
				BitmapFontCharacter value = (BitmapFontCharacter)Marshal.PtrToStructure(gCHandle.AddrOfPinnedObject(), typeof(BitmapFontCharacter));
				Characters.Add(value.ID, value);
				gCHandle.Free();
			}
		}
		else
		{
			BitmapFontCharacter value2 = default(BitmapFontCharacter);
			for (int j = 0; j < CharacterCount; j++)
			{
				XmlNode xmlNode = xmlElement4.ChildNodes[j];
				value2.ID = Convert.ToInt32(xmlNode.Attributes["id"].Value);
				value2.X = Convert.ToInt32(xmlNode.Attributes["x"].Value);
				value2.Y = Convert.ToInt32(xmlNode.Attributes["y"].Value);
				value2.Width = Convert.ToInt32(xmlNode.Attributes["width"].Value);
				value2.Height = Convert.ToInt32(xmlNode.Attributes["height"].Value);
				value2.XOffset = Convert.ToInt32(xmlNode.Attributes["xoffset"].Value);
				value2.YOffset = Convert.ToInt32(xmlNode.Attributes["yoffset"].Value);
				value2.XAdvance = Convert.ToInt32(xmlNode.Attributes["xadvance"].Value);
				Characters.Add(value2.ID, value2);
			}
		}
		SpritePart spritePart = null;
		spritePart = (spriteData.GetSprite(fileNameWithoutExtension) as SpriteGeneric)?.SpritePart;
		FontSprite = spritePart;
		Size = (int)((float)_realSize / CustomScale);
	}

	public float GetWordWidth(string word, float extraPadding)
	{
		float num = 0f;
		for (int i = 0; i < word.Length; i++)
		{
			num += GetCharacterWidth(word[i], extraPadding);
		}
		return num;
	}

	public float GetCharacterWidth(char character, float extraPadding)
	{
		int key = character;
		if (!Characters.ContainsKey(key))
		{
			key = 0;
		}
		return 0f + ((float)Characters[key].XAdvance + extraPadding);
	}

	public override string ToString()
	{
		if (string.IsNullOrEmpty(Name))
		{
			return base.ToString();
		}
		return Name;
	}
}
