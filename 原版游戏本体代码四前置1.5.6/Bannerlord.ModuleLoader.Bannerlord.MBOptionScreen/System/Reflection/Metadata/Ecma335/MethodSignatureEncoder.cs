namespace System.Reflection.Metadata.Ecma335;

internal readonly struct MethodSignatureEncoder
{
	public BlobBuilder Builder { get; }

	public bool HasVarArgs { get; }

	public MethodSignatureEncoder(BlobBuilder builder, bool hasVarArgs)
	{
		Builder = builder;
		HasVarArgs = hasVarArgs;
	}

	public void Parameters(int parameterCount, out ReturnTypeEncoder returnType, out ParametersEncoder parameters)
	{
		if ((uint)parameterCount > 536870911u)
		{
			System.Reflection.Throw.ArgumentOutOfRange("parameterCount");
		}
		Builder.WriteCompressedInteger(parameterCount);
		returnType = new ReturnTypeEncoder(Builder);
		parameters = new ParametersEncoder(Builder, HasVarArgs);
	}

	public void Parameters(int parameterCount, Action<ReturnTypeEncoder> returnType, Action<ParametersEncoder> parameters)
	{
		if (returnType == null)
		{
			System.Reflection.Throw.ArgumentNull("returnType");
		}
		if (parameters == null)
		{
			System.Reflection.Throw.ArgumentNull("parameters");
		}
		Parameters(parameterCount, out var returnType2, out var parameters2);
		returnType(returnType2);
		parameters(parameters2);
	}
}
