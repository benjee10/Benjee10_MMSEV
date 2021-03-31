using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlanetsideExplorationTechnologies.Extensions
{
    public static class Utilities
    {
        public static string[] cardinals = { "N", "NE", "E", "SE", "S", "SW", "W", "NW", "N" };

        public static string GetCardinalDirection(float direction)
        {
            string cardinal = cardinals[(int)Math.Round(direction % 360 / 45)];
            return $"{direction:N0}° {cardinal}";
        }

        public static bool FloatRange(float value, float min, float max)
        {
            return (value >= min && value <= max);
        }  
    }
}
