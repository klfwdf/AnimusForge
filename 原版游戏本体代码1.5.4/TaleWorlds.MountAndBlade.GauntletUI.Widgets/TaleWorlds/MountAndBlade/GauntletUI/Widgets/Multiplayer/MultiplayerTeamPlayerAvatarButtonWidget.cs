using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer;

public class MultiplayerTeamPlayerAvatarButtonWidget : ButtonWidget
{
	private bool _isInitialized;

	private float _originalAvatarImageAlpha = 1f;

	private bool _isDead;

	private float _deathAlphaFactor;

	private ImageIdentifierWidget _avatarImage;

	[DataSourceProperty]
	public bool IsDead
	{
		get
		{
			return _isDead;
		}
		set
		{
			if (_isDead != value)
			{
				_isDead = value;
				OnPropertyChanged(value, "IsDead");
				UpdateGlobalAlpha();
			}
		}
	}

	[DataSourceProperty]
	public float DeathAlphaFactor
	{
		get
		{
			return _deathAlphaFactor;
		}
		set
		{
			if (_deathAlphaFactor != value)
			{
				_deathAlphaFactor = value;
				OnPropertyChanged(value, "DeathAlphaFactor");
			}
		}
	}

	[DataSourceProperty]
	public ImageIdentifierWidget AvatarImage
	{
		get
		{
			return _avatarImage;
		}
		set
		{
			if (_avatarImage != value)
			{
				_avatarImage = value;
				OnPropertyChanged(value, "AvatarImage");
			}
		}
	}

	public MultiplayerTeamPlayerAvatarButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_isInitialized && AvatarImage != null)
		{
			_originalAvatarImageAlpha = AvatarImage.ReadOnlyBrush.GlobalAlphaFactor;
			UpdateGlobalAlpha();
			_isInitialized = true;
		}
	}

	private void UpdateGlobalAlpha()
	{
		if (_isInitialized)
		{
			float num = (IsDead ? DeathAlphaFactor : 1f);
			float globalAlphaFactor = num * _originalAvatarImageAlpha;
			this.SetGlobalAlphaRecursively(num);
			AvatarImage.Brush.GlobalAlphaFactor = globalAlphaFactor;
		}
	}
}
