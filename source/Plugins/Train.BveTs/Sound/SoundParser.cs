using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenBveApi;
using OpenBveApi.Interface;
using OpenBveApi.Math;
using SoundManager;
using TrainManager.BrakeSystems;
using TrainManager.Trains;

namespace Train.BveTs
{
	internal partial class SoundParser
	{
		internal readonly Plugin Plugin;

		internal SoundParser(Plugin plugin)
		{
			Plugin = plugin;
		}

		//Default sound radii
		internal const double largeRadius = 30.0;
		internal const double mediumRadius = 10.0;
		internal const double smallRadius = 5.0;
		internal const double tinyRadius = 2.0;

		/// <summary>Parses the sound configuration file for a train</summary>
		/// <param name="train">The train to which to apply the new sound configuration</param>
		/// <param name="soundFile">The path to the sound configuration file</param>
		internal void ParseSoundConfig(TrainBase train, string soundFile)
		{
			LoadDefaultATSSounds(train);
			string soundFolder = System.IO.Path.GetDirectoryName(soundFile);

			if (System.IO.File.Exists(soundFile))
			{
				Plugin.FileSystem.AppendToLogFile("Loading sound file: " + soundFile);

				//Default sound positions

				//3D center of the car
				Vector3 center = Vector3.Zero;
				//Positioned to the left of the car, but centered Y & Z
				Vector3 left = new Vector3(-1.3, 0.0, 0.0);
				//Positioned to the right of the car, but centered Y & Z
				Vector3 right = new Vector3(1.3, 0.0, 0.0);
				//Positioned at the front of the car, centered X and Y
				Vector3 front = new Vector3(0.0, 0.0, 0.5 * train.Cars[train.DriverCar].Length);
				//Positioned at the position of the panel / 3D cab (Remember that the panel is just an object in the world...)
				Vector3 panel = new Vector3(train.Cars[train.DriverCar].Driver.X, train.Cars[train.DriverCar].Driver.Y, train.Cars[train.DriverCar].Driver.Z + 1.0);

				//Radius at which the sound is audible at full volume, presumably in m
				//TODO: All radii are much too small in external mode, but we can't change them by default.....

				Encoding Encoding = TextEncoding.GetSystemEncodingFromFile(soundFile);

				// parse configuration file
				System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.InvariantCulture;
				List<string> Lines = System.IO.File.ReadAllLines(soundFile, Encoding).ToList();
				for (int i = Lines.Count - 1; i >= 0; i--)
				{
					// Strip comments and remove empty resulting lines etc.
					int j = Lines[i].IndexOf(';');
					if (j >= 0)
					{
						Lines[i] = Lines[i].Substring(0, j).Trim();
					}
					else
					{
						Lines[i] = Lines[i].Trim();
					}
					if (string.IsNullOrEmpty(Lines[i]))
					{
						Lines.RemoveAt(i);
					}
				}

				if (Lines.Count == 0)
				{
					Plugin.currentHost.AddMessage(MessageType.Error, false, "Sound config file is empty in " + soundFile + ".");
				}
				else if (string.Compare(Lines[0], "bvets vehicle sound 3.01", StringComparison.OrdinalIgnoreCase) != 0)
				{
					Plugin.currentHost.AddMessage(MessageType.Error, false, "Invalid file format encountered in " + soundFile + ". The first line is expected to be \"Version 1.0\".");
				}
				string[] MotorFiles = new string[] { };
				double invfac = Lines.Count == 0 ? 0.1 : 0.1 / Lines.Count;
				for (int i = 0; i < Lines.Count; i++)
				{
					Plugin.CurrentProgress = Plugin.LastProgress + invfac * i;
					if ((i & 7) == 0)
					{
						System.Threading.Thread.Sleep(1);
						if (Plugin.Cancel) return;
					}
					switch (Lines[i].ToLowerInvariant())
					{
						case "[run]":
						case "[rolling]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									if (!int.TryParse(a, System.Globalization.NumberStyles.Integer, Culture, out var k))
									{
										Plugin.currentHost.AddMessage(MessageType.Error, false, "Invalid index appeared at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
									}
									else
									{
										if (k >= 0)
										{
											for (int c = 0; c < train.Cars.Length; c++)
											{
												if (train.Cars[c].Sounds.Run == null)
												{
													train.Cars[c].Sounds.Run = new Dictionary<int, CarSound>();
												}

												if (train.Cars[c].Sounds.Run.ContainsKey(k))
												{
													train.Cars[c].Sounds.Run[k] = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.mediumRadius, center);
												}
												else
												{
													train.Cars[c].Sounds.Run.Add(k, new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.mediumRadius, center));
												}
											}
										}
										else
										{
											Plugin.currentHost.AddMessage(MessageType.Error, false, "RunIndex must be greater than or equal to zero at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
										}
									}
								}
								i++;
							}
							i--; break;
						case "[flange]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									if (!int.TryParse(a, System.Globalization.NumberStyles.Integer, Culture, out var k))
									{
										Plugin.currentHost.AddMessage(MessageType.Error, false, "Invalid index appeared at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
									}
									else
									{
										if (k >= 0)
										{
											for (int c = 0; c < train.Cars.Length; c++)
											{
												if (train.Cars[c].Sounds.Flange.ContainsKey(k))
												{
													train.Cars[c].Sounds.Flange[k] = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.mediumRadius, center);
												}
												else
												{
													train.Cars[c].Sounds.Flange.Add(k, new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.mediumRadius, center));
												}
											}
										}
										else
										{
											Plugin.currentHost.AddMessage(MessageType.Error, false, "FlangeIndex must be greater than or equal to zero at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
										}
									}
								}
								i++;
							}
							i--; break;
						case "[motor]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									if (!int.TryParse(a, System.Globalization.NumberStyles.Integer, Culture, out var k))
									{
										Plugin.currentHost.AddMessage(MessageType.Error, false, "Invalid index appeared at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
									}
									else
									{
										if (k >= 0)
										{
											if (k >= MotorFiles.Length)
											{
												Array.Resize(ref MotorFiles, k + 1);
											}
											MotorFiles[k] = Path.CombineFile(soundFolder, b);
											if (!System.IO.File.Exists(MotorFiles[k]))
											{
												Plugin.currentHost.AddMessage(MessageType.Error, true, "File " + MotorFiles[k] + " does not exist at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
												MotorFiles[k] = null;
											}
										}
										else
										{
											Plugin.currentHost.AddMessage(MessageType.Error, false, "MotorIndex must be greater than or equal to zero at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
										}
									}
								}
								i++;
							}
							i--; break;
						case "[joint]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									if (NumberFormats.TryParseIntVb6(a, out var switchIndex))
									{
										if (switchIndex < 0)
										{
											Plugin.currentHost.AddMessage(MessageType.Error, false, "SwitchIndex must be greater than or equal to zero at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
											continue;
										}
										for (int c = 0; c < train.Cars.Length; c++)
										{
											int n = train.Cars[c].FrontAxle.PointSounds.Length;
											if (switchIndex >= n)
											{
												Array.Resize(ref train.Cars[c].FrontAxle.PointSounds, switchIndex + 1);
												Array.Resize(ref train.Cars[c].RearAxle.PointSounds, switchIndex + 1);
												for (int h = n; h < switchIndex; h++)
												{
													train.Cars[c].FrontAxle.PointSounds[h] = new CarSound();
													train.Cars[c].RearAxle.PointSounds[h] = new CarSound();
												}
											}
											Vector3 frontaxle = new Vector3(0.0, 0.0, train.Cars[c].FrontAxle.Position);
											Vector3 rearaxle = new Vector3(0.0, 0.0, train.Cars[c].RearAxle.Position);
											train.Cars[c].FrontAxle.PointSounds[switchIndex] = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, frontaxle);
											train.Cars[c].RearAxle.PointSounds[switchIndex] = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, rearaxle);
										}
									}
									else
									{
										Plugin.currentHost.AddMessage(MessageType.Warning, false, "Unsupported index " + a + " encountered at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
									}
								}
								i++;
							}
							i--; break;
						case "[brake]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									switch (a.ToLowerInvariant())
									{
										case "bcreleasehigh":
											for (int c = 0; c < train.Cars.Length; c++)
											{
												train.Cars[c].CarBrake.AirHigh = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, center);
											}

