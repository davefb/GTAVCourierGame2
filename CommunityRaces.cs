﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GTA;
using GTA.Math;
using GTA.Native;
using MapEditor;
using MapEditor.API;
using NativeUI;
using Font = GTA.Font;

namespace CommunityRaces
{
    public class CommunityRaces : Script
    {
		private static readonly Random RandGen = new Random();
		
	    private Blip _nextBlip;
	    private Blip _secondBlip;

	    private bool _isInRace;
	    private int _countdown = -1;
	    private uint _missionStart;
	    private uint _seconds;
	    private int _totalLaps;
	    private float _oldAngle;
	    private UIMenu _quitMenu;
		private Race _previewRace;
	    private Race _currentRace;
	    private Sprite _fadeoutSprite;
		private Vehicle _previewVehicle;
	    private MissionPassedScreen _passed;
		private DateTime _lasttime = DateTime.Now;
		private List<Vector3> _checkpoints = new List<Vector3>();

		private readonly List<Race> _races = new List<Race>();
		private readonly List<Entity> _cleanupBag = new List<Entity>();
		private readonly List<Vehicle> _participants = new List<Vehicle>();
		private readonly List<Rival> _currentRivals = new List<Rival>();
		private readonly List<Rival>  _finishedParticipants = new List<Rival>();
		private readonly List<Tuple<Rival, int>> _rivalCheckpointStatus = new List<Tuple<Rival, int>>();
		private readonly Dictionary<string, dynamic> _raceSettings = new Dictionary<string, dynamic>();

	    private const int Mode = 4;
        private List<Delivery> activeDeliveries = new List<Delivery>();
        private uint lastSpawn;
        protected override void Dispose(bool A_0)
        {
            foreach(Delivery d in activeDeliveries)
            {
                d.blip?.Remove();
            }

        }
        class Delivery
        {
            public bool _isStarted;
            public bool isInit;
            public Vector3 start;
            public Vector3 end;
            public bool isActive;
            public Blip blip;
            public string start_text;
            //public Hash start_street;
            public string end_text;
            //public Hash end_street;

            public void activate()
            {
                blip?.Remove();
                blip = World.CreateBlip(start);
                blip.Color = BlipColor.Green;
                isActive = true;
            }

            public void render()
            {

                if (!this._isStarted)
                {
                    World.DrawMarker(MarkerType.VerticalCylinder, start, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(5f, 5f, 1f), Color.FromArgb(200, 255, 255, 255));

                }
                else {
                    World.DrawMarker(MarkerType.VerticalCylinder, end, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(5f, 5f, 1f), Color.FromArgb(200, 0, 255, 255));
                }


            }

            public void tick()
            {
                if (isActive)
                {
                    render();
                    hit();
                }


            }
            public void hit()
            {
                if (Game.Player.Character.IsInRangeOf(start, 3f) && !_isStarted)
                {
                    UI.Notify("Picked up package");
                    _isStarted = true;
                    blip?.Remove();
                    blip = World.CreateBlip(end);
                    blip.Color = BlipColor.Red;
                }
                if (Game.Player.Character.IsInRangeOf(end, 3f) && _isStarted)
                {
                    _isStarted = false;
                    UI.Notify("Finished!");
                    Game.Player.Money += RandGen.Next(2,5);
                    blip.Remove();
                    isActive = false;
                }
            }

            public static Vector3 randomOnRoad(Vector3 me, float radius)
            {
                float t;
                Vector3 o = new Vector3(me.X, me.Y, me.Z);
                o.X += 2 * radius * ((float)RandGen.NextDouble()) - radius;
                o.Y += 2 * radius * ((float)RandGen.NextDouble()) - radius;

                unsafe
                {
                    Function.Call(Hash.GET_GROUND_Z_FOR_3D_COORD, o.X, o.Y, 10000f, &t);
                    //Function.Call(Hash.GET_CLOSEST_ROAD, 1986.5509f, 3802.58447f, 32.2808743f, 1.0f /*default: 1f*/, 1 /*default: 1*/, &x1, &y1, &za, &x2, &y2, false /*default: False*/);
                    if (t == 0)
                        return new Vector3(0, 0, 0);

                    bool onR = Function.Call<bool>(Hash.IS_POINT_ON_ROAD, o.X, o.Y, t, 0);
                    
                    o.Z = t;
                    
                    if (onR)
                        return o;
                }
                return new Vector3(0, 0, 0);
            }
            public unsafe static Delivery CreateDelivery()
            {
                Delivery d = new Delivery();
                Vector3 me = Game.Player.Character.Position;

                int k = 0;
                while (k < 100) {
                    Vector3 end = randomOnRoad(me, 500);
                    d.end = end;

                    int junk, street;
                    Function.Call(Hash.GET_STREET_NAME_AT_COORD, d.end.X, d.end.Y, d.end.Z, &street, &junk);
                    d.end_text = Function.Call<string>(Hash.GET_STREET_NAME_FROM_HASH_KEY, street);

                    if (end != new Vector3(0, 0, 0) && isPassedStreetFilter(d.end_text))
                        break;

                    k += 1;
                }


                k = 0;
                while (k < 100)
                {
                    Vector3 start = randomOnRoad(me, 500);
                    d.start = start;
                    int junk, street;
                    Function.Call(Hash.GET_STREET_NAME_AT_COORD, d.start.X, d.start.Y, d.start.Z, &street, &junk);
                    d.start_text = Function.Call<string>(Hash.GET_STREET_NAME_FROM_HASH_KEY, street);


                    if (start != new Vector3(0, 0, 0) && isPassedStreetFilter(d.start_text))
                        break;


                    k += 1;
                }


                if (k == 100)
                {
                    return null;
                    UI.Notify("OHFUK");
                }
                else {
                    unsafe
                    {




                    }
                    return d;
                }



            }




        }

