using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace Bannerlord.ModuleManager
{
	// Token: 0x0200005F RID: 95
	[NullableContext(1)]
	[Nullable(0)]
	internal class SubModuleInfoExtended : IEquatable<SubModuleInfoExtended>
	{
		// Token: 0x1700010A RID: 266
		// (get) Token: 0x06000380 RID: 896 RVA: 0x0000CC93 File Offset: 0x0000AE93
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(SubModuleInfoExtended);
			}
		}

		// Token: 0x1700010B RID: 267
		// (get) Token: 0x06000381 RID: 897 RVA: 0x0000CC9F File Offset: 0x0000AE9F
		// (set) Token: 0x06000382 RID: 898 RVA: 0x0000CCA7 File Offset: 0x0000AEA7
		public string Name { get; set; }

		// Token: 0x1700010C RID: 268
		// (get) Token: 0x06000383 RID: 899 RVA: 0x0000CCB0 File Offset: 0x0000AEB0
		// (set) Token: 0x06000384 RID: 900 RVA: 0x0000CCB8 File Offset: 0x0000AEB8
		public string DLLName { get; set; }

		// Token: 0x1700010D RID: 269
		// (get) Token: 0x06000385 RID: 901 RVA: 0x0000CCC1 File Offset: 0x0000AEC1
		// (set) Token: 0x06000386 RID: 902 RVA: 0x0000CCC9 File Offset: 0x0000AEC9
		public IReadOnlyList<string> Assemblies { get; set; }

		// Token: 0x1700010E RID: 270
		// (get) Token: 0x06000387 RID: 903 RVA: 0x0000CCD2 File Offset: 0x0000AED2
		// (set) Token: 0x06000388 RID: 904 RVA: 0x0000CCDA File Offset: 0x0000AEDA
		public string SubModuleClassType { get; set; }

		// Token: 0x1700010F RID: 271
		// (get) Token: 0x06000389 RID: 905 RVA: 0x0000CCE3 File Offset: 0x0000AEE3
		// (set) Token: 0x0600038A RID: 906 RVA: 0x0000CCEB File Offset: 0x0000AEEB
		public IReadOnlyDictionary<string, IReadOnlyList<string>> Tags { get; set; }

		// Token: 0x0600038B RID: 907 RVA: 0x0000CCF4 File Offset: 0x0000AEF4
		public SubModuleInfoExtended()
		{
			this.Name = string.Empty;
			this.DLLName = string.Empty;
			this.Assemblies = Array.Empty<string>();
			this.SubModuleClassType = string.Empty;
			this.Tags = new Dictionary<string, IReadOnlyList<string>>();
			base..ctor();
		}

		// Token: 0x0600038C RID: 908 RVA: 0x0000CD34 File Offset: 0x0000AF34
		public SubModuleInfoExtended(string name, string dllName, IReadOnlyList<string> assemblies, string subModuleClassType, IReadOnlyDictionary<string, IReadOnlyList<string>> tags)
		{
			this.Name = string.Empty;
			this.DLLName = string.Empty;
			this.Assemblies = Array.Empty<string>();
			this.SubModuleClassType = string.Empty;
			this.Tags = new Dictionary<string, IReadOnlyList<string>>();
			base..ctor();
			this.Name = name;
			this.DLLName = dllName;
			this.Assemblies = assemblies;
			this.SubModuleClassType = subModuleClassType;
			this.Tags = tags;
		}

		// Token: 0x0600038D RID: 909 RVA: 0x0000CDA4 File Offset: 0x0000AFA4
		[NullableContext(2)]
		public static SubModuleInfoExtended FromXml(XmlNode subModuleNode)
		{
			if (subModuleNode == null)
			{
				return null;
			}
			XmlNode xmlNode = subModuleNode.SelectSingleNode("Name");
			string text;
			if (xmlNode == null)
			{
				text = null;
			}
			else
			{
				XmlAttributeCollection attributes = xmlNode.Attributes;
				if (attributes == null)
				{
					text = null;
				}
				else
				{
					XmlAttribute xmlAttribute = attributes["value"];
					text = ((xmlAttribute != null) ? xmlAttribute.InnerText : null);
				}
			}
			string name = text ?? string.Empty;
			XmlNode xmlNode2 = subModuleNode.SelectSingleNode("DLLName");
			string text2;
			if (xmlNode2 == null)
			{
				text2 = null;
			}
			else
			{
				XmlAttributeCollection attributes2 = xmlNode2.Attributes;
				if (attributes2 == null)
				{
					text2 = null;
				}
				else
				{
					XmlAttribute xmlAttribute2 = attributes2["value"];
					text2 = ((xmlAttribute2 != null) ? xmlAttribute2.InnerText : null);
				}
			}
			string dllName = text2 ?? string.Empty;
			XmlNode xmlNode3 = subModuleNode.SelectSingleNode("SubModuleClassType");
			string text3;
			if (xmlNode3 == null)
			{
				text3 = null;
			}
			else
			{
				XmlAttributeCollection attributes3 = xmlNode3.Attributes;
				if (attributes3 == null)
				{
					text3 = null;
				}
				else
				{
					XmlAttribute xmlAttribute3 = attributes3["value"];
					text3 = ((xmlAttribute3 != null) ? xmlAttribute3.InnerText : null);
				}
			}
			string subModuleClassType = text3 ?? string.Empty;
			string[] assemblies = Array.Empty<string>();
			if (subModuleNode.SelectSingleNode("Assemblies") != null)
			{
				XmlNode xmlNode4 = subModuleNode.SelectSingleNode("Assemblies");
				XmlNodeList assembliesList = ((xmlNode4 != null) ? xmlNode4.SelectNodes("Assembly") : null);
				assemblies = new string[(assembliesList != null) ? assembliesList.Count : 0];
				int i = 0;
				for (;;)
				{
					int num = i;
					int? num2 = ((assembliesList != null) ? new int?(assembliesList.Count) : null);
					if (!((num < num2.GetValueOrDefault()) & (num2 != null)))
					{
						break;
					}
					string[] array = assemblies;
					int num3 = i;
					string text4;
					if (assembliesList == null)
					{
						text4 = null;
					}
					else
					{
						XmlNode xmlNode5 = assembliesList[i];
						if (xmlNode5 == null)
						{
							text4 = null;
						}
						else
						{
							XmlAttributeCollection attributes4 = xmlNode5.Attributes;
							if (attributes4 == null)
							{
								text4 = null;
							}
							else
							{
								XmlAttribute xmlAttribute4 = attributes4["value"];
								text4 = ((xmlAttribute4 != null) ? xmlAttribute4.InnerText : null);
							}
						}
					}
					string value = text4;
					array[num3] = ((value != null) ? value : string.Empty);
					i++;
				}
			}
			XmlNode xmlNode6 = subModuleNode.SelectSingleNode("Tags");
			XmlNodeList tagsList = ((xmlNode6 != null) ? xmlNode6.SelectNodes("Tag") : null);
			Dictionary<string, List<string>> tags = new Dictionary<string, List<string>>();
			int j = 0;
			for (;;)
			{
				int num4 = j;
				int? num2 = ((tagsList != null) ? new int?(tagsList.Count) : null);
				if (!((num4 < num2.GetValueOrDefault()) & (num2 != null)))
				{
					break;
				}
				string text5;
				if (tagsList == null)
				{
					text5 = null;
				}
				else
				{
					XmlNode xmlNode7 = tagsList[j];
					if (xmlNode7 == null)
					{
						text5 = null;
					}
					else
					{
						XmlAttributeCollection attributes5 = xmlNode7.Attributes;
						if (attributes5 == null)
						{
							text5 = null;
						}
						else
						{
							XmlAttribute xmlAttribute5 = attributes5["key"];
							text5 = ((xmlAttribute5 != null) ? xmlAttribute5.InnerText : null);
						}
					}
				}
				string key = text5;
				if (key != null)
				{
					XmlNode xmlNode8 = tagsList[j];
					string text6;
					if (xmlNode8 == null)
					{
						text6 = null;
					}
					else
					{
						XmlAttributeCollection attributes6 = xmlNode8.Attributes;
						if (attributes6 == null)
						{
							text6 = null;
						}
						else
						{
							XmlAttribute xmlAttribute6 = attributes6["value"];
							text6 = ((xmlAttribute6 != null) ? xmlAttribute6.InnerText : null);
						}
					}
					string value2 = text6;
					if (value2 != null)
					{
						List<string> list;
						if (tags.TryGetValue(key, out list))
						{
							list.Add(value2);
						}
						else
						{
							tags[key] = new List<string> { value2 };
						}
					}
				}
				j++;
			}
			SubModuleInfoExtended subModuleInfoExtended = new SubModuleInfoExtended();
			subModuleInfoExtended.Name = name;
			subModuleInfoExtended.DLLName = dllName;
			subModuleInfoExtended.Assemblies = assemblies;
			subModuleInfoExtended.SubModuleClassType = subModuleClassType;
			subModuleInfoExtended.Tags = new ReadOnlyDictionary<string, IReadOnlyList<string>>(tags.ToDictionary((KeyValuePair<string, List<string>> x) => x.Key, (KeyValuePair<string, List<string>> x) => new ReadOnlyCollection<string>(x.Value)));
			return subModuleInfoExtended;
		}

		// Token: 0x0600038E RID: 910 RVA: 0x0000D0C4 File Offset: 0x0000B2C4
		public override string ToString()
		{
			return this.Name + " - " + this.DLLName;
		}

		// Token: 0x0600038F RID: 911 RVA: 0x0000D0DC File Offset: 0x0000B2DC
		[NullableContext(2)]
		public virtual bool Equals(SubModuleInfoExtended other)
		{
			return other != null && (this == other || this.Name == other.Name);
		}

		// Token: 0x06000390 RID: 912 RVA: 0x0000D0FA File Offset: 0x0000B2FA
		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		// Token: 0x06000391 RID: 913 RVA: 0x0000D108 File Offset: 0x0000B308
		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("Name = ");
			builder.Append(this.Name);
			builder.Append(", DLLName = ");
			builder.Append(this.DLLName);
			builder.Append(", Assemblies = ");
			builder.Append(this.Assemblies);
			builder.Append(", SubModuleClassType = ");
			builder.Append(this.SubModuleClassType);
			builder.Append(", Tags = ");
			builder.Append(this.Tags);
			return true;
		}

		// Token: 0x06000392 RID: 914 RVA: 0x0000D198 File Offset: 0x0000B398
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(SubModuleInfoExtended left, SubModuleInfoExtended right)
		{
			return !(left == right);
		}

		// Token: 0x06000393 RID: 915 RVA: 0x0000D1A4 File Offset: 0x0000B3A4
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(SubModuleInfoExtended left, SubModuleInfoExtended right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000394 RID: 916 RVA: 0x0000D1B8 File Offset: 0x0000B3B8
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as SubModuleInfoExtended);
		}

		// Token: 0x06000396 RID: 918 RVA: 0x0000D1D0 File Offset: 0x0000B3D0
		[CompilerGenerated]
		protected SubModuleInfoExtended(SubModuleInfoExtended original)
		{
			this.Name = original.<Name>k__BackingField;
			this.DLLName = original.<DLLName>k__BackingField;
			this.Assemblies = original.<Assemblies>k__BackingField;
			this.SubModuleClassType = original.<SubModuleClassType>k__BackingField;
			this.Tags = original.<Tags>k__BackingField;
		}
	}
}
