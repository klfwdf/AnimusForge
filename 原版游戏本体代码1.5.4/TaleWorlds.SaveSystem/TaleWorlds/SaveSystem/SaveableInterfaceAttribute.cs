using System;

namespace TaleWorlds.SaveSystem;

[AttributeUsage(AttributeTargets.Interface)]
public class SaveableInterfaceAttribute : Attribute
{
	public int SaveId { get; set; }

	public SaveableInterfaceAttribute(int saveId)
	{
		SaveId = saveId;
	}
}
