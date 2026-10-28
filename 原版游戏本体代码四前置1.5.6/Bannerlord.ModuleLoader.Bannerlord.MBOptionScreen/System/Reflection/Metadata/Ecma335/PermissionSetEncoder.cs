using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct PermissionSetEncoder
{
	public BlobBuilder Builder { get; }

	public PermissionSetEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public PermissionSetEncoder AddPermission(string typeName, System.Collections.Immutable.ImmutableArray<byte> encodedArguments)
	{
		if (typeName == null)
		{
			System.Reflection.Throw.ArgumentNull("typeName");
		}
		if (encodedArguments.IsDefault)
		{
			System.Reflection.Throw.ArgumentNull("encodedArguments");
		}
		if (encodedArguments.Length > 536870911)
		{
			System.Reflection.Throw.BlobTooLarge("encodedArguments");
		}
		Builder.WriteSerializedString(typeName);
		Builder.WriteCompressedInteger(encodedArguments.Length);
		Builder.WriteBytes(encodedArguments);
		return this;
	}

	public PermissionSetEncoder AddPermission(string typeName, BlobBuilder encodedArguments)
	{
		if (typeName == null)
		{
			System.Reflection.Throw.ArgumentNull("typeName");
		}
		if (encodedArguments == null)
		{
			System.Reflection.Throw.ArgumentNull("encodedArguments");
		}
		if (encodedArguments.Count > 536870911)
		{
			System.Reflection.Throw.BlobTooLarge("encodedArguments");
		}
		Builder.WriteSerializedString(typeName);
		Builder.WriteCompressedInteger(encodedArguments.Count);
		encodedArguments.WriteContentTo(Builder);
		return this;
	}
}
