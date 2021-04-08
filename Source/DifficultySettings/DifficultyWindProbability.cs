using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanetsideExplorationTechnologies.DifficultySettings
{
    public class DifficultyWindProbability : GameParameters.CustomParameterNode
    {
        private const string DISPLAYNAME = "#LOC_B10_MMSEV_WindProbabilitySettings_title"; 
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
            get { return 4; }
        }

        public override GameParameters.GameMode GameMode
        {
            get { return GameParameters.GameMode.ANY; }
        }

        public override bool HasPresets
        {
            get { return false; }
        }

        public static DifficultyWindProbability Instance
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<DifficultyWindProbability>(); }
        }

        [GameParameters.CustomFloatParameterUI("#LOC_B10_MMSEV_Setting_ProbabilityHighWind_title", toolTip = "#LOC_B10_MMSEV_Setting_ProbabilityHighWind_tooltip", asPercentage = true, minValue = 0, maxValue = 1, displayFormat = "F2")]
        public float probabilityHighWinds = 0.10f;

        [GameParameters.CustomFloatParameterUI("#LOC_B10_MMSEV_Setting_ProbabilityMediumWind_title", toolTip = "#LOC_B10_MMSEV_Setting_ProbabilityMediumWind_tooltip", asPercentage = true, minValue = 0, maxValue = 1, displayFormat = "F2")]
        public float probabilityMidWinds = 0.55f;

        [GameParameters.CustomFloatParameterUI("#LOC_B10_MMSEV_Setting_ProbabilityLowWind_title", toolTip = "#LOC_B10_MMSEV_Setting_ProbabilityLowWind_tooltip", asPercentage = true, minValue = 0, maxValue = 1, displayFormat = "F2")]
        public float probabilityLowWinds = 0.25f;

        [GameParameters.CustomFloatParameterUI("#LOC_B10_MMSEV_Setting_ProbabilityNoWind_title", toolTip = "#LOC_B10_MMSEV_Setting_ProbabilityNoWind_tooltip", asPercentage = true, minValue = 0, maxValue = 1, displayFormat = "F2")]
        public float probabilityNoWinds = 0.15f;


        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    probabilityHighWinds = 0.20f;
                    probabilityMidWinds = 0.50f;
                    probabilityLowWinds = 0.35f;
                    probabilityNoWinds = 0.01f;
                    break;
                case GameParameters.Preset.Normal:
                    probabilityHighWinds = 0.14f;
                    probabilityMidWinds = 0.50f;
                    probabilityLowWinds = 0.35f;
                    probabilityNoWinds = 0.01f;
                    break;
                case GameParameters.Preset.Moderate:
                    probabilityHighWinds = 0.14f;
                    probabilityMidWinds = 0.50f;
                    probabilityLowWinds = 0.35f;
                    probabilityNoWinds = 0.01f;
                    break;
                case GameParameters.Preset.Hard:
                    probabilityHighWinds = 0.14f;
                    probabilityMidWinds = 0.50f;
                    probabilityLowWinds = 0.35f;
                    probabilityNoWinds = 0.05f;
                    break;
            }
        }
    }
}
