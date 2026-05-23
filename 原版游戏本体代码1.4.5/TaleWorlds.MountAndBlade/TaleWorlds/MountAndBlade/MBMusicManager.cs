using System.Collections.Generic;
using System.Threading;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;
using psai.net;

namespace TaleWorlds.MountAndBlade;

public class MBMusicManager
{
	private class CampaignMusicMode
	{
		private const float DefaultSelectionFactorForFactionSpecificCampaignTheme = 0.35f;

		private const float SelectionFactorDecayAmountForFactionSpecificCampaignTheme = 0.1f;

		private const float SelectionFactorGrowthAmountForFactionSpecificCampaignTheme = 0.1f;

		private float _factionSpecificCampaignThemeSelectionFactor;

		private float _factionSpecificCampaignDramaticThemeSelectionFactor;

		public CampaignMusicMode()
		{
			_factionSpecificCampaignThemeSelectionFactor = 0.35f;
			_factionSpecificCampaignDramaticThemeSelectionFactor = 0.35f;
		}

		public MusicTheme GetCampaignTheme(BasicCultureObject culture, bool isDark)
		{
			if (isDark)
			{
				return MusicTheme.CampaignDark;
			}
			MusicTheme campaignThemeWithCulture = GetCampaignThemeWithCulture(culture);
			MusicTheme result;
			if (campaignThemeWithCulture == MusicTheme.None)
			{
				result = MusicTheme.CampaignStandard;
				_factionSpecificCampaignThemeSelectionFactor += 0.1f;
				MBMath.ClampUnit(ref _factionSpecificCampaignThemeSelectionFactor);
			}
			else
			{
				result = campaignThemeWithCulture;
				_factionSpecificCampaignThemeSelectionFactor -= 0.1f;
				MBMath.ClampUnit(ref _factionSpecificCampaignThemeSelectionFactor);
			}
			return result;
		}

		private MusicTheme GetCampaignThemeWithCulture(BasicCultureObject culture)
		{
			if (MBRandom.NondeterministicRandomFloat <= _factionSpecificCampaignThemeSelectionFactor)
			{
				_factionSpecificCampaignThemeSelectionFactor -= 0.1f;
				MBMath.ClampUnit(ref _factionSpecificCampaignThemeSelectionFactor);
				if (culture.StringId == "empire")
				{
					if (!(MBRandom.NondeterministicRandomFloat < 0.5f))
					{
						return MusicTheme.EmpireCampaignB;
					}
					return MusicTheme.EmpireCampaignA;
				}
				if (culture.StringId == "sturgia")
				{
					return MusicTheme.SturgiaCampaignA;
				}
				if (culture.StringId == "aserai")
				{
					return MusicTheme.AseraiCampaignA;
				}
				if (culture.StringId == "vlandia")
				{
					return MusicTheme.VlandiaCampaignA;
				}
				if (culture.StringId == "khuzait")
				{
					return MusicTheme.KhuzaitCampaignA;
				}
				if (culture.StringId == "battania")
				{
					return MusicTheme.BattaniaCampaignA;
				}
				if (culture.StringId == "nord")
				{
					return MusicTheme.NordCampaign;
				}
			}
			return MusicTheme.None;
		}

		public MusicTheme GetCampaignDramaticThemeWithCulture(BasicCultureObject culture)
		{
			if (MBRandom.NondeterministicRandomFloat <= _factionSpecificCampaignDramaticThemeSelectionFactor)
			{
				_factionSpecificCampaignDramaticThemeSelectionFactor -= 0.1f;
				MBMath.ClampUnit(ref _factionSpecificCampaignDramaticThemeSelectionFactor);
				if (culture.StringId == "empire")
				{
					return MusicTheme.EmpireCampaignDramatic;
				}
				if (culture.StringId == "sturgia")
				{
					return MusicTheme.SturgiaCampaignDramatic;
				}
				if (culture.StringId == "aserai")
				{
					return MusicTheme.AseraiCampaignDramatic;
				}
				if (culture.StringId == "vlandia")
				{
					return MusicTheme.VlandiaCampaignDramatic;
				}
				if (culture.StringId == "khuzait")
				{
					return MusicTheme.KhuzaitCampaignDramatic;
				}
				if (culture.StringId == "battania")
				{
					return MusicTheme.BattaniaCampaignDramatic;
				}
				if (culture.StringId == "nord")
				{
					return MusicTheme.NordCampaign;
				}
			}
			_factionSpecificCampaignDramaticThemeSelectionFactor += 0.1f;
			MBMath.ClampUnit(ref _factionSpecificCampaignDramaticThemeSelectionFactor);
			return MusicTheme.None;
		}

