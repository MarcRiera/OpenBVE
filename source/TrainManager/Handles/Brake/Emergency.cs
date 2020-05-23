﻿using SoundManager;

namespace TrainManager.Handles
{
	/// <summary>Represents an emergency brake handle</summary>
	public class EmergencyHandle
	{
		public bool Safety;
		public bool Actual;
		public bool Driver;
		public double ApplicationTime;
		/// <summary>The sound played when the emergency brake is applied</summary>
		public CarSound ApplicationSound;
		/// <summary>The sound played when the emergency brake is released</summary>
		/*
		 * NOTE:	This sound is deliberately not initialised by default.
		 *			If uninitialised, the sim will fall back to the previous behaviour
		 *			of using the Brake release sound when EB is released.
		 */
		public CarSound ReleaseSound;
		/// <summary>The behaviour of the other handles when the EB handle is activated</summary>
		public EbHandleBehaviour OtherHandlesBehaviour = EbHandleBehaviour.NoAction;

		public EmergencyHandle()
		{
			ApplicationSound = new CarSound();
		}

		public void Update(double CurrentTime)
		{
			if (Safety & !Actual)
			{
				if (CurrentTime < ApplicationTime) ApplicationTime = CurrentTime;
				if (ApplicationTime <= CurrentTime)
				{
					Actual = true;
					ApplicationTime = double.MaxValue;
				}
			}
			else if (!Safety)
			{
				Actual = false;
			}
		}
	}
}
