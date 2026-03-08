using System;
using System.Xml;
using Newtonsoft.Json;

namespace TaleWorlds.Library
{
	// Token: 0x0200000A RID: 10
	[JsonConverter(typeof(ApplicationVersionJsonConverter))]
	[Serializable]
	public struct ApplicationVersion
	{
		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600000C RID: 12 RVA: 0x000020D8 File Offset: 0x000002D8
		// (set) Token: 0x0600000D RID: 13 RVA: 0x000020E0 File Offset: 0x000002E0
		[JsonIgnore]
		public ApplicationVersionType ApplicationVersionType { get; private set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600000E RID: 14 RVA: 0x000020E9 File Offset: 0x000002E9
		// (set) Token: 0x0600000F RID: 15 RVA: 0x000020F1 File Offset: 0x000002F1
		[JsonIgnore]
		public int Major { get; private set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000010 RID: 16 RVA: 0x000020FA File Offset: 0x000002FA
		// (set) Token: 0x06000011 RID: 17 RVA: 0x00002102 File Offset: 0x00000302
		[JsonIgnore]
		public int Minor { get; private set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000012 RID: 18 RVA: 0x0000210B File Offset: 0x0000030B
		// (set) Token: 0x06000013 RID: 19 RVA: 0x00002113 File Offset: 0x00000313
		[JsonIgnore]
		public int Revision { get; private set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000014 RID: 20 RVA: 0x0000211C File Offset: 0x0000031C
		// (set) Token: 0x06000015 RID: 21 RVA: 0x00002124 File Offset: 0x00000324
		[JsonIgnore]
		public int ChangeSet { get; private set; }

		// Token: 0x06000016 RID: 22 RVA: 0x0000212D File Offset: 0x0000032D
		public ApplicationVersion(ApplicationVersionType applicationVersionType, int major, int minor, int revision, int changeSet)
		{
			this.ApplicationVersionType = applicationVersionType;
			this.Major = major;
			this.Minor = minor;
			this.Revision = revision;
			this.ChangeSet = changeSet;
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002154 File Offset: 0x00000354
		public static ApplicationVersion FromParametersFile(string customParameterFilePath = null)
		{
			string filePath = ((customParameterFilePath == null) ? (BasePath.Name + "Parameters/Version.xml") : customParameterFilePath);
			XmlDocument xmlDocument = new XmlDocument();
			string fileContent = VirtualFolders.GetFileContent(filePath, null);
			if (fileContent == "")
			{
				return ApplicationVersion.Empty;
			}
			xmlDocument.LoadXml(fileContent);
			return ApplicationVersion.FromString(xmlDocument.ChildNodes[0].ChildNodes[0].Attributes["Value"].InnerText, 0);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000021D0 File Offset: 0x000003D0
		public static ApplicationVersion FromString(string versionAsString, int defaultChangeSet = 0)
		{
			string[] array = versionAsString.Split(new char[] { '.' });
			if (array.Length != 3 && array.Length != 4)
			{
				throw new Exception("Wrong version as string");
			}
			ApplicationVersionType applicationVersionType = ApplicationVersion.ApplicationVersionTypeFromString(array[0][0].ToString());
			string value = array[0].Substring(1);
			string value2 = array[1];
			string value3 = array[2];
			int major = Convert.ToInt32(value);
			int minor = Convert.ToInt32(value2);
			int revision = Convert.ToInt32(value3);
			int changeSet = ((array.Length > 3) ? Convert.ToInt32(array[3]) : defaultChangeSet);
			return new ApplicationVersion(applicationVersionType, major, minor, revision, changeSet);
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002264 File Offset: 0x00000464
		public bool IsSame(ApplicationVersion other, bool checkChangeSet)
		{
			return this.ApplicationVersionType == other.ApplicationVersionType && this.Major == other.Major && this.Minor == other.Minor && this.Revision == other.Revision && (!checkChangeSet || this.ChangeSet == other.ChangeSet);
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000022C4 File Offset: 0x000004C4
		public bool IsOlderThan(ApplicationVersion other)
		{
			if (this.ApplicationVersionType < other.ApplicationVersionType)
			{
				return true;
			}
			if (this.ApplicationVersionType == other.ApplicationVersionType)
			{
				if (this.Major < other.Major)
				{
					return true;
				}
				if (this.Major == other.Major)
				{
					if (this.Minor < other.Minor)
					{
						return true;
					}
					if (this.Minor == other.Minor)
					{
						if (this.Revision < other.Revision)
						{
							return true;
						}
						if (this.Revision == other.Revision && this.ChangeSet < other.ChangeSet)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002363 File Offset: 0x00000563
		public bool IsNewerThan(ApplicationVersion other)
		{
			return !this.IsSame(other, true) && !this.IsOlderThan(other);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x0000237C File Offset: 0x0000057C
		public static ApplicationVersionType ApplicationVersionTypeFromString(string applicationVersionTypeAsString)
		{
			ApplicationVersionType result;
			if (!(applicationVersionTypeAsString == "a"))
			{
				if (!(applicationVersionTypeAsString == "b"))
				{
					if (!(applicationVersionTypeAsString == "e"))
					{
						if (!(applicationVersionTypeAsString == "v"))
						{
							if (!(applicationVersionTypeAsString == "d"))
							{
								Debug.FailedAssert("Invalid version type.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\ApplicationVersion.cs", "ApplicationVersionTypeFromString", 158);
								result = ApplicationVersionType.Invalid;
							}
							else
							{
								result = ApplicationVersionType.Development;
							}
						}
						else
						{
							result = ApplicationVersionType.Release;
						}
					}
					else
					{
						result = ApplicationVersionType.EarlyAccess;
					}
				}
				else
				{
					result = ApplicationVersionType.Beta;
				}
			}
			else
			{
				result = ApplicationVersionType.Alpha;
			}
			return result;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002400 File Offset: 0x00000600
		public static string GetPrefix(ApplicationVersionType applicationVersionType)
		{
			string result;
			switch (applicationVersionType)
			{
			case ApplicationVersionType.Alpha:
				result = "a";
				break;
			case ApplicationVersionType.Beta:
				result = "b";
				break;
			case ApplicationVersionType.EarlyAccess:
				result = "e";
				break;
			case ApplicationVersionType.Release:
				result = "v";
				break;
			case ApplicationVersionType.Development:
				result = "d";
				break;
			default:
				result = "i";
				break;
			}
			return result;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002460 File Offset: 0x00000660
		public override string ToString()
		{
			string prefix = ApplicationVersion.GetPrefix(this.ApplicationVersionType);
			return string.Concat(new object[] { prefix, this.Major, ".", this.Minor, ".", this.Revision, ".", this.ChangeSet });
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000024D8 File Offset: 0x000006D8
		public static bool operator ==(ApplicationVersion a, ApplicationVersion b)
		{
			return a.Major == b.Major && a.Minor == b.Minor && a.Revision == b.Revision && a.ApplicationVersionType == b.ApplicationVersionType;
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002527 File Offset: 0x00000727
		public static bool operator !=(ApplicationVersion a, ApplicationVersion b)
		{
			return !(a == b);
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002533 File Offset: 0x00000733
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002545 File Offset: 0x00000745
		public override bool Equals(object obj)
		{
			return obj != null && !(base.GetType() != obj.GetType()) && (ApplicationVersion)obj == this;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x0000257C File Offset: 0x0000077C
		public static bool operator >(ApplicationVersion a, ApplicationVersion b)
		{
			if (a.ApplicationVersionType > b.ApplicationVersionType)
			{
				return true;
			}
			if (a.ApplicationVersionType == b.ApplicationVersionType)
			{
				if (a.Major > b.Major)
				{
					return true;
				}
				if (a.Major == b.Major)
				{
					if (a.Minor > b.Minor)
					{
						return true;
					}
					if (a.Minor == b.Minor && a.Revision > b.Revision)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002602 File Offset: 0x00000802
		public static bool operator <(ApplicationVersion a, ApplicationVersion b)
		{
			return !(a == b) && !(a > b);
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002619 File Offset: 0x00000819
		public static bool operator >=(ApplicationVersion a, ApplicationVersion b)
		{
			return a == b || a > b;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002630 File Offset: 0x00000830
		public static bool operator <=(ApplicationVersion a, ApplicationVersion b)
		{
			return a == b || a < b;
		}

		// Token: 0x0400001F RID: 31
		public const int DefaultChangeSet = 106033;

		// Token: 0x04000020 RID: 32
		[JsonIgnore]
		public static readonly ApplicationVersion Empty = new ApplicationVersion(ApplicationVersionType.Invalid, -1, -1, -1, -1);
	}
}
