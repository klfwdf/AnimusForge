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
        _material("civil_war",AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_5898349b9786")+kingdom.Name,text,key,kingdom.StringId,true,true);
        string kind=CivilWarPoliticalRules.PoliticalResultKind(key);
        if(bulletin) _bulletin?.Invoke(kind,key,CivilWarPoliticalRules.PoliticalResultPriority,kingdom.Name+"："+text,
            Clan.PlayerClan?.Kingdom==kingdom,"realm:"+kingdom.StringId,text,
            WorldBulletinEventCaptureAdapter.BulletinParticipants((actor, AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_0d6138e44bcf")), (factionLeader, AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_97363cc224ea")), (kingdom.Leader, AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_3ba4b9d2c0d0"))), kingdom.StringId);
    }
}
