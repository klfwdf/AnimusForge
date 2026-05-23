using System;
using System.Diagnostics;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineClass("rglResource")]
public abstract class Resource : NativeObject
{
	public bool IsValid => base.Pointer != UIntPtr.Zero;

	protected Resource()
	{
	}

	internal Resource(UIntPtr pointer)
	{
		Construct(pointer);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	protected void CheckResourceParameter(Resource param, string paramName = "")
	{
		if (param == null)
		{
			throw new NullReferenceException(paramName);
		}
		if (!param.IsValid)
		{
			throw new ArgumentException(paramName);
		}
	}
}
