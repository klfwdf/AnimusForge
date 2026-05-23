using System;
using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json;
using TaleWorlds.Library;

namespace TaleWorlds.Core
{
	// Token: 0x02000021 RID: 33
	[JsonConverter(typeof(BodyPropertiesJsonConverter))]
	[Serializable]
	public struct BodyProperties
	{
		// Token: 0x17000079 RID: 121
		// (get) Token: 0x0600018C RID: 396 RVA: 0x000069B5 File Offset: 0x00004BB5
		public StaticBodyProperties StaticProperties
		{
			get
			{
				return this._staticBodyProperties;
			}
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x0600018D RID: 397 RVA: 0x000069BD File Offset: 0x00004BBD
		public DynamicBodyProperties DynamicProperties
		{
			get
			{
				return this._dynamicBodyProperties;
			}
		}

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x0600018E RID: 398 RVA: 0x000069C5 File Offset: 0x00004BC5
		public float Age
		{
			get
			{
				return this._dynamicBodyProperties.Age;
			}
		}

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x0600018F RID: 399 RVA: 0x000069D2 File Offset: 0x00004BD2
		public float Weight
		{
			get
			{
				return this._dynamicBodyProperties.Weight;
			}
		}

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x06000190 RID: 400 RVA: 0x000069DF File Offset: 0x00004BDF
		public float Build
		{
			get
			{
				return this._dynamicBodyProperties.Build;
			}
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x06000191 RID: 401 RVA: 0x000069EC File Offset: 0x00004BEC
		public ulong KeyPart1
		{
			get
			{
				return this._staticBodyProperties.KeyPart1;
			}
		}

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x06000192 RID: 402 RVA: 0x00006A08 File Offset: 0x00004C08
		public ulong KeyPart2
		{
			get
			{
				return this._staticBodyProperties.KeyPart2;
			}
		}

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x06000193 RID: 403 RVA: 0x00006A24 File Offset: 0x00004C24
		public ulong KeyPart3
		{
			get
			{
				return this._staticBodyProperties.KeyPart3;
			}
		}

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x06000194 RID: 404 RVA: 0x00006A40 File Offset: 0x00004C40
		public ulong KeyPart4
		{
			get
			{
				return this._staticBodyProperties.KeyPart4;
			}
		}

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x06000195 RID: 405 RVA: 0x00006A5C File Offset: 0x00004C5C
		public ulong KeyPart5
		{
			get
			{
				return this._staticBodyProperties.KeyPart5;
			}
		}

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x06000196 RID: 406 RVA: 0x00006A78 File Offset: 0x00004C78
		public ulong KeyPart6
		{
			get
			{
				return this._staticBodyProperties.KeyPart6;
			}
		}

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x06000197 RID: 407 RVA: 0x00006A94 File Offset: 0x00004C94
		public ulong KeyPart7
		{
			get
			{
				return this._staticBodyProperties.KeyPart7;
			}
		}

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x06000198 RID: 408 RVA: 0x00006AB0 File Offset: 0x00004CB0
		public ulong KeyPart8
		{
			get
			{
				return this._staticBodyProperties.KeyPart8;
			}
		}

		// Token: 0x06000199 RID: 409 RVA: 0x00006ACB File Offset: 0x00004CCB
		public BodyProperties(DynamicBodyProperties dynamicBodyProperties, StaticBodyProperties staticBodyProperties)
		{
			this._dynamicBodyProperties = dynamicBodyProperties;
			this._staticBodyProperties = staticBodyProperties;
		}

		// Token: 0x0600019A RID: 410 RVA: 0x00006ADC File Offset: 0x00004CDC
		public static bool FromXmlNode(XmlNode node, out BodyProperties bodyProperties)
		{
			float age = 30f;
			float weight = 0.5f;
			float build = 0.5f;
			if (node.Attributes["age"] != null)
			{
				float.TryParse(node.Attributes["age"].Value, out age);
			}
			if (node.Attributes["weight"] != null)
			{
				float.TryParse(node.Attributes["weight"].Value, out weight);
			}
			if (node.Attributes["build"] != null)
			{
				float.TryParse(node.Attributes["build"].Value, out build);
			}
			DynamicBodyProperties dynamicBodyProperties = new DynamicBodyProperties(age, weight, build);
			StaticBodyProperties staticBodyProperties;
			if (StaticBodyProperties.FromXmlNode(node, out staticBodyProperties))
			{
				bodyProperties = new BodyProperties(dynamicBodyProperties, staticBodyProperties);
				return true;
			}
			bodyProperties = default(BodyProperties);
			return false;
		}

		// Token: 0x0600019B RID: 411 RVA: 0x00006BB8 File Offset: 0x00004DB8
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
					BodyProperties.FromXmlNode(xmlDocument.FirstChild, out bodyProperties);
					float age = 20f;
					float weight = 0f;
					float build = 0f;
					if (xmlDocument.FirstChild.Attributes["age"] != null)
					{
						float.TryParse(xmlDocument.FirstChild.Attributes["age"].Value, out age);
					}
					if (xmlDocument.FirstChild.Attributes["weight"] != null)
					{
						float.TryParse(xmlDocument.FirstChild.Attributes["weight"].Value, out weight);
					}
					if (xmlDocument.FirstChild.Attributes["build"] != null)
					{
						float.TryParse(xmlDocument.FirstChild.Attributes["build"].Value, out build);
					}
					bodyProperties = new BodyProperties(new DynamicBodyProperties(age, weight, build), bodyProperties.StaticProperties);
					return true;
				}
				bodyProperties = default(BodyProperties);
				return false;
			}
			Debug.FailedAssert("unknown body properties format:\n" + keyValue, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\BodyProperties.cs", "FromString", 148);
			bodyProperties = default(BodyProperties);
			return false;
		}

