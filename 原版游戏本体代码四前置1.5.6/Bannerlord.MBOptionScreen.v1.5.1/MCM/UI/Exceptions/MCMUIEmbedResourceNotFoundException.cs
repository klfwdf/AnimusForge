using System;
using System.Runtime.Serialization;
using MCM.Common;

namespace MCM.UI.Exceptions;

[Serializable]
public class MCMUIEmbedResourceNotFoundException : MCMException
{
	public MCMUIEmbedResourceNotFoundException()
	{
	}

	public MCMUIEmbedResourceNotFoundException(string message)
		: base(message)
	{
	}

	public MCMUIEmbedResourceNotFoundException(string message, Exception inner)
		: base(message, inner)
	{
	}

	protected MCMUIEmbedResourceNotFoundException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
