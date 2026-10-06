using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Settlements.Locations;

public sealed class LocationCharacter
{
    public enum CharacterRelations { Neutral }
    public AgentData Data;
    public LocationCharacter(AgentData data, object behavior, object tag, bool fixedLocation,
        CharacterRelations relation, object action, bool civilian, bool overrideBodyProperties)
    { Data = data; }
}
public sealed class Location
{
    public readonly List<LocationCharacter> Characters = new();
    public LocationCharacter GetLocationCharacter(object origin) => Characters.FirstOrDefault(c => ReferenceEquals(c.Data.Origin, origin));
    public void AddCharacter(LocationCharacter character) => Characters.Add(character);
    public void RemoveLocationCharacter(LocationCharacter character) => Characters.Remove(character);
}
public sealed class LocationComplex
{
    public readonly List<Location> Locations = new();
    public bool FailRemoval;
    public void RemoveCharacterIfExists(LocationCharacter character)
    {
        if (FailRemoval) throw new InvalidOperationException("Removal unavailable");
        foreach (var location in Locations) location.RemoveLocationCharacter(character);
    }
}
