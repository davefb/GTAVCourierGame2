using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using Font = GTA.Font;

namespace CommunityRaces
{
	public static class GUICourier
	{
		public static Camera MainCamera;
        public static UIMenu MainMenu;
		public static bool IsInMenu;
        public static bool IsInit;


        public static void Init()
        {
            if (!IsInit)
            {
                var res = UIMenu.GetScreenResolutionMantainRatio();
                MainMenu = new UIMenu("", "~w~COURIER JOBS", new Point((int)(res.Width-200), 0));
                IsInit = true;
            }
        }

		public static void DrawSettings()
		{
			// UI DRAWING

            /*
            new UIResText("VEHICLE", new Point(safe.X + 1000, safe.Y + 110), 0.35f, Color.White, Font.ChaletLondon, UIResText.Alignment.Centered).Draw();

			string vehClass = "";
			switch (Function.Call<int>(Hash.GET_VEHICLE_CLASS, previewVehicle.Handle))
			{
				case 0:
					vehClass = "Compacts";
					break;
				case 1:
					vehClass = "Sedans";
					break;
				case 2:
					vehClass = "SUVs";
					break;
				case 3:
					vehClass = "Coupes";
					break;
				case 4:
					vehClass = "Muscle";
					break;
				case 5:
					vehClass = "Sports Classics";
					break;
				case 6:
					vehClass = "Sports";
					break;
				case 7:
					vehClass = "Super";
					break;
				case 8:
					vehClass = "Motorcycle";
					break;
				case 9:
					vehClass = "Offroad";
					break;
				case 10:
					vehClass = "Industrial";
					break;
				case 11:
					vehClass = "Utility";
					break;
				case 12:
					vehClass = "Vans";
					break;
				case 13:
					vehClass = "Bicycle";
					break;
				case 14:
					vehClass = "Boats";
					break;
				case 15:
					vehClass = "Helicopter";
					break;
				case 16:
					vehClass = "Airplane";
					break;
				case 17:
					vehClass = "Service";
					break;
				case 18:
					vehClass = "Emergency";
					break;
				case 19:
					vehClass = "Military";
					break;
				case 20:
					vehClass = "Commercial";
					break;
			}

			new UIResText(vehClass, new Point(Convert.ToInt32(res.Width) - 430 - safe.X, 800 - safe.Y), 1.5f, Color.White, Font.ChaletComprimeCologne, UIResText.Alignment.Left) {DropShadow = true}.Draw();
			new UIResText(previewVehicle.FriendlyName, new Point(Convert.ToInt32(res.Width) - 400 - safe.X, 840 - safe.Y), 1.5f, Color.DodgerBlue, Font.HouseScript, UIResText.Alignment.Left) {DropShadow = true}.Draw();

			// MENU CORRECTIONS
			MainMenu.Subtitle.Position = new Point(safe.X + 200, MainMenu.Subtitle.Position.Y);
			MainMenu.Subtitle.TextAlignment = UIResText.Alignment.Centered;
			*/
		}

	}
}