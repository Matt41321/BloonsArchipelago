using System;
using System.Collections.Generic;

namespace BloonsArchipelago.Utils
{
    public static class TrapLink
    {
        public const string Tag = "TrapLink";

        public static readonly HashSet<string> NativeTraps = new(StringComparer.Ordinal)
        {
            "Modified Bloons", "Freeze Trap", "Bee Trap", "Speed Up Trap", "Literature Trap", "144p Trap",
            "Flood Trap", "Swap Trap", "Shuffle Trap", "Zoom Trap", "Screen Flip Trap", "Chaos Control Trap",
            "Math Quiz Trap", "Trivia Trap", "Pokemon Trivia Trap", "Number Sequence Trap",
            "Input Sequence Trap", "Yap Trap",
        };

        private static readonly Dictionary<string, string> _linkedTrapNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["144p Trap"]              = "144p Trap",
            ["CRT Trap"]               = "144p Trap",
            ["Fuzzy Trap"]             = "144p Trap",
            ["Pixelate Trap"]          = "144p Trap",
            ["Pixellation Trap"]       = "144p Trap",
            ["PSP Trap"]               = "144p Trap",
            ["SvC Effect"]             = "144p Trap",
            ["Vintage Trap"]           = "144p Trap",

            ["Bee Trap"]               = "Bee Trap",
            ["Frog Trap"]              = "Bee Trap",
            ["Furry Convention Trap"]  = "Bee Trap",

            ["Chaos Control Trap"]     = "Chaos Control Trap",
            ["Bullet Time Trap"]       = "Chaos Control Trap",
            ["Invert Colors Trap"]     = "Chaos Control Trap",
            ["Slow Trap"]              = "Chaos Control Trap",
            ["Slowness Trap"]          = "Chaos Control Trap",
            ["Stun Trap"]              = "Chaos Control Trap",
            ["Stutter Trap"]           = "Chaos Control Trap",
            ["Timer Trap"]             = "Chaos Control Trap",

            ["Flood Trap"]             = "Flood Trap",
            ["Fishing Trap"]           = "Flood Trap",
            ["Fishin' Boo Trap"]       = "Flood Trap",
            ["Meteor Trap"]            = "Flood Trap",
            ["Nut Trap"]               = "Flood Trap",
            ["Sinking Trap"]           = "Flood Trap",
            ["Sticky Floor Trap"]      = "Flood Trap",
            ["Summon Trap"]            = "Flood Trap",
            ["Underwater Trap"]        = "Flood Trap",
            ["Weather Cloudy Trap"]    = "Flood Trap",
            ["Weather Rainy Trap"]     = "Flood Trap",
            ["Weather Stormy Trap"]    = "Flood Trap",
            ["Whirlpool Trap"]         = "Flood Trap",

            ["Freeze Trap"]            = "Freeze Trap",
            ["Bonk Trap"]              = "Freeze Trap",
            ["Bubble Trap"]            = "Freeze Trap",
            ["Crystal Trap"]           = "Freeze Trap",
            ["Disarm Trap"]            = "Freeze Trap",
            ["Frost Trap"]             = "Freeze Trap",
            ["Frozen Trap"]            = "Freeze Trap",
            ["Honey Trap"]             = "Freeze Trap",
            ["Ice Floor Trap"]         = "Freeze Trap",
            ["Ice Trap"]               = "Freeze Trap",
            ["Instant Crystal Trap"]   = "Freeze Trap",
            ["Paralysis Trap"]         = "Freeze Trap",
            ["Paralyze Trap"]          = "Freeze Trap",
            ["Random Status Trap"]     = "Freeze Trap",
            ["Sleep Trap"]             = "Freeze Trap",
            ["Sleeptoad"]              = "Freeze Trap",

            ["Input Sequence Trap"]    = "Input Sequence Trap",
            ["Breakout Trap"]          = "Input Sequence Trap",
            ["Light Up Path Trap"]     = "Input Sequence Trap",
            ["Monkey Mash Trap"]       = "Input Sequence Trap",

            ["Literature Trap"]        = "Literature Trap",
            ["Exposition Trap"]        = "Literature Trap",
            ["PowerPoint Trap"]        = "Literature Trap",
            ["Research Trap"]          = "Literature Trap",
            ["Spam Trap"]              = "Literature Trap",
            ["Syntax Jumpscare Trap"]  = "Literature Trap",
            ["Text Trap"]              = "Literature Trap",

            ["Math Quiz Trap"]         = "Math Quiz Trap",
            ["Market Crash Trap"]      = "Math Quiz Trap",

