using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal interface IWorldDiplomacyDraftRepairWorld : IWorldDiplomacyPromptWorld
{
    string BuildBilateralState(string author, string target);
    string BuildGovernmentHardFact(string author);
    string NewId(string prefix);
    void Log(string text);
}
