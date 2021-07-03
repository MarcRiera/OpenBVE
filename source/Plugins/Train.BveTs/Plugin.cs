using System;
using System.IO;
using System.Text;
using System.Threading;
using LibRender2;
using LibRender2.Trains;
using OpenBveApi;
using OpenBveApi.FileSystem;
using OpenBveApi.Graphics;
using OpenBveApi.Hosts;
using OpenBveApi.Interface;
using OpenBveApi.Objects;
using OpenBveApi.Trains;
using TrainManager.Trains;
using Path = OpenBveApi.Path;

namespace Train.BveTs
{
	public class Plugin : TrainInterface
    {
	    internal static HostInterface currentHost;

	    internal static FileSystem FileSystem;

	    internal static BaseOptions CurrentOptions;

	    internal static Random RandomNumberGenerator = new Random();

	    internal static BaseRenderer Renderer;

	    internal TrainDatParser TrainDatParser;
	    
	    internal SoundParser SoundParser;
	    
	    internal PanelParser PanelParser;
	    
	    internal Control[] CurrentControls;

	    internal double LastProgress;

		public Plugin()
	    {
		    if (TrainDatParser == null)
		    {
			    TrainDatParser = new TrainDatParser(this);
		    }

		    if (SoundParser == null)
		    {
			    SoundParser = new SoundParser(this);
		    }

		    if (PanelParser == null)
		    {
			    PanelParser = new PanelParser(this);
		    }
		   
	    }

		public override void Load(HostInterface host, FileSystem fileSystem, BaseOptions Options, object rendererReference)
		{
			currentHost = host;
			FileSystem = fileSystem;
			CurrentOptions = Options;
			// ReSharper disable once MergeCastWithTypeCheck
			if (rendererReference is BaseRenderer)
			{
				Renderer = (BaseRenderer)rendererReference;
			}
		}

		public override bool CanLoadTrain(string path)
		{
			if (path == null)
			{
				return false;
			}
			if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
			{
				string vehicleTxt = Path.CombineFile(path, "vehicle.txt");
				if (File.Exists(vehicleTxt))
				{
					Functions.BvetsHeader header = Functions.ReadBvetsHeader(vehicleTxt);
					if (header.IsValid && header.Type.Equals("vehicle", StringComparison.InvariantCultureIgnoreCase) && header.Version.Major <= 2)
					{
						return true;
					}
				}
				
				return false;
			}

			if (File.Exists(path))
			{
				if (path.EndsWith("vehicle.txt", StringComparison.InvariantCultureIgnoreCase))
				{
					Functions.BvetsHeader header = Functions.ReadBvetsHeader(path);
					if (header.IsValid && header.Type.Equals("vehicle", StringComparison.InvariantCultureIgnoreCase) && header.Version.Major <= 2)
					{
						return true;
					}
				}
			}

			return false;
		}