		// Token: 0x0600019C RID: 412 RVA: 0x00006D60 File Offset: 0x00004F60
		public static BodyProperties GetRandomBodyProperties(int race, bool isFemale, BodyProperties bodyPropertiesMin, BodyProperties bodyPropertiesMax, int hairCoverType, int seed, string hairTags, string beardTags, string tattooTags, float variationAmount = 0f)
		{
			variationAmount = MathF.Max(variationAmount, 0f);
			return FaceGen.GetRandomBodyProperties(race, isFemale, bodyPropertiesMin, bodyPropertiesMax, hairCoverType, seed, hairTags, beardTags, tattooTags, variationAmount);
		}

		// Token: 0x0600019D RID: 413 RVA: 0x00006D90 File Offset: 0x00004F90
		public static bool operator ==(BodyProperties a, BodyProperties b)
		{
			return a == b || (a != null && b != null && a._staticBodyProperties == b._staticBodyProperties && a._dynamicBodyProperties == b._dynamicBodyProperties);
		}

		// Token: 0x0600019E RID: 414 RVA: 0x00006DE5 File Offset: 0x00004FE5
		public static bool operator !=(BodyProperties a, BodyProperties b)
		{
			return !(a == b);
		}

		// Token: 0x0600019F RID: 415 RVA: 0x00006DF4 File Offset: 0x00004FF4
		public override string ToString()
		{
			MBStringBuilder mbstringBuilder = default(MBStringBuilder);
			mbstringBuilder.Initialize(150, "ToString");
			mbstringBuilder.Append<string>("<BodyProperties version=\"4\" ");
			mbstringBuilder.Append<string>(this._dynamicBodyProperties.ToString() + " ");
			mbstringBuilder.Append<string>(this._staticBodyProperties.ToString());
			mbstringBuilder.Append<string>(" />");
			return mbstringBuilder.ToStringAndRelease();
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x00006E80 File Offset: 0x00005080
		public override bool Equals(object obj)
		{
			if (!(obj is BodyProperties))
			{
				return false;
			}
			BodyProperties bodyProperties = (BodyProperties)obj;
			return EqualityComparer<DynamicBodyProperties>.Default.Equals(this._dynamicBodyProperties, bodyProperties._dynamicBodyProperties) && EqualityComparer<StaticBodyProperties>.Default.Equals(this._staticBodyProperties, bodyProperties._staticBodyProperties);
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x00006ECE File Offset: 0x000050CE
		public override int GetHashCode()
		{
			return (2041866711 * -1521134295 + EqualityComparer<DynamicBodyProperties>.Default.GetHashCode(this._dynamicBodyProperties)) * -1521134295 + EqualityComparer<StaticBodyProperties>.Default.GetHashCode(this._staticBodyProperties);
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x00006F04 File Offset: 0x00005104
		public BodyProperties ClampForMultiplayer()
		{
			float age = MathF.Clamp(this.DynamicProperties.Age, 22f, 128f);
			DynamicBodyProperties dynamicBodyProperties = new DynamicBodyProperties(age, 0.5f, 0.5f);
			StaticBodyProperties staticProperties = this.StaticProperties;
			StaticBodyProperties staticBodyProperties = this.ClampHeightMultiplierFaceKey(staticProperties);
			return new BodyProperties(dynamicBodyProperties, staticBodyProperties);
		}

		// Token: 0x060001A3 RID: 419 RVA: 0x00006F54 File Offset: 0x00005154
		private StaticBodyProperties ClampHeightMultiplierFaceKey(in StaticBodyProperties staticBodyProperties)
		{
			StaticBodyProperties staticBodyProperties2 = staticBodyProperties;
			ulong keyPart = staticBodyProperties2.KeyPart8;
			float num = (float)BodyProperties.GetBitsValueFromKey(keyPart, 19, 6) / 63f;
			if (num < 0.25f || num > 0.75f)
			{
				num = 0.5f;
				int inewValue = (int)(num * 63f);
				ulong num2 = BodyProperties.SetBits(keyPart, 19, 6, inewValue);
				staticBodyProperties2 = staticBodyProperties;
				ulong keyPart2 = staticBodyProperties2.KeyPart1;
				staticBodyProperties2 = staticBodyProperties;
				ulong keyPart3 = staticBodyProperties2.KeyPart2;
				staticBodyProperties2 = staticBodyProperties;
				ulong keyPart4 = staticBodyProperties2.KeyPart3;
				staticBodyProperties2 = staticBodyProperties;
				ulong keyPart5 = staticBodyProperties2.KeyPart4;
				staticBodyProperties2 = staticBodyProperties;
				ulong keyPart6 = staticBodyProperties2.KeyPart5;
				staticBodyProperties2 = staticBodyProperties;
				ulong keyPart7 = staticBodyProperties2.KeyPart6;
				ulong keyPart8 = num2;
				staticBodyProperties2 = staticBodyProperties;
				return new StaticBodyProperties(keyPart2, keyPart3, keyPart4, keyPart5, keyPart6, keyPart7, keyPart8, staticBodyProperties2.KeyPart8);
			}
			return staticBodyProperties;
		}

		// Token: 0x060001A4 RID: 420 RVA: 0x00007024 File Offset: 0x00005224
		private static ulong SetBits(in ulong ipart7, int startBit, int numBits, int inewValue)
		{
			ulong num = ipart7;
			ulong num2 = MathF.PowTwo64(numBits) - 1UL << startBit;
			return (num & ~num2) | (ulong)((ulong)((long)inewValue) << startBit);
		}

		// Token: 0x060001A5 RID: 421 RVA: 0x00007050 File Offset: 0x00005250
		private static int GetBitsValueFromKey(in ulong part, int startBit, int numBits)
		{
			ulong num = part >> startBit;
			ulong num2 = MathF.PowTwo64(numBits) - 1UL;
			return (int)(num & num2);
		}

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x060001A6 RID: 422 RVA: 0x00007074 File Offset: 0x00005274
		public static BodyProperties Default
		{
			get
			{
				return new BodyProperties(new DynamicBodyProperties(20f, 0f, 0f), default(StaticBodyProperties));
			}
		}

		// Token: 0x04000170 RID: 368
		private readonly DynamicBodyProperties _dynamicBodyProperties;

		// Token: 0x04000171 RID: 369
		private readonly StaticBodyProperties _staticBodyProperties;

		// Token: 0x04000172 RID: 370
		private const float DefaultAge = 30f;

		// Token: 0x04000173 RID: 371
		private const float DefaultWeight = 0.5f;

		// Token: 0x04000174 RID: 372
		private const float DefaultBuild = 0.5f;
	}
}
