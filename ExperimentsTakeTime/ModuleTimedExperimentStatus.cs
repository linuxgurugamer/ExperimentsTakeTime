using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using ClickThroughFix;
using ToolbarControl_NS;
using SpaceTuxUtility;
using static ExperimentsTakeTime.RegisterToolbar;

namespace ExperimentsTakeTime
{
    [KSPAddon(KSPAddon.Startup.FlightEditorAndKSC, true)]
    public class TimedExperimentStatus : MonoBehaviour
    {
        internal const string MODID = "ExperimentsTakeTime";
        internal const string MODNAME = "Experiments Take Time";
        private static ToolbarControl toolbarControl;
        bool hide = false;

        bool guiActive = false;

        static Dictionary<int, ActiveExperiment> activeExperiments = new Dictionary<int, ActiveExperiment>();
        static string activeGame = "";
        static public int GetNextExperimentCnt { get { ETT_Scenario.experimentCnt++; Log.Info("GetNextExperimentCnt: " + ETT_Scenario.experimentCnt); return ETT_Scenario.experimentCnt; } }

        static public void AddExperiment(int id, ModuleTimedScienceExperiment exp)
        {
            Log.Info("AddExperment: " + id +
                ", expTitle: " + exp.experiment.experimentTitle +
                ", startTime: " + exp.startTime.ToString("F0") +
                ", endTime: " + exp.endTime.ToString("F0") +
                ", calculatedTimeToRunSecs: " + exp.calculatedTimeToRunSecs
                );
            if (!activeExperiments.ContainsKey(id))
                activeExperiments.Add(id, new ActiveExperiment(id, exp.vessel, exp.experiment.experimentTitle, exp.startTime, exp.endTime, exp.calculatedTimeToRunSecs));
        }

        static public void RemoveExperiment(int id)
        {
            Log.Info("RemoveExperiment: " + id);
            if (activeExperiments.ContainsKey(id))
                activeExperiments.Remove(id);
            else
                Log.Info("Missing experiment id: " + id);
        }

        public void Start()
        {
            Log.Info("Start");

            if (toolbarControl == null)
            {
                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(GUIToggle, GUIToggle,
                     ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.MAPVIEW,
                     MODID,
                     "ETT_Btn",
                     "ExperimentsTakeTime/PluginData/btn38",
                     "ExperimentsTakeTime/PluginData/btn24",
                    MODNAME);
            }

            statusWindowPos = new Rect((Screen.width - WINDOW_WIDTH) / 2, (Screen.height - WINDOW_HEIGHT) / 2, WINDOW_WIDTH, WINDOW_HEIGHT);

            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onPartDie.Add(onPartDie);
            GameEvents.onVesselRecovered.Add(onVesselRecovered);
            GameEvents.onVesselTerminated.Add(onVesselTerminated);
            GameEvents.onVesselRecoveryProcessing.Add(onVesselRecoveryProcessing);
            GameEvents.onLevelWasLoadedGUIReady.Add(onLevelWasLoadedGUIReady);

            DontDestroyOnLoad(this);
        }

        void onLevelWasLoadedGUIReady(GameScenes gs)
        {
            if (gs == GameScenes.SPACECENTER)
            {
                if (HighLogic.CurrentGame.Title != activeGame)
                {
                    activeGame = HighLogic.CurrentGame.Title;
                    activeExperiments.Clear();

                    ScanAllVesselsForActiveExperiments();
                }
            }
        }
        void onVesselRecovered(ProtoVessel v, bool b)
        {
            Log.Info("onVesselRecovered, vesselName: " + v.vesselName);
            onVesselTerminated(v);
        }

        void onVesselRecoveryProcessing(ProtoVessel v, MissionRecoveryDialog mrd, float f)
        {
            Log.Info("onVesselRecoveryProcessing, vesselName: " + v.vesselName);
            onVesselTerminated(v);
        }

        void onVesselTerminated(ProtoVessel v)
        {
            Log.Info("onVesselTerminated, vesselName: " + v.vesselName);
            StartCoroutine(DoVesselTermination(v));
        }

        IEnumerator DoVesselTermination(ProtoVessel v)
        {
            // Need to look at all the parts to see if any are active experiments
            for (int i = 0; i < v.protoPartSnapshots.Count; i++)
            {
                var m = v.protoPartSnapshots[i].modules;

                for (int modIdx = 0; modIdx < m.Count; modIdx++)
                {

                    if (m[modIdx].moduleName == "ModuleTimedScienceExperiment")
                    {
                        int experimentCnt = m[modIdx].moduleValues.SafeLoad("experimentCnt", 0);
                        if (experimentCnt > 0)
                            RemoveExperiment(experimentCnt);
                    }
                }
            }
            yield return null;
        }

        void onPartDie(Part p)
        {
            Log.Info("onPartDie, part: " + p.partInfo.title + ", vessel: " + p.vessel.vesselName);
            StartCoroutine(DoOnPartDie(p));
        }

