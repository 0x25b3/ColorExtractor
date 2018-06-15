using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorExtractor
{
    public enum Sort
    {
        [Description("HSL - Hue, Saturation, Lightness (\"Best\" Result)")]
        HueSaturationLightness = 0,
        [Description("Hue and Perceived Brightness")]
        HuePerceivedBrightness,
        [Description("Only Perceived Brightness")]
        PerceivedBrightness,
        [Description("Only Hue")]
        Hue,
    }
}
