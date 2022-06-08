using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using ClickThroughFix;
using ToolbarControl_NS;
using static ExperimentsTakeTime.RegisterToolbar;


namespace ExperimentsTakeTime
{

    public class ModuleTimedScienceExperiment : ModuleScienceExperiment
    {
        const float WAIT_TIME = 1f;

        [KSPField(isPersistant = true)]
        public bool physicalSample;

        [KSPField]
        public string timeToRun = "60s";
        [KSPField(isPersistant = true)]
        public float timeToRunSecs = 60f;

        [KSPField]
        public string maxTimeToRun = "";

        [KSPField(isPersistant = true)]
        public float maxTimeToRunSecs = 0f;

        [KSPField(isPersistant = true)]
        public float calculatedTimeToRunSecs = 0f;

        [KSPField]
        public string delayBeforeAbort = "15s";

        [KSPField]
        public float delayBeforeAbortSecs = 15;


        [KSPField(isPersistant = true)]
        public float updateInterval = WAIT_TIME;

        [KSPField(isPersistant = true)]
        public double startTime = 0f;

        [KSPField(isPersistant = true)]
        public double endTime = 0f;

        [KSPField(isPersistant = true)]
        public string displayString = "";

        [KSPField(isPersistant = true)]
        public int experimentCnt = 0;

        [KSPField(isPersistant = true)]
        public string expTitle = "";

        void ResetTimedExperiment()
        {
            Log.Info("ResetTimedExperiment");
            startTime = endTime = 0f;
            displayString = Events["StartExperiment"].guiName = experimentActionName;

            TimedExperimentStatus.RemoveExperiment(experimentCnt);
            experimentCnt = 0;
        }

        [KSPAction("Deploy")]
        new public void DeployAction(KSPActionParam actParams)
        {
            StartExperiment();
        }

        [KSPAction("#autoLOC_6001436")]
        new public void ResetAction(KSPActionParam actParams)
        {
            Log.Info("ResetAction");
            ResetTimedExperiment();
        }

        [KSPEvent(active = true, guiActive = true, guiName = "#autoLOC_6001436")]
        new public void ResetExperiment()
        {
            Log.Info("ResetExperiment"); 
            ResetTimedExperiment();
        }


        [KSPEvent(active = true, guiActive = true, guiName = "Start experiment")]
        public void StartExperiment()
        {
            if (startTime == 0)
            {
                if (ValidSituation())
                {
                    timeOutsideValidSituation = 0;
                    startTime = Planetarium.GetUniversalTime();
                    experimentCnt = TimedExperimentStatus.GetNextExperimentCnt;


                    if (maxTimeToRunSecs > 0)
                    {
                        endTime = startTime + Random.Range(timeToRunSecs, maxTimeToRunSecs);
                        calculatedTimeToRunSecs = (float)(endTime - startTime);
                    }
                    else
                    {
                        endTime = startTime + timeToRunSecs;
                        calculatedTimeToRunSecs = (float)timeToRunSecs;
                    }

                       TimedExperimentStatus.AddExperiment(experimentCnt, this);

                 StartCoroutine(WatchExperiment());
                    Events["StartExperiment"].guiName = "Starting";
                    ScreenMessages.PostScreenMessage("Starting " + experiment.experimentTitle, 10);
                    StartCoroutine(SetFXModules(1f));
                }
                else
                {
                    // Since not a valid situation, let the stock code handle this
                    base.DeployExperiment();
                }
            }
        }

        private IEnumerator SetFXModules(float tgt)
        {
            Log.Info("SetFXModules");
            if (fxModules != null)
            {
                bool allFXDone = false;
                while (!allFXDone)
                {
                    for (int i = 0; i < fxModules.Count; i++)
                    {
                        if (Mathf.Abs(fxModules[i].GetScalar - tgt) < 0.01f)
                            allFXDone = true;
                        else
                            fxModules[i].SetScalar(tgt);
                    }
                    if (allFXDone)
                        continue;

                    yield return null;
                }
            }
        }


