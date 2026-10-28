using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Reflection.Metadata;

[DebuggerDisplay("{FullName}")]
internal sealed class AssemblyNameInfo
{
	internal readonly AssemblyNameFlags _flags;

	private string _fullName;

	public string Name { get; }

	public Version? Version { get; }

	public string? CultureName { get; }

	public AssemblyNameFlags Flags => _flags;

	public System.Collections.Immutable.ImmutableArray<byte> PublicKeyOrToken { get; }

	public string FullName
	{
		get
		{
			if (_fullName == null)
			{
				bool flag = (Flags & AssemblyNameFlags.PublicKey) != 0;
				byte[] array = ((!PublicKeyOrToken.IsDefault) ? PublicKeyOrToken.ToArray() : null);
				_fullName = AssemblyNameFormatter.ComputeDisplayName(Name, Version, CultureName, flag ? null : array, ExtractAssemblyNameFlags(_flags), ExtractAssemblyContentType(_flags), flag ? array : null);
			}
			return _fullName;
		}
	}

	public AssemblyNameInfo(string name, Version? version = null, string? cultureName = null, AssemblyNameFlags flags = AssemblyNameFlags.None, System.Collections.Immutable.ImmutableArray<byte> publicKeyOrToken = default(System.Collections.Immutable.ImmutableArray<byte>))
	{
		Name = name ?? throw new ArgumentNullException("name");
		Version = version;
		CultureName = cultureName;
		_flags = flags;
		PublicKeyOrToken = publicKeyOrToken;
	}

	internal AssemblyNameInfo(AssemblyNameParser.AssemblyNameParts parts)
	{
		Name = parts._name;
		Version = parts._version;
		CultureName = parts._cultureName;
		_flags = parts._flags;
		PublicKeyOrToken = ((parts._publicKeyOrToken == null) ? default(System.Collections.Immutable.ImmutableArray<byte>) : ((parts._publicKeyOrToken.Length == 0) ? System.Collections.Immutable.ImmutableArray<byte>.Empty : System.Collections.Immutable.ImmutableArray.Create(parts._publicKeyOrToken)));
	}

	public AssemblyName ToAssemblyName()
	{
		AssemblyName assemblyName = new AssemblyName();
		assemblyName.Name = Name;
		assemblyName.CultureName = CultureName;
		assemblyName.Version = Version;
		assemblyName.Flags = Flags;
		assemblyName.ContentType = ExtractAssemblyContentType(_flags);
		assemblyName.ProcessorArchitecture = ExtractProcessorArchitecture(_flags);
		if (!PublicKeyOrToken.IsDefault)
		{
			if ((Flags & AssemblyNameFlags.PublicKey) != AssemblyNameFlags.None)
			{
				assemblyName.SetPublicKey(PublicKeyOrToken.ToArray());
			}
			else
			{
				assemblyName.SetPublicKeyToken(PublicKeyOrToken.ToArray());
			}
		}
		return assemblyName;
	}

	public static AssemblyNameInfo Parse(ReadOnlySpan<char> assemblyName)
	{
		if (!TryParse(assemblyName, out AssemblyNameInfo result))
		{
			throw new ArgumentException(System.SR.InvalidAssemblyName, "assemblyName");
		}
		return result;
	}

	public static bool TryParse(ReadOnlySpan<char> assemblyName, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AssemblyNameInfo? result)
	{
		AssemblyNameParser.AssemblyNameParts parts = default(AssemblyNameParser.AssemblyNameParts);
		if (!assemblyName.IsEmpty && AssemblyNameParser.TryParse(assemblyName, ref parts))
		{
			result = new AssemblyNameInfo(parts);
			return true;
		}
		result = null;
		return false;
	}

	internal static AssemblyNameFlags ExtractAssemblyNameFlags(AssemblyNameFlags combinedFlags)
	{
		return combinedFlags & (AssemblyNameFlags)(-3825);
	}

	internal static AssemblyContentType ExtractAssemblyContentType(AssemblyNameFlags flags)
	{
		return (AssemblyContentType)(((int)flags >> 9) & 7);
	}

	internal static ProcessorArchitecture ExtractProcessorArchitecture(AssemblyNameFlags flags)
	{
		return (ProcessorArchitecture)(((int)flags >> 4) & 7);
	}
}
