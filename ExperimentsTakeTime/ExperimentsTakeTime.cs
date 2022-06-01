using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using KSP_Log;


namespace ExperimentsTakeTime
{
    public class ModuleTimedScienceExperiment : ModuleScienceExperiment
    {
        internal static Log Log = null;

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

        void ResetTimedExperiment()
        {
            Log.Info("ResetTimedExperiment");
            startTime = endTime = 0f;
            displayString = Events["StartExperiment"].guiName = experimentActionName;
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
                    if (maxTimeToRunSecs > 0)
                        endTime = startTime + Random.Range(timeToRunSecs, maxTimeToRunSecs);
                    else
                        endTime = startTime + timeToRunSecs;

                    StartCoroutine(WatchExperiment());
                    Events["StartExperiment"].guiName = "Starting";

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

        new public void ResetAction(KSPActionParam actParams)
        {
            Log.Info("ResetAction");
            base.ResetAction(actParams);
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
                if (Planetarium.GetUniversalTime() > endTime)
                {
                    base.DeployExperiment();
                    Events["StartExperiment"].guiName = "Experiment completed";
                    break;
                }
                else
                {
                    double timeLeftToRun = endTime - Planetarium.GetUniversalTime();
                    float dd = 0, hh = 0, mm = 0;
                    string timeLeftstr = "";
                    if (timeLeftToRun >= Planetarium.fetch.Home.solarDayLength)
                    {
                        dd = Mathf.Floor((float)timeLeftToRun * inverseSolarDayLength); //   / (float)Planetarium.fetch.Home.solarDayLength);
                        timeLeftToRun -= dd * Planetarium.fetch.Home.solarDayLength;
                        timeLeftstr = dd.ToString() + "d ";
                    }
                    if (timeLeftToRun >= 3600)
                    {
                        hh = Mathf.Floor((float)timeLeftToRun * 0.000277777777777777777f); //   / 3600f);
                        timeLeftToRun -= hh * 3600;
                        timeLeftstr += hh.ToString("") + ":";
                    }
                    if (timeLeftToRun >= 60)
                    {
                        mm = Mathf.Floor((float)timeLeftToRun * 0.0166666666666667f); //  / 60f);
                        timeLeftToRun -= mm * 60;
                        if (timeLeftstr.Length > 0)
                            timeLeftstr += mm.ToString("00") + ":";
                        else
                            timeLeftstr += mm.ToString("") + ":";
                    }
                    if (timeLeftstr.Length > 0)
                        timeLeftstr += Mathf.Floor((float)timeLeftToRun).ToString("00");
                    else
                        timeLeftstr += Mathf.Floor((float)timeLeftToRun).ToString("");
                    Events["StartExperiment"].guiName = "Running: " + timeLeftstr;
                }
            }
            yield return null;
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
        float inverseSolarDayLength;
        public void Start()
        {
            if (Log == null)
#if DEBUG
                Log = new Log("ExperimentsTakeTime", Log.LEVEL.INFO);
#else
                Log = new Log("ExperimentsTakeTime", Log.LEVEL.ERROR);
#endif
            Log.Info("Start");
            timeToRunSecs = GetNum(timeToRun);
            maxTimeToRunSecs = GetNum(maxTimeToRun);
            delayBeforeAbortSecs = GetNum(delayBeforeAbort);
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
                Events["StartExperiment"].guiName = displayString;
            if (startTime > 0 && Planetarium.GetUniversalTime() < endTime)
                StartCoroutine(WatchExperiment());
        }
    }
}