            ["Modified Bloons"]        = "Modified Bloons",
            ["Modified Bloons Trap"]   = "Modified Bloons",
            ["Army Trap"]              = "Modified Bloons",
            ["Buyon Trap"]             = "Modified Bloons",
            ["Conga Artillery"]        = "Modified Bloons",
            ["Enemy Ball Trap"]        = "Modified Bloons",
            ["Gooey Bag"]              = "Modified Bloons",
            ["Police Trap"]            = "Modified Bloons",

            ["Number Sequence Trap"]   = "Number Sequence Trap",
            ["Depletion Trap"]         = "Number Sequence Trap",
            ["Pinball Trap"]           = "Number Sequence Trap",

            ["Pokemon Trivia Trap"]    = "Pokemon Trivia Trap",
            ["Icon Trap"]              = "Pokemon Trivia Trap",
            ["Pokemon Count Trap"]     = "Pokemon Trivia Trap",

            ["Screen Flip Trap"]       = "Screen Flip Trap",
            ["Camera Rotate Trap"]     = "Screen Flip Trap",
            ["Confound Trap"]          = "Screen Flip Trap",
            ["Deisometric Trap"]       = "Screen Flip Trap",
            ["Flip Horizontal Trap"]   = "Screen Flip Trap",
            ["Flip Trap"]              = "Screen Flip Trap",
            ["Flip Vertical Trap"]     = "Screen Flip Trap",
            ["Inverted Mouse Trap"]    = "Screen Flip Trap",
            ["Mirror Trap"]            = "Screen Flip Trap",
            ["Reversal Trap"]          = "Screen Flip Trap",
            ["Reverse Controls Trap"]  = "Screen Flip Trap",
            ["Reverse Trap"]           = "Screen Flip Trap",
            ["Squash Trap"]            = "Screen Flip Trap",

            ["Shuffle Trap"]           = "Shuffle Trap",
            ["Chaos Trap"]             = "Shuffle Trap",
            ["Explosion Trap"]         = "Shuffle Trap",
            ["Extreme Chaos Mode"]     = "Shuffle Trap",
            ["Monkey Trap"]            = "Shuffle Trap",
            ["Teleport Trap"]          = "Shuffle Trap",
            ["Transmute Trap"]         = "Shuffle Trap",

            ["Speed Up Trap"]          = "Speed Up Trap",
            ["Fast Trap"]              = "Speed Up Trap",
            ["Impatience Trap"]        = "Speed Up Trap",
            ["Jump Trap"]              = "Speed Up Trap",
            ["Push Trap"]              = "Speed Up Trap",
            ["Time Warp Trap"]         = "Speed Up Trap",

            ["Swap Trap"]              = "Swap Trap",
            ["Fracture Trap"]          = "Swap Trap",
            ["Gadget Shuffle Trap"]    = "Swap Trap",
            ["My Turn! Trap"]          = "Swap Trap",
            ["Posession Trap"]         = "Swap Trap",
            ["Tool Swap Trap"]         = "Swap Trap",
            ["Whoops! Trap"]           = "Swap Trap",

            ["Trivia Trap"]            = "Trivia Trap",
            ["Tutorial Trap"]          = "Trivia Trap",
            ["UNO Challenge"]          = "Trivia Trap",

            ["Yap Trap"]               = "Yap Trap",
            ["Aaa Trap"]               = "Yap Trap",
            ["Cutscene Trap"]          = "Yap Trap",
            ["Dad Trap"]               = "Yap Trap",
            ["E. Gadd Ramblings"]      = "Yap Trap",
            ["Help Trap"]              = "Yap Trap",
            ["Hey! Trap"]              = "Yap Trap",
            ["Laughter Trap"]          = "Yap Trap",
            ["OmoTrap"]                = "Yap Trap",
            ["Phone Trap"]             = "Yap Trap",
            ["Tip Trap"]               = "Yap Trap",
            ["Well Done Trap"]         = "Yap Trap",

            ["Zoom Trap"]              = "Zoom Trap",
            ["Camera Trap"]            = "Zoom Trap",
            ["Controller Drift Trap"]  = "Zoom Trap",
            ["Fish Eye Trap"]          = "Zoom Trap",
            ["Frame Slime Trap"]       = "Zoom Trap",
            ["Hidden Trap"]            = "Zoom Trap",
            ["Spotlight Trap"]         = "Zoom Trap",
            ["Sticky Hands Trap"]      = "Zoom Trap",
            ["W I D E Trap"]           = "Zoom Trap",
            ["Zoom In Trap"]           = "Zoom Trap",
            ["Zoom Out Trap"]          = "Zoom Trap",
        };

        public static bool IsNativeTrap(string itemName) => NativeTraps.Contains(itemName);

        public static bool TryGetNativeTrap(string trapName, out string nativeTrap)
        {
            nativeTrap = null;
            return !string.IsNullOrEmpty(trapName) && _linkedTrapNames.TryGetValue(trapName, out nativeTrap);
        }
    }
}
