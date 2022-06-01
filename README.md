Experiments Take Time

A simple mod to make experiments which us the ModuleScienceExperiment module take time.

The following are the default values for the various stock experiments:

Part name			experiment				timeToRun	maxTimeToRun	delayBeforeAbort

InfraredTelescope	infraredTelescope		1 day		30 days				15 secs
sensorAtmosphere	atmosphereAnalysis		1 hour							15 secs
GooExperiment		mysteryGoo				1 hour		1 day				15 secs
Magnetometer		magnetometer			1 hour		1 day				15 secs
science_module		mobileMaterialsLab		1 day		1 day				15 secs
sensorAccelerometer	seismicScan				1 day							15 secs
sensorBarometer		barometerScan			1 day							15 secs
sensorGravimeter	gravityScan				1 day		1 day				15 secs
sensorThermometer	temperatureScan			1 hour							15 secs

					crewReport				Instantaneous

All unlisted parts with experiments (other than crew reports)			10 minutes

While an experiment is running, the amount of time left before completion is show in the PAW

An experiment can only be started if the situation allows.  While in physics range, the situation is 
checked every second.  If a disallowed situation occurs for more than the delayBeforeAbort value, the research is terminated and will have to be restarted

When the research time is done the situation is checked to determine which situation the 
experiment/research will apply to.  So, if you have an experiment such as Mystery Goo, which can 
be run both in low and high orbit, and you start it in low orbit and then move to a high orbit, the 
high orbit situation will be the one to be completed.

Note that checks are not done on unloaded vessels

As of now, there are no displays to show any active experiments which are on unloaded vessels


To add custom configs, you need a patch which will look like this:

@PART[partName]:HAS[@MODULE[ModuleScienceExperiment]]
{
	@MODULE[ModuleScienceExperiment]
	{
		@name = ModuleTimedScienceExperiment
		timeToRun = 1d	// 1 day
		delayBeforeAbort = 15 // How many seconds the vessel can leave the defined situations
		maxTimeToRun = 30d // optional, 1 month of 6 hour days
	}
}

Replace the "partName" with the name of the part being changed.

Note that the timeToRun, delayBeforeAbort and maxTimeToRun can have an optional suffix of one the following:
	s	seconds (optional, if no suffix specified, will assume seconds) 
	m	minutes
	h	hours
	d	days

If maxTimeToRun is specified, then when the experiment is started, a random time will be calculated to be between the timeToRun and maxTimeToRun

delayBeforeAbort  specifies how many seconds the vessel can leave the allowable situations before the experiment is canceled

There is a commented out section for the Crew Reports, to enable them, just remove the double slashes at the beginning of the line

