// ╔═════════════════════════════════════════════════════════════╗
// ║ Loading.cs for the Route Viewer                             ║
// ╠═════════════════════════════════════════════════════════════╣
// ║ This file cannot be used in the openBVE main program.       ║
// ║ The file from the openBVE main program cannot be used here. ║
// ╚═════════════════════════════════════════════════════════════╝

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibRender2;
using LibRender2.Cameras;
using OpenBveApi.Interface;
using OpenBveApi.Math;
using OpenBveApi.Routes;
using OpenBveApi.Runtime;
using RouteManager2;

namespace OpenBve {
	internal static class Loading {

		internal static bool Cancel
		{
			get
			{
				return _cancel;
			}
			set
			{
				if (value)
				{
					//Send cancellation call to plugins
					for (int i = 0; i < Program.CurrentHost.Plugins.Length; i++)
					{
						if (Program.CurrentHost.Plugins[i].Route != null && Program.CurrentHost.Plugins[i].Route.IsLoading)
						{
							Program.CurrentHost.Plugins[i].Route.Cancel = true;
						}
					}
					_cancel = true;
				}
				else
				{
					_cancel = false;
				}
			}
		}

		private static bool _cancel;
		internal static bool Complete;
		private static string CurrentRouteFile;
		private static Encoding CurrentRouteEncoding;

		internal static bool JobAvailable;

		// load
		internal static void Load(string RouteFile, Encoding RouteEncoding) {
			// reset
			Game.Reset();
			Program.Renderer.Loading.InitLoading(Program.FileSystem.GetDataFolder("In-game"), typeof(NewRenderer).Assembly.GetName().Version.ToString());
			
			// members
			Cancel = false;
			Complete = false;
			CurrentRouteFile = RouteFile;
			CurrentRouteEncoding = RouteEncoding;
			// thread
			Loading.LoadAsynchronously(CurrentRouteFile, Encoding.UTF8);
			RouteViewer.LoadingScreenLoop();
		}

		// get railway folder
		private static string GetRailwayFolder(string RouteFile) {
			try {
				string Folder = System.IO.Path.GetDirectoryName(RouteFile);
				while (true) {
					string Subfolder = OpenBveApi.Path.CombineDirectory(Folder, "Railway");
					if (System.IO.Directory.Exists(Subfolder)) {
						return Subfolder;
					}
					System.IO.DirectoryInfo Info = System.IO.Directory.GetParent(Folder);
					if (Info == null) break;
					Folder = Info.FullName;
				}
			} catch { }
			//If the Route, Object and Sound folders exist, but are not in a railway folder...
			try
			{
				string Folder = System.IO.Path.GetDirectoryName(RouteFile);
				while (true)
				{
					string RouteFolder = OpenBveApi.Path.CombineDirectory(Folder, "Route");
					string ObjectFolder = OpenBveApi.Path.CombineDirectory(Folder, "Object"); 
					string SoundFolder = OpenBveApi.Path.CombineDirectory(Folder, "Sound");
					if (System.IO.Directory.Exists(RouteFolder) && System.IO.Directory.Exists(ObjectFolder) && System.IO.Directory.Exists(SoundFolder))
					{
						return Folder;
					}
					System.IO.DirectoryInfo Info = System.IO.Directory.GetParent(Folder);
					if (Info == null) break;
					Folder = Info.FullName;
				}
			}
			catch { }
			return Application.StartupPath;
		}

		// load threaded
		private static async Task LoadThreaded()
		{
			try
			{
				await Task.Run(() => LoadEverythingThreaded());
			}
			catch (Exception ex)
			{
				Interface.AddMessage(MessageType.Critical, false, "The route and train loader encountered the following critical error: " + ex.Message);
			}

			Complete = true;
		}

		internal static void LoadAsynchronously(string RouteFile, Encoding RouteEncoding)
		{
			// members
			Cancel = false;
			Complete = false;
			CurrentRouteFile = RouteFile;
			CurrentRouteEncoding = RouteEncoding;

			//Set the route and train folders in the info class
			// ReSharper disable once UnusedVariable
			Task loadThreaded = LoadThreaded();
		}

