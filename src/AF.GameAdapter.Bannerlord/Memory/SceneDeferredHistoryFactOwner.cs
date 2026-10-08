using System;
using System.Collections.Generic;
namespace AnimusForge;

// One pending fact authority, preserving clear-before-submit and detached target capture.
internal sealed class SceneDeferredHistoryFactOwner
{
 internal string Fact = "";
 internal List<NpcDataPacket> Targets = new List<NpcDataPacket>();
 internal int PersonalizedAgentIndex = -1;
 private readonly Func<List<NpcDataPacket>,List<NpcDataPacket>> _capture;
 private readonly Action<string,int> _promote;
 private readonly Func<string,List<NpcDataPacket>,int,bool,bool> _persist;
 internal SceneDeferredHistoryFactOwner(Func<List<NpcDataPacket>,List<NpcDataPacket>> capture,
     Action<string,int> promote, Func<string,List<NpcDataPacket>,int,bool,bool> persist)
 { _capture=capture??throw new ArgumentNullException(nameof(capture)); _promote=promote??throw new ArgumentNullException(nameof(promote)); _persist=persist??throw new ArgumentNullException(nameof(persist)); }
 internal void Set(string extraFact, List<NpcDataPacket> nearbyData, int personalizedAgentIndex = -1)
 {
     Fact = (extraFact ?? "").Trim();
     Targets = _capture(nearbyData) ?? new List<NpcDataPacket>();
     PersonalizedAgentIndex = personalizedAgentIndex;
 }
 internal bool Flush(bool requireMemoryReceipt = false)
 {
     string text = (Fact ?? "").Trim();
     List<NpcDataPacket> list = Targets ?? new List<NpcDataPacket>();
     int personalizedAgentIndex = PersonalizedAgentIndex;
     Fact = "";
     Targets = new List<NpcDataPacket>();
     PersonalizedAgentIndex = -1;
     if (string.IsNullOrWhiteSpace(text)) return true;
     if (personalizedAgentIndex >= 0) _promote(text, personalizedAgentIndex);
     return _persist(text,list,personalizedAgentIndex,requireMemoryReceipt);
 }
}
