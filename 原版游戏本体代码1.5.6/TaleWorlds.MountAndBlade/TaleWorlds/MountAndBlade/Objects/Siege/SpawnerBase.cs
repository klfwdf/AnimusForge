using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class SpawnerBase : ScriptComponentBehavior
{
	public class SpawnerPermissionField : EditorVisibleScriptComponentVariable
	{
		public SpawnerPermissionField()
			: base(visible: false)
		{
		}
	}

	[EditorVisibleScriptComponentVariable(true)]
	public string ToBeSpawnedOverrideName = "";

	[EditorVisibleScriptComponentVariable(true)]
	public string ToBeSpawnedOverrideNameForFireVersion = "";

	protected SpawnerEntityEditorHelper _spawnerEditorHelper;

	protected SpawnerEntityMissionHelper _spawnerMissionHelper;

	protected SpawnerEntityMissionHelper _spawnerMissionHelperFire;

	protected internal override bool OnCheckForProblems()
	{
		return !_spawnerEditorHelper.IsValid;
	}

	public virtual void AssignParameters(SpawnerEntityMissionHelper _spawnerMissionHelper)
	{
		Debug.FailedAssert("Please override 'AssignParameters' function in the derived class.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Objects\\Siege\\SpawnerBase.cs", "AssignParameters", 40);
	}
}
