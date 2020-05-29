﻿using OpenBveApi.Runtime;
using OpenBveApi.Trains;
using SoundManager;

namespace OpenBve.SafetySystems
{
	internal class PilotLamp
	{
		/// <summary>Played once when all doors are closed</summary>
		internal CarSound OnSound;
		/// <summary>Played once when the first door opens</summary>
		internal CarSound OffSound;
		/// <summary>Holds the reference to the base car</summary>
		private readonly AbstractCar baseCar;
		/// <summary>The previous state of the train doors</summary>
		private DoorStates oldState;
		/// <summary>Whether the pilot lamp is currently lit</summary>
		internal bool Lit;

		internal PilotLamp(AbstractCar car)
		{
			baseCar = car;
			oldState = DoorStates.None;
			OnSound = new CarSound();
			OffSound = new CarSound();
		}

		internal void Update(DoorStates newState)
		{
			if (oldState != DoorStates.None & newState == DoorStates.None)
			{
				Lit = true;
				OnSound.Play(1.0, 1.0, false);
			}
			else if (oldState == DoorStates.None & newState != DoorStates.None)
			{
				Lit = false;
				OffSound.Play(1.0, 1.0, false);
			}
			oldState = newState;
		}
	}
}
