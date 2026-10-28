using SandBox.BoardGames.Objects;
using SandBox.BoardGames.Pawns;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.BoardGames.Tiles;

public abstract class TileBase
{
	public PawnBase PawnOnTile;

	private bool _showTile;

	private float _tileFadeTimer;

	private const float TileFadeDuration = 0.2f;

	public GameEntity Entity { get; }

	public BoardGameDecal ValidMoveDecal { get; }

	protected TileBase(GameEntity entity, BoardGameDecal decal)
	{
		Entity = entity;
		ValidMoveDecal = decal;
	}

	public virtual void Reset()
	{
		PawnOnTile = null;
	}

	public void Tick(float dt)
	{
		int num = (_showTile ? 1 : (-1));
		_tileFadeTimer += (float)num * dt * 5f;
		_tileFadeTimer = MBMath.ClampFloat(_tileFadeTimer, 0f, 1f);
		ValidMoveDecal.SetAlpha(_tileFadeTimer);
	}

	public void SetVisibility(bool isVisible)
	{
		_showTile = isVisible;
	}
}
