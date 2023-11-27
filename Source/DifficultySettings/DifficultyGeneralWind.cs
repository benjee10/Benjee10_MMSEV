using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanetsideExplorationTechnologies.DifficultySettings
{
    public class DifficultyGeneralWind : GameParameters.CustomParameterNode
    {
        private const string DISPLAYNAME = "#LOC_B10_MMSEV_GeneralWindSettings_title";
        private const string SECTIONNAME = "#LOC_B10_MMSEV_SettingsSection_title";

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

        [GameParameters.CustomFloatParameterUI("#LOC_B10_MMSEV_Setting_WindInterval_title", toolTip = "#LOC_B10_MMSEV_Setting_WindInterval_tooltip", minValue = 0.1f, maxValue = 12, displayFormat = "F2")]
        public float windInterval = 2.5f;
    }
}
