﻿using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using LibRender2;
using OpenBveApi;
using RouteManager2;

namespace OpenBve
{
	public partial class Menu
	{
		private BackgroundWorker routeWorkerThread;
		private static string RouteSearchDirectory;
		private static string RouteFile;
		private static Encoding RouteEncoding;
		private static string RouteDescription;

		private void routeWorkerThread_doWork(object sender, DoWorkEventArgs e)
		{
			if (string.IsNullOrEmpty(RouteFile))
			{
				return;
			}

			if (!Plugins.LoadPlugins())
			{
				throw new Exception("Unable to load the required plugins- Please reinstall OpenBVE");
			}
			Game.Reset(false);
			bool loaded = false;
			for (int i = 0; i < Program.CurrentHost.Plugins.Length; i++)
			{
				if (Program.CurrentHost.Plugins[i].Route != null && Program.CurrentHost.Plugins[i].Route.CanLoadRoute(RouteFile))
				{
					object Route = (object)Program.CurrentRoute; //must cast to allow us to use the ref keyword.
					string RailwayFolder = Loading.GetRailwayFolder(RouteFile);
					string ObjectFolder = OpenBveApi.Path.CombineDirectory(RailwayFolder, "Object");
					string SoundFolder = OpenBveApi.Path.CombineDirectory(RailwayFolder, "Sound");
					if (Program.CurrentHost.Plugins[i].Route.LoadRoute(RouteFile, RouteEncoding, null, ObjectFolder, SoundFolder, true, ref Route))
					{
						Program.CurrentRoute = (CurrentRoute) Route;
					}
					else
					{
						if (Program.CurrentHost.Plugins[i].Route.LastException != null)
						{
							throw Program.CurrentHost.Plugins[i].Route.LastException; //Re-throw last exception generated by the route parser plugin so that the UI thread captures it
						}
						throw new Exception("An unknown error was enountered whilst attempting to parser the routefile " + RouteFile);
					}
					loaded = true;
					break;
				}
			}

			if (!loaded)
			{
				throw new Exception("No plugins capable of loading routefile " + RouteFile + " were found.");
			}
		}

		private void routeWorkerThread_completed(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null || Program.CurrentRoute == null)
			{
				//TryLoadImage(pictureboxRouteImage, "route_error.png");
				if (e.Error != null)
				{
					RouteDescription = e.Error.Message;
				}
				//pictureboxRouteMap.Image = null;
				//pictureboxRouteGradient.Image = null;
				//Result.ErrorFile = Result.RouteFile;
				//RouteFile = string.Empty;
				//checkboxTrainDefault.Text = Translations.GetInterfaceString("start_train_usedefault");
				routeWorkerThread.Dispose();
				return;
			}
			try
			{
				lock (BaseRenderer.GdiPlusLock)
				{
					//pictureboxRouteMap.Image = Illustrations.CreateRouteMap(pictureboxRouteMap.Width, pictureboxRouteMap.Height, false);
					//pictureboxRouteGradient.Image = Illustrations.CreateRouteGradientProfile(pictureboxRouteGradient.Width,
					//	pictureboxRouteGradient.Height, false);
				}
				// image
				if (!string.IsNullOrEmpty(Program.CurrentRoute.Image))
				{
					//TryLoadImage(pictureboxRouteImage, Program.CurrentRoute.Image);
				}
				else
				{
					string[] f = {".png", ".bmp", ".gif", ".tiff", ".tif", ".jpeg", ".jpg"};
					int i;
					for (i = 0; i < f.Length; i++)
					{
						string g = OpenBveApi.Path.CombineFile(System.IO.Path.GetDirectoryName(RouteFile),
							System.IO.Path.GetFileNameWithoutExtension(RouteFile) + f[i]);
						if (System.IO.File.Exists(g))
						{
							try
							{
								using (var fs = new FileStream(g, FileMode.Open, FileAccess.Read))
								{
									//pictureboxRouteImage.Image = new Bitmap(fs);
								}
							}
							catch
							{
								//pictureboxRouteImage.Image = null;
							}
							break;
						}
					}
					if (i == f.Length)
					{
						//TryLoadImage(pictureboxRouteImage, "route_unknown.png");
					}
				}

				// description
				string Description = Program.CurrentRoute.Comment.ConvertNewlinesToCrLf();
				if (Description.Length != 0)
				{
					RouteDescription = Description;
				}
				else
				{
					RouteDescription = System.IO.Path.GetFileNameWithoutExtension(RouteFile);
				}

				//textboxRouteEncodingPreview.Text = Description.ConvertNewlinesToCrLf();
				if (Interface.CurrentOptions.TrainName != null)
				{
				//	checkboxTrainDefault.Text = Translations.GetInterfaceString("start_train_usedefault") + @" (" + Interface.CurrentOptions.TrainName + @")";
				}
				else
				{
				//	checkboxTrainDefault.Text = Translations.GetInterfaceString("start_train_usedefault");
				}
				//Result.ErrorFile = null;
			}
			catch (Exception ex)
			{
				//TryLoadImage(pictureboxRouteImage, "route_error.png");
				RouteDescription = ex.Message;
				//textboxRouteEncodingPreview.Text = "";
				//pictureboxRouteMap.Image = null;
				//pictureboxRouteGradient.Image = null;
				//Result.ErrorFile = Result.RouteFile;
				RouteFile = null;
				//checkboxTrainDefault.Text = Translations.GetInterfaceString("start_train_usedefault");
			}
		}
	}
}
