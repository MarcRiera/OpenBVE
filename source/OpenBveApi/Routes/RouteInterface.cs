﻿namespace OpenBveApi.Routes
{
	/// <summary>Represents the interface for loading objects. Plugins must implement this interface if they wish to expose objects.</summary>
	public abstract class RouteInterface
	{
		/// <summary>Called when the plugin is loaded.</summary>
		/// <param name="host">The host that loaded the plugin.</param>
		/// <param name="fileSystem">The program filesystem object</param>
		public virtual void Load(Hosts.HostInterface host, FileSystem.FileSystem fileSystem)
		{
		}
		
		/// <summary>Called when the plugin is unloaded.</summary>
		public virtual void Unload()
		{
		}

		/// <summary>Sets various hacks to workaround buggy objects</summary>
		public virtual void SetCompatibilityHacks(bool BveTsHacks, bool CylinderHack)
		{
		}

		/// <summary>Checks whether the plugin can load the specified object.</summary>
		/// <param name="path">The path to the file or folder that contains the object.</param>
		/// <returns>Whether the plugin can load the specified object.</returns>
		public abstract bool CanLoadRoute(string path);

		/// <summary>Loads the specified object.</summary>
		/// <param name="path">The path to the file or folder that contains the object.</param>
		/// <param name="Route">Receives the object.</param>
		/// <param name="Encoding">The encoding for the object</param>
		/// <returns>Whether loading the texture was successful.</returns>
		public abstract bool LoadRoute(string path, System.Text.Encoding Encoding, out object Route);
	}
}