		public MusicTheme GetSeaCampignMusic(BasicCultureObject culture)
		{
			if (culture.StringId == "sturgia" || culture.StringId == "battania" || culture.StringId == "nord")
			{
				return MusicTheme.SeaCampaignNorthern;
			}
			if (culture.StringId == "aserai" || culture.StringId == "vlandia" || culture.StringId == "khuzait" || culture.StringId == "empire")
			{
				return MusicTheme.SeaCampaignSouthern;
			}
			return MusicTheme.None;
		}
	}

	private class BattleMusicMode
	{
		private const float DefaultSelectionFactorForFactionSpecificBattleTheme = 0.35f;

		private const float SelectionFactorDecayAmountForFactionSpecificBattleTheme = 0.1f;

		private const float SelectionFactorGrowthAmountForFactionSpecificBattleTheme = 0.1f;

		private const float DefaultSelectionFactorForFactionSpecificVictoryTheme = 0.65f;

		private float _factionSpecificBattleThemeSelectionFactor;

		private float _factionSpecificSiegeThemeSelectionFactor;

		public BattleMusicMode()
		{
			_factionSpecificBattleThemeSelectionFactor = 0.35f;
			_factionSpecificSiegeThemeSelectionFactor = 0.35f;
		}

		private MusicTheme GetBattleThemeWithCulture(BasicCultureObject culture, out bool isPaganBattle)
		{
			isPaganBattle = false;
			MusicTheme result = MusicTheme.None;
			if (MBRandom.NondeterministicRandomFloat <= _factionSpecificBattleThemeSelectionFactor)
			{
				_factionSpecificBattleThemeSelectionFactor -= 0.1f;
				MBMath.ClampUnit(ref _factionSpecificBattleThemeSelectionFactor);
				if (!(culture.StringId == "sturgia") && !(culture.StringId == "aserai") && !(culture.StringId == "khuzait") && !(culture.StringId == "battania"))
				{
					result = ((!(culture.StringId == "nord")) ? ((MBRandom.NondeterministicRandomFloat < 0.5f) ? MusicTheme.CombatA : MusicTheme.CombatB) : MusicTheme.BattleNord);
				}
				else
				{
					isPaganBattle = true;
					result = ((MBRandom.NondeterministicRandomFloat < 0.5f) ? MusicTheme.BattlePaganA : MusicTheme.BattlePaganB);
				}
			}
			return result;
		}

		private MusicTheme GetSiegeThemeWithCulture(BasicCultureObject culture)
		{
			MusicTheme result = MusicTheme.None;
			if (MBRandom.NondeterministicRandomFloat <= _factionSpecificSiegeThemeSelectionFactor)
			{
				_factionSpecificSiegeThemeSelectionFactor -= 0.1f;
				MBMath.ClampUnit(ref _factionSpecificSiegeThemeSelectionFactor);
				if (culture.StringId == "sturgia" || culture.StringId == "aserai" || culture.StringId == "khuzait" || culture.StringId == "battania")
				{
					result = MusicTheme.PaganSiege;
				}
			}
			return result;
		}

