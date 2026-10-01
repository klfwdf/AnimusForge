using System;
namespace AnimusForge;
public partial class MyBehavior {
 private static string ComputeMemorySummarySourceFingerprint(MemorySummarySourceView source) => MemorySourceFingerprintRules.Compute(source);
}
