using System;
using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json;
using TaleWorlds.Library;

namespace TaleWorlds.Core;

[Serializable]
[JsonConverter(typeof(BodyPropertiesJsonConverter))]
public struct BodyProperties
{
	private readonly DynamicBodyProperties _dynamicBodyProperties;

	private readonly StaticBodyProperties _staticBodyProperties;

	private const float DefaultAge = 30f;

	private const float DefaultWeight = 0.5f;

	private const float DefaultBuild = 0.5f;

	public StaticBodyProperties StaticProperties => _staticBodyProperties;

	public DynamicBodyProperties DynamicProperties => _dynamicBodyProperties;

	public float Age => _dynamicBodyProperties.Age;

	public float Weight => _dynamicBodyProperties.Weight;

	public float Build => _dynamicBodyProperties.Build;

	public ulong KeyPart1 => _staticBodyProperties.KeyPart1;

	public ulong KeyPart2 => _staticBodyProperties.KeyPart2;

	public ulong KeyPart3 => _staticBodyProperties.KeyPart3;

	public ulong KeyPart4 => _staticBodyProperties.KeyPart4;

	public ulong KeyPart5 => _staticBodyProperties.KeyPart5;

	public ulong KeyPart6 => _staticBodyProperties.KeyPart6;

	public ulong KeyPart7 => _staticBodyProperties.KeyPart7;

	public ulong KeyPart8 => _staticBodyProperties.KeyPart8;

	public static BodyProperties Default => new BodyProperties(new DynamicBodyProperties(20f, 0f, 0f), default(StaticBodyProperties));

	public BodyProperties(DynamicBodyProperties dynamicBodyProperties, StaticBodyProperties staticBodyProperties)
	{
		_dynamicBodyProperties = dynamicBodyProperties;
		_staticBodyProperties = staticBodyProperties;
	}

	public static bool FromXmlNode(XmlNode node, out BodyProperties bodyProperties)
	{
		float result = 30f;
		float result2 = 0.5f;
		float result3 = 0.5f;
		if (node.Attributes["age"] != null)
		{
			float.TryParse(node.Attributes["age"].Value, out result);
		}
		if (node.Attributes["weight"] != null)
		{
			float.TryParse(node.Attributes["weight"].Value, out result2);
		}
		if (node.Attributes["build"] != null)
		{
			float.TryParse(node.Attributes["build"].Value, out result3);
		}
		DynamicBodyProperties dynamicBodyProperties = new DynamicBodyProperties(result, result2, result3);
		if (StaticBodyProperties.FromXmlNode(node, out var staticBodyProperties))
		{
			bodyProperties = new BodyProperties(dynamicBodyProperties, staticBodyProperties);
			return true;
		}
		bodyProperties = default(BodyProperties);
		return false;
	}

