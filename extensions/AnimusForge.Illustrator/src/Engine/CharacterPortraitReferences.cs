using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Context;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;

namespace AnimusForge.Illustrator.Engine
{
    /// <summary>Two independently rendered views of one immutable appearance; values are encoded JPEG base64.</summary>
    public sealed class CharacterPortraitReferences
    {
        public string FullBody { get; }
        public string HeadDetail { get; }

        public CharacterPortraitReferences(string fullBody, string headDetail)
        {
            FullBody = fullBody;
            HeadDetail = headDetail;
        }
    }

    public static partial class ScreenCaptureHelper
    {
        // CharacterViewModel.StanceTypes values verified in the 1.3 and 1.4 sources.
        // EmphasizeFace uses the native tableau camera, not a crop of the full-body PNG.
        // Keep this dependency on the widget's public int API rather than adding a VM DLL dependency.
        internal const int FullBodyPortraitStance = 0;
        internal const int HeadDetailPortraitStance = 1;
        internal const int HeadDetailRenderDimension = 768;

        public static async Task<CharacterPortraitReferences> ExtractHeroPortraitReferencesAsync(Hero hero, bool useCivilian = false, int maxDimension = 768, int timeoutMs = 3500, CancellationToken cancellationToken = default, bool cleanTempFiles = false, string equipmentCodeOverride = null, CharacterAppearanceSnapshot appearance = null)
        {
            if (hero == null) return new CharacterPortraitReferences(null, null);
            try
            {
                // Snapshot once before either render, including randomized NPC bodies and cosmetic equipment.
                var frozen = await PrepareHeroPortraitAppearanceAsync(hero, useCivilian, equipmentCodeOverride, appearance, cancellationToken).ConfigureAwait(false);
                if (frozen == null) return new CharacterPortraitReferences(null, null);
                return await CollectCharacterPortraitReferencesAsync(
                    (headDetail, token) => ExtractAppearancePortraitAsync(frozen, headDetail, maxDimension, timeoutMs, token, cleanTempFiles),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print("[OffscreenRenderer] Hero reference preparation failed: " + ex.Message);
                return new CharacterPortraitReferences(null, null);
            }
        }

        public static async Task<CharacterPortraitReferences> ExtractCharacterPortraitReferencesAsync(CharacterObject character, int maxDimension = 768, int timeoutMs = 3500, CancellationToken cancellationToken = default, string bodyProperties = null, bool cleanTempFiles = false, string equipmentCodeOverride = null, CharacterAppearanceSnapshot appearance = null)
        {
            if (character == null) return new CharacterPortraitReferences(null, null);
            try
            {
                var frozen = await PrepareCharacterPortraitAppearanceAsync(character, bodyProperties, equipmentCodeOverride, appearance, cancellationToken).ConfigureAwait(false);
                if (frozen == null) return new CharacterPortraitReferences(null, null);
                return await CollectCharacterPortraitReferencesAsync(
                    (headDetail, token) => ExtractAppearancePortraitAsync(frozen, headDetail, maxDimension, timeoutMs, token, cleanTempFiles),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print("[OffscreenRenderer] Character reference preparation failed: " + ex.Message);
                return new CharacterPortraitReferences(null, null);
            }
        }

        // One extra capture per requested character, never a tick/update hot path. Both operations
        // use the existing stage lock and wait for native retirement before another stage starts.
        internal static async Task<CharacterPortraitReferences> CollectCharacterPortraitReferencesAsync(Func<bool, CancellationToken, Task<string>> capture, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            string fullBody = await capture(false, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(fullBody)) return new CharacterPortraitReferences(null, null);

            string headDetail = null;
            try
            {
                headDetail = await capture(true, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print("[OffscreenRenderer] Head detail unavailable; retaining full-body reference: " + ex.Message);
            }
            return new CharacterPortraitReferences(fullBody, string.IsNullOrWhiteSpace(headDetail) ? null : headDetail);
        }

        private static Task<CharacterAppearanceSnapshot> PrepareHeroPortraitAppearanceAsync(Hero hero, bool useCivilian, string equipmentCodeOverride, CharacterAppearanceSnapshot appearance, CancellationToken token)
        {
            if (appearance != null) return Task.FromResult(WithPortraitEquipmentOverride(appearance, equipmentCodeOverride));
            return RunOnGameThreadAsync(() =>
            {
                var equipment = !string.IsNullOrWhiteSpace(equipmentCodeOverride)
                    ? Equipment.CreateFromEquipmentCode(equipmentCodeOverride)
                    : (useCivilian ? hero.CivilianEquipment : hero.BattleEquipment);
                return CharacterAppearanceSnapshot.FromHero(hero, equipment);
            }, token);
        }

        private static Task<CharacterAppearanceSnapshot> PrepareCharacterPortraitAppearanceAsync(CharacterObject character, string bodyProperties, string equipmentCodeOverride, CharacterAppearanceSnapshot appearance, CancellationToken token)
        {
            if (appearance != null) return Task.FromResult(WithPortraitEquipmentOverride(appearance, equipmentCodeOverride));
            return RunOnGameThreadAsync(() =>
            {
                var equipment = !string.IsNullOrWhiteSpace(equipmentCodeOverride)
                    ? Equipment.CreateFromEquipmentCode(equipmentCodeOverride)
                    : (character.Equipment ?? character.FirstBattleEquipment);
                var frozen = CharacterAppearanceSnapshot.FromCharacter(character, equipment);
                if (frozen == null || string.IsNullOrWhiteSpace(bodyProperties)) return frozen;
                return new CharacterAppearanceSnapshot(frozen.EquipmentCode, bodyProperties, frozen.BannerCode,
                    frozen.Color1, frozen.Color2, frozen.Race, frozen.IsFemale);
            }, token);
        }

        internal static CharacterAppearanceSnapshot WithPortraitEquipmentOverride(CharacterAppearanceSnapshot appearance, string equipmentCodeOverride)
        {
            if (appearance == null || string.IsNullOrWhiteSpace(equipmentCodeOverride)) return appearance;
            return new CharacterAppearanceSnapshot(equipmentCodeOverride, appearance.BodyProperties, appearance.BannerCode,
                appearance.Color1, appearance.Color2, appearance.Race, appearance.IsFemale);
        }

        private static async Task<string> ExtractAppearancePortraitAsync(CharacterAppearanceSnapshot appearance, bool headDetail, int maxDimension, int timeoutMs, CancellationToken token, bool cleanTempFiles)
        {
            token.ThrowIfCancellationRequested();
            string path = await ExtractViaStageAsync("OffscreenCharacter", widget =>
            {
                var characterWidget = widget as CharacterTableauWidget;
                if (characterWidget == null) throw new InvalidOperationException("Portrait tableau widget is unavailable.");
                ApplyAppearance(characterWidget, appearance);
                // The same native equipment renderer preserves hair/beard cover flags and helmets.
                // Only native camera framing changes; no equipment or BodyProperties are stripped.
                characterWidget.StanceIndex = headDetail ? HeadDetailPortraitStance : FullBodyPortraitStance;
                characterWidget.SuggestedWidth = headDetail ? HeadDetailRenderDimension : 384;
                characterWidget.SuggestedHeight = 768;
                characterWidget.CustomRenderScale = 1f;
            }, warmupTicks: 20, maxTicks: 240, timeoutMs: timeoutMs, cancellationToken: token, cleanTempFiles: cleanTempFiles).ConfigureAwait(false);
            // Reuse the existing native-producer colour adapter exactly once for each independent PNG.
            return await ReadOffscreenPngBase64(path, maxDimension, token).ConfigureAwait(false);
        }
    }
}
