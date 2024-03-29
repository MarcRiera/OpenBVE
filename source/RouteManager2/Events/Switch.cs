﻿using System;
using OpenBveApi.Routes;

namespace RouteManager2.Events
{
	/// <summary>Event controlling a switch</summary>
	public class SwitchEvent : GeneralEvent
	{
		/// <summary>The GUID of the switch</summary>
		public readonly Guid Index;
		/// <summary>The track position of the switch</summary>
		public readonly double TrackPosition;
		/// <summary>The direction the switch faces</summary>
		public readonly int SwitchDirection;

		private readonly CurrentRoute currentRoute;

		public SwitchEvent(Guid idx, int direction, double trackPosition, CurrentRoute route)
		{
			Index = idx;
			SwitchDirection = direction;
			TrackPosition = trackPosition;
			currentRoute = route;
		}

		public override void Trigger(int direction, TrackFollower trackFollower)
		{
			switch (trackFollower.TriggerType)
			{
				case EventTriggerType.FrontBogieAxle:
				case EventTriggerType.RearBogieAxle:
				case EventTriggerType.RearCarRearAxle:
				case EventTriggerType.OtherCarRearAxle:
				case EventTriggerType.FrontCarFrontAxle:
				case EventTriggerType.OtherCarFrontAxle:
					if (direction == (int)currentRoute.Switches[Index].Direction)
					{
						// Moving from the toe direction means we are always correct and go to the set track
						trackFollower.TrackIndex = currentRoute.Switches[Index].CurrentlySetTrack;
					}
					else
					{
						if (trackFollower.TrackIndex != currentRoute.Switches[Index].CurrentlySetTrack && trackFollower.TrackIndex != currentRoute.Switches[Index].ToeRail)
						{
							// Our track follower is not on the set track or the toe rail, so the switch must be against us
							// If derailments are off, this will just return in the car derail function
							trackFollower.Car.Derail();
							currentRoute.Switches[Index].RunThrough = true;
						}
						trackFollower.TrackIndex = currentRoute.Switches[Index].ToeRail;
					}
					trackFollower.UpdateWorldCoordinates(false);
					break;
				case EventTriggerType.TrainFront:
					if (SwitchDirection == 1)
					{
						trackFollower.Train.Switch = Index;
					}
					break;
				case EventTriggerType.TrainRear:
					if (SwitchDirection == -1)
					{
						trackFollower.Train.Switch = Index;
					}
					break;
			}
		}
	}
}

