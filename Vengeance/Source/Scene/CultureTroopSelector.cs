using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RichExecutions.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace RichExecutions.Scene;

/// <summary>
/// Selects regular soldiers from the execution venue's culture without
/// advancing Bannerlord's global random-number stream.
/// </summary>
internal static class CultureTroopSelector
{
    private const int GuardMinimumTier = 3;
    private const int ExecutionerMinimumTier = 5;

    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    internal static CharacterObject? SelectGuard(ExecutionRequest request, int slot)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return SelectGuard(request.SessionId, request.Venue, slot);
    }

    internal static CharacterObject? SelectExecutioner(ExecutionRequest request, int slot = 0)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return SelectExecutioner(request.SessionId, request.Venue, slot);
    }
    internal static CharacterObject? SelectCeremonyGuard(ExecutionRequest request, int slot)
    {
        var playerTroops = GetPlayerPartyRegularSoldiers();
        return playerTroops.Count > 0
            ? SelectStable(playerTroops, request.SessionId, request.Venue.StringId, "player-guard", slot)
            : SelectGuard(request, slot);
    }

    internal static CharacterObject? SelectCeremonyExecutioner(ExecutionRequest request, int slot = 0)
    {
        var playerTroops = GetPlayerPartyRegularSoldiers()
            .OrderByDescending(character => character.Tier)
            .ThenBy(character => character.StringId, StringComparer.Ordinal)
            .ToList();
        return playerTroops.Count > 0
            ? SelectStable(playerTroops, request.SessionId, request.Venue.StringId, "player-executioner", slot)
            : SelectExecutioner(request, slot);
    }

    internal static CharacterObject? SelectCeremonyCrossbowman(ExecutionRequest request, int slot)
    {
        var playerTroops = GetPlayerPartyRegularSoldiers();
        if (playerTroops.Count > 0)
        {
            var crossbowmen = playerTroops.Where(HasCrossbowWeapon).ToList();
            var candidates = crossbowmen.Count > 0 ? crossbowmen : playerTroops;
            return SelectStable(candidates, request.SessionId, request.Venue.StringId, "player-crossbowman", slot);
        }

        return SelectCrossbowman(request, slot);
    }

    internal static CharacterObject? SelectPresetExecutioner(
        ExecutionRequest request,
        string? requestedCharacterId,
        int slot = 0) =>
        TrySelectRequestedPlayerTroop(requestedCharacterId, "executioner", out var selected)
            ? selected
            : SelectExecutioner(request, slot);

    internal static CharacterObject? SelectPresetGuard(
        ExecutionRequest request,
        string? requestedCharacterId,
        int slot) =>
        TrySelectRequestedPlayerTroop(requestedCharacterId, "melee guard", out var selected)
            ? selected
            : SelectGuard(request, slot);

    internal static CharacterObject? SelectPresetCrossbowman(
        ExecutionRequest request,
        string? requestedCharacterId,
        int slot) =>
        TrySelectRequestedPlayerTroop(requestedCharacterId, "ranged guard", out var selected)
            ? selected
            : SelectCrossbowman(request, slot);

    private static bool TrySelectRequestedPlayerTroop(
        string? requestedCharacterId,
        string role,
        out CharacterObject? selected)
    {
        selected = null;
        if (string.IsNullOrWhiteSpace(requestedCharacterId))
        {
            return false;
        }

        selected = GetPlayerPartyRegularSoldiers().FirstOrDefault(character =>
            string.Equals(character.StringId, requestedCharacterId, StringComparison.Ordinal));
        if (selected is not null)
        {
            return true;
        }

        RichExecutions.Diagnostics.RexLog.Warning(
            $"Saved custom-site {role} template '{requestedCharacterId}' is not present in the player party; falling back to the venue culture.");
        return false;
    }

    internal static CharacterObject? SelectCrossbowman(ExecutionRequest request, int slot)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var culture = request.Venue.Culture;
        var regulars = GetRegularSoldiers(culture, GuardMinimumTier, requireMeleeWeapon: false);
        var nativeCrossbowmen = regulars
            .Where(HasCrossbowWeapon)
            .ToList();
        var candidates = nativeCrossbowmen.Count > 0 ? nativeCrossbowmen : regulars;
        return candidates.Count > 0
            ? SelectStable(
                candidates,
                request.SessionId,
                request.Venue.StringId,
                "crossbowman",
                slot)
            : null;
    }

    internal static bool TryCreateGuardCeremonyEquipment(
        ExecutionRequest request,
        CharacterObject guard,
        out Equipment? equipment,
        out string polearmId)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (guard is null)
        {
            throw new ArgumentNullException(nameof(guard));
        }

        equipment = null;
        polearmId = string.Empty;
        var baseEquipment = guard.FirstBattleEquipment;
        if (baseEquipment is null || baseEquipment.IsEmpty() ||
            !TryFindCulturePolearm(request.Venue.Culture, guard, out var polearm))
        {
            return false;
        }

        equipment = baseEquipment.Clone(cloneWithoutWeapons: true);
        equipment.AddEquipmentToSlotWithoutAgent(
            EquipmentIndex.WeaponItemBeginSlot,
            new EquipmentElement(polearm));
        polearmId = polearm.Item?.StringId ?? string.Empty;
        return !string.IsNullOrWhiteSpace(polearmId);
    }

    internal static CharacterObject? SelectGuard(Guid sessionId, Settlement venue, int slot)
    {
        if (venue is null)
        {
            throw new ArgumentNullException(nameof(venue));
        }

        var culture = venue.Culture;
        var role = "guard";
        var qualified = GetRegularSoldiers(culture, GuardMinimumTier, requireMeleeWeapon: true);
        if (qualified.Count > 0)
        {
            return SelectStable(qualified, sessionId, venue.StringId, role, slot);
        }

        return null;
    }

    internal static CharacterObject? SelectExecutioner(Guid sessionId, Settlement venue, int slot = 0)
    {
        if (venue is null)
        {
            throw new ArgumentNullException(nameof(venue));
        }

        var culture = venue.Culture;
        var role = "executioner";
        var qualified = GetRegularSoldiers(culture, ExecutionerMinimumTier, requireMeleeWeapon: false);
        if (qualified.Count > 0)
        {
            return SelectStable(qualified, sessionId, venue.StringId, role, slot);
        }

        return null;
    }

    internal static IReadOnlyList<CharacterObject> GetPlayerPartyRegularSoldierTemplates() =>
        GetPlayerPartyRegularSoldiers();

    private static List<CharacterObject> GetPlayerPartyRegularSoldiers()
    {
        var mainParty = MobileParty.MainParty;
        if (mainParty?.Party?.MemberRoster is null)
        {
            return new List<CharacterObject>();
        }

        return mainParty.Party.MemberRoster.GetTroopRoster()
            .Where(element => element.Number > 0)
            .Select(element => element.Character)
            .Where(character => character is not null && !character.IsHero && !character.IsPlayerCharacter)
            .Where(character => IsUsableRegularSoldier(character, requireMeleeWeapon: false))
            .Distinct()
            .OrderBy(character => character.StringId, StringComparer.Ordinal)
            .ToList();
    }
    private static List<CharacterObject> GetRegularSoldiers(
        CultureObject? culture,
        int minimumTier,
        bool requireMeleeWeapon)
    {
        if (culture is null || BannerlordCampaign.Current is null)
        {
            return new List<CharacterObject>();
        }

        return BuildCultureTroopTree(culture)
            .Where(character => IsUsableRegularSoldier(character, requireMeleeWeapon))
            .Where(character => character.Tier >= minimumTier)
            .OrderBy(character => character.StringId, StringComparer.Ordinal)
            .ToList();
    }

    private static HashSet<CharacterObject> BuildCultureTroopTree(CultureObject culture)
    {
        var result = new HashSet<CharacterObject>();
        var pending = new Queue<CharacterObject>();
        if (culture.BasicTroop is not null)
        {
            pending.Enqueue(culture.BasicTroop);
        }

        if (culture.EliteBasicTroop is not null)
        {
            pending.Enqueue(culture.EliteBasicTroop);
        }

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            if (current is null || !result.Add(current))
            {
                continue;
            }

            foreach (var upgradeTarget in current.UpgradeTargets)
            {
                if (upgradeTarget is not null && !result.Contains(upgradeTarget))
                {
                    pending.Enqueue(upgradeTarget);
                }
            }
        }

        return result;
    }

    private static bool IsUsableRegularSoldier(
        CharacterObject? character,
        bool requireMeleeWeapon) =>
        character is not null &&
        character.Occupation == Occupation.Soldier &&
        character.IsRegular &&
        IsUsableCultureTroop(character, requireMeleeWeapon);

    private static bool IsUsableCultureTroop(
        CharacterObject character,
        bool requireMeleeWeapon)
    {
        if (character.IsHero ||
            character.IsTemplate ||
            character.IsChildTemplate ||
            !character.IsReady ||
            character.IsObsolete ||
            character.Age < 18f)
        {
            return false;
        }

        var equipment = character.FirstBattleEquipment;
        return equipment is not null &&
               !equipment.IsEmpty() &&
               (!requireMeleeWeapon || HasMeleeWeapon(equipment));
    }

    private static bool HasMeleeWeapon(Equipment equipment)
    {
        for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
             slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
             slotIndex++)
        {
            var item = equipment[(EquipmentIndex)slotIndex].Item;
            var weapon = item?.PrimaryWeapon;
            if (weapon is not null && weapon.IsMeleeWeapon && !weapon.IsConsumable)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasCrossbowWeapon(CharacterObject character)
    {
        foreach (var equipment in character.BattleEquipments)
        {
            if (equipment is null)
            {
                continue;
            }

            for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
                 slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
                 slotIndex++)
            {
                if (equipment[(EquipmentIndex)slotIndex].Item?.PrimaryWeapon?.WeaponClass ==
                    WeaponClass.Crossbow)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryFindCulturePolearm(
        CultureObject? culture,
        CharacterObject selectedGuard,
        out EquipmentElement polearm)
    {
        var equipmentSources = new List<IEnumerable<Equipment>>
        {
            selectedGuard.BattleEquipments
        };
        var cultureGuard = culture?.Guard;
        if (cultureGuard is not null && !ReferenceEquals(cultureGuard, selectedGuard))
        {
            equipmentSources.Add(cultureGuard.BattleEquipments);
        }

        if (culture is not null)
        {
            var cultureTroops = BuildCultureTroopTree(culture)
                .Where(character => IsUsableRegularSoldier(character, requireMeleeWeapon: false))
                .OrderByDescending(character => character.Tier >= GuardMinimumTier)
                .ThenByDescending(character => character.Tier)
                .ThenBy(character => character.StringId, StringComparer.Ordinal);
            foreach (var character in cultureTroops)
            {
                equipmentSources.Add(character.BattleEquipments);
            }
        }

        // act_guard_idle_spear accepts the normal one-handed spear stance.
        // Prefer a culture-owned weapon that can retain that stance for every
        // guard, then retain the older any-polearm fallback for replacement
        // cultures that provide no compatible spear at all.
        if (TryFindPolearm(
                equipmentSources,
                requireLordHallCompatiblePolearm: true,
                out polearm))
        {
            return true;
        }

        if (TryFindPolearm(
                equipmentSources,
                requireLordHallCompatiblePolearm: false,
                out polearm))
        {
            return true;
        }

        polearm = default;
        return false;
    }

    private static bool TryFindPolearm(
        IEnumerable<IEnumerable<Equipment>> equipmentSources,
        bool requireLordHallCompatiblePolearm,
        out EquipmentElement polearm)
    {
        foreach (var equipmentSets in equipmentSources)
        {
            foreach (var equipment in equipmentSets)
            {
                if (equipment is null)
                {
                    continue;
                }

                for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
                     slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
                     slotIndex++)
                {
                    var element = equipment[(EquipmentIndex)slotIndex];
                    var weapon = element.Item?.PrimaryWeapon;
                    if (weapon is not null &&
                        weapon.IsPolearm &&
                        weapon.IsMeleeWeapon &&
                        !weapon.IsConsumable &&
                        (!requireLordHallCompatiblePolearm ||
                         weapon.WeaponClass == WeaponClass.OneHandedPolearm))
                    {
                        polearm = element;
                        return true;
                    }
                }
            }
        }

        polearm = default;
        return false;
    }

    private static CharacterObject SelectStable(
        IReadOnlyList<CharacterObject> candidates,
        Guid sessionId,
        string? venueId,
        string role,
        int slot)
    {
        var hash = FnvOffsetBasis;
        AddBytes(ref hash, sessionId.ToByteArray());
        AddSeparator(ref hash);
        AddString(ref hash, venueId ?? string.Empty);
        AddSeparator(ref hash);
        AddString(ref hash, role);
        AddSeparator(ref hash);
        AddInt32(ref hash, slot);

        hash = Mix64(hash);
        return candidates[(int)(hash % (ulong)candidates.Count)];
    }

    private static void AddString(ref ulong hash, string value) =>
        AddBytes(ref hash, Encoding.UTF8.GetBytes(value));

    private static void AddInt32(ref ulong hash, int value)
    {
        unchecked
        {
            AddByte(ref hash, (byte)value);
            AddByte(ref hash, (byte)(value >> 8));
            AddByte(ref hash, (byte)(value >> 16));
            AddByte(ref hash, (byte)(value >> 24));
        }
    }

    private static void AddSeparator(ref ulong hash) => AddByte(ref hash, 0xFF);

    private static void AddBytes(ref ulong hash, IEnumerable<byte> bytes)
    {
        foreach (var value in bytes)
        {
            AddByte(ref hash, value);
        }
    }

    private static void AddByte(ref ulong hash, byte value)
    {
        unchecked
        {
            hash ^= value;
            hash *= FnvPrime;
        }
    }

    private static ulong Mix64(ulong value)
    {
        unchecked
        {
            value ^= value >> 30;
            value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27;
            value *= 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