		private MusicTheme GetVictoryThemeForCulture(BasicCultureObject culture)
		{
			if (MBRandom.NondeterministicRandomFloat <= 0.65f)
			{
				if (culture.StringId == "empire")
				{
					return MusicTheme.EmpireVictory;
				}
				if (culture.StringId == "sturgia" || culture.StringId == "nord")
				{
					return MusicTheme.SturgiaVictory;
				}
				if (culture.StringId == "aserai")
				{
					return MusicTheme.AseraiVictory;
				}
				if (culture.StringId == "vlandia")
				{
					return MusicTheme.VlandiaVictory;
				}
				if (culture.StringId == "khuzait")
				{
					return MusicTheme.KhuzaitVictory;
				}
				if (culture.StringId == "battania")
				{
					return MusicTheme.BattaniaVictory;
				}
			}
			return MusicTheme.None;
		}

		public MusicTheme GetBattleTheme(BasicCultureObject culture, int battleSize, out bool isPaganBattle)
		{
			MusicTheme battleThemeWithCulture = GetBattleThemeWithCulture(culture, out isPaganBattle);
			MusicTheme result;
			if (battleThemeWithCulture == MusicTheme.None)
			{
				result = (((float)battleSize < (float)MusicParameters.SmallBattleTreshold - (float)MusicParameters.SmallBattleTreshold * 0.2f * MBRandom.NondeterministicRandomFloat) ? MusicTheme.BattleSmall : MusicTheme.BattleMedium);
				_factionSpecificBattleThemeSelectionFactor += 0.1f;
				MBMath.ClampUnit(ref _factionSpecificBattleThemeSelectionFactor);
			}
			else
			{
				result = battleThemeWithCulture;
				_factionSpecificBattleThemeSelectionFactor -= 0.1f;
				MBMath.ClampUnit(ref _factionSpecificBattleThemeSelectionFactor);
			}
			return result;
		}

		public MusicTheme GetSiegeTheme(BasicCultureObject culture)
		{
			MusicTheme siegeThemeWithCulture = GetSiegeThemeWithCulture(culture);
			MusicTheme result;
			if (siegeThemeWithCulture == MusicTheme.None)
			{
				result = MusicTheme.BattleSiege;
				_factionSpecificSiegeThemeSelectionFactor += 0.1f;
				MBMath.ClampUnit(ref _factionSpecificSiegeThemeSelectionFactor);
			}
			else
			{
				result = siegeThemeWithCulture;
				_factionSpecificSiegeThemeSelectionFactor -= 0.1f;
				MBMath.ClampUnit(ref _factionSpecificSiegeThemeSelectionFactor);
			}
			return result;
		}

		public MusicTheme GetBattleEndTheme(BasicCultureObject culture, bool isVictorious)
		{
			if (isVictorious)
			{
				MusicTheme victoryThemeForCulture = GetVictoryThemeForCulture(culture);
				if (victoryThemeForCulture == MusicTheme.None)
				{
					return MusicTheme.BattleVictory;
				}
				return victoryThemeForCulture;
			}
			return MusicTheme.BattleDefeat;
		}
	}

	private const string CultureEmpire = "empire";

	private const string CultureSturgia = "sturgia";

	private const string CultureAserai = "aserai";

	private const string CultureVlandia = "vlandia";

	private const string CultureBattania = "battania";

	private const string CultureKhuzait = "khuzait";

	private const string CultureNord = "nord";

	private const float DefaultFadeOutDurationInSeconds = 3f;

	private const float MenuModeActivationTimerInSeconds = 0.5f;

	private BattleMusicMode _battleMode;

	private CampaignMusicMode _campaignMode;

	private IMusicHandler _campaignMusicHandler;

	private IMusicHandler _battleMusicHandler;

	private IMusicHandler _silencedMusicHandler;

	private IMusicHandler _activeMusicHandler;

	private static bool _initialized;

	private static bool _creationCompleted;

	private float _menuModeActivationTimer;

	private bool _systemPaused;

	private int _latestFrameUpdatedNo = -1;

	public static MBMusicManager Current { get; private set; }

	public MusicMode CurrentMode { get; private set; }

