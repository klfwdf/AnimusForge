using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AnimusForge.Illustrator
{
    internal static class IllustratorText
    {
        private static readonly Lazy<Dictionary<string, string>> Prompts =
            new Lazy<Dictionary<string, string>>(LoadPrompts);
        private static readonly Lazy<Dictionary<string, string>> Compatibility =
            new Lazy<Dictionary<string, string>>(() => Load("IllustratorCompatibilityTexts.json"));
        private static readonly Regex ProtocolMarker = new Regex(@"\[\[(AFI_PROTOCOL_[A-Z_]+)\]\]", RegexOptions.Compiled);

        private static Dictionary<string, string> LoadPrompts()
        {
            Dictionary<string, string> values = Load("IllustratorPromptTexts.json");
            foreach (string key in new List<string>(values.Keys))
                values[key] = ProtocolMarker.Replace(values[key], match => Raw(match.Groups[1].Value));
            return values;
        }

        private static Dictionary<string, string> Load(string file)
            => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
                AnimusForgeModulePaths.GetModuleDataFilePath(file), Encoding.UTF8));

        private static readonly Lazy<Dictionary<string, string>> LegacyCases = new Lazy<Dictionary<string, string>>(() =>
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            result[Raw("Residual_IllustrationFailureUiText_9bd8c12490b0")] = "AFICASE9bd8c12490b0fd1f";
            result[Raw("Residual_IllustrationFailureUiText_8b24a74c6c0c")] = "AFICASE8b24a74c6c0c7319";
            result[Raw("Residual_IllustrationFailureUiText_51e1db2786b4")] = "AFICASE51e1db2786b4a269";
            result[Raw("Residual_IllustrationFailureUiText_cdf7c2522d1c")] = "AFICASEcdf7c2522d1c86b7";
            result[Raw("Residual_IllustrationFailureUiText_3c00eaf66c61")] = "AFICASE3c00eaf66c61a225";
            result[Raw("Residual_IllustrationFailureUiText_abec1504a532")] = "AFICASEabec1504a532f44d";
            result[Raw("Residual_IllustrationFailureUiText_cc9ccfb372d0")] = "AFICASEcc9ccfb372d02f70";
            result[Raw("Residual_IllustrationFailureUiText_c71b263fefa5")] = "AFICASEc71b263fefa57520";
            result[Raw("Residual_IllustrationFailureUiText_7c13f5e61eb1")] = "AFICASE7c13f5e61eb17341";
            result[Raw("Residual_IllustrationFailureUiText_57dc41db19e0")] = "AFICASE57dc41db19e0f065";
            result[Raw("Residual_IllustrationFailureUiText_222185ef18d8")] = "AFICASE222185ef18d806ef";
            result[Raw("Residual_IllustrationFailureUiText_09f096a542be")] = "AFICASE09f096a542bee8f8";
            result[Raw("Residual_IllustrationFailureUiText_115753d3609f")] = "AFICASE115753d3609f1b0b";
            result[Raw("Residual_IllustrationFailureUiText_440ceae2c62c")] = "AFICASE440ceae2c62cdf87";
            result[Raw("Residual_IllustrationFailureUiText_50f5a67bd7a6")] = "AFICASE50f5a67bd7a60fab";
            result[Raw("Residual_IllustrationFailureUiText_18b670f4b65a")] = "AFICASE18b670f4b65a7fa0";
            result[Raw("Residual_IllustrationFailureUiText_e6a5add8fa93")] = "AFICASEe6a5add8fa93bc78";
            result[Raw("Residual_IllustrationFailureUiText_b85aa6999a9f")] = "AFICASEb85aa6999a9f2750";
            result[Raw("Residual_IllustrationFailureUiText_5dbd9db503bf")] = "AFICASE5dbd9db503bf3f17";
            result[Raw("Residual_IllustrationFailureUiText_5244fc23802d")] = "AFICASE5244fc23802dd5f6";
            result[Raw("Residual_IllustrationFailureUiText_af317e0b68d0")] = "AFICASEaf317e0b68d09d29";
            result[Raw("Residual_IllustrationFailureUiText_6a0ceb2c1689")] = "AFICASE6a0ceb2c1689c39c";
            result[Raw("Residual_IllustrationFailureUiText_98fdfef07992")] = "AFICASE98fdfef079923148";
            result[Raw("Residual_IllustrationFailureUiText_c11cd77a6f72")] = "AFICASEc11cd77a6f725244";
            result[Raw("Residual_IllustrationFailureUiText_c1913753081d")] = "AFICASEc1913753081d45ba";
            result[Raw("Residual_IllustrationFailureUiText_dfcff5821b1c")] = "AFICASEdfcff5821b1cfde3";
            result[Raw("Residual_IllustrationFailureUiText_b3f9754519dd")] = "AFICASEb3f9754519dd6940";
            result[Raw("Residual_IllustrationFailureUiText_a35df81b044a")] = "AFICASEa35df81b044a6e8d";
            result[Raw("Residual_IllustrationFailureUiText_406e133d8493")] = "AFICASE406e133d8493b4e8";
            result[Raw("Residual_IllustrationFailureUiText_7f9e5f019afd")] = "AFICASE7f9e5f019afd8af7";
            result[Raw("Residual_IllustrationFailureUiText_4e470f8e0206")] = "AFICASE4e470f8e02062a49";
            result[Raw("Residual_IllustrationFailureUiText_20f4234a9700")] = "AFICASE20f4234a9700a407";
            result[Raw("Residual_IllustrationFailureUiText_24e617455b9f")] = "AFICASE24e617455b9fad44";
            result[Raw("Residual_IllustrationFailureUiText_2630c97bdb59")] = "AFICASE2630c97bdb59fc38";
            result[Raw("Residual_IllustrationFailureUiText_13f8c940a283")] = "AFICASE13f8c940a28375e0";
            result[Raw("Residual_IllustrationFailureUiText_f681e28dc6d3")] = "AFICASEf681e28dc6d3c240";
            result[Raw("Residual_IllustrationFailureUiText_8d4f171a95c8")] = "AFICASE8d4f171a95c824cd";
            result[Raw("Residual_IllustrationFailureUiText_01f18bcf9fa5")] = "AFICASE01f18bcf9fa5ed1b";
            result[Raw("Residual_IllustrationFailureUiText_77ab8283f295")] = "AFICASE77ab8283f295c564";
            result[Raw("Residual_IllustrationFailureUiText_a4805ab773ef")] = "AFICASEa4805ab773ef9b1e";
            result[Raw("Residual_IllustrationFailureUiText_b9f5a52fddda")] = "AFICASEb9f5a52fdddaa20f";
            result[Raw("Residual_IllustrationFailureUiText_7e0355adf78b")] = "AFICASE7e0355adf78bfb9e";
            result[Raw("Residual_IllustrationFailureUiText_dab6b96ba2bc")] = "AFICASEdab6b96ba2bc2420";
            result[Raw("Residual_IllustrationFailureUiText_089c10d6bd73")] = "AFICASE089c10d6bd73985c";
            result[Raw("Residual_IllustrationFailureUiText_4b5743ce4e1b")] = "AFICASE4b5743ce4e1b6004";
            result[Raw("Residual_IllustrationFailureUiText_071fbe581926")] = "AFICASE071fbe5819267d46";
            result[Raw("Residual_IllustrationFailureUiText_0afb057e12b2")] = "AFICASE0afb057e12b2b05a";
            result[Raw("Residual_IllustrationFailureUiText_c710e36ee5c2")] = "AFICASEc710e36ee5c20737";
            result[Raw("Residual_IllustrationFailureUiText_a8a0c7c2d4a1")] = "AFICASEa8a0c7c2d4a1840e";
            result[Raw("Residual_IllustrationFailureUiText_320de3434d42")] = "AFICASE320de3434d428aa9";
            result[Raw("Residual_IllustrationFailureUiText_ff334deb0f4b")] = "AFICASEff334deb0f4bc9ab";
            result[Raw("Residual_IllustrationFailureUiText_06cc9b12d340")] = "AFICASE06cc9b12d34011ff";
            result[Raw("Residual_IllustrationFailureUiText_b04e90bd8966")] = "AFICASEb04e90bd8966c02e";
            result[Raw("Residual_IllustrationFailureUiText_4a72b6d89ffa")] = "AFICASE4a72b6d89ffad816";
            result[Raw("Residual_IllustrationFailureUiText_cbc4a27f1d3d")] = "AFICASEcbc4a27f1d3d5d50";
            result[Raw("Residual_IllustrationFailureUiText_b2e8aa393cfa")] = "AFICASEb2e8aa393cfae238";
            result[Raw("Residual_IllustrationFailureUiText_601d9c6011f9")] = "AFICASE601d9c6011f90ec6";
            result[Raw("Residual_IllustrationFailureUiText_182c62faa16a")] = "AFICASE182c62faa16ab067";
            result[Raw("Residual_IllustrationFailureUiText_60459c35f13e")] = "AFICASE60459c35f13e804e";
            result[Raw("Residual_IllustrationFailureUiText_bcb2153ce82f")] = "AFICASEbcb2153ce82f08e2";
            result[Raw("Residual_IllustrationFailureUiText_20a10394bb25")] = "AFICASE20a10394bb25e364";
            result[Raw("Residual_IllustrationFailureUiText_4ce6d9401066")] = "AFICASE4ce6d9401066e872";
            result[Raw("Residual_IllustrationFailureUiText_3bceb09264b0")] = "AFICASE3bceb09264b09be5";
            result[Raw("Residual_IllustrationFailureUiText_0f13bf8f410e")] = "AFICASE0f13bf8f410e2f04";
            result[Raw("Residual_IllustrationFailureUiText_8f8115f1c6c5")] = "AFICASE8f8115f1c6c5d007";
            result[Raw("Residual_IllustrationFailureUiText_77d088ff8786")] = "AFICASE77d088ff8786fe8c";
            result[Raw("Residual_IllustrationFailureUiText_86b249622eb8")] = "AFICASE86b249622eb8b6a2";
            result[Raw("Residual_IllustrationFailureUiText_c65b9fd83e41")] = "AFICASEc65b9fd83e41f029";
            result[Raw("Residual_IllustrationFailureUiText_66121e8376f2")] = "AFICASE66121e8376f2ecc5";
            result[Raw("Residual_IllustrationFailureUiText_0a8aba7dd54d")] = "AFICASE0a8aba7dd54d70e4";
            result[Raw("Residual_IllustrationFailureUiText_cbcec16045cb")] = "AFICASEcbcec16045cb873a";
            result[Raw("Residual_IllustrationFailureUiText_c7311b67df53")] = "AFICASEc7311b67df5333f6";
            result[Raw("Residual_IllustrationFailureUiText_8f3bdd88a319")] = "AFICASE8f3bdd88a3193eda";
            result[Raw("Residual_IllustrationFailureUiText_a2968d9300e9")] = "AFICASEa2968d9300e9f708";
            result[Raw("Residual_IllustrationFailureUiText_d14a4d68df5d")] = "AFICASEd14a4d68df5d946e";
            result[Raw("Residual_IllustrationFailureUiText_195cbdc4af52")] = "AFICASE195cbdc4af5263b4";
            result[Raw("Residual_IllustrationFailureUiText_3b66687d42ff")] = "AFICASE3b66687d42ff8dab";
            result[Raw("Residual_IllustrationFailureUiText_da1c04128fde")] = "AFICASEda1c04128fde447d";
            result[Raw("Residual_IllustrationFailureUiText_030188035ef5")] = "AFICASE030188035ef5f96a";
            result[Raw("Residual_IllustrationFailureUiText_16fb0e39da0a")] = "AFICASE16fb0e39da0ac7f9";
            result[Raw("Residual_IllustrationFailureUiText_28017cb5f03c")] = "AFICASE28017cb5f03c5e32";
            result[Raw("Residual_IllustrationFailureUiText_08549a13859d")] = "AFICASE08549a13859da2ec";
            result[Raw("Residual_IllustrationFailureUiText_47868f884990")] = "AFICASE47868f8849902e56";
            result[Raw("Residual_IllustrationFailureUiText_faecb22b638c")] = "AFICASEfaecb22b638c013b";
            result[Raw("Residual_IllustrationFailureUiText_bc7a82e77f79")] = "AFICASEbc7a82e77f79206f";
            result[Raw("Residual_IllustrationFailureUiText_dd46ec78f458")] = "AFICASEdd46ec78f458f1fd";
            result[Raw("Residual_IllustrationFailureUiText_24df76e57095")] = "AFICASE24df76e570957027";
            result[Raw("Residual_IllustrationFailureUiText_f17c8cf08b2c")] = "AFICASEf17c8cf08b2c7ee4";
            result[Raw("Residual_IllustrationFailureUiText_e8186ab9e492")] = "AFICASEe8186ab9e492e981";
            result[Raw("Residual_IllustrationFailureUiText_abbc64da0999")] = "AFICASEabbc64da09994bae";
            result[Raw("Residual_IllustrationFailureUiText_a9399e2f0a46")] = "AFICASEa9399e2f0a46fe8d";
            result[Raw("Residual_IllustrationFailureUiText_9129d4acfad5")] = "AFICASE9129d4acfad5416f";
            result[Raw("Residual_IllustrationFailureUiText_046a2f857098")] = "AFICASE046a2f857098dc27";
            result[Raw("Residual_IllustrationFailureUiText_9bd69992dcc0")] = "AFICASE9bd69992dcc0d932";
            result[Raw("Residual_IllustrationFailureUiText_11ec79e731ce")] = "AFICASE11ec79e731ce368f";
            result[Raw("Residual_IllustrationFailureUiText_953728313135")] = "AFICASE9537283131352e60";
            result[Raw("Residual_IllustrationFailureUiText_6c12ed179562")] = "AFICASE6c12ed1795622ddb";
            result[Raw("Residual_IllustrationFailureUiText_d4dff0c6bb32")] = "AFICASEd4dff0c6bb32ef53";
            result[Raw("Residual_IllustrationFailureUiText_000cfa23b3b0")] = "AFICASE000cfa23b3b0805d";
            result[Raw("Residual_IllustrationFailureUiText_3c96f6b6297b")] = "AFICASE3c96f6b6297b0e97";
            result[Raw("Residual_IllustrationFailureUiText_5a3d96ea40de")] = "AFICASE5a3d96ea40de08f8";
            result[Raw("Residual_IllustrationFailureUiText_6683777e115f")] = "AFICASE6683777e115f24cd";
            result[Raw("Residual_IllustrationFailureUiText_89237dcf38f8")] = "AFICASE89237dcf38f85267";
            result[Raw("Residual_IllustrationFailureUiText_738ca0d7d9fa")] = "AFICASE738ca0d7d9fa875f";
            result[Raw("Residual_IllustrationFailureUiText_6278c7a5a21c")] = "AFICASE6278c7a5a21c1516";
            result[Raw("Residual_IllustrationFailureUiText_b1de378f1825")] = "AFICASEb1de378f18256092";
            result[Raw("Residual_IllustrationFailureUiText_6807c364800b")] = "AFICASE6807c364800bc2ae";
            result[Raw("Residual_IllustrationFailureUiText_2d59e27a67c9")] = "AFICASE2d59e27a67c9f873";
            result[Raw("Residual_IllustrationFailureUiText_a7ed57581c9b")] = "AFICASEa7ed57581c9b3aa1";
            result[Raw("Residual_IllustrationFailureUiText_da8123a1d875")] = "AFICASEda8123a1d875aa73";
            result[Raw("Residual_IllustrationFailureUiText_368844dbc2fb")] = "AFICASE368844dbc2fb7b02";
            result[Raw("Residual_IllustrationFailureUiText_ed007c0ab308")] = "AFICASEed007c0ab3089dc8";
            result[Raw("Residual_IllustrationFailureUiText_4f01dcd5a015")] = "AFICASE4f01dcd5a01528db";
            result[Raw("Residual_IllustrationFailureUiText_9ddde1e6fa0f")] = "AFICASE9ddde1e6fa0f0ac4";
            result[Raw("Residual_IllustrationFailureUiText_8266acd7d091")] = "AFICASE8266acd7d0919d0d";
            result[Raw("Residual_IllustrationFailureUiText_f1fb7acc80a0")] = "AFICASEf1fb7acc80a0b85d";
            result[Raw("Residual_IllustrationFailureUiText_4f55b71adc2e")] = "AFICASE4f55b71adc2e9cef";
            result[Raw("Residual_IllustrationFailureUiText_ac3a90182716")] = "AFICASEac3a901827164126";
            result[Raw("Residual_IllustrationFailureUiText_a34bb829a4df")] = "AFICASEa34bb829a4df7490";
            result[Raw("Residual_IllustrationFailureUiText_b3aff0c14c5c")] = "AFICASEb3aff0c14c5ccca4";
            result[Raw("Residual_IllustrationFailureUiText_fb98400f52e3")] = "AFICASEfb98400f52e3287e";
            result[Raw("Residual_IllustrationFailureUiText_2700dd579ad4")] = "AFICASE2700dd579ad4f599";
            result[Raw("Residual_IllustrationFailureUiText_5be584f3d9b6")] = "AFICASE5be584f3d9b63a5f";
            result[Raw("Residual_IllustrationFailureUiText_fda42ba646e0")] = "AFICASEfda42ba646e06c94";
            result[Raw("Residual_IllustrationFailureUiText_94debb6b2ddc")] = "AFICASE94debb6b2ddc49d6";
            result[Raw("Residual_IllustrationFailureUiText_38423e6cc29f")] = "AFICASE38423e6cc29fedb7";
            result[Raw("Residual_IllustrationFailureUiText_b482cbd99342")] = "AFICASEb482cbd99342eadf";
            result[Raw("Residual_IllustrationFailureUiText_90a5f647c95e")] = "AFICASE90a5f647c95e96f8";
            result[Raw("Residual_IllustrationFailureUiText_bdbdf94f8e0b")] = "AFICASEbdbdf94f8e0baab0";
            result[Raw("Residual_IllustrationFailureUiText_12a8f325a609")] = "AFICASE12a8f325a60959c8";
            result[Raw("Residual_IllustrationFailureUiText_ad29686613d1")] = "AFICASEad29686613d1e39f";
            result[Raw("Residual_IllustrationFailureUiText_5149b45e08cc")] = "AFICASE5149b45e08cc9bfa";
            result[Raw("Residual_IllustrationFailureUiText_f7591a2b5e3c")] = "AFICASEf7591a2b5e3c6d8c";
            result[Raw("Residual_IllustrationFailureUiText_2ea7db48b671")] = "AFICASE2ea7db48b671b715";
            result[Raw("Residual_IllustrationFailureUiText_bec3bbf325dc")] = "AFICASEbec3bbf325dca0ac";
            result[Raw("Residual_IllustrationFailureUiText_beadce8c85bb")] = "AFICASEbeadce8c85bb169f";
            result[Raw("Residual_IllustrationFailureUiText_d77d276bcea6")] = "AFICASEd77d276bcea67350";
            result[Raw("Residual_IllustrationFailureUiText_056066715081")] = "AFICASE056066715081e0ba";
            result[Raw("Residual_IllustrationFailureUiText_94eba315b783")] = "AFICASE94eba315b78347ac";
            result[Raw("Residual_IllustrationFailureUiText_70709b676992")] = "AFICASE70709b676992b1e3";
            result[Raw("Residual_IllustrationFailureUiText_0501fd1e67bb")] = "AFICASE0501fd1e67bb74cc";
            result[Raw("Residual_IllustrationFailureUiText_4f7bb69cc916")] = "AFICASE4f7bb69cc9161914";
            result[Raw("Residual_IllustrationFailureUiText_92af66dc9f00")] = "AFICASE92af66dc9f00dc24";
            result[Raw("Residual_IllustrationFailureUiText_687f51115d86")] = "AFICASE687f51115d86f4e3";
            result[Raw("Residual_IllustrationFailureUiText_c43af8a48fcb")] = "AFICASEc43af8a48fcbf232";
            result[Raw("Residual_IllustrationFailureUiText_25341083f09c")] = "AFICASE25341083f09cab04";
            result[Raw("Residual_IllustrationFailureUiText_ef4a13b5ed18")] = "AFICASEef4a13b5ed18eeaf";
            result[Raw("Residual_IllustrationFailureUiText_8c3466a000fc")] = "AFICASE8c3466a000fc33f1";
            result[Raw("Residual_IllustrationFailureUiText_4fee99491739")] = "AFICASE4fee994917391cc7";
            result[Raw("Residual_IllustrationFailureUiText_2138c4705e71")] = "AFICASE2138c4705e7120d7";
            result[Raw("Residual_IllustrationFailureUiText_e6ecb865f88e")] = "AFICASEe6ecb865f88e4fe6";
            result[Raw("Residual_IllustrationFailureUiText_612a69631eeb")] = "AFICASE612a69631eebd8c1";
            result[Raw("Residual_IllustrationFailureUiText_4ce414b787fe")] = "AFICASE4ce414b787fe6a77";
            result[Raw("Residual_IllustrationFailureUiText_81a976c54cf9")] = "AFICASE81a976c54cf9c68d";
            result[Raw("Residual_IllustrationFailureUiText_0d74292082e1")] = "AFICASE0d74292082e12424";
            result[Raw("Residual_IllustrationFailureUiText_9d533584d819")] = "AFICASE9d533584d8193eb7";
            result[Raw("Residual_IllustrationFailureUiText_43fe8d2ff206")] = "AFICASE43fe8d2ff2069a1a";
            result[Raw("Residual_IllustrationFailureUiText_573323997754")] = "AFICASE57332399775473fe";
            result[Raw("Residual_IllustrationFailureUiText_1c0e9b578437")] = "AFICASE1c0e9b57843701b7";
            result[Raw("Residual_IllustrationFailureUiText_5ebe388db56d")] = "AFICASE5ebe388db56d9106";
            result[Raw("Residual_IllustrationFailureUiText_19ce89a05a43")] = "AFICASE19ce89a05a432700";
            result[Raw("Residual_IllustrationFailureUiText_31492fedefc8")] = "AFICASE31492fedefc8eb58";
            result[Raw("Residual_IllustrationFailureUiText_765de577eda3")] = "AFICASE765de577eda396dd";
            result[Raw("Residual_IllustrationStatusUiText_8989e163cce9")] = "AFICASE8989e163cce94d4b";
            result[Raw("Residual_IllustrationStatusUiText_226ef240347e")] = "AFICASE226ef240347eeb15";
            result[Raw("Residual_IllustrationStatusUiText_1f88a73b3f5e")] = "AFICASE1f88a73b3f5e8a7d";
            result[Raw("Residual_IllustrationStatusUiText_9b51875a1934")] = "AFICASE9b51875a1934a3b6";
            result[Raw("Residual_IllustrationStatusUiText_6bab834a9317")] = "AFICASE6bab834a93172e19";
            result[Raw("Residual_IllustrationStatusUiText_9807866671a1")] = "AFICASE9807866671a11e30";
            result[Raw("Residual_IllustrationStatusUiText_ac7bd50a6013")] = "AFICASEac7bd50a6013b00d";
            result[Raw("Residual_IllustrationStatusUiText_3c08919c0643")] = "AFICASE3c08919c06430fe7";
            result[Raw("Residual_IllustrationStatusUiText_77f3d1765c98")] = "AFICASE77f3d1765c9838f7";
            result[Raw("Residual_IllustrationStatusUiText_fd4f3116db18")] = "AFICASEfd4f3116db18cbda";
            result[Raw("Residual_IllustrationStatusUiText_a01eeb634c60")] = "AFICASEa01eeb634c60faff";
            result[Raw("Residual_IllustrationStatusUiText_0f18bd4735d5")] = "AFICASE0f18bd4735d56b6e";
            return result;
        });
        internal static string NormalizeLegacyCase(string value)
            {
            if (value != null && LegacyCases.Value.TryGetValue(value, out string token)) return token;
            // An unknown provider/user detail that happens to equal an internal case
            // token must still take the original unknown-value branch.
            return value != null && value.StartsWith("AFICASE", StringComparison.Ordinal) ? value + "_UNMATCHED" : value;
        }

        internal static string Get(string key) => Prompts.Value[key];
        internal static string Format(string key, params object[] values) => string.Format(Get(key), values);
        internal static string Raw(string key) => Compatibility.Value[key];
        internal static string FormatRaw(string key, params object[] values) => string.Format(Raw(key), values);
    }
}