        IEnumerator DoOnPartDie(Part p)
        {
            // need to check to see if this part is an active experiment

            var m = p.FindModulesImplementing<ModuleTimedScienceExperiment>();
            for (int i = 0; i < m.Count; i++)
            {
                if (m[i].experimentCnt > 0)
                    RemoveExperiment(m[i].experimentCnt);
            }
            yield return null;
        }

        void ScanAllVesselsForActiveExperiments()
        {
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                for (int i = 0; i < v.protoVessel.protoPartSnapshots.Count; i++)
                {
                    var m = v.protoVessel.protoPartSnapshots[i].modules;

                    for (int modIdx = 0; modIdx < m.Count; modIdx++)
                    {

                        if (m[modIdx].moduleName == "ModuleTimedScienceExperiment")
                        {
                            int experimentCnt = m[modIdx].moduleValues.SafeLoad("experimentCnt", 0);
                            if (experimentCnt > 0)
                            {
                                string expTitle = m[modIdx].moduleValues.SafeLoad("expTitle", "Unknown"); ; // exp.experiment.experimentTitle
                                double startTime = m[modIdx].moduleValues.SafeLoad("startTime", 0);
                                double endTime = m[modIdx].moduleValues.SafeLoad("endTime", 0);
                                float calculatedTimeToRunSecs = m[modIdx].moduleValues.SafeLoad("calculatedTimeToRunSecs", 0);
                                activeExperiments.Add(experimentCnt, new ActiveExperiment(experimentCnt, v, expTitle, startTime, endTime, calculatedTimeToRunSecs));
                            }
                        }
                    }
                }

            }
        }

        private void GUIToggle() { guiActive = !guiActive; }
        void OnHideUI() { hide = true; }
        void OnShowUI() { hide = false; }

        public const float WINDOW_WIDTH = 500;
        public const float WINDOW_HEIGHT = 100;

        Rect statusWindowPos;
        private void OnGUI()
        {
            if (ETT_Scenario.kspSkin)
                GUI.skin = HighLogic.Skin;
            if (guiActive && !hide && !RegisterToolbar.GamePaused &&
                (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION))
            {
                statusWindowPos = ClickThruBlocker.GUILayoutWindow(99988, statusWindowPos, TimedExperimentWindow, "Experiments Take Time"); //, LifeSupportDisplay.layoutOptions);
            }
        }

        Vector2 infoScrollPos;
        void TimedExperimentWindow(int id)
        {
            if (GUI.Button(new Rect(8, 2, 24, 20), "S"))
            {
                ETT_Scenario.kspSkin = !ETT_Scenario.kspSkin;
                if (ETT_Scenario.kspSkin)
                {
                    labelStyle = new GUIStyle(HighLogic.Skin.label);
                    labelStyle.wordWrap = false;
                }
                else
                {
                    labelStyle = new GUIStyle(GUI.skin.label);
                    labelStyle.wordWrap = false;
                }
            }
            if (GUI.Button(new Rect(statusWindowPos.width - 24, 3f, 23, 20f), new GUIContent("X")))
                GUIToggle();

            using (new GUILayout.HorizontalScope())
            {
                if (activeExperiments.Count == 0)
                {
                    GUILayout.Label("No Active Experiments");
                }
                else
                {
                    infoScrollPos = GUILayout.BeginScrollView(infoScrollPos, GUILayout.MaxHeight(345), GUILayout.Width(WINDOW_WIDTH - 20));
                    foreach (ActiveExperiment e in activeExperiments.Values)
                    {
                        var vesselName = e.vessel.vesselName;
                        var expTitle = e.expTitle;
                        var elapsedTime = Mathf.Min((float)(Planetarium.GetUniversalTime() - e.startTime), e.calculatedTimeToRunSecs);
                        var timeToGo = Mathf.Max(0, (float)(e.endTime - Planetarium.GetUniversalTime()));
                        expTitle += ", Elapsed time: " + ModuleTimedScienceExperiment.FormatTime(elapsedTime) + ", Time to completion: " +
                            ModuleTimedScienceExperiment.FormatTime(timeToGo);
                        using (new GUILayout.HorizontalScope())
                            GUILayout.Label(vesselName + ": " + expTitle, RegisterToolbar.labelStyle);
                    }
                    GUILayout.EndScrollView();
                }
            }
            GUI.DragWindow();
        }
    }


    internal class ActiveExperiment
    {
        internal Vessel vessel;
        internal int experimentID;
        internal string expTitle;
        internal double startTime;
        internal double endTime;
        internal float calculatedTimeToRunSecs;

        internal ActiveExperiment(int id, Vessel v, string t, double st, double et, float cttrs)
        {
            vessel = v;
            experimentID = id;
            expTitle = t;
            startTime = st;
            endTime = et;
            calculatedTimeToRunSecs = cttrs;
        }
    }
}
