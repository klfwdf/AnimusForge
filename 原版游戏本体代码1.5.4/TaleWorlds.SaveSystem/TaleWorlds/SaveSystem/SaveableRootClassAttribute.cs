using System;

namespace TaleWorlds.SaveSystem;

[AttributeUsage(AttributeTargets.Class)]
public class SaveableRootClassAttribute : Attribute
{
	public int SaveId { get; set; }

	public SaveableRootClassAttribute(int saveId)
	{
		SaveId = saveId;
	}
}