        [KSPEvent(active = true, guiActive = true, guiName = "#autoLOC_6001436")]
        public void ResetTExperiment()
        {
            Log.Info("ResetExperiment");
            base.ResetExperiment();
            ResetTimedExperiment();
        }

        [KSPEvent(active = true, guiActive = true, guiName = "#autoLOC_6001436")]
        public void ResetTExperimentExternal()
        {
            Log.Info("ResetExperimentExternal");
            base.ResetExperimentExternal();
            ResetTimedExperiment();
        }


        bool ValidSituation()
        {
            ExperimentSituations situation = ScienceUtil.GetExperimentSituation(base.vessel);
            return experiment.IsAvailableWhile(situation, base.vessel.mainBody);
        }

        int timeOutsideValidSituation = 0;
        IEnumerator WatchExperiment()
        {
            Log.Info("WatchExperiment");
            OnExperimentReset += ResetTimedExperiment;

            while (true)
            {
                yield return new WaitForSeconds(updateInterval);

                ExperimentSituations situation = ScienceUtil.GetExperimentSituation(base.vessel);
                if (!ValidSituation())
                {
                    timeOutsideValidSituation++;
                    if (timeOutsideValidSituation > delayBeforeAbortSecs)
                    {
                        List<string> msgList = new List<string>();
#if true
                        if (((ExperimentSituations)experiment.situationMask & ExperimentSituations.SrfLanded) > 0) msgList.Add("Landed");
                        if (((ExperimentSituations)experiment.situationMask & ExperimentSituations.SrfSplashed) > 0) msgList.Add("Splashed down");
                        if (((ExperimentSituations)experiment.situationMask & ExperimentSituations.FlyingLow) > 0) msgList.Add("Flying low");
                        if (((ExperimentSituations)experiment.situationMask & ExperimentSituations.FlyingHigh) > 0) msgList.Add("Flying high");
                        if (((ExperimentSituations)experiment.situationMask & ExperimentSituations.InSpaceLow) > 0) msgList.Add("In space low");
                        if (((ExperimentSituations)experiment.situationMask & ExperimentSituations.InSpaceHigh) > 0) msgList.Add("In space high");
#endif
                        string result = string.Join(", ", msgList);

                        ScreenMessages.PostScreenMessage(experiment.experimentTitle + " canceled, exited defined situations: ", 10f);

                        ResetTimedExperiment();
                        break;
                    }
                }
                else
                    timeOutsideValidSituation = 0;
                if (endTime > 0)
                {
                    if (Planetarium.GetUniversalTime() > endTime)
                    {
                        base.DeployExperiment();
                        Events["StartExperiment"].guiName = "Experiment completed";
                        TimedExperimentStatus.RemoveExperiment(experimentCnt);
                        experimentCnt = 0;
                        break;
                    }
                    else
                    {
                        double timeLeftToRun = endTime - Planetarium.GetUniversalTime();
                        Events["StartExperiment"].guiName = "Running: " + FormatTime(timeLeftToRun);
                    }
                }
            }
            yield return null;
        }

        internal static string FormatTime(double timeToFormat, bool doDays = true)
        {
            string timeLeftstr = "";
            float dd = 0, hh = 0, mm = 0;
            if (doDays && timeToFormat >= Planetarium.fetch.Home.solarDayLength)
            {
                dd = Mathf.Floor((float)timeToFormat * inverseSolarDayLength); //   / (float)Planetarium.fetch.Home.solarDayLength);
                timeToFormat -= dd * Planetarium.fetch.Home.solarDayLength;
                timeLeftstr = dd.ToString() + "d ";
            }
            if (timeToFormat >= 3600)
            {
                hh = Mathf.Floor((float)timeToFormat * 0.000277777777777777777f); //   / 3600f);
                timeToFormat -= hh * 3600;
                timeLeftstr += hh.ToString("") + ":";
            }
            if (timeToFormat >= 60)
            {
                mm = Mathf.Floor((float)timeToFormat * 0.0166666666666667f); //  / 60f);
                timeToFormat -= mm * 60;
                if (timeLeftstr.Length > 0)
                    timeLeftstr += mm.ToString("00") + ":";
                else
                    timeLeftstr += mm.ToString("") + ":";
            }
            if (timeLeftstr.Length > 0)
                timeLeftstr += Mathf.Floor((float)timeToFormat).ToString("00");
            else
                timeLeftstr += Mathf.Floor((float)timeToFormat).ToString("");
            return timeLeftstr;
        }

