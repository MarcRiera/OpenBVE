﻿using System;
using System.Drawing;
using System.Reflection;			// for AssemblyVersion
using OpenBveApi.Colors;
using OpenTK.Graphics.OpenGL;

namespace OpenBve {
	internal static partial class Renderer {
		
		/* --------------------------------------------------------------
		 * This file contains the drawing routines for the loading screen
		 * -------------------------------------------------------------- */

//		private static Color128	ColourBackground	= new Color128(0.39f, 0.39f, 0.39f, 1.00f);
		// the openBVE yellow
		private static Color128	ColourProgressBar	= new Color128(1.00f, 0.69f, 0.00f, 1.00f);
		private const int		progrBorder			= 1;
		private const int		progrMargin			= 24;
		private const int		numOfLoadingBkgs	= 2;

		private static bool				customLoadScreen	= false;
		private static Textures.Texture	TextureLoadingBkg	= null;
		private static Textures.Texture	TextureLogo			= null;
		private static string			LogoFileName		= "logo_512.png";

		//
		// INIT LOADING RESOURCES
		//
		/// <summary>Initializes the textures used for the loading screen</summary>
		internal static void InitLoading()
		{
			customLoadScreen	= false;
			string Path = Program.FileSystem.GetDataFolder("In-game");
			if (TextureLoadingBkg == null)
			{
				int bkgNo = Program.RandomNumberGenerator.Next (numOfLoadingBkgs);
				Textures.RegisterTexture(OpenBveApi.Path.CombineFile(Path, "loadingbkg_"+bkgNo+".png"),
					out TextureLoadingBkg);
				Textures.RegisterTexture(OpenBveApi.Path.CombineFile(Path, "loadingbkg_1.png"),
					out TextureLoadingBkg);
			}
			if (Renderer.TextureLogo == null)
			{
				Textures.RegisterTexture(OpenBveApi.Path.CombineFile(Path, LogoFileName), out TextureLogo);
			}
		}

		//
		// SET CUSTOM LOADING SCREEN BACKGROUND
		//
		/// <summary>Sets the loading screen background to a custom image</summary>
		internal static void SetLoadingBkg(string fileName)
		{
			Textures.RegisterTexture(fileName, out TextureLoadingBkg);
			customLoadScreen = true;
		}

		//
		// DRAW LOADING SCREEN
		//
		/// <summary>Draws on OpenGL canvas the route/train loading screen</summary>
		internal static void DrawLoadingScreen()
		{
			// begin HACK //
			if (!BlendEnabled) {
				GL.Enable(EnableCap.Blend);
				GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
				BlendEnabled = true;
			}
			if (LightingEnabled) {
				GL.Disable(EnableCap.Lighting);
				LightingEnabled = false;
			}
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.PushMatrix();
//			int		blankHeight;
			int		bkgHeight, bkgWidth;
			int		fontHeight	= (int)Fonts.SmallFont.FontSize;
			int		logoBottom;
//			int		versionTop;
			int		halfWidth	= Screen.Width/2;
			bool	bkgLoaded	= TextureLoadingBkg.Height > 0;
			// stretch the background image to fit at least one screen dimension
			double	ratio	= bkgLoaded ? (double)TextureLoadingBkg.Width / (double)TextureLoadingBkg.Height : 1.0;
			if (Screen.Width / ratio > Screen.Height)		// if screen ratio is shorter than bkg...
			{
				bkgHeight	= Screen.Height;				// set height to screen height
				bkgWidth	= (int)(Screen.Height * ratio);	// and scale width proprtionally
			}
			else											// if screen ratio is wider than bkg...
			{
				bkgWidth	= Screen.Width;					// set width to screen width
				bkgHeight	= (int)(Screen.Width / ratio);	// and scale height accordingly
			}
			// draw the background image down from the top screen edge
			DrawRectangle(TextureLoadingBkg, new Point((Screen.Width - bkgWidth) / 2, 0),
				new Size(bkgWidth, bkgHeight), Color128.White);
			// if the route has no custom loading image, add the openBVE logo
			// (the route custom image is lodaded in OldParsers/CsvRwRouteParser.cs)
			if (!customLoadScreen)
			{
				// place the centre of the logo at the golden ratio of the screen height
				int logoTop	= (int)(Screen.Height * 0.381966 - TextureLogo.Height / 2);
				logoBottom	= logoTop + TextureLogo.Height;
				DrawRectangle(TextureLogo,
					new Point((Screen.Width - TextureLogo.Width) / 2, logoTop),
					new Size(TextureLogo.Width, TextureLogo.Height), Color128.White);
			}
			else
				logoBottom	= Screen.Height / 2;
			if (!bkgLoaded)				// if the background texture not yet loaded, do nothing else
				return;
			// take the height remaining below the logo and divide in 3 horiz. parts
			int	blankHeight	= (Screen.Height - logoBottom) / 3;
			int	versionTop	= logoBottom + blankHeight - fontHeight;
			// draw version number and web site URL
			DrawString(Fonts.SmallFont, "Version " + typeof(Renderer).Assembly.GetName().Version,
				new Point(halfWidth, versionTop), TextAlignment.TopMiddle, Color128.White);
			// for the moment, do not show any URL
//			DrawString(Fonts.SmallFont, "https://sites.google.com/site/openbvesim/home",
//				new Point(halfWidth, versionTop + fontHeight+2),
//				TextAlignment.TopMiddle, Color128.White);
			// draw progress message and bar
			int		progressTop		= Screen.Height - blankHeight;
			int		progressWidth	= Screen.Width - progrMargin * 2;
			double	routeProgress	= Math.Max(0.0, Math.Min(1.0, Loading.RouteProgress));
			double	trainProgress	= Math.Max(0.0, Math.Min(1.0, Loading.TrainProgress));
			string	text			= Interface.GetInterfaceString(
				routeProgress < 1.0 ? "loading_loading_route" :
				(trainProgress < 1.0 ? "loading_loading_train" : "message_loading") );
			DrawString(Fonts.SmallFont, text, new Point(halfWidth, progressTop - fontHeight - 6),
				TextAlignment.TopMiddle, Color128.White);
			// sum of route progress and train progress arrives up to 2.0:
			// => 50.0 * to convert to %
			double	percent	= 50.0 * (routeProgress + trainProgress);
			string	percStr	= percent.ToString("0") + "%";
			// progress frame
			DrawRectangle(null, new Point(progrMargin-progrBorder, progressTop-progrBorder),
				new Size(progressWidth+progrBorder*2, fontHeight+6), Color128.White);
			// progress bar
			DrawRectangle(null, new Point(progrMargin, progressTop),
				new Size(progressWidth * (int)percent / 100, fontHeight+4), ColourProgressBar);
			// progress percent
			DrawString(Fonts.SmallFont, percStr, new Point(halfWidth, progressTop),
				TextAlignment.TopMiddle, Color128.Black);
			GL.PopMatrix();             
		}
		
	}
}