        public static bool isPassedStreetFilter(String s)
        {
            if (s == null || s.Length == 0)
                return false;

            if (s.IndexOf("Fwy") != -1)
                return false;

            return true;

                    
        }

        public CommunityRaces()
	    {
		    Tick += OnTick;
		    int racesLoaded = LoadRaces();

			_quitMenu = new UIMenu("", "~r~ARE YOU SURE YOU WANT TO QUIT?", new Point(0, -107));
			var qitem = new UIMenuItem("Quit current race.");
			qitem.Activated += (item, index) =>
			{
				if (_currentRace == null) return;
				_quitMenu.Visible = false;
				Game.FadeScreenOut(500);
				Wait(1000);
				Game.Player.Character.Position = _currentRace.Trigger;
				EndRace();
				Game.FadeScreenIn(500);
			};
            _quitMenu.AddItem(qitem);
			var citem = new UIMenuItem("Cancel.");
			citem.Activated += (item, index) =>
			{
				_quitMenu.Visible = false;
			};
            _quitMenu.AddItem(citem);
			_quitMenu.RefreshIndex();
			_quitMenu.SetBannerType(new UIResRectangle());

		    foreach (Race race in _races)
		    {
			    var tmpBlip = World.CreateBlip(race.Trigger);
			    tmpBlip.IsShortRange = true;
				tmpBlip.Sprite = BlipSprite.Race;
		    }
		    UI.Notify("~b~~h~Community Races~h~~n~~w~Loaded ~b~" + racesLoaded + "~w~ race(s).");

		    if (File.Exists("scripts\\MapEditor.dll"))
				AttachMapEditor();
	    }

		/// <summary>
		/// This method is encapsulated, so if MapEditor.dll is missing, the script won't crash!
		/// </summary>
	    private void AttachMapEditor()
	    {
			var thisMod = new ModListener()
			{
				ButtonString = "Create a Community Race",
				Description = "Create a race for the Community Races mod.",
				Name = "Community Races",
			};
			ModManager.SuscribeMod(thisMod);
			thisMod.OnMapSaved += SaveMap;
		}

	    private int LoadRaces()
	    {
		    int counter = 0;
		    if(!Directory.Exists("scripts\\Races")) return 0;
		    foreach (string path in Directory.GetFiles("scripts\\Races", "*.xml"))
		    {
			    XmlSerializer serializer = new XmlSerializer(typeof(Race));
				StreamReader file = new StreamReader(path);
			    var raceout = (Race)serializer.Deserialize(file);
				file.Close();
				_races.Add(raceout);
			    counter++;
		    }
		    return counter;
	    }

	    private int CalculatePlayerPositionInRace()
	    {
		    int output = 0;
		    int playerCheckpoint = _currentRace.Checkpoints.Length - _checkpoints.Count;

		    int beforeYou = _rivalCheckpointStatus.Count(tuple => tuple.Item2 > playerCheckpoint);
		    output += beforeYou;

		    var samePosAsYou = _rivalCheckpointStatus.Where(tuple => tuple.Item2 == playerCheckpoint);
		    output +=
			    samePosAsYou.Count(
				    tuple =>
					    (_currentRace.Checkpoints[playerCheckpoint] - tuple.Item1.Vehicle.Position).Length() <
					    (_currentRace.Checkpoints[playerCheckpoint] - Game.Player.Character.Position).Length());

		    return output;
	    }