        float GetNum(string str)
        {
            float f = 0f;

            if (str.Length > 0)
            {
                string lastChar = str.Substring(str.Length - 1);
                if ("smhd".Contains(lastChar))
                {
                    f = float.Parse(str.Substring(0, str.Length - 1));
                }
                else
                    f = float.Parse(str);
                switch (lastChar[0])
                {
                    case 'm': f *= 60; break;
                    case 'h': f *= 3600; break;
                    case 'd':
                        f *= (float)Planetarium.fetch.Home.solarDayLength;
                        break;
                }

            }
            return f;
        }

        List<IScalarModule> fxModules;
        internal static float inverseSolarDayLength;
        public void Start()
        {
            Log.Info("Start");
            timeToRunSecs = GetNum(timeToRun);
            maxTimeToRunSecs = GetNum(maxTimeToRun);
            delayBeforeAbortSecs = GetNum(delayBeforeAbort);

            expTitle = experiment.experimentTitle;

            Log.Info("Experiment: " + experiment.experimentTitle + ", timeToRunSecs: " + timeToRunSecs);
            Log.Info("Experiment: " + experiment.experimentTitle + ", maxTimeToRunSecs: " + maxTimeToRunSecs);

            if (HighLogic.CurrentGame == null || !HighLogic.LoadedSceneIsFlight) return;

            inverseSolarDayLength = 1f / (float)Planetarium.fetch.Home.solarDayLength;
            Log.Info("Start, part: " + part.partInfo.title);
            Events["DeployExperiment"].guiActive =
            Events["DeployExperimentExternal"].guiActive = false;
            Events["DeployExperiment"].active =
            Events["DeployExperimentExternal"].active = false;

            if (physicalSample)
                xmitDataScalar = 0;

            if (fxModuleIndices != null)
            {
                fxModules = new List<IScalarModule>();
                for (int i = 0; i < fxModuleIndices.Length; i++)
                {
                    int num2 = fxModuleIndices[i];
                    if (base.part.Modules[num2] is IScalarModule)
                    {
                        IScalarModule scalarModule = (IScalarModule)base.part.Modules[num2];
                        if (hideFxModuleUI)
                        {
                            scalarModule.SetUIWrite(state: false);
                            scalarModule.SetUIRead(state: false);
                        }
                        fxModules.Add(scalarModule);
                    }
                    else
                    {
                        Debug.LogError("[ModuleScienceExperiment]: Part Module " + num2 + " doesn't implement IScalarModule", base.gameObject);
                    }
                }
            }

            if (startTime == 0)
                ResetTimedExperiment();
            else
            {
                Events["StartExperiment"].guiName = displayString;
                //experimentCnt = TimedExperimentStatus.GetNextExperimentCnt;
                TimedExperimentStatus.AddExperiment(experimentCnt, this);
            }
            if (startTime > 0 && Planetarium.GetUniversalTime() < endTime)
                StartCoroutine(WatchExperiment());
        }

        public override string GetInfo()
        {
            string str = base.GetInfo();
            if (maxTimeToRunSecs == 0)
            {
                str += "\nTime to run: " + FormatTime(timeToRunSecs, false);
            }
            else
            {
                str += "\nMin time to run: " + FormatTime(timeToRunSecs, false) +
                        "\nMax time to run: " + FormatTime(maxTimeToRunSecs, false);
            }
            return str;
        }
    }
}
