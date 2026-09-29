using AnimusForge;
using S = AnimusForge.ScenePresentationParticipantState;

int passed = 0;
void Require(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL: " + message);
    passed++;
}

// Seal cycle: 参与 → 屏蔽 → 锁定 → 参与; the addressee can never be excluded.
Require(ScenePresentationPolicy.NextState(S.Participating, false) == S.Excluded, "participating -> excluded");
Require(ScenePresentationPolicy.NextState(S.Excluded, false) == S.Locked, "excluded -> locked");
Require(ScenePresentationPolicy.NextState(S.Locked, false) == S.Participating, "locked -> participating");
Require(ScenePresentationPolicy.NextState(S.Participating, true) == S.Locked, "addressee skips excluded");
Require(ScenePresentationPolicy.NextState(S.Locked, true) == S.Participating, "addressee locked -> participating");

// Range: locked ignores distance, participating needs to be within max range.
Require(ScenePresentationPolicy.IsInRange(S.Locked, 1e9f, 100f), "locked at any distance");
Require(ScenePresentationPolicy.IsInRange(S.Participating, 100f, 100f), "participating on boundary");
Require(!ScenePresentationPolicy.IsInRange(S.Participating, 100.5f, 100f), "participating beyond range");
Require(!ScenePresentationPolicy.IsInRange(S.Excluded, 200f, 100f), "excluded beyond range");

// Audience: excluded never, dead never, out of range never.
Require(ScenePresentationPolicy.IsAudience(S.Participating, true, true), "live participating in range");
Require(ScenePresentationPolicy.IsAudience(S.Locked, true, true), "live locked");
Require(!ScenePresentationPolicy.IsAudience(S.Excluded, true, true), "excluded is not audience");
Require(!ScenePresentationPolicy.IsAudience(S.Locked, false, true), "dead locked is not audience");
Require(!ScenePresentationPolicy.IsAudience(S.Participating, true, false), "out of range is not audience");

// Addressee choice: first locked audience member, else first audience member, else none.
Require(ScenePresentationPolicy.ChooseAddressee(new[] { 5, 7, 9 }, new[] { S.Participating, S.Locked, S.Locked }, new[] { true, true, true }) == 7, "prefers first locked");
Require(ScenePresentationPolicy.ChooseAddressee(new[] { 5, 7 }, new[] { S.Participating, S.Locked }, new[] { true, false }) == 5, "skips non-audience locked");
Require(ScenePresentationPolicy.ChooseAddressee(new[] { 5, 7 }, new[] { S.Excluded, S.Participating }, new[] { false, true }) == 7, "first audience member");
Require(ScenePresentationPolicy.ChooseAddressee(new[] { 5 }, new[] { S.Excluded }, new[] { false }) == -1, "no audience -> -1");
Require(ScenePresentationPolicy.ChooseAddressee(Array.Empty<int>(), Array.Empty<S>(), Array.Empty<bool>()) == -1, "empty -> -1");

Console.WriteLine("PASS: " + passed + " scene presentation policy assertions.");
