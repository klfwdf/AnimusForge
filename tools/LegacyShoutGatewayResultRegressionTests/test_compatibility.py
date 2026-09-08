from pathlib import Path
import importlib.util,unittest
ROOT=Path(__file__).resolve().parents[2];GATEWAY='Refactor/Contracts/LegacyShoutNetworkGateway.cs'
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
class Compatibility(unittest.TestCase):
 def test_legacy_transports_and_stream_generation_are_unchanged(self):
  current=ex.source(GATEWAY,None);baseline=ex.source(GATEWAY,'5ce8767a')
  for signature in ['public static Task<string> SendLegacyMessagesAsync(','public static Task SendLegacyMessagesStreamAsync(','public async Task<LlmGenerateResult> GenerateStreamAsync(']:
   self.assertEqual(ex.declaration(current,signature),ex.declaration(baseline,signature),signature)
 def test_error_prefixes_are_emitted_by_real_shout_network(self):
  network=(ROOT/'ShoutNetwork.cs').read_text(encoding='utf-8-sig')
  for prefix in ['（错误：未配置 API Key）','（错误：未配置模型名称）','（API请求失败:','（API响应格式错误:','（程序错误:']:
   self.assertIn(prefix,network)
  self.assertIn('【模型回复（完整）】',(ROOT/'LlmRetryPrompt.cs').read_text(encoding='utf-8-sig'))
 def test_classifier_does_not_use_broad_exception_keywords(self):
  m=ex.declaration(ex.source(GATEWAY,None),'private static bool TryClassifyLegacyTransportFailure(')
  for broad in ['IsRetryableLlmError','HttpRequestException','TaskCanceledException','operation was canceled']:
   self.assertNotIn(broad,m)
if __name__=='__main__':unittest.main()