	private MBMusicManager()
	{
		if (NativeConfig.DisableSound)
		{
			return;
		}
		List<string> list = new List<string>();
		foreach (MbObjectXmlInformation mbprojXml in XmlResource.MbprojXmls)
		{
			if (mbprojXml.Id == "soln_soundtrack")
			{
				string moduleName = mbprojXml.ModuleName;
				list.Add(moduleName);
			}
		}
		PsaiCore.Instance.LoadSoundtrackFromProjectFile(list);
	}

	public static bool IsCreationCompleted()
	{
		return _creationCompleted;
	}

	private static void ProcessCreation(object callback)
	{
		Current = new MBMusicManager();
		MusicParameters.LoadFromXml();
		_creationCompleted = true;
	}

	public static void Create()
	{
		ThreadPool.QueueUserWorkItem(ProcessCreation);
	}

	public static void Initialize()
	{
		if (!_initialized)
		{
			Current._battleMode = new BattleMusicMode();
			Current._campaignMode = new CampaignMusicMode();
			Current.CurrentMode = MusicMode.Paused;
			Current._menuModeActivationTimer = 0.5f;
			_initialized = true;
			Debug.Print("MusicManager Initialize completed.", 0, Debug.DebugColor.Green, 281474976710656uL);
		}
	}

	public void OnCampaignMusicHandlerInit(IMusicHandler campaignMusicHandler)
	{
		_campaignMusicHandler = campaignMusicHandler;
		_activeMusicHandler = _campaignMusicHandler;
	}

	public void OnCampaignMusicHandlerFinalize()
	{
		_campaignMusicHandler = null;
		CheckActiveHandler();
	}

	public void OnBattleMusicHandlerInit(IMusicHandler battleMusicHandler)
	{
		_battleMusicHandler = battleMusicHandler;
		_activeMusicHandler = _battleMusicHandler;
	}

	public void OnBattleMusicHandlerFinalize()
	{
		_battleMusicHandler = null;
		CheckActiveHandler();
	}

	public void OnSilencedMusicHandlerInit(IMusicHandler silencedMusicHandler)
	{
		_silencedMusicHandler = silencedMusicHandler;
		_activeMusicHandler = _silencedMusicHandler;
	}

	public void OnSilencedMusicHandlerFinalize()
	{
		_silencedMusicHandler = null;
		CheckActiveHandler();
	}

	private void CheckActiveHandler()
	{
		_activeMusicHandler = _battleMusicHandler ?? _silencedMusicHandler ?? _campaignMusicHandler;
	}

	private void ActivateMenuMode()
	{
		if (!_systemPaused)
		{
			CurrentMode = MusicMode.Menu;
			MusicTheme menuThemeId = (ModuleHelper.IsModuleActive("NavalDLC") ? MusicTheme.NavalMainTheme : MusicTheme.MainTheme);
			PsaiCore.Instance.MenuModeEnter((int)menuThemeId, 0.5f);
		}
	}

	private void DeactivateMenuMode()
	{
		PsaiCore.Instance.MenuModeLeave();
		CurrentMode = MusicMode.Paused;
	}

	public void ActivateBattleMode()
	{
		if (!_systemPaused)
		{
			CurrentMode = MusicMode.Battle;
		}
	}

	public void DeactivateBattleMode()
	{
		PsaiCore.Instance.StopMusic(immediately: true, 3f);
		CurrentMode = MusicMode.Paused;
	}

	public void ActivateCampaignMode()
	{
		if (!_systemPaused)
		{
			CurrentMode = MusicMode.Campaign;
		}
	}

	public void DeactivateCampaignMode()
	{
		PsaiCore.Instance.StopMusic(immediately: true, 3f);
		CurrentMode = MusicMode.Paused;
	}

	public void DeactivateCurrentMode()
	{
		switch (CurrentMode)
		{
		case MusicMode.Campaign:
			DeactivateCampaignMode();
			break;
		case MusicMode.Battle:
			DeactivateBattleMode();
			break;
		case MusicMode.Menu:
			break;
		}
	}

	private bool CheckMenuModeActivationTimer()
	{
		return _menuModeActivationTimer <= 0f;
	}

