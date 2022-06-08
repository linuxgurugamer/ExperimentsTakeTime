using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpaceTuxUtility;
using static ExperimentsTakeTime.RegisterToolbar;

namespace ExperimentsTakeTime
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER)]
    class ETT_Scenario : ScenarioModule
    {
        const string NODE = "ExperimentsTakeTime";

        internal static int experimentCnt = 0;
        internal static bool kspSkin = true;

        public override void OnLoad(ConfigNode parentNode)
        {
            Log.Info("ScenarioModule.OnLoad");
            base.OnLoad(parentNode);
            ConfigNode node = parentNode.GetNode(NODE);
            if (node != null)
            {
                experimentCnt = node.SafeLoad("experimentCnt", 0);
                kspSkin = node.SafeLoad("kspSkin", true);
            }
        }

        public override void OnSave(ConfigNode parentNode)
        {
            Log.Info("ScenarioModule.OnSave");
            ConfigNode node = new ConfigNode(NODE);

            node.AddValue("experimentCnt", experimentCnt);
            node.AddValue("kspSkin", kspSkin);

            parentNode.AddNode(node);
            base.OnSave(parentNode);

        }
    }
}

