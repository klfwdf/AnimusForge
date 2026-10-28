namespace System.Reflection.Metadata.Ecma335;

internal readonly struct ParametersEncoder
{
	public BlobBuilder Builder { get; }

	public bool HasVarArgs { get; }

	public ParametersEncoder(BlobBuilder builder, bool hasVarArgs = false)
	{
		Builder = builder;
		HasVarArgs = hasVarArgs;
	}

	public ParameterTypeEncoder AddParameter()
	{
		return new ParameterTypeEncoder(Builder);
	}

	public ParametersEncoder StartVarArgs()
	{
		if (!HasVarArgs)
		{
			System.Reflection.Throw.SignatureNotVarArg();
		}
		Builder.WriteByte(65);
		return new ParametersEncoder(Builder);
	}
}
