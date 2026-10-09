using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

[assembly: AssemblyVersion("1.5.4.0")]
[assembly: AssemblyFileVersion("1.5.7.0")]
[assembly: AssemblyInformationalVersion("1.5.4")]
[assembly: InternalsVisibleTo("PolicyEffectModule.ContractTests")]

#if BANNERLORD_1_4_OR_GREATER
[assembly: AssemblyMetadata("AnimusForge.BuildFlavor", "ANIMUSFORGE_BANNERLORD_API_1_4")]
[assembly: AssemblyMetadata("AnimusForge.BannerlordApi", "1.4")]
#else
[assembly: AssemblyMetadata("AnimusForge.BuildFlavor", "ANIMUSFORGE_BANNERLORD_API_1_3")]
[assembly: AssemblyMetadata("AnimusForge.BannerlordApi", "1.3")]
#endif
