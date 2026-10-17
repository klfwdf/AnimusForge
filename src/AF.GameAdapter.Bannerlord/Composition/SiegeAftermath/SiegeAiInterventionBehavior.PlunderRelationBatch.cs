using System;
using System.Collections.Generic;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public partial class SiegeAiInterventionBehavior
{
	// Main-thread, synchronous mission-end scope only. No state survives the callback.
	private static TownPlunderRelationBatch _townPlunderRelationBatch;

	private static IDisposable BeginTownPlunderRelationBatch()
	{
		Settlement settlement = ResolveCurrentSettlement();
		if (_townPlunderRelationBatch != null || settlement?.IsTown != true || settlement.IsCastle)
		{
			return null;
		}
		return _townPlunderRelationBatch = new TownPlunderRelationBatch(settlement);
	}

	private static bool TryQueueTownPlunderRelation(Settlement settlement, Hero notable, int delta, string reason)
	{
		TownPlunderRelationBatch batch = _townPlunderRelationBatch;
		if (batch == null)
		{
			return false;
		}
		if (batch.Settlement != settlement || delta >= 0 || reason != TownPlunderConsequenceDelta.NotableRelationReason)
		{
			// Keep preceding plunder losses before any other relation effect (and its clamp).
			batch.Dispose();
			return false;
		}
		batch.Add(notable, delta);
		return true;
	}

	private static void FlushTownPlunderRelations()
	{
		_townPlunderRelationBatch?.Dispose();
	}

	private sealed class TownPlunderRelationBatch : IDisposable
	{
		private Dictionary<Hero, int> _deltas = new Dictionary<Hero, int>();

		internal Settlement Settlement { get; }

		internal TownPlunderRelationBatch(Settlement settlement)
		{
			Settlement = settlement;
		}

		internal void Add(Hero notable, int delta)
		{
			_deltas.TryGetValue(notable, out int previous);
			_deltas[notable] = previous + delta;
		}

		public void Dispose()
		{
			Dictionary<Hero, int> pending = _deltas;
			if (pending == null)
			{
				return;
			}
			// Detach before native events: repeated cleanup/reentry cannot replay this batch.
			_deltas = null;
			if (_townPlunderRelationBatch == this)
			{
				_townPlunderRelationBatch = null;
			}
			foreach (KeyValuePair<Hero, int> item in pending)
			{
				Hero notable = item.Key;
				if (notable == null || !notable.IsAlive || notable == Hero.MainHero || item.Value == 0)
				{
					continue;
				}
				try
				{
					float before = notable.GetRelation(Hero.MainHero);
					ChangeRelationAction.ApplyPlayerRelation(notable, item.Value, true, true);
					float after = notable.GetRelation(Hero.MainHero);
					GcczDiagnosticLog.Log("PlunderRelation", "settlement=" + Settlement.StringId
						+ " notable=" + notable.StringId + " requestedDelta=" + item.Value
						+ " before=" + before + " after=" + after + " appliedDelta=" + (after - before));
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "Batched plunder relation application failed. Settlement="
						+ Settlement.StringId + ", Notable=" + notable.StringId + ": " + ex.Message);
				}
			}
		}
	}
}
