using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

[assembly: AssemblyVersion("0.0.0.0")]
[assembly: InternalsVisibleTo("PolicyEffectModule.ContractTests")]

#if BANNERLORD_1_4_OR_GREATER
[assembly: AssemblyMetadata("AnimusForge.BannerlordApi", "1.4")]
[assembly: AssemblyMetadata("AnimusForge.BuildFlavor", "ANIMUSFORGE_BANNERLORD_API_1_4")]
#else
[assembly: AssemblyMetadata("AnimusForge.BannerlordApi", "1.3")]
[assembly: AssemblyMetadata("AnimusForge.BuildFlavor", "ANIMUSFORGE_BANNERLORD_API_1_3")]
#endif
