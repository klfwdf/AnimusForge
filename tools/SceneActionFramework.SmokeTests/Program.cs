using System;
using AnimusForge;

internal static class Program
{
	private static int _failures;

	private static int Main()
	{
		ExpectPlayerInput("跪下", "kneel", SceneActionTargetKind.FramedNpcs, forced: false);
		ExpectPlayerInput("都给我跪下！", "kneel", SceneActionTargetKind.FramedNpcs, forced: false);
		ExpectPlayerInput("我自己跪下", "kneel", SceneActionTargetKind.Player, forced: false);
		ExpectPlayerInput("起身", "standup", SceneActionTargetKind.FramedNpcs, forced: false);
		ExpectPlayerInput("我站起来。", "standup", SceneActionTargetKind.Player, forced: false);
		ExpectPlayerInput("*西海", "xihai", SceneActionTargetKind.FramedNpcs, forced: true);
		ExpectPlayerInput("*act_taunt_02", "act_taunt_02", SceneActionTargetKind.FramedNpcs, forced: true);
		ExpectNoPlayerInput("我解释跪下是什么意思");
		ExpectNoPlayerInput("不许跪下");
		ExpectNoPlayerInput("西海是什么");
		Console.WriteLine(_failures == 0
			? "SceneActionFramework smoke tests passed."
			: "SceneActionFramework smoke tests failed: " + _failures);
		return _failures == 0 ? 0 : 1;
	}

	private static void ExpectPlayerInput(
		string input,
		string expectedKey,
		SceneActionTargetKind expectedTarget,
		bool forced)
	{
		if (!SceneActionIntentResolver.TryResolvePlayerInput(input, out var intent) ||
			intent.ActionKey != expectedKey ||
			intent.TargetKind != expectedTarget ||
			intent.IsForced != forced)
		{
			Fail("player input", input);
		}
	}

	private static void ExpectNoPlayerInput(string input)
	{
		if (SceneActionIntentResolver.TryResolvePlayerInput(input, out _))
		{
			Fail("unexpected player input match", input);
		}
	}

	private static void Fail(string kind, string value)
	{
		_failures++;
		Console.Error.WriteLine("FAIL " + kind + ": " + value);
	}
}
