using System;
using System.Xml;
using Newtonsoft.Json;

namespace TaleWorlds.Library;

[Serializable]
[JsonConverter(typeof(ApplicationVersionJsonConverter))]
public struct ApplicationVersion
{
	public const int DefaultChangeSet = 114896;

	[JsonIgnore]
	public static readonly ApplicationVersion Empty = new ApplicationVersion(ApplicationVersionType.Invalid, -1, -1, -1, -1);

	[JsonIgnore]
	public ApplicationVersionType ApplicationVersionType { get; private set; }

	[JsonIgnore]
	public int Major { get; private set; }

	[JsonIgnore]
	public int Minor { get; private set; }

	[JsonIgnore]
	public int Revision { get; private set; }

	[JsonIgnore]
	public int ChangeSet { get; private set; }

	public ApplicationVersion(ApplicationVersionType applicationVersionType, int major, int minor, int revision, int changeSet)
	{
		ApplicationVersionType = applicationVersionType;
		Major = major;
		Minor = minor;
		Revision = revision;
		ChangeSet = changeSet;
	}

	public static ApplicationVersion FromParametersFile(string customParameterFilePath = null)
	{
		string filePath = ((customParameterFilePath == null) ? (BasePath.Name + "Parameters/Version.xml") : customParameterFilePath);
		XmlDocument xmlDocument = new XmlDocument();
		string fileContent = VirtualFolders.GetFileContent(filePath);
		if (fileContent == "")
		{
			return Empty;
		}
		xmlDocument.LoadXml(fileContent);
		return FromString(xmlDocument.ChildNodes[0].ChildNodes[0].Attributes["Value"].InnerText);
	}

	public static ApplicationVersion FromString(string versionAsString, int defaultChangeSet = 0)
	{
		string[] array = versionAsString.Split(new char[1] { '.' });
		if (array.Length != 3 && array.Length != 4)
		{
			throw new Exception("Wrong version as string");
		}
		ApplicationVersionType applicationVersionType = ApplicationVersionTypeFromString(array[0][0].ToString());
		string value = array[0].Substring(1);
		string value2 = array[1];
		string value3 = array[2];
		int major = Convert.ToInt32(value);
		int minor = Convert.ToInt32(value2);
		int revision = Convert.ToInt32(value3);
		int changeSet = ((array.Length > 3) ? Convert.ToInt32(array[3]) : defaultChangeSet);
		return new ApplicationVersion(applicationVersionType, major, minor, revision, changeSet);
	}

	public bool IsSame(ApplicationVersion other, bool checkChangeSet)
	{
		if (ApplicationVersionType == other.ApplicationVersionType && Major == other.Major && Minor == other.Minor && Revision == other.Revision)
		{
			if (checkChangeSet)
			{
				return ChangeSet == other.ChangeSet;
			}
			return true;
		}
		return false;
	}

	public bool IsOlderThan(ApplicationVersion other)
	{
		if (ApplicationVersionType < other.ApplicationVersionType)
		{
			return true;
		}
		if (ApplicationVersionType == other.ApplicationVersionType)
		{
			if (Major < other.Major)
			{
				return true;
			}
			if (Major == other.Major)
			{
				if (Minor < other.Minor)
				{
					return true;
				}
				if (Minor == other.Minor)
				{
					if (Revision < other.Revision)
					{
						return true;
					}
					if (Revision == other.Revision && ChangeSet < other.ChangeSet)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public bool IsNewerThan(ApplicationVersion other)
	{
		if (!IsSame(other, checkChangeSet: true))
		{
			return !IsOlderThan(other);
		}
		return false;
	}

	public static ApplicationVersionType ApplicationVersionTypeFromString(string applicationVersionTypeAsString)
	{
		ApplicationVersionType applicationVersionType = ApplicationVersionType.Release;
		switch (applicationVersionTypeAsString)
		{
		case "a":
			return ApplicationVersionType.Alpha;
		case "b":
			return ApplicationVersionType.Beta;
		case "e":
			return ApplicationVersionType.EarlyAccess;
		case "v":
			return ApplicationVersionType.Release;
		case "d":
			return ApplicationVersionType.Development;
		default:
			Debug.FailedAssert("Invalid version type.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\ApplicationVersion.cs", "ApplicationVersionTypeFromString", 158);
			return ApplicationVersionType.Invalid;
		}
	}

	public static string GetPrefix(ApplicationVersionType applicationVersionType)
	{
		string text = "v";
		return applicationVersionType switch
		{
			ApplicationVersionType.Alpha => "a", 
			ApplicationVersionType.Beta => "b", 
			ApplicationVersionType.EarlyAccess => "e", 
			ApplicationVersionType.Release => "v", 
			ApplicationVersionType.Development => "d", 
			_ => "i", 
		};
	}

	public override string ToString()
	{
		string prefix = GetPrefix(ApplicationVersionType);
		return prefix + Major + "." + Minor + "." + Revision + "." + ChangeSet;
	}

	public static bool operator ==(ApplicationVersion a, ApplicationVersion b)
	{
		if (a.Major == b.Major && a.Minor == b.Minor && a.Revision == b.Revision)
		{
			return a.ApplicationVersionType == b.ApplicationVersionType;
		}
		return false;
	}

	public static bool operator !=(ApplicationVersion a, ApplicationVersion b)
	{
		return !(a == b);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		return (ApplicationVersion)obj == this;
	}

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

	public static bool operator <(ApplicationVersion a, ApplicationVersion b)
	{
		if (a == b || a > b)
		{
			return false;
		}
		return true;
	}

	public static bool operator >=(ApplicationVersion a, ApplicationVersion b)
	{
		if (a == b || a > b)
		{
			return true;
		}
		return false;
	}

	public static bool operator <=(ApplicationVersion a, ApplicationVersion b)
	{
		if (a == b || a < b)
		{
			return true;
		}
		return false;
	}
}
