using System;

namespace TaleWorlds.SaveSystem;

[AttributeUsage(AttributeTargets.Field)]
public class SaveableFieldAttribute : Attribute
{
	public short LocalSaveId { get; set; }

	public SaveableFieldAttribute(short localSaveId)
	{
		LocalSaveId = localSaveId;
	}
}