	public static bool FromString(string keyValue, out BodyProperties bodyProperties)
	{
		if (keyValue.StartsWith("<BodyProperties ", StringComparison.InvariantCultureIgnoreCase) || keyValue.StartsWith("<BodyPropertiesMax ", StringComparison.InvariantCultureIgnoreCase))
		{
			XmlDocument xmlDocument = new XmlDocument();
			try
			{
				xmlDocument.LoadXml(keyValue);
			}
			catch (XmlException)
			{
				bodyProperties = default(BodyProperties);
				return false;
			}
			if (xmlDocument.FirstChild.Name.Equals("BodyProperties", StringComparison.InvariantCultureIgnoreCase) || xmlDocument.FirstChild.Name.Equals("BodyPropertiesMax", StringComparison.InvariantCultureIgnoreCase))
			{
				FromXmlNode(xmlDocument.FirstChild, out bodyProperties);
				float result = 20f;
				float result2 = 0f;
				float result3 = 0f;
				if (xmlDocument.FirstChild.Attributes["age"] != null)
				{
					float.TryParse(xmlDocument.FirstChild.Attributes["age"].Value, out result);
				}
				if (xmlDocument.FirstChild.Attributes["weight"] != null)
				{
					float.TryParse(xmlDocument.FirstChild.Attributes["weight"].Value, out result2);
				}
				if (xmlDocument.FirstChild.Attributes["build"] != null)
				{
					float.TryParse(xmlDocument.FirstChild.Attributes["build"].Value, out result3);
				}
				bodyProperties = new BodyProperties(new DynamicBodyProperties(result, result2, result3), bodyProperties.StaticProperties);
				return true;
			}
			bodyProperties = default(BodyProperties);
			return false;
		}
		Debug.FailedAssert("unknown body properties format:\n" + keyValue, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\BodyProperties.cs", "FromString", 148);
		bodyProperties = default(BodyProperties);
		return false;
	}

	public static BodyProperties GetRandomBodyProperties(int race, bool isFemale, BodyProperties bodyPropertiesMin, BodyProperties bodyPropertiesMax, int hairCoverType, int seed, string hairTags, string beardTags, string tattooTags, float variationAmount = 0f)
	{
		variationAmount = TaleWorlds.Library.MathF.Max(variationAmount, 0f);
		return FaceGen.GetRandomBodyProperties(race, isFemale, bodyPropertiesMin, bodyPropertiesMax, hairCoverType, seed, hairTags, beardTags, tattooTags, variationAmount);
	}

	public static bool operator ==(BodyProperties a, BodyProperties b)
	{
		if ((object)a == (object)b)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		if (a._staticBodyProperties == b._staticBodyProperties)
		{
			return a._dynamicBodyProperties == b._dynamicBodyProperties;
		}
		return false;
	}

	public static bool operator !=(BodyProperties a, BodyProperties b)
	{
		return !(a == b);
	}

	public override string ToString()
	{
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(150, "ToString");
		mBStringBuilder.Append("<BodyProperties version=\"4\" ");
		mBStringBuilder.Append(_dynamicBodyProperties.ToString() + " ");
		mBStringBuilder.Append(_staticBodyProperties.ToString());
		mBStringBuilder.Append(" />");
		return mBStringBuilder.ToStringAndRelease();
	}

	public override bool Equals(object obj)
	{
		if (!(obj is BodyProperties bodyProperties))
		{
			return false;
		}
		if (EqualityComparer<DynamicBodyProperties>.Default.Equals(_dynamicBodyProperties, bodyProperties._dynamicBodyProperties))
		{
			return EqualityComparer<StaticBodyProperties>.Default.Equals(_staticBodyProperties, bodyProperties._staticBodyProperties);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (2041866711 * -1521134295 + EqualityComparer<DynamicBodyProperties>.Default.GetHashCode(_dynamicBodyProperties)) * -1521134295 + EqualityComparer<StaticBodyProperties>.Default.GetHashCode(_staticBodyProperties);
	}

	public BodyProperties ClampForMultiplayer()
	{
		float age = TaleWorlds.Library.MathF.Clamp(DynamicProperties.Age, 22f, 128f);
		DynamicBodyProperties dynamicBodyProperties = new DynamicBodyProperties(age, 0.5f, 0.5f);
		StaticBodyProperties staticBodyProperties = ClampHeightMultiplierFaceKey(StaticProperties);
		return new BodyProperties(dynamicBodyProperties, staticBodyProperties);
	}

	private StaticBodyProperties ClampHeightMultiplierFaceKey(in StaticBodyProperties staticBodyProperties)
	{
		ulong part = staticBodyProperties.KeyPart8;
		float num = (float)GetBitsValueFromKey(in part, 19, 6) / 63f;
		if (num < 0.25f || num > 0.75f)
		{
			num = 0.5f;
			int inewValue = (int)(num * 63f);
			ulong keyPart = SetBits(in part, 19, 6, inewValue);
			return new StaticBodyProperties(staticBodyProperties.KeyPart1, staticBodyProperties.KeyPart2, staticBodyProperties.KeyPart3, staticBodyProperties.KeyPart4, staticBodyProperties.KeyPart5, staticBodyProperties.KeyPart6, keyPart, staticBodyProperties.KeyPart8);
		}
		return staticBodyProperties;
	}

	private static ulong SetBits(in ulong ipart7, int startBit, int numBits, int inewValue)
	{
		ulong num = ipart7;
		ulong num2 = TaleWorlds.Library.MathF.PowTwo64(numBits) - 1 << startBit;
		return (num & ~num2) | (ulong)((long)inewValue << startBit);
	}

	private static int GetBitsValueFromKey(in ulong part, int startBit, int numBits)
	{
		ulong num = part >> startBit;
		ulong num2 = TaleWorlds.Library.MathF.PowTwo64(numBits) - 1;
		return (int)(num & num2);
	}
}
