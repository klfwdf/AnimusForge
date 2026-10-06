using TaleWorlds.Engine;

namespace SandBox.BoardGames.Objects;

public class BoardGameDecal : ScriptComponentBehavior
{
	protected override void OnInit()
	{
		base.OnInit();
		SetAlpha(0f);
	}

	public void SetAlpha(float alpha)
	{
		base.GameEntity.SetAlpha(alpha);
	}

	protected override bool MovesEntity()
	{
		return false;
	}
}
