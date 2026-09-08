"""Source-boundary checks supplement executable Courier owner tests."""
import unittest
import run


class ExtractionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.blocks = run.extract()
        cls.courier = run.ex.source("CourierDeliveryBehavior.cs", None)

    def test_complete_production_owner_partial(self):
        self.assertEqual(run.ex.source("CourierDeliveryBehavior.DetachedPostprocess.cs", None), self.blocks["PARTIAL"])
        self.assertEqual(1, self.blocks["PARTIAL"].count("Task.Delay(30000)"))

    def test_factory_capture_and_mapping_are_verbatim(self):
        for signature in run.SIGNATURES:
            with self.subTest(signature=signature):
                self.assertIn(run.ex.declaration(self.courier, signature), self.blocks["METHODS"])

    def test_real_work_item_and_parser(self):
        shout = run.ex.source("ShoutBehavior.cs", None)
        self.assertIn(self.blocks["WORK_ITEM"], shout)
        self.assertIn("Refactor/Adapters/LegacyActionTagParser.cs", run.LINKS)
        self.assertIn("LlmVisibleReplyNormalizer.cs", run.LINKS)
        self.assertIn("CourierVisibleLetterSanitizer.cs", run.LINKS)

    def test_raw_reply_not_display_text_feeds_owner(self):
        prepare = run.ex.declaration(self.blocks["PARTIAL"], "private static async Task<PromptPackage> PrepareCourierDetachedPostprocessAsync(")
        self.assertIn("PrepareNpcReplyForActionPostprocess(rawReply)", prepare)
        self.assertIn("request.LetterText, request.HistoryText, reply", prepare)
        self.assertNotIn("CleanNpcReply(rawReply)", prepare)

    def test_normalization_precedes_actual_parser(self):
        complete = run.ex.declaration(self.blocks["PARTIAL"], "private static async Task<ActionPlan> CompleteCourierDetachedPostprocessAsync(")
        self.assertLess(complete.index("CompleteOnMainThread(rawText)"), complete.index("parser.Parse(normalized, context)"))
        self.assertIn("!owners.Remove(context)", complete)
        self.assertIn("ReferenceEquals(owner.Recipient", complete)

    def test_default_entry_reuses_prepared_envelope(self):
        method = run.ex.declaration(self.courier, "private async Task PrepareAndGenerateCourierReplyOffMainThreadAsync(")
        self.assertEqual(1, method.count("CapturePreparedCourierReplyEnvelope("))
        self.assertIn("CreateCourierDetachedPorts(LegacyActionTagCatalog.DefaultAllowedTagFamilies, true, 64, preparedMainPrompt)", method)
        self.assertIn("_ => preparedEnvelope", method)
        self.assertNotIn("CreateCourierReplyRefactorFacadeForExternal(", method)
        self.assertNotIn("CaptureCourierReplyRefactorEnvelopeForExternal(", method)
        capture = run.ex.declaration(self.courier, "private InteractionEnvelope CapturePreparedCourierReplyEnvelope(")
        self.assertNotIn("BuildCourierReplyGenerationRequestOnMainThread", capture)

    def test_public_unbound_parser_cannot_grant_actions(self):
        factory = run.ex.declaration(self.courier, "private static LegacyInteractionPipelinePorts CreateCourierDetachedPorts(")
        self.assertIn("(rawText, context) => new ActionPlan(Array.Empty<ActionRequest>(), string.Empty)", factory)
        self.assertIn("PrepareCourierDetachedPostprocessAsync(envelope, rawReply, context, postprocessOwners, token)", factory)
        self.assertIn("CompleteCourierDetachedPostprocessAsync(rawText, context, postprocessOwners, actionParser, token)", factory)


if __name__ == "__main__": unittest.main()
