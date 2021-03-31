using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanetsideExplorationTechnologies.DifficultySettings
{
    public class DifficultyGeneralWind : GameParameters.CustomParameterNode
    {
        private const string DISPLAYNAME = "General Wind Settings";
        private const string SECTIONNAME = "Planetside Exploration Technologies";

        public override string Title
        {
            get { return DISPLAYNAME; }
        }

        public override string DisplaySection
        {
            get { return SECTIONNAME; }
        }

        public override string Section
        {
            get { return SECTIONNAME; }
        }

        public override int SectionOrder
        {
            get { return 1; }
        }

        public override GameParameters.GameMode GameMode
        {
            get { return GameParameters.GameMode.ANY; }
        }

        public override bool HasPresets
        {
            get { return false; }
        }

        public static DifficultyGeneralWind Instance
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<DifficultyGeneralWind>(); }
        }

        /*[GameParameters.CustomParameterUI("Allow Third-Party Wind Models", toolTip = "Set to allow third party mod wind models (i.e Kerbal Wind)")]
        public bool allowThirdPartyWindModels = true;*/

        [GameParameters.CustomFloatParameterUI("Wind Interval (Hours)", toolTip = "Sets how often wind gets recalculated", minValue = 0.1f, maxValue = 12, displayFormat = "F2")]
        public float windInterval = 2.5f;
    }
}
