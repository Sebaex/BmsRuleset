using System.IO;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Game.Rulesets.BmsRuleset.Media;

internal sealed class BmsBgaLayerTextureLoaderStore(IResourceStore<byte[]> store) : TextureLoaderStore(store)
{
    protected override Image<TPixel> ImageFromStream<TPixel>(Stream stream)
    {
        var image = base.ImageFromStream<TPixel>(stream);
        // beatoraja's layer.frag keys pure black only on image overlays; base/Poor and videos keep their own alpha.
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var colour = default(Rgba32);
                    row[x].ToRgba32(ref colour);
                    if (colour.R != 0 || colour.G != 0 || colour.B != 0)
                        continue;

                    colour.A = 0;
                    row[x].FromRgba32(colour);
                }
            }
        });
        return image;
    }
}
