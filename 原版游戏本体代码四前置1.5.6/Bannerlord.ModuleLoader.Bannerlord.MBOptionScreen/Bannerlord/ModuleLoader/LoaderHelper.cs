#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.ModuleManager;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ModuleLoader;

internal static class LoaderHelper
{
	private delegate MBSubModuleBase ConstructorDelegate();

	private static ApplicationVersion? GameVersion()
	{
		if (!ApplicationVersion.TryParse(ApplicationVersionHelper.GameVersionStr(), out ApplicationVersion version))
		{
			return null;
		}
		return version;
	}

	public static IEnumerable<MBSubModuleBase> LoadAllImplementations(string filterWildcard)
	{
		Trace.TraceInformation("Loading implementations...");
		FileInfo fileInfo = new FileInfo(typeof(LoaderHelper).Assembly.Location);
		if (!fileInfo.Exists)
		{
			Trace.TraceError("Assembly file does not exists!");
			yield break;
		}
		DirectoryInfo directory = fileInfo.Directory;
		if (directory == null || !directory.Exists)
		{
			Trace.TraceError("Assembly directory does not exists!");
			yield break;
		}
		FileInfo[] files = directory.GetFiles(filterWildcard);
		if (files.Length == 0)
		{
			Trace.TraceError("No implementations found.");
			yield break;
		}
		ApplicationVersion gameVersion = GameVersion();
		if ((object)gameVersion == null)
		{
			Trace.TraceError("Failed to get Game version!");
			yield break;
		}
		HashSet<string> loadedAssemblies = (from a in AppDomain.CurrentDomain.GetAssemblies()
			where !a.IsDynamic && !string.IsNullOrEmpty(a.Location)
			select Path.GetFileNameWithoutExtension(a.Location)).ToHashSet();
		List<ImplementationFile> implementations = GetImplementations(files.Where((FileInfo x) => !loadedAssemblies.Contains(Path.GetFileNameWithoutExtension(x.Name))).ToList());
		if (implementations.Count == 0)
		{
			Trace.TraceError("No compatible implementations were found!");
			yield break;
		}
		List<ImplementationFile> list = implementations.Where((ImplementationFile i) => gameVersion.IsSame(i.Version)).ToList();
		int count = list.Count;
		ImplementationFile implementationFile;
		if (count <= 1)
		{
			if (count == 1)
			{
				Trace.TraceInformation("Found one matching implementation.");
				implementationFile = list.First();
			}
			else
			{
				Trace.TraceInformation("No exact match found. Selecting the latest available implementation.");
				implementationFile = SelectLatestImplementation(implementations);
			}
		}
		else
		{
			Trace.TraceInformation("Found multiple matching implementations:");
			foreach (ImplementationFile item in list)
			{
				Trace.TraceInformation("Implementation {0} for game {1}", item.Implementation.Name, item.Version);
			}
			Trace.TraceInformation("Selecting the latest available implementation.");
			implementationFile = SelectLatestImplementation(list);
		}
		if ((object)implementationFile == null)
		{
			yield break;
		}
		Trace.TraceInformation("Loading implementation {0} for game {1}", implementationFile.Implementation.Name, implementationFile.Version);
		Assembly assembly = Assembly.LoadFrom(implementationFile.Implementation.FullName);
		bool found = false;
		foreach (MBSubModuleBase item2 in LoadSubModules(assembly))
		{
			found = true;
			yield return item2;
		}
		if (!found)
		{
			Trace.TraceError("No implementation was initialized!");
		}
		Trace.TraceInformation("Finished loading implementations.");
	}

	private static ImplementationFile? SelectLatestImplementation(IEnumerable<ImplementationFile> implementations)
	{
		ApplicationVersion maxKey;
		return implementations.MaxBy<ImplementationFile, ApplicationVersion>((ImplementationFile x) => x.Version, new ApplicationVersionComparer(), out maxKey);
	}

	private static IEnumerable<MBSubModuleBase> LoadSubModules(Assembly assembly)
	{
		IEnumerable<Type> enumerable = from t in AccessTools2.GetTypesFromAssembly(assembly)
			where !t.IsAbstract && typeof(MBSubModuleBase).IsAssignableFrom(t)
			select t;
		foreach (Type item in enumerable)
		{
			ConstructorInfo constructorInfo = AccessTools2.Constructor(item, Type.EmptyTypes, searchForStatic: false, logErrorInTrace: false);
			if ((object)constructorInfo == null)
			{
				Trace.TraceError("SubModule {0} is missing a default constructor! Assembly: {1}", item.FullName, item.Assembly.FullName);
				continue;
			}
			ConstructorDelegate constructorDelegate = AccessTools2.GetDelegate<ConstructorDelegate>(constructorInfo, logErrorInTrace: false);
			if (constructorDelegate == null)
			{
				Trace.TraceError("SubModule {0}'s default constructor could not be converted to a delegate! Assembly: {1}", item.FullName, assembly.FullName);
			}
			else
			{
				yield return constructorDelegate();
			}
		}
	}

	private static List<ImplementationFile> GetImplementations(IEnumerable<FileInfo> implementations)
	{
		List<ImplementationFile> list = new List<ImplementationFile>();
		foreach (FileInfo implementation in implementations)
		{
			Trace.TraceInformation("Found implementation: {0}", implementation.Name);
			ApplicationVersion applicationVersion = ExtractGameVersion(implementation);
			if ((object)applicationVersion == null)
			{
				Trace.TraceError("Implementation {0} is missing or has an invalid GameVersion AssemblyMetadataAttribute!", implementation.Name);
			}
			else
			{
				list.Add(new ImplementationFile(implementation, applicationVersion));
			}
		}
		return list;
	}

	private static ApplicationVersion? ExtractGameVersion(FileInfo implementation)
	{
		using FileStream peStream = File.OpenRead(implementation.FullName);
		using System.Reflection.PortableExecutable.PEReader peReader = new System.Reflection.PortableExecutable.PEReader(peStream);
		System.Reflection.Metadata.MetadataReader mdReader = peReader.GetMetadataReader(System.Reflection.Metadata.MetadataReaderOptions.None);
		foreach (CustomAttribute item in from ah in mdReader.GetAssemblyDefinition().GetCustomAttributes()
			select mdReader.GetCustomAttribute(ah))
		{
			EntityHandle constructor = item.Constructor;
			if (constructor.Kind != System.Reflection.Metadata.HandleKind.MemberReference)
			{
				continue;
			}
			EntityHandle parent = mdReader.GetMemberReference((MemberReferenceHandle)constructor).Parent;
			StringHandle name = mdReader.GetTypeReference((TypeReferenceHandle)parent).Name;
			if (string.Equals(mdReader.GetString(name), "AssemblyMetadataAttribute", StringComparison.Ordinal))
			{
				System.Reflection.Metadata.BlobReader blobReader = mdReader.GetBlobReader(item.Value);
				blobReader.ReadByte();
				blobReader.ReadByte();
				string? a = blobReader.ReadSerializedString();
				string versionAsString = blobReader.ReadSerializedString();
				if (string.Equals(a, "GameVersion", StringComparison.Ordinal) && ApplicationVersion.TryParse(versionAsString, out ApplicationVersion version))
				{
					return version;
				}
			}
		}
		return null;
	}
}
