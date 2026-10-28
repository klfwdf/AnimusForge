using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond.Rest;

[Serializable]
[DataContract]
public class RestObjectFunctionResult : RestFunctionResult
{
	[DataMember]
	private FunctionResult _functionResult;

	public override FunctionResult GetFunctionResult()
	{
		return _functionResult;
	}

	public RestObjectFunctionResult()
	{
	}

	public RestObjectFunctionResult(FunctionResult functionResult)
	{
		_functionResult = functionResult;
	}
}
