using UnityEditor;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Imports the pixel TTFs in Fonts/TTF with hinted raster rendering, so their edges stay hard
    /// instead of anti-aliased. Covers every font in the folder, including ones added later.
    /// </summary>
    public class PixelFontImporter : AssetPostprocessor
    {
        private const string TtfFolder = "Assets/_Sandbox/nilly-ctrl/Fonts/TTF/";

        private void OnPreprocessAsset()
        {
            if (assetPath.StartsWith(TtfFolder) && assetImporter is TrueTypeFontImporter importer)
            {
                importer.fontRenderingMode = FontRenderingMode.HintedRaster;
            }
        }
    }
}
