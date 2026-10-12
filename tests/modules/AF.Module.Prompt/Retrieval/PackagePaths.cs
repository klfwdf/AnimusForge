using System;
using System.IO;
namespace AnimusForge;
internal static class AnimusForgeModulePaths
{
    internal static string GetModuleDataFilePath(string name) => Path.Combine(Environment.GetEnvironmentVariable("AF_TEST_RETRIEVAL_PACKAGE"), name);
}
