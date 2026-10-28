using System;

namespace HarmonyLib;

public class ExceptionBlock
{
	public ExceptionBlockType blockType;

	public Type catchType;

	public ExceptionBlock(ExceptionBlockType blockType, Type catchType = null)
	{
		this.blockType = blockType;
		this.catchType = catchType ?? typeof(object);
	}
}
