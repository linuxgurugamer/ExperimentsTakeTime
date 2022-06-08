
using UnityEngine;
using ToolbarControl_NS;
using KSP_Log;

namespace ExperimentsTakeTime
{

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        internal static Log Log = null;
        static bool _gamePaused = false;
        static double lastTime = 0;
        internal static GUIStyle labelStyle;

        public static bool GamePaused
        {
            get
            {
                if (_gamePaused)
                {
                    if (Planetarium.GetUniversalTime() != lastTime)
                        _gamePaused = false;
                }
                lastTime = Planetarium.GetUniversalTime();
                return _gamePaused;
            }
        }


        void Awake()
        {
            if (Log == null)
#if DEBUG
                Log = new Log("ExperimentsTakeTime", Log.LEVEL.INFO);
#else
                Log = new Log("ExperimentsTakeTime", Log.LEVEL.ERROR);
#endif
            GameEvents.onGamePause.Add(onGamePause);
            GameEvents.onGameUnpause.Add(onGameUnpause);

            DontDestroyOnLoad(this);
        }

        void onGamePause() { lastTime = Planetarium.GetUniversalTime(); _gamePaused = true; }
        void onGameUnpause() { if (HighLogic.CurrentGame != null) lastTime = Planetarium.GetUniversalTime(); _gamePaused = false; }

        void Start()
        {
            ToolbarControl.RegisterMod(TimedExperimentStatus.MODID, TimedExperimentStatus.MODNAME);
        }

        static public bool initted = false;

        void OnGUI()
        {
            if (!initted)
            {
                labelStyle = new GUIStyle(HighLogic.Skin.label); ;
                labelStyle.wordWrap = false;
                initted = true;
            }
        }
    }
}