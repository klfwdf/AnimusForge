using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace AnimusForge;

/// <summary>
/// Keeps scene notifications emitted by AnimusForge marriages valid even when a
/// custom hero, banner, or another mod's notification-character patch returns
/// data that the native Gauntlet scene loader does not null-check.
/// </summary>
internal static class MarriageSceneNotificationSafety
{
	private const string HarmonyId = "AnimusForge.marriage.scene_notification.safety";

	private static readonly object PatchSync = new object();

	private static volatile bool _patchAttempted;

	private static bool _patchInstalled;

	[ThreadStatic]
	private static NotificationScopeState _currentScope;

	internal static IDisposable BeginScope(Hero left, Hero right)
	{
		EnsurePatched();
		NotificationScopeState currentScope = _currentScope;
		if (currentScope != null && IsSamePair(currentScope.Left, currentScope.Right, left, right))
		{
			currentScope.Depth++;
			return new NotificationScopeLease(currentScope);
		}
		NotificationScopeState notificationScopeState = new NotificationScopeState
		{
			Left = left,
			Right = right,
			Depth = 1,
			Parent = currentScope
		};
		_currentScope = notificationScopeState;
		return new NotificationScopeLease(notificationScopeState);
	}

	internal static bool ShowSafeNotification(Hero left, Hero right, SceneNotificationData.RelevantContextType relevantContext)
	{
		if (left == null || right == null)
		{
			return false;
		}
		NotificationScopeState currentScope = _currentScope;
		if (currentScope != null && IsSamePair(currentScope.Left, currentScope.Right, left, right))
		{
			if (currentScope.NotificationQueued)
			{
				return false;
			}
			currentScope.NotificationQueued = true;
		}
		try
		{
			MBInformationManager.ShowSceneNotification(CreateSafeNotification(left, right, relevantContext));
			return true;
		}
		catch (Exception ex)
		{
			if (currentScope != null && IsSamePair(currentScope.Left, currentScope.Right, left, right))
			{
				currentScope.NotificationQueued = false;
			}
			Logger.Log("Romance", "[ERROR] Queue safe marriage scene notification failed: " + ex);
			return false;
		}
	}

	private static void EnsurePatched()
	{
		if (_patchAttempted)
		{
			return;
		}
		lock (PatchSync)
		{
			if (_patchAttempted)
			{
				return;
			}
			try
			{
				var target = AccessTools.Method(typeof(MBInformationManager), nameof(MBInformationManager.ShowSceneNotification), new Type[1] { typeof(SceneNotificationData) });
				if (target == null)
				{
					Logger.Log("Romance", "[WARN] Marriage scene notification safety patch target was not found.");
				}
				else
				{
					new Harmony(HarmonyId).Patch(target, prefix: new HarmonyMethod(typeof(MarriageSceneNotificationSafety), nameof(ShowSceneNotificationPrefix)));
					_patchInstalled = true;
					Logger.Log("Romance", "Marriage scene notification safety patch installed.");
				}
			}
			catch (Exception ex)
			{
				Logger.Log("Romance", "[ERROR] Marriage scene notification safety patch failed: " + ex);
			}
			finally
			{
				_patchAttempted = true;
			}
		}
	}

	private static bool ShowSceneNotificationPrefix(ref SceneNotificationData __0)
	{
		try
		{
			if (!_patchInstalled || __0 == null || __0 is SafeMarriageSceneNotificationItem)
			{
				return true;
			}
			NotificationScopeState currentScope = _currentScope;
			if (currentScope == null || !(__0 is MarriageSceneNotificationItem) || currentScope.Left == null || currentScope.Right == null)
			{
				return true;
			}
			if (currentScope.NotificationQueued)
			{
				Logger.Log("Romance", "Skipped a duplicate marriage scene notification for the current AnimusForge marriage action.");
				return false;
			}
			SceneNotificationData.RelevantContextType relevantContext = SceneNotificationData.RelevantContextType.Any;
			try
			{
				relevantContext = __0.RelevantContext;
			}
			catch
			{
			}
			__0 = CreateSafeNotification(currentScope.Left, currentScope.Right, relevantContext);
			currentScope.NotificationQueued = true;
			Logger.Log("Romance", "Replaced native marriage scene notification with validated AnimusForge notification data.");
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[ERROR] Marriage scene notification replacement failed; leaving native notification unchanged: " + ex);
		}
		return true;
	}

	private static SafeMarriageSceneNotificationItem CreateSafeNotification(Hero left, Hero right, SceneNotificationData.RelevantContextType relevantContext)
	{
		Hero groom = left.IsFemale ? right : left;
		Hero bride = left.IsFemale ? left : right;
		return new SafeMarriageSceneNotificationItem(groom, bride, CampaignTime.Now, relevantContext);
	}

	private static bool IsSamePair(Hero firstLeft, Hero firstRight, Hero secondLeft, Hero secondRight)
	{
		return firstLeft == secondLeft && firstRight == secondRight || firstLeft == secondRight && firstRight == secondLeft;
	}