		private static void LoadEverythingThreaded() {
			Game.Reset();
			string RailwayFolder = GetRailwayFolder(CurrentRouteFile);
			string ObjectFolder = OpenBveApi.Path.CombineDirectory(RailwayFolder, "Object");
			string SoundFolder = OpenBveApi.Path.CombineDirectory(RailwayFolder, "Sound");
			Program.Renderer.Camera.CurrentMode = CameraViewMode.Track;
			// load route
			bool IsRW = string.Equals(System.IO.Path.GetExtension(CurrentRouteFile), ".rw", StringComparison.OrdinalIgnoreCase);
			bool loaded = false;
			for (int i = 0; i < Program.CurrentHost.Plugins.Length; i++)
			{
				if (Program.CurrentHost.Plugins[i].Route != null && Program.CurrentHost.Plugins[i].Route.CanLoadRoute(CurrentRouteFile))
				{
					object Route = (object)Program.CurrentRoute; //must cast to allow us to use the ref keyword.
					Program.CurrentHost.Plugins[i].Route.LoadRoute(CurrentRouteFile, CurrentRouteEncoding, null, ObjectFolder, SoundFolder, false, ref Route);
					Program.CurrentRoute = (CurrentRoute) Route;
					Program.Renderer.Lighting.OptionAmbientColor = Program.CurrentRoute.Atmosphere.AmbientLightColor;
					Program.Renderer.Lighting.OptionDiffuseColor = Program.CurrentRoute.Atmosphere.DiffuseLightColor;
					Program.Renderer.Lighting.OptionLightPosition = Program.CurrentRoute.Atmosphere.LightPosition;
					loaded = true;
					break;
				}
			}

			if (!loaded)
			{
				throw new Exception("No plugins capable of loading routefile " + CurrentRouteFile + " were found.");
			}
			Program.Renderer.CameraTrackFollower = new TrackFollower(Program.CurrentHost);
			System.Threading.Thread.Sleep(1); if (Cancel) return;
			Program.CurrentRoute.Atmosphere.CalculateSeaLevelConstants();
			// camera
			Program.Renderer.CameraTrackFollower.UpdateAbsolute( 0.0, true, false);
			Program.Renderer.CameraTrackFollower.UpdateAbsolute(0.1, true, false);
			Program.Renderer.CameraTrackFollower.UpdateAbsolute(-0.1, true, false);
			Program.Renderer.CameraTrackFollower.TriggerType = EventTriggerType.Camera;
			// default starting time
			Game.SecondsSinceMidnight = 0.0;
			Game.StartupTime = 0.0;
			// finished created objects
			System.Threading.Thread.Sleep(1); if (Cancel) return;
			// signals
			System.Threading.Thread.Sleep(1); if (Cancel) return;
			// ReSharper disable once CoVariantArrayConversion
			Program.CurrentRoute.Trains = TrainManager.Trains;
			Program.CurrentRoute.UpdateAllSections();
			// starting track position
			System.Threading.Thread.Sleep(1); if (Cancel) return;
			// int FirstStationIndex = -1;
			double FirstStationPosition = 0.0;
			for (int i = 0; i < Program.CurrentRoute.Stations.Length; i++) {
				if (Program.CurrentRoute.Stations[i].Stops.Length != 0) {
					// FirstStationIndex = i;
					FirstStationPosition = Program.CurrentRoute.Stations[i].Stops[Program.CurrentRoute.Stations[i].Stops.Length - 1].TrackPosition;
					if (Program.CurrentRoute.Stations[i].ArrivalTime < 0.0) {
						if (Program.CurrentRoute.Stations[i].DepartureTime < 0.0) {
							Game.SecondsSinceMidnight = 0.0;
						} else {
							Game.SecondsSinceMidnight = Program.CurrentRoute.Stations[i].DepartureTime - Program.CurrentRoute.Stations[i].StopTime;
						}
					} else {
						Game.SecondsSinceMidnight = Program.CurrentRoute.Stations[i].ArrivalTime;
					}
					Game.StartupTime = Game.SecondsSinceMidnight;
					break;
				}
			}
			// initialize camera
			Program.Renderer.CameraTrackFollower.UpdateAbsolute(-1.0, true, false);
			Program.Renderer.CameraTrackFollower.UpdateAbsolute(FirstStationPosition, true, false);
			Program.Renderer.Camera.Alignment = new CameraAlignment(new Vector3(0.0, 2.5, 0.0), 0.0, 0.0, 0.0, FirstStationPosition, 1.0);
			World.UpdateAbsoluteCamera(0.0);
		}

	}
}
