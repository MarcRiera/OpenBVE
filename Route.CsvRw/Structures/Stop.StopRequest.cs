﻿using OpenBveApi.Trains;

namespace Plugin
{
	internal struct StopRequest
	{
		internal int StationIndex;
		internal int MaxNumberOfCars;
		internal double TrackPosition;
		internal RequestStop Early;
		internal RequestStop OnTime;
		internal RequestStop Late;
		internal bool FullSpeed;
	}
}