	private sealed class NotificationScopeState
	{
		internal Hero Left;

		internal Hero Right;

		internal int Depth;

		internal bool NotificationQueued;

		internal NotificationScopeState Parent;
	}

	private sealed class NotificationScopeLease : IDisposable
	{
		private NotificationScopeState _state;

		internal NotificationScopeLease(NotificationScopeState state)
		{
			_state = state;
		}

		public void Dispose()
		{
			NotificationScopeState state = _state;
			if (state == null)
			{
				return;
			}
			_state = null;
			state.Depth--;
			if (state.Depth <= 0 && ReferenceEquals(_currentScope, state))
			{
				_currentScope = state.Parent;
			}
		}
	}

	private sealed class SafeMarriageSceneNotificationItem : MarriageSceneNotificationItem
	{
		internal SafeMarriageSceneNotificationItem(Hero groom, Hero bride, CampaignTime creationTime, SceneNotificationData.RelevantContextType relevantContext)
			: base(groom, bride, creationTime, relevantContext)
		{
		}

		public override Banner[] GetBanners()
		{
			Banner[] banners;
			try
			{
				banners = base.GetBanners();
			}
			catch (Exception ex)
			{
				Logger.Log("Romance", "[WARN] Marriage notification banner generation failed; wedding will open without banners: " + ex);
				return Array.Empty<Banner>();
			}
			if (banners == null || banners.Length == 0)
			{
				return Array.Empty<Banner>();
			}
			List<Banner> list = new List<Banner>(banners.Length);
			for (int i = 0; i < banners.Length; i++)
			{
				Banner banner = banners[i];
				try
				{
					if (banner != null && banner.BannerVisual != null)
					{
						list.Add(banner);
					}
				}
				catch (Exception ex2)
				{
					Logger.Log("Romance", "[WARN] Ignored an invalid marriage notification banner: " + ex2.GetType().Name);
				}
			}
			if (list.Count != banners.Length)
			{
				Logger.Log("Romance", $"Marriage notification removed {banners.Length - list.Count} invalid banner slot(s).");
			}
			return list.ToArray();
		}

		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			SceneNotificationData.SceneNotificationCharacter[] characters;
			try
			{
				characters = base.GetSceneNotificationCharacters();
			}
			catch (Exception ex)
			{
				Logger.Log("Romance", "[WARN] Native marriage notification character generation failed; using minimal wedding cast: " + ex);
				return BuildMinimalCharacters();
			}
			if (characters == null || characters.Length == 0)
			{
				return BuildMinimalCharacters();
			}
			int invalidCount = 0;
			for (int i = 0; i < characters.Length; i++)
			{
				if (!IsRenderable(characters[i]))
				{
					characters[i] = new SceneNotificationData.SceneNotificationCharacter(null);
					invalidCount++;
				}
			}
			if (invalidCount > 0)
			{
				Logger.Log("Romance", $"Marriage notification replaced {invalidCount} invalid character slot(s) with empty placeholders.");
			}
			return characters;
		}

		private SceneNotificationData.SceneNotificationCharacter[] BuildMinimalCharacters()
		{
			return new SceneNotificationData.SceneNotificationCharacter[2]
			{
				CreateMinimalCharacter(GroomHero),
				CreateMinimalCharacter(BrideHero)
			};
		}

		private static SceneNotificationData.SceneNotificationCharacter CreateMinimalCharacter(Hero hero)
		{
			try
			{
				var characterObject = hero?.CharacterObject;
				if (characterObject == null)
				{
					return new SceneNotificationData.SceneNotificationCharacter(null);
				}
				var character = new SceneNotificationData.SceneNotificationCharacter(characterObject, hero.CivilianEquipment);
				return IsRenderable(character) ? character : new SceneNotificationData.SceneNotificationCharacter(null);
			}
			catch (Exception ex)
			{
				Logger.Log("Romance", "[WARN] Could not create a minimal marriage notification character: " + ex.GetType().Name);
				return new SceneNotificationData.SceneNotificationCharacter(null);
			}
		}

		private static bool IsRenderable(SceneNotificationData.SceneNotificationCharacter sceneCharacter)
		{
			var character = sceneCharacter.Character;
			if (character == null)
			{
				return true;
			}
			try
			{
				if (character.Culture == null || character.Equipment == null)
				{
					return false;
				}
				var equipment = sceneCharacter.OverriddenEquipment;
				if (equipment == null)
				{
					equipment = sceneCharacter.UseCivilianEquipment ? character.FirstCivilianEquipment : character.FirstBattleEquipment;
				}
				if (equipment == null)
				{
					return false;
				}
				character.GetBodyProperties(character.Equipment);
				return FaceGen.GetBaseMonsterFromRace(character.Race) != null;
			}
			catch
			{
				return false;
			}
		}
	}
}
