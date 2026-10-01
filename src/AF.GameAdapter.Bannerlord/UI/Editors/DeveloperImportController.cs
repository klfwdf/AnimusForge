using System;

namespace AnimusForge;

internal sealed class DeveloperImportPlan
{
 internal Func<bool, bool> Persona, Memory;
 internal Action<bool> Debt, Voice, Weekly, Kingdom, UnnamedPersona, Knowledge, Completed;
}

// Owns editor confirmation lifetime and the existing non-transactional domain order.
internal sealed class DeveloperImportController
{
 private readonly Func<long> _generation;
 private readonly Func<long, bool> _current;
 private long _ticket;
 internal DeveloperImportController(Func<long> generation, Func<long, bool> current)
 { _generation = generation; _current = current; }
 internal Action[] BeginConfirmation(Action overwrite, Action skip, Action cancel)
 {
  long generation = _generation();
  long ticket = ++_ticket;
  bool answered = false;
  Action Guard(Action action) => () =>
  {
   if (answered || ticket != _ticket || !_current(generation)) return;
   answered = true;
   action?.Invoke();
  };
  return new[] { Guard(overwrite), Guard(skip), Guard(cancel) };
 }
 internal void Execute(DeveloperImportPlan plan, bool overwriteExisting, long generation)
 {
  if (!_current(generation) || plan == null) return;
  if (plan.Persona != null && !plan.Persona(overwriteExisting)) return;
  if (plan.Memory != null && !plan.Memory(overwriteExisting)) return;
  plan.Debt?.Invoke(overwriteExisting);
  plan.Voice?.Invoke(overwriteExisting);
  plan.Weekly?.Invoke(overwriteExisting);
  plan.Kingdom?.Invoke(overwriteExisting);
  plan.UnnamedPersona?.Invoke(overwriteExisting);
  plan.Knowledge?.Invoke(overwriteExisting);
  plan.Completed?.Invoke(overwriteExisting);
 }
}