	    public override bool LoadTrain(Encoding Encoding, string trainPath, ref AbstractTrain train, ref Control[] currentControls)
	    {
		    CurrentProgress = 0.0;
		    LastProgress = 0.0;
		    IsLoading = true;
		    CurrentControls = currentControls;
		    TrainBase currentTrain = train as TrainBase;
		    if (currentTrain == null)
		    {
				currentHost.ReportProblem(ProblemType.InvalidData, "Train was not valid");
				IsLoading = false;
				return false;
		    }

		    if (currentTrain.State == TrainState.Bogus)
		    {
			    // bogus train
			    string TrainData = Path.CombineFile(FileSystem.GetDataFolder("Compatibility", "PreTrain"), "train.dat");
			    TrainDatParser.Parse(TrainData, Encoding.UTF8, currentTrain);
			    Thread.Sleep(1);

			    if (Cancel)
			    {
				    IsLoading = false;
				    return false;
			    }
		    }
		    else
		    {
				currentTrain.TrainFolder = trainPath;
				string vehicleFile = Path.CombineFile(currentTrain.TrainFolder, "vehicle.txt");
				string soundFile = string.Empty;
				string panelFile = string.Empty;

				Functions.BvetsHeader header = Functions.ReadBvetsHeader(vehicleFile);
				string[] lines = File.ReadAllLines(vehicleFile, Encoding.UTF8);
				lines = Functions.RemoveComments(lines);

				for (int i = 0; i < lines.Length; i++)
				{
					int j = lines[i].IndexOf("=", StringComparison.Ordinal);
					if (j >= 0)
					{
						string a = lines[i].Substring(0, j).TrimEnd();
						string b = lines[i].Substring(j + 1).TrimStart();
						switch (a.ToLowerInvariant())
						{
							case "panel":
								panelFile = Path.CombineFile(currentTrain.TrainFolder, b);
								break;
							case "sound":
								soundFile = Path.CombineFile(currentTrain.TrainFolder, b);
								break;
						}
					}
				}

				// real train
				if (currentTrain.IsPlayerTrain)
			    {
				    FileSystem.AppendToLogFile("Loading player train: " + currentTrain.TrainFolder);
			    }
			    else
			    {
				    FileSystem.AppendToLogFile("Loading AI train: " + currentTrain.TrainFolder);
			    }

				string TrainData2 = Path.CombineFile(currentTrain.TrainFolder, "train2.dat");

				TrainDatParser.Parse(TrainData2, Encoding, currentTrain);
			    LastProgress = 0.1;
			    Thread.Sleep(1);
			    if (Cancel) return false;
			    SoundParser.ParseSoundConfig(currentTrain, soundFile);
			    LastProgress = 0.2;
			    Thread.Sleep(1);
			    if (Cancel)
			    {
				    IsLoading = false;
				    return false;
			    }
			    // door open/close speed
			    for (int i = 0; i < currentTrain.Cars.Length; i++)
			    {
				    currentTrain.Cars[i].DetermineDoorClosingSpeed();
			    }
		    }
		    // add panel section
		    if (currentTrain.IsPlayerTrain) {	
			    ParsePanelConfig(currentTrain, Encoding);
			    LastProgress = 0.6;
			    Thread.Sleep(1);
			    if (Cancel)
			    {
				    IsLoading = false;
				    return false;
			    }
			    FileSystem.AppendToLogFile("Train panel loaded sucessfully.");
		    }
			// add exterior section
			if (currentTrain.State != TrainState.Bogus)
			{
				bool[] VisibleFromInterior;
				UnifiedObject[] CarObjects = new UnifiedObject[currentTrain.Cars.Length];
				UnifiedObject[] BogieObjects = new UnifiedObject[currentTrain.Cars.Length * 2];
				UnifiedObject[] CouplerObjects = new UnifiedObject[currentTrain.Cars.Length];
				
				currentTrain.CameraCar = currentTrain.DriverCar;
				Thread.Sleep(1);
				if (Cancel)
				{
					IsLoading = false;
					return false;
				}
				//Stores the current array index of the bogie object to add
				//Required as there are two bogies per car, and we're using a simple linear array....
				int currentBogieObject = 0;
				for (int i = 0; i < currentTrain.Cars.Length; i++)
				{
					if (CarObjects[i] == null)
					{
						// load default exterior object
						string file = Path.CombineFile(FileSystem.GetDataFolder("Compatibility"), "exterior.csv");
						currentHost.LoadStaticObject(file, Encoding.UTF8, false, out var so);
						if (so == null)
						{
							CarObjects[i] = null;
						}
						else
						{
							StaticObject c = (StaticObject) so.Clone(); //Clone as otherwise the cached object doesn't scale right
							c.ApplyScale(currentTrain.Cars[i].Width, currentTrain.Cars[i].Height, currentTrain.Cars[i].Length);
							CarObjects[i] = c;
						}
					}

					if (CarObjects[i] != null)
					{
						// add object
						currentTrain.Cars[i].LoadCarSections(CarObjects[i], false);
					}

					if (CouplerObjects[i] != null)
					{
						currentTrain.Cars[i].Coupler.LoadCarSections(CouplerObjects[i], false);
					}

					//Load bogie objects
					if (BogieObjects[currentBogieObject] != null)
					{
						currentTrain.Cars[i].FrontBogie.LoadCarSections(BogieObjects[currentBogieObject], false);
					}

					currentBogieObject++;
					if (BogieObjects[currentBogieObject] != null)
					{
						currentTrain.Cars[i].RearBogie.LoadCarSections(BogieObjects[currentBogieObject], false);
					}

					currentBogieObject++;
				}
			}
			// place cars
			currentTrain.PlaceCars(0.0);
			currentControls = CurrentControls;
			IsLoading = false;
			return true;
	    }


	    /// <summary>Attempts to load and parse the current train's panel configuration file.</summary>
	    /// <param name="Train">The train</param>
	    /// <param name="Encoding">The selected train encoding</param>
	    internal void ParsePanelConfig(TrainBase Train, Encoding Encoding)
	    {
		    Train.Cars[Train.DriverCar].CarSections = new CarSection[1];
		    Train.Cars[Train.DriverCar].CarSections[0] = new CarSection(currentHost, ObjectType.Overlay, true);

		    try
		    {
			    string File = Path.CombineFile(Train.TrainFolder, "panel/panel.txt");
			    if (System.IO.File.Exists(File))
			    {
				    FileSystem.AppendToLogFile("Loading train panel: " + File);
				    PanelParser.ParsePanel2Config("panel/panel.txt", Train.TrainFolder, Train.Cars[Train.DriverCar]);
				    Train.Cars[Train.DriverCar].CameraRestrictionMode = CameraRestrictionMode.On;
				    Renderer.Camera.CurrentRestriction = CameraRestrictionMode.On;
			    }
		    }
		    catch
		    {
			    var currentError = Translations.GetInterfaceString("errors_critical_file");
			    currentError = currentError.Replace("[file]", "panel.txt");
			    currentHost.ReportProblem(ProblemType.InvalidData, currentError);
			    Cancel = true;
		    }
	    }
    }
}
