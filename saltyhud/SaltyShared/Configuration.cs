using System;
using System.Collections.Generic;
using System.Text;

namespace SaltyShared
{
    public class Configuration
    {
        public bool Enabled { get; set; } = true;
        public float RangeModifier { get; set; } = 1f;
        public string RangeText { get; set; } = "{voicerange} meters";
        public string RadioText { get; set; } = "Channel {channel}";
        public int[] Position { get; set; } = { 0, 0 };
        public bool HideWhilePauseMenuOpen { get; set; } = true;
    }
}
