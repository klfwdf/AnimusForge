using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.Illustrator.Engine
{
    // Native renderer owns the image. This adapter never reconstructs banner shaders.
    internal static class BannerEmblemComposer
    {
        private static readonly NativeBannerPipeline Pipeline = new NativeBannerPipeline(
            ScreenCaptureHelper.RenderNativeBannerPngAsync);

        public static Task<string> ComposeToBase64Async(string bannerCode, int canvasSize = 256,
            bool cleanTempFiles = false, CancellationToken cancellationToken = default)
            => Pipeline.GetAsync(bannerCode, canvasSize, cleanTempFiles, cancellationToken);

        internal static void Reset() => Pipeline.Reset();
    }
}