	public void UnpauseMusicManagerSystem()
	{
		if (_systemPaused)
		{
			_systemPaused = false;
			_menuModeActivationTimer = 1f;
		}
	}

	public void PauseMusicManagerSystem()
	{
		if (!_systemPaused)
		{
			if (CurrentMode == MusicMode.Menu)
			{
				DeactivateMenuMode();
			}
			_systemPaused = true;
		}
	}

	public void StartTheme(MusicTheme theme, float startIntensity, bool queueEndSegment = false)
	{
		PsaiCore.Instance.TriggerMusicTheme((int)theme, startIntensity);
		if (queueEndSegment)
		{
			PsaiCore.Instance.StopMusic(immediately: false, 3f);
		}
	}

	public void StartThemeWithConstantIntensity(MusicTheme theme, bool queueEndSegment = false)
	{
		PsaiCore.Instance.HoldCurrentIntensity(hold: true);
		StartTheme(theme, 0f, queueEndSegment);
	}

	public void ForceStopThemeWithFadeOut()
	{
		PsaiCore.Instance.StopMusic(immediately: true, 3f);
	}

	public void ChangeCurrentThemeIntensity(float deltaIntensity)
	{
		PsaiCore.Instance.AddToCurrentIntensity(deltaIntensity);
	}

	public void Update(float dt)
	{
		if (Utilities.EngineFrameNo == _latestFrameUpdatedNo)
		{
			return;
		}
		_latestFrameUpdatedNo = Utilities.EngineFrameNo;
		if (_menuModeActivationTimer > 0f)
		{
			_menuModeActivationTimer -= dt;
		}
		if (!_systemPaused)
		{
			if (GameStateManager.Current != null && GameStateManager.Current.ActiveState != null)
			{
				GameState activeState = GameStateManager.Current.ActiveState;
				switch (CurrentMode)
				{
				case MusicMode.Paused:
					if (activeState.IsMusicMenuState && CheckMenuModeActivationTimer())
					{
						ActivateMenuMode();
					}
					break;
				case MusicMode.Menu:
					if (!activeState.IsMusicMenuState)
					{
						DeactivateMenuMode();
					}
					break;
				}
			}
			if (_activeMusicHandler != null)
			{
				_activeMusicHandler.OnUpdated(dt);
			}
		}
		PsaiCore.Instance.Update();
	}

	public MusicTheme GetSiegeTheme(BasicCultureObject culture)
	{
		return _battleMode.GetSiegeTheme(culture);
	}

	public MusicTheme GetBattleTheme(BasicCultureObject culture, int battleSize, out bool isPaganBattle)
	{
		return _battleMode.GetBattleTheme(culture, battleSize, out isPaganBattle);
	}

	public MusicTheme GetBattleEndTheme(BasicCultureObject culture, bool isVictory)
	{
		return _battleMode.GetBattleEndTheme(culture, isVictory);
	}

	public MusicTheme GetBattleTurnsOneSideTheme(BasicCultureObject culture, bool isPositive, bool isPaganBattle)
	{
		if (isPaganBattle)
		{
			if (!isPositive)
			{
				return MusicTheme.PaganTurnsNegative;
			}
			return MusicTheme.PaganTurnsPositive;
		}
		if (!isPositive)
		{
			return MusicTheme.BattleTurnsNegative;
		}
		return MusicTheme.BattleTurnsPositive;
	}

	public MusicTheme GetCampaignMusicTheme(BasicCultureObject culture, bool isDark, bool isWarMode, bool isAtSea)
	{
		MusicTheme musicTheme = MusicTheme.None;
		if (!isDark && isWarMode)
		{
			musicTheme = _campaignMode.GetCampaignDramaticThemeWithCulture(culture);
		}
		if (isAtSea)
		{
			musicTheme = _campaignMode.GetSeaCampignMusic(culture);
		}
		if (musicTheme != MusicTheme.None)
		{
			return musicTheme;
		}
		return _campaignMode.GetCampaignTheme(culture, isDark);
	}
}
