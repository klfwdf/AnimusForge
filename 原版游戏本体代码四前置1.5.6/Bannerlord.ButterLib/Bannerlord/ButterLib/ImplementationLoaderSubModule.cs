using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.Common.Helpers;
using Bannerlord.ButterLib.SubModuleWrappers2;
using HarmonyLib.BUTR.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib;

public sealed class ImplementationLoaderSubModule : MBSubModuleBaseListWrapper
{
	private delegate MBSubModuleBase ConstructorDelegate();

	private bool ServiceRegistrationWasCalled { get; set; }

	private static IEnumerable<MBSubModuleBase> LoadAllImplementations(ILogger? logger)
	{
		logger?.LogInformation("Loading implementations...");
		List<Assembly> list = new List<Assembly>();
		List<Assembly> assemblies = (from a in AppDomain.CurrentDomain.GetAssemblies()
			where !a.IsDynamic
			select a).ToList();
		FileInfo fileInfo = new FileInfo(typeof(ImplementationLoaderSubModule).Assembly.Location);
		if (!fileInfo.Exists)
		{
			logger?.LogError("Assembly file does not exists!");
			yield break;
		}
		DirectoryInfo directory = fileInfo.Directory;
		if (directory == null || !directory.Exists)
		{
			logger?.LogError("Assembly directory does not exists!");
			yield break;
		}
		FileInfo[] files = directory.GetFiles("Bannerlord.ButterLib.Implementation.*.dll");
		if (files.Length == 0)
		{
			logger?.LogError("No implementations found");
			yield break;
		}
		ApplicationVersion? applicationVersion = ApplicationVersionHelper.GameVersion();
		if (!applicationVersion.HasValue)
		{
			logger?.LogError("Failed to get Game version!");
			yield break;
		}
		List<(FileInfo, ApplicationVersion)> list2 = GetImplementations(files.Where((FileInfo x) => assemblies.All((Assembly a) => Path.GetFileNameWithoutExtension(a.Location) != Path.GetFileNameWithoutExtension(x.Name))), logger).ToList();
		if (list2.Count == 0)
		{
			logger?.LogError("No compatible implementations were found!");
			yield break;
		}
		List<(FileInfo, ApplicationVersion)> list3 = ImplementationForGameVersion(applicationVersion.Value, list2).ToList();
		int count = list3.Count;
		if (count <= 1)
		{
			switch (count)
			{
			case 1:
				logger?.LogInformation("Found matching implementation. Loading it");
				var (fileInfo3, applicationVersion3) = list3[0];
				logger?.LogInformation("Implementation {Name} for game {GameVersion} is loaded", fileInfo3.Name, applicationVersion3);
				list.Add(Assembly.LoadFrom(fileInfo3.FullName));
				break;
			case 0:
				logger?.LogInformation("Found no matching implementations. Loading the latest available");
				var (fileInfo2, applicationVersion2) = ImplementationLatest(list2);
				logger?.LogInformation("Implementation {Name} for game {GameVersion} is loaded", fileInfo2.Name, applicationVersion2);
				list.Add(Assembly.LoadFrom(fileInfo2.FullName));
				break;
			}
		}
		else
		{
			logger?.LogInformation("Found multiple matching implementations:");
			foreach (var (fileInfo4, applicationVersion4) in list3)
			{
				logger?.LogInformation("Implementation {Name} for game {GameVersion}", fileInfo4.Name, applicationVersion4);
			}
			logger?.LogInformation("Loading the latest available");
			var (fileInfo5, applicationVersion5) = ImplementationLatest(list3);
			logger?.LogInformation("Implementation {Name} for game {GameVersion} is loaded", fileInfo5.Name, applicationVersion5);
			list.Add(Assembly.LoadFrom(fileInfo5.FullName));
		}
		List<Type> list4 = list.SelectMany(delegate(Assembly a)
		{
			try
			{
				return from t in a.GetTypes()
					where typeof(MBSubModuleBase).IsAssignableFrom(t)
					select t;
			}
			catch (Exception ex) when (ex is ReflectionTypeLoadException)
			{
				logger?.LogError(ex, "Implementation {Name} is not compatible with the current game!", Path.GetFileName(a.Location));
				return Array.Empty<Type>();
			}
		}).ToList();
		if (list4.Count == 0)
		{
			logger?.LogError("No implementation was initialized!");
		}
		foreach (Type item in list4)
		{
			ConstructorInfo constructor = item.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, Type.EmptyTypes, null);
			if ((object)constructor == null)
			{
				logger?.LogError("SubModule {SubModuleType} is missing a default constructor!", item);
				continue;
			}
			ConstructorDelegate constructorDelegate = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<ConstructorDelegate>(constructor);
			if (constructorDelegate == null)
			{
				logger?.LogError("SubModule {SubModuleType}'s default constructor could not be converted to a delegate!", item);
			}
			else
			{
				yield return constructorDelegate();
			}
		}
		logger?.LogInformation("Finished loading implementations");
	}

	private static IEnumerable<(FileInfo Implementation, ApplicationVersion Version)> GetImplementations(IEnumerable<FileInfo> implementations, ILogger? logger = null)
	{
		foreach (FileInfo implementation in implementations)
		{
			bool found = false;
			logger?.LogInformation("Found implementation {Name}", implementation.Name);
			using FileStream fs = File.OpenRead(implementation.FullName);
			using PEReader peReader = new PEReader(fs);
			MetadataReader mdReader = peReader.GetMetadataReader(MetadataReaderOptions.None);
			foreach (CustomAttribute item in from ah in mdReader.GetAssemblyDefinition().GetCustomAttributes()
				select mdReader.GetCustomAttribute(ah))
			{
				EntityHandle constructor = item.Constructor;
				if (constructor.Kind != HandleKind.MemberReference)
				{
					continue;
				}
				EntityHandle parent = mdReader.GetMemberReference((MemberReferenceHandle)constructor).Parent;
				StringHandle name = mdReader.GetTypeReference((TypeReferenceHandle)parent).Name;
				if (!string.Equals(mdReader.GetString(name), "AssemblyMetadataAttribute"))
				{
					continue;
				}
				BlobReader blobReader = mdReader.GetBlobReader(item.Value);
				blobReader.ReadByte();
				blobReader.ReadByte();
				string a = blobReader.ReadSerializedString();
				string versionAsString = blobReader.ReadSerializedString();
				if (string.Equals(a, "GameVersion"))
				{
					if (ApplicationVersionHelper.TryParse(versionAsString, out var version))
					{
						found = true;
						yield return (Implementation: implementation, Version: version);
						break;
					}
					logger?.LogError("Implementation {Name} has invalid GameVersion AssemblyMetadataAttribute!", implementation.Name);
				}
			}
			if (!found)
			{
				logger?.LogError("Implementation {Name} is missing GameVersion AssemblyMetadataAttribute!", implementation.Name);
			}
		}
	}

	private static IEnumerable<(FileInfo Implementation, ApplicationVersion Version)> ImplementationForGameVersion(ApplicationVersion gameVersion, IEnumerable<(FileInfo Implementation, ApplicationVersion Verion)> implementations)
	{
		foreach (var (item, applicationVersion) in implementations)
		{
			if (applicationVersion.Revision == -1)
			{
				if (gameVersion.IsSameWithoutRevision(applicationVersion))
				{
					yield return (Implementation: item, Version: applicationVersion);
				}
			}
			else if (gameVersion.IsSameWithRevision(applicationVersion))
			{
				yield return (Implementation: item, Version: applicationVersion);
			}
		}
	}

	private static (FileInfo Implementation, ApplicationVersion Version) ImplementationLatest(IEnumerable<(FileInfo Implementation, ApplicationVersion Version)> implementations)
	{
		ApplicationVersion maxKey;
		return implementations.MaxBy<(FileInfo, ApplicationVersion), ApplicationVersion>(((FileInfo Implementation, ApplicationVersion Version) x) => x.Version, new ApplicationVersionComparer(), out maxKey);
	}

	public override void OnServiceRegistration()
	{
		ServiceRegistrationWasCalled = true;
		ILogger<ImplementationLoaderSubModule> logger = this.GetTempServiceProvider()?.GetService<ILogger<ImplementationLoaderSubModule>>() ?? NullLogger<ImplementationLoaderSubModule>.Instance;
		_subModules.AddRange((from x in LoadAllImplementations(logger)
			select new MBSubModuleBaseWrapper(x)).ToList());
		base.OnServiceRegistration();
	}

	public override void OnSubModuleLoad()
	{
		if (!ServiceRegistrationWasCalled)
		{
			ILogger<ImplementationLoaderSubModule> logger = this.GetTempServiceProvider()?.GetService<ILogger<ImplementationLoaderSubModule>>() ?? NullLogger<ImplementationLoaderSubModule>.Instance;
			_subModules.AddRange((from x in LoadAllImplementations(logger)
				select new MBSubModuleBaseWrapper(x)).ToList());
		}
		base.OnSubModuleLoad();
	}
}
