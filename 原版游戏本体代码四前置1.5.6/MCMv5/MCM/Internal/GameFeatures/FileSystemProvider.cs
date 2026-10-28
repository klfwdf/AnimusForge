using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions.GameFeatures;
using MCM.Internal.Extensions;
using TaleWorlds.Library;

namespace MCM.Internal.GameFeatures;

internal sealed class FileSystemProvider : IFileSystemProvider
{
	private static class PlatformFileHelperUtils
	{
		private delegate PlatformFilePath[] V1Delegate(object instance, PlatformDirectoryPath path, string searchPattern);

		private delegate PlatformFilePath[] V2Delegate(object instance, PlatformDirectoryPath path, string searchPattern, int searchOption);

		private static readonly object _sync = new object();

		private static Type? CapturedType;

		private static V1Delegate? V1;

		private static V2Delegate? V2;

		private static void ReCheck(Type type)
		{
			if (CapturedType == type)
			{
				return;
			}
			lock (_sync)
			{
				if (CapturedType == type)
				{
					return;
				}
				V1 = null;
				V2 = null;
				foreach (MethodInfo item in from x in AccessTools.GetDeclaredMethods(type)
					where x.Name == "GetFiles"
					select x)
				{
					ParameterInfo[] parameters = item.GetParameters();
					if (parameters.Length == 2)
					{
						V1 = AccessTools2.GetDelegate<V1Delegate>(item);
					}
					if (parameters.Length == 3)
					{
						V2 = AccessTools2.GetDelegate<V2Delegate>(item);
					}
				}
				CapturedType = type;
			}
		}

		public static PlatformFilePath[] GetFiles(object instance, PlatformDirectoryPath path, string searchPattern)
		{
			ReCheck(instance.GetType());
			if (V1 != null)
			{
				return V1(instance, path, searchPattern);
			}
			if (V2 != null)
			{
				return V2(instance, path, searchPattern, 0);
			}
			return Array.Empty<PlatformFilePath>();
		}
	}

	private IPlatformFileHelper PlatformFileHelper => TaleWorlds.Library.Common.PlatformFileHelper;

	private static string EnsureDirectoryEndsInSeparator(string directory)
	{
		return directory.EndsWith("\\") ? directory : (directory + "\\");
	}

	private static GameDirectory GetConfigsDirectory()
	{
		PlatformDirectoryPath platformDirectoryPath = new PlatformDirectoryPath(PlatformFileType.User, EnsureDirectoryEndsInSeparator("Configs"));
		return new GameDirectory((PlatformDirectoryType)platformDirectoryPath.Type, platformDirectoryPath.Path);
	}

	public GameDirectory GetModSettingsDirectory()
	{
		return GetOrCreateDirectory(GetConfigsDirectory(), EnsureDirectoryEndsInSeparator("ModSettings"));
	}

	public GameDirectory? GetDirectory(GameDirectory directory, string name)
	{
		return directory with
		{
			Path = directory.Path + EnsureDirectoryEndsInSeparator(name)
		};
	}

	public GameDirectory GetOrCreateDirectory(GameDirectory directory, string name)
	{
		return directory with
		{
			Path = directory.Path + EnsureDirectoryEndsInSeparator(name)
		};
	}

	public GameFile[] GetFiles(GameDirectory directory, string searchPattern)
	{
		return (from x in PlatformFileHelperUtils.GetFiles(PlatformFileHelper, new PlatformDirectoryPath((PlatformFileType)directory.Type, directory.Path), searchPattern)
			select new GameFile(directory, x.FileName)).ToArray();
	}

	public GameFile? GetFile(GameDirectory directory, string fileName)
	{
		PlatformFilePath path = new PlatformFilePath(new PlatformDirectoryPath((PlatformFileType)directory.Type, directory.Path), fileName);
		return (!PlatformFileHelper.FileExists(path)) ? null : new GameFile(directory, fileName);
	}

	public GameFile GetOrCreateFile(GameDirectory directory, string fileName)
	{
		PlatformFilePath path = new PlatformFilePath(new PlatformDirectoryPath((PlatformFileType)directory.Type, directory.Path), fileName);
		if (!PlatformFileHelper.FileExists(path))
		{
			PlatformFileHelper.SaveFile(path, Array.Empty<byte>());
		}
		return new GameFile(directory, fileName);
	}

	public bool WriteData(GameFile file, byte[]? data)
	{
		PlatformFilePath path = new PlatformFilePath(new PlatformDirectoryPath((PlatformFileType)file.Owner.Type, file.Owner.Path), file.Name);
		if (data == null)
		{
			return PlatformFileHelper.DeleteFile(path);
		}
		return PlatformFileHelper.SaveFile(path, data) == SaveResult.Success;
	}

	public byte[]? ReadData(GameFile file)
	{
		PlatformFilePath platformFilePath = new PlatformFilePath(new PlatformDirectoryPath((PlatformFileType)file.Owner.Type, file.Owner.Path), file.Name);
		return (!PlatformFileHelper.FileExists(platformFilePath)) ? null : PlatformFileHelper.GetFileContent(platformFilePath);
	}

	public string? GetSystemPath(GameFile file)
	{
		PlatformFilePath filePath = new PlatformFilePath(new PlatformDirectoryPath((PlatformFileType)file.Owner.Type, file.Owner.Path), file.Name);
		return PlatformFileHelper.GetFileFullPath(filePath);
	}

	public string? GetSystemPath(GameDirectory directory)
	{
		PlatformDirectoryPath directoryPath = new PlatformDirectoryPath((PlatformFileType)directory.Type, directory.Path);
		return PlatformFileHelperPCExtended.GetDirectoryFullPath(directoryPath);
	}
}
