using System; using System.Collections.Generic;
namespace TaleWorlds.Library { internal static class MBMath { internal static int ClampInt(int value,int min,int max)=>Math.Clamp(value,min,max); } }
namespace AnimusForge { internal static class MemoryDeveloperEditOwner { internal static string NormalizeMultiline(string input)=>input??""; internal static List<string> ParseLineList(string input,int maxCount,bool ignoreCase)=>new(); } }

namespace TaleWorlds.Library { internal static class TWParallel { internal static bool IsMainThread()=>true; } }
namespace AnimusForge { internal sealed class NpcActionEntry { internal NpcActionEntry CopyForSummary()=>this; } internal static class MyBehavior { internal static bool IsValidMemoryCommitMarker(string a,string b,string c)=>false; internal static bool IsMemoryRecoveryHexDigest(string value)=>false; } }
