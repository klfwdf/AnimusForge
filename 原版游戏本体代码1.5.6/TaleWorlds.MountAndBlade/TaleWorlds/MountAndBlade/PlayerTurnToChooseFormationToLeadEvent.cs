using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade;

public delegate void PlayerTurnToChooseFormationToLeadEvent(Dictionary<int, Agent> lockedFormationIndicesAndSergeants, List<int> remainingFormationIndices);
