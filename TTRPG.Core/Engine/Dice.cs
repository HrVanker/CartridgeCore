using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace TTRPG.Core.Engine
{
    public static class Dice
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Rolls dice based on a string notation (e.g., "1d6", "2d6+4", "1d20-1").
        /// </summary>
        public static int Roll(string notation)
        {
            if (string.IsNullOrWhiteSpace(notation)) return 0;

            // Normalize
            notation = notation.ToLower().Replace(" ", "");

            // Regex breakdown:
            // Group 1 (Optional): Number of dice (default 1)
            // "d" separator
            // Group 2: Number of faces
            // Group 3 (Optional): Modifier (+5, -2)
            var match = Regex.Match(notation, @"^(\d*)d(\d+)([\+\-]\d+)?$");

            if (!match.Success)
            {
                // Fallback: Try parsing as a raw integer (e.g. fixed damage "5")
                if (int.TryParse(notation, out int fixedValue)) return fixedValue;
                throw new ArgumentException($"Invalid dice notation: {notation}");
            }

            // Parse Count (default to 1 if empty, e.g. "d6")
            int count = string.IsNullOrEmpty(match.Groups[1].Value) ? 1 : int.Parse(match.Groups[1].Value);

            // Parse Faces
            int faces = int.Parse(match.Groups[2].Value);

            // Parse Modifier
            int modifier = 0;
            if (match.Groups[3].Success)
            {
                modifier = int.Parse(match.Groups[3].Value);
            }

            int total = 0;
            for (int i = 0; i < count; i++)
            {
                total += _random.Next(1, faces + 1);
            }

            return total + modifier;
        }
    }
}