	    private void StartRace(Race race)
	    {
			race = new Race(race);
			Game.FadeScreenOut(500);
			Wait(500);
		    _isInRace = true;
			_currentRace = race;
		    if (_raceSettings["Laps"] > 1)
		    {
			    _totalLaps = race.Checkpoints.Length;
			    List<Vector3> tmpCheckpoints = new List<Vector3>();
			    for (int i = 0; i < _raceSettings["Laps"]; i++)
			    {
				    tmpCheckpoints.AddRange(race.Checkpoints);
			    }
			    _currentRace.Checkpoints = tmpCheckpoints.ToArray();
		    }

		    if (_raceSettings["Weather"] != "Current")
		    {
			    Weather wout;
			    Enum.TryParse(_raceSettings["Weather"], out wout);
			    World.Weather = wout;
		    }

		    
			switch ((string)_raceSettings["TOD"])
			{
				case "Current":
					break;
				case "Sunrise":
					World.CurrentDayTime = new TimeSpan(06, 00, 00);
					break;
				case "Day":
					World.CurrentDayTime = new TimeSpan(16, 00, 00);
					break;
				case "Sunset":
					World.CurrentDayTime = new TimeSpan(20, 00, 00);
					break;
				case "Night":
					World.CurrentDayTime = new TimeSpan(02, 00, 00);
					break;
			}




			List<SpawnPoint> availalbleSpawnPoints = new List<SpawnPoint>(race.SpawnPoints);

		    int spawnId = RandGen.Next(availalbleSpawnPoints.Count);
            var spawn = availalbleSpawnPoints[spawnId];
			availalbleSpawnPoints.RemoveAt(spawnId);


		    var car = _previewVehicle;
		    car.Position = spawn.Position;
		    car.Heading = spawn.Heading;
			Function.Call(Hash.SET_PED_INTO_VEHICLE, Game.Player.Character.Handle, car.Handle, (int)VehicleSeat.Driver);
		    car.IsPersistent = false;
		    car.FreezePosition = true;

		    int spawnlen = _raceSettings["Opponents"].ToString() == "Random" ? RandGen.Next(1, race.SpawnPoints.Length - 1) : Convert.ToInt32(_raceSettings["Opponents"]);

		    for (int i = 0; i < spawnlen; i++)
		    {
			    var spid = RandGen.Next(availalbleSpawnPoints.Count);
			    Model mod = Helpers.RequestModel((int) race.AvailableVehicles[RandGen.Next(race.AvailableVehicles.Length)]);
			    var riv = new Rival(availalbleSpawnPoints[spid].Position, availalbleSpawnPoints[spid].Heading, mod);
				_participants.Add(riv.Vehicle);
				availalbleSpawnPoints.RemoveAt(spid);
				Function.Call(Hash.TASK_VEHICLE_MISSION_COORS_TARGET, riv.Character.Handle, riv.Vehicle.Handle, race.Checkpoints[0].X, race.Checkpoints[0].Y, race.Checkpoints[0].Z, Mode, 200f, Rival.MainDrivingStyle, 5f, 0f, 0);
				_rivalCheckpointStatus.Add(new Tuple<Rival, int>(riv, 0));
			    var tmpblip = riv.Character.AddBlip();
				tmpblip.Color = BlipColor.Blue;
			    tmpblip.IsShortRange = true;
			    tmpblip.Scale = 0.6f;
				_currentRivals.Add(riv);
		    }

		    foreach (SavedProp prop in race.DecorativeProps)
		    {
			    var tmpProp = World.CreateProp(Helpers.RequestModel(prop.Hash), prop.Position, prop.Rotation, false, false);
			    tmpProp.Position = prop.Position;
			    if (prop.Dynamic)
				    tmpProp.FreezePosition = true;
                _cleanupBag.Add(tmpProp);
		    }

		    _checkpoints = race.Checkpoints.ToList();
		    _missionStart = _seconds;
			Game.FadeScreenIn(500);
			Wait(500);
		    _countdown = 5;
			_participants.Add(car);
			_cleanupBag.Add(car);
	    }

	    private void EndRace()
	    {
		    _isInRace = false;
		    _currentRace = null;

			_secondBlip?.Remove();
			_nextBlip?.Remove();
			_checkpoints.Clear();
		    foreach (Entity entity in _cleanupBag)
		    {
			    entity?.Delete();
		    }
			_cleanupBag.Clear();
			_participants.Clear();
		    _countdown = -1;
		    foreach (Rival rival in _currentRivals)
		    {
			    rival.Clean();
		    }
			_currentRivals.Clear();
			_rivalCheckpointStatus.Clear();
			_finishedParticipants.Clear();
	    }
		
