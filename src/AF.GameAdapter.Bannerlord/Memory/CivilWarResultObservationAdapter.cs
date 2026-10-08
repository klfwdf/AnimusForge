using System;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal delegate void CivilWarObservationMaterial(string kind,string label,string text,string key,string kingdom,bool world,bool realm);

internal sealed class CivilWarResultObservationAdapter
{
    private readonly CivilWarObservationMaterial _material;
    private readonly ExecutionObservationBulletin _bulletin;
    internal CivilWarResultObservationAdapter(CivilWarObservationMaterial material,ExecutionObservationBulletin bulletin)
    { _material=material ?? throw new ArgumentNullException(nameof(material)); _bulletin=bulletin; }
    internal void Record(Kingdom kingdom,string key,string text,bool bulletin,Hero actor=null,Hero factionLeader=null)
    {
        if(kingdom==null||string.IsNullOrWhiteSpace(text)) return;
        _material("civil_war","内战政治 - "+kingdom.Name,text,key,kingdom.StringId,true,true);
        string kind=CivilWarPoliticalRules.PoliticalResultKind(key);
        if(bulletin) _bulletin?.Invoke(kind,key,CivilWarPoliticalRules.PoliticalResultPriority,kingdom.Name+"："+text,
            Clan.PlayerClan?.Kingdom==kingdom,"realm:"+kingdom.StringId,text,
            WorldBulletinEventCaptureAdapter.BulletinParticipants((actor, "本次政治行动方族长"), (factionLeader, "反对派领袖"), (kingdom.Leader, "王国君主，是否亲临现场依事实")), kingdom.StringId);
    }
}
