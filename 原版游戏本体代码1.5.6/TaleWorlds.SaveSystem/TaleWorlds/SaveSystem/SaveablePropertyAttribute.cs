using System;

namespace TaleWorlds.SaveSystem;

[AttributeUsage(AttributeTargets.Property)]
public class SaveablePropertyAttribute : Attribute
{
	public short LocalSaveId { get; set; }

	public SaveablePropertyAttribute(short localSaveId)
	{
		LocalSaveId = localSaveId;
	}
}
