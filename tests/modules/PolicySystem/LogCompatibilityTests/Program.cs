using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;
namespace AnimusForge {
internal static class AnimusForgeModulePaths {
 internal static string Directory;
 internal static int Reads;
 internal static string GetModuleDataFilePath(string name) { Reads++; return Path.Combine(Directory, name); }
}
internal static class Logger {
 internal static bool IsVerboseModLogicEnabled = false;
 internal static List<string> Lines = new List<string>();
 internal static void LogToFile(string name, string value) { Lines.Add(value); }
}
internal static class Program {
 static bool Marker(string method, string value) => (bool)typeof(PolicySystemLog).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[]{value});
 static void Require(bool ok, string message) { if(!ok) throw new Exception(message); }
 static void Main(string[] args) {
  AnimusForgeModulePaths.Directory = args[0];
  Require(!Marker("ContainsFailureMarker", "ordinary event"), "ASCII event falsely failed");
  Require(Marker("ContainsFailureMarker", "ERROR"), "English failure lost");
  Require(!Marker("ContainsOperationalWarningMarker", "ordinary event"), "ASCII event falsely warned");
  Require(Marker("ContainsOperationalWarningMarker", "REJECTED"), "English rejection lost");
  Require(AnimusForgeModulePaths.Reads == 0, "Early ASCII logging loaded package resource");
  foreach(var value in new[]{"\u5931\u8d25", "\u5f02\u5e38", "\u9519\u8bef"}) Require(Marker("ContainsFailureMarker", value), "Legacy failure lost");
  foreach(var value in new[]{"\u8b66\u544a", "\u62d2\u7edd", "\u9694\u79bb"}) Require(Marker("ContainsOperationalWarningMarker", value), "Legacy warning lost");
  Require(!Marker("ContainsFailureMarker", "\u6210\u529f"), "Chinese success falsely failed");
  PolicySystemLog.Write("Policy", "legacy-check", "\u5904\u7406\u5931\u8d25");
  PolicySystemLog.Write("Policy", "legacy-check", "\u8bf7\u6c42\u62d2\u7edd");
  Require(Logger.Lines.Count == 3, "Release logging dropped legacy failure/warning");
  Require(Logger.Lines[1].Contains("\"severity\":\"error\""), "Failure severity incorrect");
  Require(Logger.Lines[2].Contains("\"severity\":\"warning\""), "Warning severity incorrect");
  for(int i=0;i<100;i++) Marker("ContainsFailureMarker", "\u5931\u8d25");
  Require(AnimusForgeModulePaths.Reads == 1, "Compatibility catalog reread");
  Console.WriteLine("PASS: real logger sources compiled; CN/EN markers, release severity, early ASCII no-I/O, single catalog read.");
 }
}
}
