using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public class BrushFactory
{
	private readonly struct BrushOverrideInfo
	{
		public readonly string OriginalBrushName;

		public readonly Brush OverrideBrush;

		public readonly Dictionary<string, string> OverrideBrushAttributes;

		public readonly XmlNode OverrideBrushNode;

		public BrushOverrideInfo(string originalBrushName, Brush overrideBrush, Dictionary<string, string> overrideBrushAttributes, XmlNode overrideBrushNode)
		{
			OriginalBrushName = originalBrushName;
			OverrideBrush = overrideBrush;
			OverrideBrushAttributes = overrideBrushAttributes;
			OverrideBrushNode = overrideBrushNode;
		}
	}

	private Dictionary<string, List<BrushOverrideInfo>> _overriddenBrushes;

	private Dictionary<string, Brush> _brushes;

	private Dictionary<string, string> _brushCategories;

	private ResourceDepot _resourceDepot;

	private readonly string _resourceFolder;

	private Dictionary<string, DateTime> _lastWriteTimes;

	private SpriteData _spriteData;

	private FontFactory _fontFactory;

	public IEnumerable<Brush> Brushes => _brushes.Values;

	public Brush DefaultBrush
	{
		get
		{
			if (_brushes.ContainsKey("DefaultBrush"))
			{
				return _brushes["DefaultBrush"];
			}
			return null;
		}
	}

	public event Action BrushChange;

	public BrushFactory(ResourceDepot resourceDepot, string resourceFolder, SpriteData spriteData, FontFactory fontFactory)
	{
		_spriteData = spriteData;
		_fontFactory = fontFactory;
		_overriddenBrushes = new Dictionary<string, List<BrushOverrideInfo>>();
		_brushes = new Dictionary<string, Brush>();
		_brushCategories = new Dictionary<string, string>();
		_resourceDepot = resourceDepot;
		_resourceDepot.OnResourceChange += OnResourceChange;
		_resourceFolder = resourceFolder;
		_lastWriteTimes = new Dictionary<string, DateTime>();
	}

	private void OnResourceChange()
	{
		CheckForUpdates();
	}

	public void Initialize()
	{
		LoadBrushes();
	}

	private BrushAnimation LoadBrushAnimationFrom(XmlNode animationNode)
	{
		BrushAnimation brushAnimation = new BrushAnimation();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (XmlAttribute attribute in animationNode.Attributes)
		{
			string name = attribute.Name;
			string value = attribute.Value;
			dictionary.Add(name, value);
		}
		foreach (KeyValuePair<string, string> item in dictionary)
		{
			string key = item.Key;
			string value2 = item.Value;
			switch (key)
			{
			case "Name":
				brushAnimation.Name = value2;
				break;
			case "Duration":
				brushAnimation.Duration = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "Loop":
				brushAnimation.Loop = value2 == "true";
				break;
			case "InterpolationType":
			{
				if (Enum.TryParse<AnimationInterpolation.Type>(value2, out var result2))
				{
					brushAnimation.InterpolationType = result2;
				}
				else
				{
					Debug.FailedAssert("Failed to resolve brush animation interpolation type: " + value2, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushFactory.cs", "LoadBrushAnimationFrom", 131);
				}
				break;
			}
			case "InterpolationFunction":
			{
				if (Enum.TryParse<AnimationInterpolation.Function>(value2, out var result))
				{
					brushAnimation.InterpolationFunction = result;
				}
				else
				{
					Debug.FailedAssert("Failed to resolve brush animation interpolation function: " + value2, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushFactory.cs", "LoadBrushAnimationFrom", 142);
				}
				break;
			}
			}
		}
		foreach (XmlNode childNode in animationNode.ChildNodes)
		{
			XmlAttribute xmlAttribute = childNode.Attributes["LayerName"];
			string layerName = null;
			if (xmlAttribute != null)
			{
				layerName = xmlAttribute.Value;
			}
			string value3 = childNode.Attributes["PropertyName"].Value;
			BrushAnimationProperty brushAnimationProperty = new BrushAnimationProperty();
			if (!Enum.TryParse<BrushAnimationProperty.BrushAnimationPropertyType>(value3, out brushAnimationProperty.PropertyType))
			{
				continue;
			}
			brushAnimationProperty.LayerName = layerName;
			brushAnimation.AddAnimationProperty(brushAnimationProperty);
			foreach (XmlNode childNode2 in childNode.ChildNodes)
			{
				float time = Convert.ToSingle(childNode2.Attributes["Time"].Value, CultureInfo.InvariantCulture);
				XmlAttribute xmlAttribute2 = childNode2.Attributes["Value"];
				BrushAnimationKeyFrame brushAnimationKeyFrame = new BrushAnimationKeyFrame();
				switch (brushAnimationProperty.PropertyType)
				{
				case BrushAnimationProperty.BrushAnimationPropertyType.Color:
				case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
				case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
				case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
					brushAnimationKeyFrame.InitializeAsColor(time, Color.ConvertStringToColor(xmlAttribute2.Value));
					break;
				case BrushAnimationProperty.BrushAnimationPropertyType.ColorFactor:
				case BrushAnimationProperty.BrushAnimationPropertyType.AlphaFactor:
				case BrushAnimationProperty.BrushAnimationPropertyType.HueFactor:
				case BrushAnimationProperty.BrushAnimationPropertyType.SaturationFactor:
				case BrushAnimationProperty.BrushAnimationPropertyType.ValueFactor:
				case BrushAnimationProperty.BrushAnimationPropertyType.OverlayXOffset:
				case BrushAnimationProperty.BrushAnimationPropertyType.OverlayYOffset:
				case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineAmount:
				case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowRadius:
				case BrushAnimationProperty.BrushAnimationPropertyType.TextBlur:
				case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowOffset:
				case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowAngle:
				case BrushAnimationProperty.BrushAnimationPropertyType.TextColorFactor:
				case BrushAnimationProperty.BrushAnimationPropertyType.TextAlphaFactor:
				case BrushAnimationProperty.BrushAnimationPropertyType.TextHueFactor:
				case BrushAnimationProperty.BrushAnimationPropertyType.TextSaturationFactor:
				case BrushAnimationProperty.BrushAnimationPropertyType.TextValueFactor:
				case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
				case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
				case BrushAnimationProperty.BrushAnimationPropertyType.Rotation:
				case BrushAnimationProperty.BrushAnimationPropertyType.OverridenWidth:
				case BrushAnimationProperty.BrushAnimationPropertyType.OverridenHeight:
				case BrushAnimationProperty.BrushAnimationPropertyType.ExtendLeft:
				case BrushAnimationProperty.BrushAnimationPropertyType.ExtendRight:
				case BrushAnimationProperty.BrushAnimationPropertyType.ExtendTop:
				case BrushAnimationProperty.BrushAnimationPropertyType.ExtendBottom:
				case BrushAnimationProperty.BrushAnimationPropertyType.FontSize:
					brushAnimationKeyFrame.InitializeAsFloat(time, Convert.ToSingle(xmlAttribute2.Value, CultureInfo.InvariantCulture));
					break;
				case BrushAnimationProperty.BrushAnimationPropertyType.Sprite:
				case BrushAnimationProperty.BrushAnimationPropertyType.OverlaySprite:
					brushAnimationKeyFrame.InitializeAsSprite(time, _spriteData.GetSprite(xmlAttribute2.Value));
					break;
				}
				brushAnimationProperty.AddKeyFrame(brushAnimationKeyFrame);
			}
		}
		return brushAnimation;
	}

	private void LoadBrushLayerInto(XmlNode styleSpriteNode, IBrushLayerData brushLayer)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (XmlAttribute attribute in styleSpriteNode.Attributes)
		{
			string name = attribute.Name;
			string value = attribute.Value;
			dictionary.Add(name, value);
		}
		foreach (KeyValuePair<string, string> item in dictionary)
		{
			string key = item.Key;
			string value2 = item.Value;
			switch (key)
			{
			case "Sprite":
				brushLayer.Sprite = _spriteData.GetSprite(value2);
				break;
			case "Color":
				brushLayer.Color = Color.ConvertStringToColor(value2);
				break;
			case "ColorFactor":
				brushLayer.ColorFactor = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "AlphaFactor":
				brushLayer.AlphaFactor = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "HueFactor":
				brushLayer.HueFactor = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "SaturationFactor":
				brushLayer.SaturationFactor = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "ValueFactor":
				brushLayer.ValueFactor = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "XOffset":
				brushLayer.XOffset = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "YOffset":
				brushLayer.YOffset = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "Rotation":
				brushLayer.Rotation = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "OverridenWidth":
				brushLayer.OverridenWidth = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "OverridenHeight":
				brushLayer.OverridenHeight = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "IsHidden":
				brushLayer.IsHidden = value2 == "true";
				break;
			case "UseOverlayAlphaAsMask":
				brushLayer.UseOverlayAlphaAsMask = value2 == "true";
				break;
			case "WidthPolicy":
				brushLayer.WidthPolicy = (BrushLayerSizePolicy)Enum.Parse(typeof(BrushLayerSizePolicy), value2);
				break;
			case "HeightPolicy":
				brushLayer.HeightPolicy = (BrushLayerSizePolicy)Enum.Parse(typeof(BrushLayerSizePolicy), value2);
				break;
			case "HorizontalFlip":
				brushLayer.HorizontalFlip = value2 == "true";
				break;
			case "VerticalFlip":
				brushLayer.VerticalFlip = value2 == "true";
				break;
			case "OverlayMethod":
				brushLayer.OverlayMethod = (BrushOverlayMethod)Enum.Parse(typeof(BrushOverlayMethod), value2);
				break;
			case "OverlaySprite":
				brushLayer.OverlaySprite = _spriteData.GetSprite(value2);
				break;
			case "ExtendTop":
				brushLayer.ExtendTop = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "ExtendBottom":
				brushLayer.ExtendBottom = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "ExtendLeft":
				brushLayer.ExtendLeft = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "ExtendRight":
				brushLayer.ExtendRight = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "OverlayXOffset":
				brushLayer.OverlayXOffset = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "OverlayYOffset":
				brushLayer.OverlayYOffset = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "UseRandomBaseOverlayXOffset":
				brushLayer.UseRandomBaseOverlayXOffset = value2 == "true";
				break;
			case "UseRandomBaseOverlayYOffset":
				brushLayer.UseRandomBaseOverlayYOffset = value2 == "true";
				break;
			case "ImageFit.Type":
			{
				if (Enum.TryParse<ImageFit.ImageFitTypes>(value2, out var result3))
				{
					brushLayer.ImageFitType = result3;
				}
				break;
			}
			case "ImageFit.HorizontalAlignment":
			{
				if (Enum.TryParse<ImageFit.ImageHorizontalAlignments>(value2, out var result2))
				{
					brushLayer.ImageFitHorizontalAlignment = result2;
				}
				break;
			}
			case "ImageFit.VerticalAlignment":
			{
				if (Enum.TryParse<ImageFit.ImageVerticalAlignments>(value2, out var result))
				{
					brushLayer.ImageFitVerticalAlignment = result;
				}
				break;
			}
			case "Name":
				if (string.IsNullOrEmpty(brushLayer.Name))
				{
					brushLayer.Name = value2;
				}
				break;
			}
		}
	}

	private void LoadStyleInto(XmlNode styleNode, Style style)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (XmlAttribute attribute in styleNode.Attributes)
		{
			string name = attribute.Name;
			string value = attribute.Value;
			dictionary.Add(name, value);
		}
		foreach (KeyValuePair<string, string> item in dictionary)
		{
			string key = item.Key;
			string value2 = item.Value;
			switch (key)
			{
			case "Name":
				style.Name = value2;
				break;
			case "FontColor":
				style.FontColor = Color.ConvertStringToColor(value2);
				break;
			case "AnimationMode":
				style.AnimationMode = (StyleAnimationMode)Enum.Parse(typeof(StyleAnimationMode), value2);
				break;
			case "AnimationToPlayOnBegin":
				style.AnimationToPlayOnBegin = value2;
				break;
			case "TextGlowColor":
				style.TextGlowColor = Color.ConvertStringToColor(value2);
				break;
			case "TextOutlineColor":
				style.TextOutlineColor = Color.ConvertStringToColor(value2);
				break;
			case "TextOutlineAmount":
				style.TextOutlineAmount = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "TextGlowRadius":
				style.TextGlowRadius = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "TextBlur":
				style.TextBlur = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "TextShadowOffset":
				style.TextShadowOffset = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "TextShadowAngle":
				style.TextShadowAngle = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "TextColorFactor":
				style.TextColorFactor = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "TextAlphaFactor":
				style.TextAlphaFactor = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "TextHueFactor":
				style.TextHueFactor = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "TextSaturationFactor":
				style.TextSaturationFactor = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "TextValueFactor":
				style.TextValueFactor = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "XOffset":
				style.XOffset = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "YOffset":
				style.YOffset = Convert.ToSingle(value2, CultureInfo.InvariantCulture);
				break;
			case "Font":
				style.Font = _fontFactory.GetFont(value2);
				break;
			case "FontSize":
				style.FontSize = Convert.ToInt32(value2);
				break;
			}
		}
		foreach (XmlNode childNode in styleNode.ChildNodes)
		{
			string value3 = childNode.Attributes["Name"].Value;
			StyleLayer layer = style.GetLayer(value3);
			LoadBrushLayerInto(childNode, layer);
		}
	}

	private void LoadSoundPropertiesInto(XmlNode soundPropertiesNode, SoundProperties soundProperties)
	{
		XmlNode xmlNode = soundPropertiesNode.SelectSingleNode("StateSounds");
		XmlNode xmlNode2 = soundPropertiesNode.SelectSingleNode("EventSounds");
		if (xmlNode != null)
		{
			foreach (XmlNode item in xmlNode)
			{
				AudioProperty audioProperty = new AudioProperty();
				string value = item.Attributes["StateName"].Value;
				string value2 = item.Attributes["Audio"].Value;
				audioProperty.AudioName = value2;
				soundProperties.AddStateSound(value, audioProperty);
			}
		}
		if (xmlNode2 == null)
		{
			return;
		}
		foreach (XmlNode item2 in xmlNode2)
		{
			AudioProperty audioProperty2 = new AudioProperty();
			string value3 = item2.Attributes["EventName"].Value;
			string value4 = item2.Attributes["Audio"].Value;
			audioProperty2.AudioName = value4;
			soundProperties.AddEventSound(value3, audioProperty2);
		}
	}

	private Brush LoadBrushFrom(XmlNode brushNode)
	{
		Brush brush = new Brush();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (XmlAttribute attribute in brushNode.Attributes)
		{
			string name = attribute.Name;
			string value = attribute.Value;
			dictionary.Add(name, value);
		}
		bool flag = false;
		if (dictionary.ContainsKey("BaseBrush"))
		{
			flag = true;
			string key = dictionary["BaseBrush"];
			if (_brushes.ContainsKey(key))
			{
				Brush brush2 = _brushes[key];
				brush.FillFrom(brush2);
			}
		}
		if (dictionary.ContainsKey("OverrideBrush"))
		{
			if (flag)
			{
				Debug.FailedAssert("A brush shouldn't have both a BaseBrush and a OverrideBrush", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushFactory.cs", "LoadBrushFrom", 592);
			}
			string text = dictionary["OverrideBrush"];
			if (!string.IsNullOrEmpty(text))
			{
				BrushOverrideInfo item = new BrushOverrideInfo(text, brush, dictionary, brushNode);
				if (_overriddenBrushes.ContainsKey(text))
				{
					_overriddenBrushes[text].Add(item);
				}
				else
				{
					_overriddenBrushes.Add(text, new List<BrushOverrideInfo> { item });
				}
			}
			else
			{
				Debug.FailedAssert("Invalid overridden brush name: " + text, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushFactory.cs", "LoadBrushFrom", 611);
			}
		}
		ApplyBrushAttributesFrom(brush, brushNode, dictionary);
		return brush;
	}

	private void ApplyBrushAttributesFrom(Brush brush, XmlNode brushNode, Dictionary<string, string> brushAttributes)
	{
		foreach (KeyValuePair<string, string> brushAttribute in brushAttributes)
		{
			string key = brushAttribute.Key;
			string value = brushAttribute.Value;
			switch (key)
			{
			case "Name":
				brush.Name = value;
				break;
			case "Font":
				brush.Font = _fontFactory.GetFont(value);
				break;
			case "FontSize":
				brush.FontSize = Convert.ToInt32(value);
				break;
			case "TransitionDuration":
				brush.TransitionDuration = Convert.ToSingle(value, CultureInfo.InvariantCulture);
				break;
			case "TextHorizontalAlignment":
				brush.TextHorizontalAlignment = (TextHorizontalAlignment)Enum.Parse(typeof(TextHorizontalAlignment), value);
				break;
			case "TextVerticalAlignment":
				brush.TextVerticalAlignment = (TextVerticalAlignment)Enum.Parse(typeof(TextVerticalAlignment), value);
				break;
			case "GlobalColorFactor":
				brush.GlobalColorFactor = Convert.ToSingle(value, CultureInfo.InvariantCulture);
				break;
			case "GlobalAlphaFactor":
				brush.GlobalAlphaFactor = Convert.ToSingle(value, CultureInfo.InvariantCulture);
				break;
			case "GlobalColor":
				brush.GlobalColor = Color.ConvertStringToColor(value);
				break;
			}
		}
		XmlNode xmlNode = brushNode.SelectSingleNode("Layers");
		if (xmlNode != null)
		{
			foreach (XmlNode item in xmlNode)
			{
				string value2 = item.Attributes["Name"].Value;
				BrushLayer layer = brush.GetLayer(value2);
				if (layer != null)
				{
					LoadBrushLayerInto(item, layer);
					continue;
				}
				layer = new BrushLayer();
				LoadBrushLayerInto(item, layer);
				brush.AddLayer(layer);
			}
		}
		XmlNode xmlNode3 = brushNode.SelectSingleNode("Styles");
		if (xmlNode3 != null)
		{
			foreach (XmlNode item2 in xmlNode3)
			{
				string value3 = item2.Attributes["Name"].Value;
				Style style = brush.GetStyle(value3);
				if (style != null)
				{
					style.DefaultStyle = brush.DefaultStyle;
					LoadStyleInto(item2, style);
					continue;
				}
				style = new Style(brush.Layers);
				style.DefaultStyle = brush.DefaultStyle;
				LoadStyleInto(item2, style);
				brush.AddStyle(style);
			}
		}
		XmlNode xmlNode5 = brushNode.SelectSingleNode("Animations");
		if (xmlNode5 != null)
		{
			foreach (XmlNode item3 in xmlNode5)
			{
				BrushAnimation animation = LoadBrushAnimationFrom(item3);
				brush.AddAnimation(animation);
			}
		}
		XmlNode xmlNode6 = brushNode.SelectSingleNode("SoundProperties");
		if (xmlNode6 != null)
		{
			LoadSoundPropertiesInto(xmlNode6, brush.SoundProperties);
		}
		else if (DefaultBrush != null && !brush.SoundProperties.RegisteredEventSounds.Any() && !brush.SoundProperties.RegisteredStateSounds.Any())
		{
			brush.SoundProperties.FillFrom(DefaultBrush.SoundProperties);
		}
	}

	private void SaveBrushTo(XmlNode brushNode, Brush brush)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		PropertyInfo[] properties = brush.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			object value = propertyInfo.GetValue(brush);
			if ((value == null && propertyInfo.GetValue(DefaultBrush) != null) || (value != null && !value.Equals(propertyInfo.GetValue(DefaultBrush))))
			{
				AddAttributeTo(propertyInfo, value, dictionary);
			}
		}
		brushNode.Attributes.RemoveAll();
		foreach (KeyValuePair<string, string> item in dictionary)
		{
			XmlAttribute xmlAttribute = brushNode.OwnerDocument.CreateAttribute(item.Key);
			xmlAttribute.InnerText = item.Value;
			brushNode.Attributes.Append(xmlAttribute);
		}
		XmlNode xmlNode = brushNode.SelectSingleNode("Layers");
		if (xmlNode == null)
		{
			xmlNode = brushNode.OwnerDocument.CreateElement("Layers");
			brushNode.AppendChild(xmlNode);
		}
		else
		{
			xmlNode.RemoveAll();
		}
		foreach (BrushLayer layer in brush.Layers)
		{
			XmlNode xmlNode2 = brushNode.OwnerDocument.CreateElement("BrushLayer");
			xmlNode.AppendChild(xmlNode2);
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			properties = layer.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (PropertyInfo propertyInfo2 in properties)
			{
				object value2 = propertyInfo2.GetValue(layer);
				if ((value2 == null && propertyInfo2.GetValue(DefaultBrush.DefaultLayer) != null) || (value2 != null && !value2.Equals(propertyInfo2.GetValue(DefaultBrush.DefaultLayer))) || propertyInfo2.Name == "Name")
				{
					AddAttributeTo(propertyInfo2, value2, dictionary2);
				}
			}
			foreach (KeyValuePair<string, string> item2 in dictionary2)
			{
				XmlAttribute xmlAttribute2 = brushNode.OwnerDocument.CreateAttribute(item2.Key);
				xmlAttribute2.InnerText = item2.Value;
				xmlNode2.Attributes.Append(xmlAttribute2);
			}
		}
		XmlNode xmlNode3 = brushNode.SelectSingleNode("Styles");
		if (xmlNode3 == null)
		{
			xmlNode3 = brushNode.OwnerDocument.CreateElement("Styles");
			brushNode.AppendChild(xmlNode3);
		}
		else
		{
			xmlNode3.RemoveAll();
		}
		foreach (Style style in brush.Styles)
		{
			XmlNode xmlNode4 = brushNode.OwnerDocument.CreateElement("Style");
			xmlNode3.AppendChild(xmlNode4);
			Dictionary<string, string> dictionary3 = new Dictionary<string, string>();
			properties = style.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (PropertyInfo propertyInfo3 in properties)
			{
				object value3 = propertyInfo3.GetValue(style);
				if ((value3 == null && propertyInfo3.GetValue(DefaultBrush.DefaultStyle) != null) || (value3 != null && !value3.Equals(propertyInfo3.GetValue(DefaultBrush.DefaultStyle))) || propertyInfo3.Name == "Name")
				{
					AddAttributeTo(propertyInfo3, value3, dictionary3);
				}
			}
			foreach (KeyValuePair<string, string> item3 in dictionary3)
			{
				XmlAttribute xmlAttribute3 = brushNode.OwnerDocument.CreateAttribute(item3.Key);
				xmlAttribute3.InnerText = item3.Value;
				xmlNode4.Attributes.Append(xmlAttribute3);
			}
			StyleLayer[] layers = style.GetLayers();
			foreach (StyleLayer styleLayer in layers)
			{
				XmlNode xmlNode5 = brushNode.OwnerDocument.CreateElement("BrushLayer");
				xmlNode4.AppendChild(xmlNode5);
				Dictionary<string, string> dictionary4 = new Dictionary<string, string>();
				properties = styleLayer.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (PropertyInfo propertyInfo4 in properties)
				{
					if (Enum.TryParse<BrushAnimationProperty.BrushAnimationPropertyType>(propertyInfo4.Name, ignoreCase: true, out var result) && (styleLayer.GetIsValueChanged(result) || result == BrushAnimationProperty.BrushAnimationPropertyType.Name))
					{
						object value4 = propertyInfo4.GetValue(styleLayer);
						AddAttributeTo(propertyInfo4, value4, dictionary4);
					}
				}
				foreach (KeyValuePair<string, string> item4 in dictionary4)
				{
					XmlAttribute xmlAttribute4 = brushNode.OwnerDocument.CreateAttribute(item4.Key);
					xmlAttribute4.InnerText = item4.Value;
					xmlNode5.Attributes.Append(xmlAttribute4);
				}
			}
		}
		if (brush.GetAnimations().Any())
		{
			XmlNode xmlNode6 = brushNode.SelectSingleNode("Animations");
			if (xmlNode6 == null)
			{
				xmlNode6 = brushNode.OwnerDocument.CreateElement("Animations");
				brushNode.AppendChild(xmlNode6);
			}
			else
			{
				xmlNode6.RemoveAll();
			}
			foreach (BrushAnimation animation in brush.GetAnimations())
			{
				XmlNode xmlNode7 = brushNode.OwnerDocument.CreateElement("Animation");
				xmlNode6.AppendChild(xmlNode7);
				XmlAttribute xmlAttribute5 = xmlNode7.OwnerDocument.CreateAttribute("Name");
				xmlAttribute5.InnerText = animation.Name;
				xmlNode7.Attributes.Append(xmlAttribute5);
				XmlAttribute xmlAttribute6 = xmlNode7.OwnerDocument.CreateAttribute("Duration");
				xmlAttribute6.InnerText = animation.Duration.ToString();
				xmlNode7.Attributes.Append(xmlAttribute6);
				XmlAttribute xmlAttribute7 = xmlNode7.OwnerDocument.CreateAttribute("Loop");
				xmlAttribute7.InnerText = (animation.Loop ? "true" : "false");
				xmlNode7.Attributes.Append(xmlAttribute7);
				foreach (BrushLayerAnimation layerAnimation in animation.GetLayerAnimations())
				{
					foreach (BrushAnimationProperty collection in layerAnimation.Collections)
					{
						XmlNode xmlNode8 = xmlNode7.OwnerDocument.CreateElement("AnimationProperty");
						xmlNode7.AppendChild(xmlNode8);
						if (!string.IsNullOrEmpty(collection.LayerName))
						{
							XmlAttribute xmlAttribute8 = xmlNode8.OwnerDocument.CreateAttribute("LayerName");
							xmlAttribute8.InnerText = collection.LayerName;
							xmlNode8.Attributes.Append(xmlAttribute8);
						}
						XmlAttribute xmlAttribute9 = xmlNode8.OwnerDocument.CreateAttribute("PropertyName");
						xmlAttribute9.InnerText = collection.PropertyType.ToString();
						xmlNode8.Attributes.Append(xmlAttribute9);
						PropertyInfo property = typeof(BrushLayer).GetProperty(collection.PropertyType.ToString());
						foreach (BrushAnimationKeyFrame keyFrame in collection.KeyFrames)
						{
							XmlNode xmlNode9 = xmlNode7.OwnerDocument.CreateElement("KeyFrame");
							xmlNode7.AppendChild(xmlNode9);
							Dictionary<string, string> dictionary5 = new Dictionary<string, string>();
							AddAttributeTo(property, keyFrame.GetValueAsObject(), dictionary5);
							foreach (KeyValuePair<string, string> item5 in dictionary5)
							{
								XmlAttribute xmlAttribute10 = xmlNode9.OwnerDocument.CreateAttribute("Time");
								xmlAttribute10.InnerText = keyFrame.Time.ToString();
								xmlNode9.Attributes.Append(xmlAttribute10);
								XmlAttribute xmlAttribute11 = xmlNode9.OwnerDocument.CreateAttribute("Value");
								xmlAttribute11.InnerText = item5.Value;
								xmlNode9.Attributes.Append(xmlAttribute11);
							}
						}
					}
				}
			}
		}
		else
		{
			XmlNode xmlNode10 = brushNode.SelectSingleNode("Animations");
			if (xmlNode10 != null)
			{
				brushNode.RemoveChild(xmlNode10);
			}
		}
		if (brush.SoundProperties != null)
		{
			XmlNode xmlNode11 = brushNode.SelectSingleNode("SoundProperties");
			if (xmlNode11 == null)
			{
				xmlNode11 = brushNode.OwnerDocument.CreateElement("SoundProperties");
				brushNode.AppendChild(xmlNode11);
			}
			else
			{
				xmlNode11.RemoveAll();
			}
			XmlNode xmlNode12 = brushNode.OwnerDocument.CreateElement("StateSounds");
			xmlNode11.AppendChild(xmlNode12);
			foreach (KeyValuePair<string, AudioProperty> registeredStateSound in brush.SoundProperties.RegisteredStateSounds)
			{
				XmlNode xmlNode13 = brushNode.OwnerDocument.CreateElement("StateSound");
				xmlNode12.AppendChild(xmlNode13);
				XmlAttribute xmlAttribute12 = xmlNode13.OwnerDocument.CreateAttribute("StateName");
				xmlAttribute12.InnerText = registeredStateSound.Key;
				xmlNode13.Attributes.Append(xmlAttribute12);
				XmlAttribute xmlAttribute13 = xmlNode13.OwnerDocument.CreateAttribute("Audio");
				xmlAttribute13.InnerText = registeredStateSound.Value.AudioName;
				xmlNode13.Attributes.Append(xmlAttribute13);
			}
			XmlNode xmlNode14 = brushNode.OwnerDocument.CreateElement("EventSounds");
			xmlNode11.AppendChild(xmlNode14);
			foreach (KeyValuePair<string, AudioProperty> registeredEventSound in brush.SoundProperties.RegisteredEventSounds)
			{
				XmlNode xmlNode15 = brushNode.OwnerDocument.CreateElement("EventSound");
				xmlNode14.AppendChild(xmlNode15);
				XmlAttribute xmlAttribute14 = xmlNode15.OwnerDocument.CreateAttribute("EventName");
				xmlAttribute14.InnerText = registeredEventSound.Key;
				xmlNode15.Attributes.Append(xmlAttribute14);
				XmlAttribute xmlAttribute15 = xmlNode15.OwnerDocument.CreateAttribute("Audio");
				xmlAttribute15.InnerText = registeredEventSound.Value.AudioName;
				xmlNode15.Attributes.Append(xmlAttribute15);
			}
		}
		Uri uri = new Uri(brushNode.OwnerDocument.BaseURI);
		brushNode.OwnerDocument.Save(uri.LocalPath);
	}

	private void AddAttributeTo(PropertyInfo targetPropertyInfo, object targetPropertyValue, Dictionary<string, string> attributePairs)
	{
		if (targetPropertyInfo.PropertyType != typeof(string) && (targetPropertyInfo.PropertyType.GetInterface("IEnumerable") != null || targetPropertyInfo.PropertyType.GetInterface("ICollection") != null || targetPropertyInfo.PropertyType.GetInterface("IList") != null))
		{
			return;
		}
		object[] customAttributesSafe = targetPropertyInfo.GetCustomAttributesSafe(typeof(EditorAttribute), inherit: true);
		if (customAttributesSafe == null || customAttributesSafe.Length == 0)
		{
			return;
		}
		if (targetPropertyInfo.PropertyType == typeof(Color))
		{
			Color color = (Color)targetPropertyValue;
			attributePairs.Add(targetPropertyInfo.Name, color.ToString());
		}
		else if (targetPropertyInfo.PropertyType == typeof(Brush))
		{
			if (targetPropertyValue is Brush brush)
			{
				attributePairs.Add(targetPropertyInfo.Name, brush.Name);
			}
		}
		else if (targetPropertyInfo.PropertyType == typeof(Font))
		{
			if (targetPropertyValue is Font font)
			{
				attributePairs.Add(targetPropertyInfo.Name, _fontFactory.GetFontName(font));
			}
		}
		else if (targetPropertyValue == null)
		{
			attributePairs.Add(targetPropertyInfo.Name, "");
		}
		else
		{
			attributePairs.Add(targetPropertyInfo.Name, targetPropertyValue.ToString());
		}
	}

	private void LoadBrushes()
	{
		_brushes.Clear();
		_brushCategories.Clear();
		_lastWriteTimes.Clear();
		List<string> brushesNames = GetBrushesNames();
		LoadBrushFile("Base");
		foreach (string item in brushesNames)
		{
			if (item != "Base")
			{
				LoadBrushFile(item);
			}
		}
	}

	public void LoadBrushFile(string name)
	{
		try
		{
			LoadBrushFromFileAux(name);
		}
		catch (Exception)
		{
			Debug.FailedAssert("Failed to load brush from file: " + name, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushFactory.cs", "LoadBrushFile", 1138);
		}
	}

	private void LoadBrushFromFileAux(string name)
	{
		string filePath = _resourceDepot.GetFilePath(_resourceFolder + "/" + name + ".xml");
		DateTime lastWriteTime = File.GetLastWriteTime(filePath);
		if (_lastWriteTimes.ContainsKey(name))
		{
			_lastWriteTimes[name] = lastWriteTime;
		}
		else
		{
			_lastWriteTimes.Add(name, lastWriteTime);
		}
		XmlDocument xmlDocument = new XmlDocument();
		XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
		xmlReaderSettings.IgnoreComments = true;
		using (XmlReader reader = XmlReader.Create(new StreamReader(filePath), xmlReaderSettings))
		{
			xmlDocument.Load(reader);
		}
		foreach (XmlNode childNode in xmlDocument.SelectSingleNode("Brushes").ChildNodes)
		{
			Brush brush = LoadBrushFrom(childNode);
			if (_brushes.ContainsKey(brush.Name))
			{
				_brushes[brush.Name] = brush;
			}
			else
			{
				_brushes.Add(brush.Name, brush);
			}
			if (_brushCategories.ContainsKey(brush.Name))
			{
				_brushCategories[brush.Name] = filePath;
			}
			else
			{
				_brushCategories.Add(brush.Name, filePath);
			}
		}
		foreach (KeyValuePair<string, List<BrushOverrideInfo>> overriddenBrush in _overriddenBrushes)
		{
			string key = overriddenBrush.Key;
			List<BrushOverrideInfo> value = overriddenBrush.Value;
			if (_brushes.TryGetValue(key, out var value2))
			{
				foreach (BrushOverrideInfo item in value)
				{
					item.OverrideBrush.FillForOverride(value2);
					ApplyBrushAttributesFrom(item.OverrideBrush, item.OverrideBrushNode, item.OverrideBrushAttributes);
					value2 = item.OverrideBrush;
				}
				_brushes[key] = value2;
			}
			else
			{
				Debug.FailedAssert("Failed to find brush for override: " + key, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushFactory.cs", "LoadBrushFromFileAux", 1211);
			}
		}
		_overriddenBrushes.Clear();
	}

	public Brush GetBrush(string name)
	{
		if (_brushes.TryGetValue(name, out var value))
		{
			return value;
		}
		return null;
	}

	public bool SaveBrushAs(string name, Brush brush)
	{
		if (!_brushCategories.ContainsKey(name))
		{
			Debug.FailedAssert("Brush not found", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushFactory.cs", "SaveBrushAs", 1234);
		}
		string filename = _brushCategories[name];
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(filename);
		foreach (XmlNode childNode in xmlDocument.SelectSingleNode("Brushes").ChildNodes)
		{
			foreach (XmlAttribute attribute in childNode.Attributes)
			{
				if (attribute.Name.Equals("Name") && attribute.Value.Equals(name))
				{
					SaveBrushTo(childNode, brush);
					return true;
				}
			}
		}
		return false;
	}

	private List<string> GetBrushesNames()
	{
		string[] files = _resourceDepot.GetFiles(_resourceFolder, ".xml");
		List<string> list = new List<string>();
		string[] array = files;
		for (int i = 0; i < array.Length; i++)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(array[i]);
			list.Add(fileNameWithoutExtension);
		}
		return list;
	}

	public void CheckForUpdates()
	{
		bool flag = false;
		List<string> brushesNames = GetBrushesNames();
		foreach (string key in _lastWriteTimes.Keys)
		{
			if (!brushesNames.Contains(key))
			{
				flag = true;
			}
		}
		foreach (string item in brushesNames)
		{
			DateTime lastWriteTime = File.GetLastWriteTime(_resourceDepot.GetFilePath(_resourceFolder + "/" + item + ".xml"));
			if (_lastWriteTimes.ContainsKey(item))
			{
				if (_lastWriteTimes[item] != lastWriteTime)
				{
					flag = true;
				}
			}
			else
			{
				flag = true;
			}
		}
		if (flag)
		{
			LoadBrushes();
			this.BrushChange?.Invoke();
		}
	}
}
