using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace AnimusForge;

/// <summary>
/// Shared disk, embedded-resource, and fail-safe loader for localized GCCZ JSON catalogs.
/// </summary>
internal static class GcczLocalizedResourceLoader
{
	internal static T Load<T>(
		string fileName,
		string embeddedResourceName,
		string logCategory,
		Func<string, T> deserialize,
		Func<T> createFallback)
	{
		try
		{
			string path = AnimusForgeModulePaths.GetModuleDataFilePath(fileName);
			if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
			{
				T diskCatalog = deserialize(File.ReadAllText(path, Encoding.UTF8));
				Logger.Log(logCategory, "Loaded localized GCCZ resource from ModuleData. File=" + fileName);
				return diskCatalog;
			}
		}
		catch (Exception ex)
		{
			Logger.Log(logCategory, "ModuleData GCCZ resource load failed. File=" + fileName + ", Error=" + ex.Message);
		}

		try
		{
			Assembly assembly = typeof(GcczLocalizedResourceLoader).Assembly;
			using Stream stream = assembly.GetManifestResourceStream(embeddedResourceName);
			if (stream != null)
			{
				using var reader = new StreamReader(stream, Encoding.UTF8, true);
				T embeddedCatalog = deserialize(reader.ReadToEnd());
				Logger.Log(logCategory, "Loaded embedded localized GCCZ resource. File=" + fileName);
				return embeddedCatalog;
			}
		}
		catch (Exception ex)
		{
			Logger.Log(logCategory, "Embedded GCCZ resource load failed. File=" + fileName + ", Error=" + ex.Message);
		}

		Logger.Log(logCategory, "Using fail-safe localized GCCZ resource. File=" + fileName);
		return createFallback();
	}
}
