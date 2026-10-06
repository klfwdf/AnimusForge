namespace TaleWorlds.ObjectSystem;

public interface IObjectManagerHandler
{
	void AfterCreateObject(MBObjectBase objectBase);

	void AfterUnregisterObject(MBObjectBase objectBase);
}