	    public void OnTick(object sender, EventArgs e)
	    {
		    if (DateTime.Now.Second != _lasttime.Second)
		    {
			    _seconds++;
			    _lasttime = DateTime.Now;
			    if (_isInRace && _countdown > 0)
			    {
				    var screen = UIMenu.GetScreenResolutionMantainRatio();
				    var w = Convert.ToInt32(screen.Width/2);
				    _countdown--;
					if(_countdown > 3) return;
				    _fadeoutSprite = new Sprite("mpinventory", "in_world_circle", new Point(w - 125, 200), new Size(250, 250), 0f, _countdown == 0 ? Color.FromArgb(49, 235, 126) : Color.FromArgb(241, 247, 57));
					Function.Call(Hash.REQUEST_SCRIPT_AUDIO_BANK, "HUD_MINI_GAME_SOUNDSET", true);
					Function.Call(Hash.PLAY_SOUND_FRONTEND, 0, "CHECKPOINT_NORMAL", "HUD_MINI_GAME_SOUNDSET");
					if (_countdown == 0)
				    {
					    _participants.ForEach(car => car.FreezePosition = false);
					    _missionStart = _seconds;
				    }
			    }
				else if (_isInRace && _countdown == 0)
				{
					_countdown = -1;
				}
		    }

			GUI.MainMenu.ProcessControl();
			GUI.MainMenu.ProcessMouse();
			GUI.MainMenu.Draw();

			_quitMenu.ProcessControl();
			_quitMenu.ProcessMouse();
			_quitMenu.Draw();

			GUI.DrawSettings(_previewRace, _previewVehicle);

			_passed?.Draw();

			if (_countdown > -1 && _countdown <= 3)
		    {
				var screen = UIMenu.GetScreenResolutionMantainRatio();
				var w = Convert.ToInt32(screen.Width / 2);
				new UIResText(_countdown == 0 ? "GO" : _countdown.ToString(), new Point(w, 260), 2f, Color.White, Font.Pricedown, UIResText.Alignment.Centered).Draw();
		    }

		    if (_fadeoutSprite?.Color.A > 5)
		    {
			    _fadeoutSprite.Color = Color.FromArgb(_fadeoutSprite.Color.A - 5, _fadeoutSprite.Color.R, _fadeoutSprite.Color.G,
				    _fadeoutSprite.Color.B);
				_fadeoutSprite.Draw();
		    }


            /* COURIER */
            GUICourier.Init();
            GUICourier.MainMenu.ProcessControl();
            GUICourier.MainMenu.ProcessMouse();
            var safe = UIMenu.GetSafezoneBounds();
            var res = UIMenu.GetScreenResolutionMantainRatio();

            new UIResRectangle(new Point(((int)res.Width-200), 0), new Size(200, 400), Color.FromArgb(200,0,0,0)).Draw();

            GUICourier.MainMenu.Draw();

            if (activeDeliveries.Count == 0 || (_seconds - lastSpawn > 60 && activeDeliveries.Count < 3))
            {
                Delivery d = Delivery.CreateDelivery();
                d.activate();
                activeDeliveries.Add(d);
                lastSpawn = _seconds;
                UI.Notify("New delivery available");
            }

            int pick = 0,drop = 0;
            Vector3 me = Game.Player.Character.Position;
            String posStr = String.Format("{0} {1} {2}", me.X.ToString("F02"), me.Y.ToString("F02"), me.Z.ToString("F02"));


            new UIResText("Pickup", new Point(Convert.ToInt32(res.Width) - 190, 20), .45f, Color.White, Font.HouseScript, UIResText.Alignment.Left) { DropShadow = true }.Draw();
            new UIResText("Dropoff", new Point(Convert.ToInt32(res.Width) - 190, 210), .45f, Color.White, Font.HouseScript, UIResText.Alignment.Left) { DropShadow = true }.Draw();
            foreach (Delivery d in activeDeliveries.GetRange(0,activeDeliveries.Count)) {
                d.tick();
                if (d.isActive == false)
                {
                    activeDeliveries.Remove(d);
                    continue;
                }

 
                if (d._isStarted)
                {
                    float dist = me.DistanceTo(d.end);
                    String posStr2 = String.Format("{0} {1} {2}", d.end.X.ToString("F02"), d.end.Y.ToString("F02"), d.end.Z.ToString("F02"));
                    new UIResText(String.Format("{0} {1}", dist.ToString("F02"), d.end_text), new Point(Convert.ToInt32(res.Width) - 190, 20 * (drop + 2) + 210), .35f, Color.White, Font.ChaletComprimeCologne, UIResText.Alignment.Left) { DropShadow = true }.Draw();

                    drop++;
                }
                else {
                    float dist = me.DistanceTo(d.start);
                    String posStr2 = String.Format("{0} {1} {2}", d.start.X.ToString("F02"), d.start.Y.ToString("F02"), d.start.Z.ToString("F02"));
                    new UIResText(String.Format("{0} {1}", dist.ToString("F02"), d.start_text), new Point(Convert.ToInt32(res.Width) - 190, 20 * (pick + 2)), .35f, Color.White, Font.ChaletComprimeCologne, UIResText.Alignment.Left) { DropShadow = true }.Draw();

                    pick++;
                }
                //new UIResText("WORLD", new Point(Convert.ToInt32(res.Width) - 190, 40), .35f, Color.DodgerBlue, Font.ChaletComprimeCologne, UIResText.Alignment.Left) { DropShadow = true }.Draw();

            }
            /* COURIER */

            if (!_isInRace)
		    {
				if(GUI.IsInMenu) return;
			    foreach (var race in _races)
			    {
				    World.DrawMarker(MarkerType.VerticalCylinder, race.Trigger, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(5f, 5f, 1f), Color.FromArgb(200, 255, 255, 255));
					if(!Game.Player.Character.IsInRangeOf(race.Trigger, 50f)) continue;
				    var tmpSF = new Scaleform(0);
				    tmpSF.Load("PLAYER_NAME_01");
					tmpSF.CallFunction("SET_PLAYER_NAME", race.Name);
				    
					tmpSF.Render3D(race.Trigger + new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, _oldAngle), new Vector3(12, 6, 2));

					var tmpT = new Scaleform(0);
					tmpT.Load("PLAYER_NAME_02");
					tmpT.CallFunction("SET_PLAYER_NAME", "Community Race");

					tmpT.Render3D(race.Trigger + new Vector3(0f, 0f, 1.5f), new Vector3(0f, 0f, _oldAngle), new Vector3(6, 3, 1));

					_oldAngle += 2f;

                    if (!Game.Player.Character.IsInRangeOf(race.Trigger, 5f)) continue;

                    Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "STRING");
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "Press ~INPUT_CONTEXT~ to participate in this Community Race.");
                    Function.Call(Hash._0x238FFE5C7B0498A6, 0, 0, 1, -1);