											break;
										case "bcrelease":
											for (int c = 0; c < train.Cars.Length; c++)
											{
												train.Cars[c].CarBrake.Air = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, center);
											}

											break;
										case "bcreleasefull":
											for (int c = 0; c < train.Cars.Length; c++)
											{
												train.Cars[c].CarBrake.AirZero = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, center);
											}

											break;
										case "emergency":
											train.Handles.EmergencyBrake.ApplicationSound = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.mediumRadius, center);
											break;
										case "emergencyrelease":
											train.Handles.EmergencyBrake.ReleaseSound = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.mediumRadius, center);
											break;
										case "bpdecomp":
											for (int c = 0; c < train.Cars.Length; c++)
											{
												train.Cars[c].CarBrake.Release = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, center);
											}

											break;
										default:
											Plugin.currentHost.AddMessage(MessageType.Warning, false, "Unsupported key " + a + " encountered at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
											break;
									}

								}
								i++;
							}
							i--; break;
						case "[compressor]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									for (int c = 0; c < train.Cars.Length; c++)
									{
										if (train.Cars[c].CarBrake.brakeType == BrakeType.Main)
										{
											switch (a.ToLowerInvariant())
											{
												case "attack":
													train.Cars[c].CarBrake.airCompressor.StartSound = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.mediumRadius, center);
													break;
												case "loop":
													train.Cars[c].CarBrake.airCompressor.LoopSound = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.mediumRadius, center);
													break;
												case "release":
													train.Cars[c].CarBrake.airCompressor.EndSound = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.mediumRadius, center);
													break;
												default:
													Plugin.currentHost.AddMessage(MessageType.Warning, false, "Unsupported key " + a + " encountered at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
													break;
											}
										}
									}

								}
								i++;
							}
							i--; break;
						case "[airspring]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									switch (a.ToLowerInvariant())
									{
										case "leftapply":
											for (int c = 0; c < train.Cars.Length; c++)
											{
												train.Cars[c].Sounds.SpringL = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, left);
											}

											break;
										case "rightapply":
											for (int c = 0; c < train.Cars.Length; c++)
											{
												train.Cars[c].Sounds.SpringR = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, right);
											}

											break;
										default:
											Plugin.currentHost.AddMessage(MessageType.Warning, false, "Unsupported key " + a + " encountered at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
											break;
									}
								}
								i++;
							}
							i--; break;
						case "[horn]":
							i++;
							while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									switch (a.ToLowerInvariant())
									{
										//PRIMARY HORN (Enter)
										case "primaryrelease":
											Plugin.currentHost.RegisterSound(Path.CombineFile(soundFolder, b), SoundParser.largeRadius, out var primaryEnd);
											train.Cars[train.DriverCar].Horns[0].EndSound = primaryEnd as SoundBuffer;
											train.Cars[train.DriverCar].Horns[0].SoundPosition = front;
											train.Cars[train.DriverCar].Horns[0].StartEndSounds = true;
											break;
										case "primary":
											Plugin.currentHost.RegisterSound(Path.CombineFile(soundFolder, b), SoundParser.largeRadius, out var primaryLoop);
											train.Cars[train.DriverCar].Horns[0].LoopSound = primaryLoop as SoundBuffer;
											train.Cars[train.DriverCar].Horns[0].SoundPosition = front;
											train.Cars[train.DriverCar].Horns[0].Loop = false;
											break;
										//SECONDARY HORN
										case "secondaryrelease":
											Plugin.currentHost.RegisterSound(Path.CombineFile(soundFolder, b), SoundParser.largeRadius, out var secondaryEnd);
											train.Cars[train.DriverCar].Horns[1].EndSound = secondaryEnd as SoundBuffer;
											train.Cars[train.DriverCar].Horns[1].SoundPosition = front;
											train.Cars[train.DriverCar].Horns[1].StartEndSounds = true;
											break;
										case "secondary":
											Plugin.currentHost.RegisterSound(Path.CombineFile(soundFolder, b), SoundParser.largeRadius, out var secondaryLoop);
											train.Cars[train.DriverCar].Horns[1].LoopSound = secondaryLoop as SoundBuffer;
											train.Cars[train.DriverCar].Horns[1].SoundPosition = front;
											train.Cars[train.DriverCar].Horns[1].Loop = false;
											break;
										//MUSIC HORN
										case "musicrelease":
											Plugin.currentHost.RegisterSound(Path.CombineFile(soundFolder, b), SoundParser.mediumRadius, out var musicEnd);
											train.Cars[train.DriverCar].Horns[2].EndSound = musicEnd as SoundBuffer;
											train.Cars[train.DriverCar].Horns[2].SoundPosition = front;
											train.Cars[train.DriverCar].Horns[2].StartEndSounds = true;
											break;
										case "music":
											Plugin.currentHost.RegisterSound(Path.CombineFile(soundFolder, b), SoundParser.mediumRadius, out var musicLoop);
											train.Cars[train.DriverCar].Horns[2].LoopSound = musicLoop as SoundBuffer;
											train.Cars[train.DriverCar].Horns[2].SoundPosition = front;
											train.Cars[train.DriverCar].Horns[2].Loop = true;
											break;
										default:
											Plugin.currentHost.AddMessage(MessageType.Warning, false, "Unsupported key " + a + " encountered at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
											break;
									}
								}
								i++;
							}
							i--; break;
						case "[door]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									switch (a.ToLowerInvariant())
									{
										case "openleft":
											for (int c = 0; c < train.Cars.Length; c++)
											{
												train.Cars[c].Doors[0].OpenSound = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, left);
											}

											break;
										case "openright":
											for (int c = 0; c < train.Cars.Length; c++)
											{
												train.Cars[c].Doors[1].OpenSound = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, right);
											}

											break;
										case "closeleft":
											for (int c = 0; c < train.Cars.Length; c++)
											{
												train.Cars[c].Doors[0].CloseSound = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, left);
											}

											break;
										case "closeright":
											for (int c = 0; c < train.Cars.Length; c++)
											{
												train.Cars[c].Doors[1].CloseSound = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, right);
											}
											break;
										default:
											Plugin.currentHost.AddMessage(MessageType.Warning, false, "Unsupported key " + a + " encountered at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
											break;
									}
								}
								i++;
							}
							i--; break;
						case "[ats]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									if (!int.TryParse(a, System.Globalization.NumberStyles.Integer, Culture, out var k))
									{
										Plugin.currentHost.AddMessage(MessageType.Error, false, "Invalid index appeared at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
									}
									else
									{
										if (k >= 0)
										{
											int n = train.Cars[train.DriverCar].Sounds.Plugin.Length;
											if (k >= n)
											{
												Array.Resize(ref train.Cars[train.DriverCar].Sounds.Plugin, k + 1);
												for (int h = n; h < k; h++)
												{
													train.Cars[train.DriverCar].Sounds.Plugin[h] = new CarSound();
												}
											}

											train.Cars[train.DriverCar].Sounds.Plugin[k] = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
										}
										else
										{
											Plugin.currentHost.AddMessage(MessageType.Warning, false, "Index must be greater than or equal to zero at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
										}
									}

								}

								i++;
							}
							i--; break;
						case "[buzzer]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									switch (a.ToLowerInvariant())
									{
										case "correct":
											train.SafetySystems.StationAdjust.AdjustAlarm = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										default:
											Plugin.currentHost.AddMessage(MessageType.Warning, false, "Unsupported key " + a + " encountered at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
											break;
									}

								}
								i++;
							}
							i--; break;
						case "[pilotlamp]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									switch (a.ToLowerInvariant())
									{
										case "on":
											train.SafetySystems.PilotLamp.OnSound = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										case "off":
											train.SafetySystems.PilotLamp.OffSound = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										default:
											Plugin.currentHost.AddMessage(MessageType.Warning, false, "Unsupported key " + a + " encountered at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
											break;
									}
								}
								i++;
							}
							i--; break;
						case "[brakehandle]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									switch (a.ToLowerInvariant())
									{
										case "apply":
											train.Handles.Brake.Increase = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										case "applyfast":
											train.Handles.Brake.IncreaseFast = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										case "release":
											train.Handles.Brake.Decrease = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										case "releasefast":
											train.Handles.Brake.DecreaseFast = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										case "min":
											train.Handles.Brake.Min = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										case "max":
											train.Handles.Brake.Max = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										default:
											Plugin.currentHost.AddMessage(MessageType.Warning, false, "Unsupported key " + a + " encountered at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
											break;
									}
								}
								i++;
							}
							i--; break;
						case "[mastercontroller]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									switch (a.ToLowerInvariant())
									{
										case "up":
											train.Handles.Power.Increase = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										case "upfast":
											train.Handles.Power.IncreaseFast = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										case "down":
											train.Handles.Power.Decrease = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										case "downfast":
											train.Handles.Power.DecreaseFast = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										case "min":
											train.Handles.Power.Min = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										case "max":
											train.Handles.Power.Max = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										default:
											Plugin.currentHost.AddMessage(MessageType.Warning, false, "Unsupported key " + a + " encountered at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
											break;
									}
								}
								i++;
							}
							i--; break;
						case "[reverser]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									switch (a.ToLowerInvariant())
									{
										case "on":
											train.Handles.Reverser.EngageSound = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										case "off":
											train.Handles.Reverser.ReleaseSound = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.tinyRadius, panel);
											break;
										default:
											Plugin.currentHost.AddMessage(MessageType.Warning, false, "Unsupported key " + a + " encountered at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
											break;
									}
								}
								i++;
							}
							i--; break;
						case "[breaker]":
							i++; while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									switch (a.ToLowerInvariant())
									{
										case "on":
											train.Cars[train.DriverCar].Breaker.Resume = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, panel);
											break;
										case "off":
											train.Cars[train.DriverCar].Breaker.ResumeOrInterrupt = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.smallRadius, panel);
											break;
										default:
											Plugin.currentHost.AddMessage(MessageType.Warning, false, "Unsupported key " + a + " encountered at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
											break;
									}
								}
								i++;
							}
							i--; break;
						case "[others]":
							i++;
							while (i < Lines.Count && !Lines[i].StartsWith("[", StringComparison.Ordinal))
							{
								int j = Lines[i].IndexOf("=", StringComparison.Ordinal);
								if (j >= 0)
								{
									string a = Lines[i].Substring(0, j).TrimEnd();
									string b = Lines[i].Substring(j + 1).TrimStart();
									switch (a.ToLowerInvariant())
									{
										case "noise":
											for (int c = 0; c < train.Cars.Length; c++)
											{
												if (train.Cars[c].Specs.IsMotorCar | c == train.DriverCar)
												{
													train.Cars[c].Sounds.Loop = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.mediumRadius, center);
												}
											}

											break;
										case "shoe":
											for (int c = 0; c < train.Cars.Length; c++)
											{
												train.Cars[c].CarBrake.Rub = new CarSound(Plugin.currentHost, soundFolder, soundFile, i, b, SoundParser.mediumRadius, center);
											}

											break;
										default:
											Plugin.currentHost.AddMessage(MessageType.Warning, false, "Unsupported key " + a + " encountered at line " + (i + 1).ToString(Culture) + " in file " + soundFile);
											break;
									}
								}
								i++;
							}
							i--; break;
					}
				}
				// motor sound
				for (int c = 0; c < train.Cars.Length; c++)
				{
					if (train.Cars[c].Specs.IsMotorCar)
					{
						train.Cars[c].Sounds.Motor.Position = center;
						for (int i = 0; i < train.Cars[c].Sounds.Motor.Tables.Length; i++)
						{
							train.Cars[c].Sounds.Motor.Tables[i].Buffer = null;
							train.Cars[c].Sounds.Motor.Tables[i].Source = null;
							for (int j = 0; j < train.Cars[c].Sounds.Motor.Tables[i].Entries.Length; j++)
							{
								int index = train.Cars[c].Sounds.Motor.Tables[i].Entries[j].SoundIndex;
								if (index >= 0 && index < MotorFiles.Length && MotorFiles[index] != null)
								{
									Plugin.currentHost.RegisterSound(MotorFiles[index], SoundParser.mediumRadius, out var motorSound);
									train.Cars[c].Sounds.Motor.Tables[i].Entries[j].Buffer = motorSound as SoundBuffer;
								}
							}
						}
					}
				}
			}
		}

	}
}
