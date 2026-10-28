using System.IO;
using Bannerlord.ModuleManager;

namespace Bannerlord.ModuleLoader;

internal record ImplementationFile(FileInfo Implementation, ApplicationVersion Version);