                    if (Game.IsControlJustPressed(0, GTA.Control.Context))
				    {
					    Game.Player.CanControlCharacter = false;
					    Game.Player.Character.Position = race.Trigger + new Vector3(4f, 0f, -1f);
					    _previewRace = race;
						BuildMenu(race);
					    GUI.MainMenu.Visible = true;
					    GUI.IsInMenu = true;
					    break;
				    }
			    }
				
		    }
		    else if(_isInRace)
		    {
				if(!_raceSettings["Wanted"])
					Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
				//if(Game.Player.Character.IsInVehicle())
				Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)GTA.Control.VehicleExit);
			    if ((Game.IsControlJustPressed(0, GTA.Control.VehicleExit) && Game.Player.Character.IsInVehicle()) || (!Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsGettingIntoAVehicle && Game.IsControlJustPressed(0, GTA.Control.Enter)))
			    {
					_quitMenu.RefreshIndex();
				    _quitMenu.Visible = !_quitMenu.Visible;
			    }
				
			    if (!Convert.ToBoolean(_raceSettings["Traffic"]))
			    {
				    Vehicle[] close = World.GetNearbyVehicles(Game.Player.Character, 10000f);
				    foreach (Vehicle vehicle in close)
				    {
					    if (_currentRivals.Any(riv => riv.Vehicle.Handle == vehicle.Handle) ||
					        Game.Player.Character.IsInVehicle(vehicle)) continue;
						vehicle.GetPedOnSeat(VehicleSeat.Driver)?.Delete();
						vehicle?.Delete();
				    }
			    }

			//    var res = UIMenu.GetScreenResolutionMantainRatio();
			//    var safe = UIMenu.GetSafezoneBounds();
			    const int interval = 45;
			    if (_countdown <= 0)
			    {
				    new UIResText("TIME",new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - (90 + (1*interval))),0.3f, Color.White).Draw();
				    new UIResText(FormatTime((int) unchecked(_seconds - _missionStart)),new Point(Convert.ToInt32(res.Width) - safe.X - 20, Convert.ToInt32(res.Height) - safe.Y - (102 + (1*interval))),0.5f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				    new Sprite("timerbars", "all_black_bg",new Point(Convert.ToInt32(res.Width) - safe.X - 248,Convert.ToInt32(res.Height) - safe.Y - (100 + (1*interval))), new Size(250, 37), 0f, Color.FromArgb(200, 255, 255, 255)).Draw();

					new UIResText("POSITION", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - (90 + (2 * interval))), 0.3f, Color.White).Draw();
					new UIResText((CalculatePlayerPositionInRace() + 1) + "/" + (_currentRivals.Count + 1), new Point(Convert.ToInt32(res.Width) - safe.X - 20, Convert.ToInt32(res.Height) - safe.Y - (102 + (2 * interval))), 0.5f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
					new Sprite("timerbars", "all_black_bg", new Point(Convert.ToInt32(res.Width) - safe.X - 248, Convert.ToInt32(res.Height) - safe.Y - (100 + (2 * interval))), new Size(250, 37), 0f, Color.FromArgb(200, 255, 255, 255)).Draw();

				    if (_raceSettings["Laps"] > 1)
				    {
						int playerCheckpoint = _currentRace.Checkpoints.Length - _checkpoints.Count;
					    int currentLap = Convert.ToInt32(Math.Floor(playerCheckpoint/(decimal)_totalLaps)) + 1;

						new UIResText("LAP",new Point(Convert.ToInt32(res.Width) - safe.X - 180,Convert.ToInt32(res.Height) - safe.Y - (90 + (3*interval))), 0.3f, Color.White).Draw();
					    new UIResText(currentLap + "/" + _raceSettings["Laps"], new Point(Convert.ToInt32(res.Width) - safe.X - 20,Convert.ToInt32(res.Height) - safe.Y - (102 + (3*interval))), 0.5f, Color.White, Font.ChaletLondon,UIResText.Alignment.Right).Draw();
					    new Sprite("timerbars", "all_black_bg",new Point(Convert.ToInt32(res.Width) - safe.X - 248,Convert.ToInt32(res.Height) - safe.Y - (100 + (3*interval))), new Size(250, 37), 0f,Color.FromArgb(200, 255, 255, 255)).Draw();
				    }
			    }


			    for (int i = 0; i < _rivalCheckpointStatus.Count; i++)
			    {
				    Tuple<Rival, int> tuple = _rivalCheckpointStatus[i];
				    if (tuple.Item1.Vehicle.IsInRangeOf(_currentRace.Checkpoints[tuple.Item2], 10f))
				    {
					    tuple.Item1.Character.Task.ClearAll();
					    if (_currentRace.Checkpoints.Length <= tuple.Item2 + 1)
					    {
						    if (!_finishedParticipants.Contains(tuple.Item1))
							    _finishedParticipants.Add(tuple.Item1);
						    tuple.Item1.Vehicle.HandbrakeOn = true;
						    continue;
					    }
					    _rivalCheckpointStatus[i] = new Tuple<Rival, int>(tuple.Item1,tuple.Item2 + 1);
					    Function.Call(Hash.TASK_VEHICLE_MISSION_COORS_TARGET, tuple.Item1.Character.Handle, tuple.Item1.Vehicle.Handle,
						    _currentRace.Checkpoints[tuple.Item2 + 1].X, _currentRace.Checkpoints[tuple.Item2 + 1].Y,
						    _currentRace.Checkpoints[tuple.Item2 + 1].Z, Mode, 200f, Rival.MainDrivingStyle, 5f, 0f, 0); // TODO: Debuggin // old - 6
				    }
			    }


			    World.DrawMarker(MarkerType.VerticalCylinder, _checkpoints[0], new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(10f, 10f, 2f), Color.FromArgb(100, 241, 247, 57));
			    if (_nextBlip == null)
				    _nextBlip = World.CreateBlip(_checkpoints[0]);
			    if (_checkpoints.Count >= 2)
			    {
				    if (_secondBlip == null)
				    {
					    _secondBlip = World.CreateBlip(_checkpoints[1]);
					    _secondBlip.Scale = 0.5f;
						if(_checkpoints.Count == 2)
							_secondBlip.Sprite = BlipSprite.RaceFinish;
				    }
				    Vector3 dir = _checkpoints[1] - _checkpoints[0];
					dir.Normalize();
					World.DrawMarker(MarkerType.ChevronUpx1, _checkpoints[0] + new Vector3(0f, 0f, 2f), dir, new Vector3(60f, 0f, 0f), new Vector3(4f, 4f, 4f), Color.FromArgb(200, 87, 193, 250));
			    }
			    else
			    {
					Vector3 dir = Game.Player.Character.Position - _checkpoints[0];
					dir.Normalize();
				    World.DrawMarker(MarkerType.CheckeredFlagRect, _checkpoints[0] + new Vector3(0f, 0f, 2f), dir, new Vector3(0f, 0f, 0f), new Vector3(4f, 4f, 4f), Color.FromArgb(200, 87, 193, 250));
					_nextBlip.Sprite = BlipSprite.RaceFinish;
			    }

			    if (Game.Player.Character.IsInVehicle() && Game.Player.Character.IsInRangeOf(_checkpoints[0], 10f))
			    {
					Function.Call(Hash.REQUEST_SCRIPT_AUDIO_BANK, "HUD_MINI_GAME_SOUNDSET", true);
					Function.Call(Hash.PLAY_SOUND_FRONTEND, 0, "CHECKPOINT_NORMAL", "HUD_MINI_GAME_SOUNDSET");
					_checkpoints.RemoveAt(0);
					_nextBlip?.Remove();
					_secondBlip?.Remove();
				    _nextBlip = null;
				    _secondBlip = null;
				    if (_checkpoints.Count == 0)
				    {
					    Game.Player.CanControlCharacter = false;
						Function.Call(Hash._START_SCREEN_EFFECT, "HeistCelebPass", 0, true);
						if(Game.Player.Character.IsInVehicle())
							Game.Player.Character.CurrentVehicle.HandbrakeOn = true;
						World.DestroyAllCameras();
					    World.RenderingCamera = World.CreateCamera(GameplayCamera.Position, GameplayCamera.Rotation, 60f);
						Function.Call(Hash.PLAY_SOUND_FRONTEND, 0, "CHECKPOINT_UNDER_THE_BRIDGE", "HUD_MINI_GAME_SOUNDSET");
						int position = _finishedParticipants.Count + 1;
					    int peoplecount = _currentRivals.Count + 1;
					    int score = 100 - ((position - 1)*10);
					    if (score < 0)
						    score = 0;
						_passed = new MissionPassedScreen(_currentRace.Name, score, score > 50 ? score > 90 ? MissionPassedScreen.Medal.Gold : MissionPassedScreen.Medal.Silver : MissionPassedScreen.Medal.Bronze);
						_passed.AddItem("Time Elapsed", FormatTime((int)unchecked(_seconds - _missionStart)), MissionPassedScreen.TickboxState.None);
						_passed.AddItem("Position", position + "/" + peoplecount, position == 1 ? MissionPassedScreen.TickboxState.Tick : MissionPassedScreen.TickboxState.Empty);
					    _passed.OnContinueHit += () =>
					    {
							Game.FadeScreenOut(1000);
							Wait(1000);
							Function.Call(Hash._STOP_SCREEN_EFFECT, "HeistCelebPass");
						    Game.Player.Character.Position = _currentRace.Trigger;
						    Game.Player.CanControlCharacter = true;
						    World.RenderingCamera = null;
							EndRace();
						    _passed = null;
							Game.FadeScreenIn(1500);
					    };
						_passed.Show();
						_isInRace = false;
				    }
			    }
			}
	    }

	    public string FormatTime(int seconds)
	    {
		    var minutes = Convert.ToInt32(Math.Floor(seconds/60f));
		    var secs = seconds%60;
		    return String.Format("{0:00}:{1:00}", minutes, secs);
	    }

	    public void SaveMap(Map map, string filename)
	    {
		    if (!filename.EndsWith(".xml"))
			    filename += ".xml";
		    Race tmpRace = new Race
		    {
			    AvailableVehicles =
				    map.Objects.Where(obj => obj.Type == ObjectTypes.Vehicle)
					    .Select(obj => (VehicleHash) obj.Hash)
					    .Distinct()
					    .ToArray(),
			    Checkpoints = map.Markers.Select(mar => mar.Position).ToArray()
		    };
		    var props = map.Objects.Where(obj => obj.Type == ObjectTypes.Prop).ToArray();
			SavedProp[] tmpProps = new SavedProp[props.Length];
		    for (int i = 0; i < props.Length; i++)
		    {
			    tmpProps[i] = new SavedProp()
			    {
				    Dynamic = props[i].Dynamic,
					Hash = props[i].Hash,
					Position = props[i].Position,
					Rotation = props[i].Rotation,
			    };
		    }
		    tmpRace.DecorativeProps = tmpProps;
		    tmpRace.Trigger = map.Objects.First(obj => obj.Type == ObjectTypes.Ped).Position - new Vector3(0f, 0f, 1f);
		    tmpRace.SpawnPoints = map.Objects.Where(obj => obj.Type == ObjectTypes.Vehicle).Select(obj =>new SpawnPoint() {Position = obj.Position, Heading = obj.Rotation.Z}).ToArray();
		    tmpRace.Name = "Nameless Map";
		    tmpRace.Description = "Cool race!";

			XmlSerializer serializer = new XmlSerializer(typeof(Race));
		    if (!Directory.Exists("scripts\\Races"))
			    Directory.CreateDirectory("scripts\\Races");
			StreamWriter writer = new StreamWriter("scripts\\Races\\" + filename);
			serializer.Serialize(writer, tmpRace);
			writer.Close();
		    UI.Notify("~b~~h~Community Races~h~~n~~w~Race saved as ~h~" + filename + "~h~!");
		    UI.Notify("Don't forget to include your name and the map description in the file!");
	    }
		
		public void BuildMenu(Race race)
		{
			GUI.MainMenu.Clear();
			GUI.MainMenu.SetBannerType(new UIResRectangle());
			_raceSettings.Clear();

			_raceSettings["TOD"] = "Current";
			_raceSettings["Weather"] = "Current";
			_raceSettings["Wanted"] = false;
			_raceSettings["Opponents"] = "Random";
			_raceSettings["Traffic"] = true;
			_raceSettings["Laps"] = 1;

			_previewVehicle = World.CreateVehicle(Helpers.RequestModel((int)race.AvailableVehicles[0]), race.Trigger);
			_previewVehicle.IsPersistent = false;

			List<dynamic> timeList = new List<dynamic> { "Current", "Sunrise", "Day", "Sunset", "Night" };
			var timeItem = new UIMenuListItem("Time of Day", timeList, 0);
			timeItem.OnListChanged += (item, index) =>
			{
				_raceSettings["TOD"] = item.IndexToItem(index);
			};

			var weatherList = new List<dynamic> { "Current" };
			Enum.GetNames(typeof(Weather)).ToList().ForEach(w => weatherList.Add(w));
			var weatherItem = new UIMenuListItem("Weather", weatherList, 0);
			weatherItem.OnListChanged += (item, index) =>
			{
				_raceSettings["Weather"] = item.IndexToItem(index);
			};

			var copItem = new UIMenuCheckboxItem("Wanted Levels", false);
			copItem.CheckboxEvent += (i, checkd) =>
			{
				_raceSettings["Wanted"] = checkd;
			};

			var opponentsList = new List<dynamic> { "Random" };
			Enumerable.Range(1, race.SpawnPoints.Length - 1).ToList().ForEach(n => opponentsList.Add(n));
			var opponentsItem = new UIMenuListItem("Number of Opponents", opponentsList, 0);
			opponentsItem.OnListChanged += (item, index) =>
			{
				_raceSettings["Opponents"] = item.IndexToItem(index);
			};

			var trafficItem = new UIMenuCheckboxItem("Traffic", true);
			trafficItem.CheckboxEvent += (i, checkd) =>
			{
				_raceSettings["Traffic"] = checkd;
			};

			List<dynamic> tmpList = new List<dynamic>();
			race.AvailableVehicles.ToList().ForEach(x => tmpList.Add(x));
			var carItem = new UIMenuListItem("Vehicle", tmpList, 0);
			carItem.OnListChanged += (item, index) =>
			{
				VehicleHash outHash;
				Enum.TryParse(item.IndexToItem(index).ToString(), out outHash);
				var oldC = _previewVehicle.PrimaryColor;
				_previewVehicle?.Delete();
				_previewVehicle = World.CreateVehicle(Helpers.RequestModel((int) outHash), race.Trigger);
				if(_previewVehicle == null) return;
				_previewVehicle.PrimaryColor = oldC;
				_previewVehicle.SecondaryColor = oldC;
				_previewVehicle.IsPersistent = false;
			};
			
			List<dynamic> colors = new List<dynamic>
			{
				VehicleColor.MatteYellow,
                VehicleColor.Orange,
                VehicleColor.MatteRed,
                VehicleColor.HotPink,
                VehicleColor.MattePurple,
                VehicleColor.MatteDarkBlue,
                VehicleColor.Blue,
                VehicleColor.EpsilonBlue,
                VehicleColor.MatteLimeGreen,
                VehicleColor.Green,
            };
			var colorItem = new UIMenuListItem("Color", colors, 0);
			colorItem.OnListChanged += (ite, index) =>
			{
				VehicleColor outHash;
				Enum.TryParse(ite.IndexToItem(index).ToString(), out outHash);
				_previewVehicle.PrimaryColor = outHash;
				_previewVehicle.SecondaryColor = outHash;
			};

			var confimItem = new UIMenuItem("Start Race");
			confimItem.Activated += (item, index) =>
			{
				GUI.MainMenu.Visible = false;
				GUI.IsInMenu = false;
				Game.Player.CanControlCharacter = true;
				World.RenderingCamera = null;
				StartRace(race);
			};

			GUI.MainMenu.OnMenuClose += menu =>
			{
				World.RenderingCamera = null;
				GUI.IsInMenu = false;
				Game.Player.CanControlCharacter = true;
				_previewVehicle?.Delete();
			};

			GUI.MainMenu.AddItem(timeItem);
			GUI.MainMenu.AddItem(weatherItem);
			GUI.MainMenu.AddItem(copItem);
			GUI.MainMenu.AddItem(carItem);
			GUI.MainMenu.AddItem(colorItem);
			GUI.MainMenu.AddItem(opponentsItem);
			GUI.MainMenu.AddItem(trafficItem);
			if (race.LapsAvailable)
			{
				var lapList = new List<dynamic>();
				Enumerable.Range(1, 20).ToList().ForEach(n => lapList.Add(n));
				var lapItem = new UIMenuListItem("Laps", lapList, 0);
				lapItem.OnListChanged += (item, index) =>
				{
					_raceSettings["Laps"] = item.IndexToItem(index);
				};
				GUI.MainMenu.AddItem(lapItem);
			}
			GUI.MainMenu.AddItem(confimItem);
			GUI.MainMenu.RefreshIndex();
		}
	}
}
