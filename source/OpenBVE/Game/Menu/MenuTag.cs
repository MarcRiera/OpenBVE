﻿namespace OpenBve
{
	/// <summary>The list of possible tags for a menu entry- These define the functionality of a given menu entry</summary>
	public enum MenuTag
	{
		/// <summary>Is unselectable</summary>
		Unselectable,
		/// <summary>Has no functionality/ is blank</summary>
		None,
		/// <summary>Is a caption for another menu item</summary>
		Caption,
		/// <summary>Moves up a menu level</summary>
		MenuBack,
		/// <summary>Enters the submenu containing the list of stations to which the player train may be jumped</summary>
		MenuJumpToStation,
		/// <summary>Enters the submenu for exiting to the main menu</summary>
		MenuExitToMainMenu,
		/// <summary>Enters the submenu for customising controls</summary>
		MenuControls,
		/// <summary>Enters the submenu for quitting the program</summary>
		MenuQuit,
		/// <summary>Returns to the simulation</summary>
		BackToSim,
		/// <summary>Jumps to the selected station</summary>
		JumpToStation,
		/// <summary>Exits to the main menu</summary>
		ExitToMainMenu,
		/// <summary>Quits the program</summary>
		Quit,
		/// <summary>Customises the selected control</summary>
		Control,
		/// <summary>Displays a list of routefiles</summary>
		RouteList,
		/// <summary>Selects a routefile to load</summary>
		RouteFile,
		/// <summary>A directory</summary>
		Directory,
		/// <summary>Enters the parent directory</summary>
		ParentDirectory,
	}
}
