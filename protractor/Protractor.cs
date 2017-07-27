//Original author: Enigma
//Development has been continued by: Addle
//Distributed according to GNU General Public License version 3, available at http://www.gnu.org/copyleft/gpl.html. All other rights reserved.
//no warrantees of any kind are made with distribution, including but not limited to warranty of merchantability and warranty for a particular purpose.


using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP.UI.Screens;
using ZKeyButtons;



namespace Protractor {
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class Protractor : MonoBehaviour
    {
		#region FIELDS
			public const string BLIZZY_NAMESPACE =			"ZKeyAerospace";
			private ZKeyLib.Logger							_logger;
			private GameObject								approach_obj;
			private Dictionary<string, Color>				bodycolorlist = new Dictionary<string, Color>();
			public Config Config							{ get; private set; }

			private UnifiedButton							_protractorMainButton;
			private SettingsWindow							_settingsWindow;
			private HelpWindow								_helpWindow;
			private bool									_launcherVisible;	// If the toolbar is shown
			private bool									_UiHidden;			// If the user hit F2 


			private ProtractorData pdata;
			private ProtractorCalcs pcalcs;

			private CelestialBody
				drawApproachToBody = null,
				focusbody = null,
				lastknownmainbody;
			protected Rect windowPos;
//			private Vector2 scrollposition;
			private bool
				psitotime = false,
				thetatotime = false,
				dvtotime = false,
				adjustejectangle = false,
				isGUIInitialized = false;


			
			private GUIStyle
				boldstyle,
				datastyle,
				databox,
				datatitle,
				dataclose,
				dataintercept,
				tooltipstyle;
				//iconstyle;
			private LineRenderer approach;
			private PlanetariumCamera cam;
			private double
				totaldv = 0,
				trackeddv = 0;

			public float t_lastUpdate = 0.0f;

			// Sample strings for GUI fields for font metrics
			private string[] colheaders = new string[6] { "", "θ", "Ψ", "Δv", "Closest", "Moon Ω" };
			private string[] colsamples = new string[6] { "XXXXXXXX", "Xy XXXd 00:00:00XX", "00:00:00XX", "0000.0 m/sXX", "000.00 XXXX", "Moon Ω" };
			private int[] colwidths = new int[6] { 70, 120, 63, 71, 100, 71 };

			private string
				psi_time,
				bodytip,
				phase_angle_time,
				linetip,
				dv_time;





			// Main GUI visibility
			public static bool isVisible = true;


		#endregion



		#region METHODS For Unity
		// Called by Unity once to initialize the class.
		protected void Awake( )
		{
			_logger = new ZKeyLib.Logger( this );
			_logger.Info( "Protractor: Awake" );

				// subscribe event listeners
				GameEvents.onGUIApplicationLauncherReady.Add ( Load );
				GameEvents.onGUIApplicationLauncherDestroyed.Add( Unload );
			_logger.Info( "Protractor: Awake DONE" );
		}



		// Called by Unity once to initialize the class, just before Update is called.
		protected void Start( )
        {
			_logger.Info( "Protractor: Start" );

			// Config
			Config = new Config( );
			Config.Load( );


			// Settings window
			_settingsWindow = new SettingsWindow( this );
			Config.UseBlizzysToolbarChanged += Settings_UseBlizzysToolbarChanged;
			
			// Help window
			_helpWindow = new HelpWindow( this );
			_logger.Info( "Made Windows" );






            pdata = new ProtractorData();
            pcalcs = new ProtractorCalcs(pdata);
            if (!KACWrapper.InitKACWrapper())
            {
                _logger.Info("Protractor: KAC integration initialized.");
            }
            _logger.Info("-------------Protractor Initialized-------------");


			DontDestroyOnLoad( this );
			_logger.Info( "Protractor: Start DONE" );
        }



		// Called by Unity when the application is destroyed.
		protected void OnApplicationQuit( )
		{
		}



		// Called by Unity when this instance is destroyed.
        // If using Blizzy78's Toolbar, the button *must* be destroyed OnDestroy
		protected void OnDestroy( )
		{
//			savesettings( );
			GameEvents.onGUIApplicationLauncherReady.Remove( Load );
			GameEvents.onGUIApplicationLauncherDestroyed.Remove( Unload );
		}




		// Called by Unity once per frame.
		protected void Update( )
		{
		}



        protected void FixedUpdate()
        {
            if( !HighLogic.LoadedSceneIsFlight )
                return;
			if( !FlightGlobals.ready )
				return;

            Vessel vessel = FlightGlobals.fetch.activeVessel;
            if( vessel == FlightGlobals.ActiveVessel )
            {
                if( vessel.situation != Vessel.Situations.PRELAUNCH )
                {
                    totaldv += TimeWarp.fixedDeltaTime * pcalcs.thrustAccel();
                    if( Config.TrackDv )
                    {
                        trackeddv += TimeWarp.fixedDeltaTime * pcalcs.thrustAccel();
                    }
                }

                // Only recalculate data at fixed intervals (very roughly speaking)
                t_lastUpdate += Time.deltaTime;
                if (t_lastUpdate > Config.UpdateInterval)
                {
                    t_lastUpdate = 0.0f;
                    // No need to update if not showing the GUI
                    if (isVisible)
                    {
                        pcalcs.update(pdata.celestials);
                    }
                }
            }
			_logger.Info( "Protractor: FixedUpdate DONE" );
        }



		// Called by Unity to draw the GUI - can be called many times per frame.
		protected void OnGUI( )
		{
			drawGUI( );
		}
		#endregion




		#region METHODS Unity Event Callbacks
		// Initializes the addon if it hasn't already been loaded.
		// Callback from onGUIApplicationLauncherReady
        private void Load( )
        {
			_logger.Info( "Protractor: Load" );
			_logger.Info( "Adding Buttons" );
			InitButtons( );
			_logger.Info( "Buttons Added" );

			_launcherVisible = true;
			ApplicationLauncher.Instance.AddOnShowCallback( Launcher_Show );
			ApplicationLauncher.Instance.AddOnHideCallback( Launcher_Hide );



			//if( !FlightGlobals.ready ) return;
			if( FlightGlobals.fetch.isActiveAndEnabled )
			{
				if( FlightGlobals.fetch.activeVessel )
				{
					lastknownmainbody = FlightGlobals.fetch.activeVessel.mainBody;
					approach_obj = new GameObject("Line");
					if ((windowPos.x == 0) && (windowPos.y == 0))//windowPos is used to position the GUI window, lets set it in the center of the screen
					{
						windowPos = new Rect(Screen.width / 2, Screen.height / 2, 10, 10);
					}

					approach_obj.layer = 9;
					cam = (PlanetariumCamera)GameObject.FindObjectOfType(typeof(PlanetariumCamera));

					approach = approach_obj.AddComponent<LineRenderer>();
					approach.transform.parent = null;
					approach.enabled = false;
					approach.SetColors(Color.green, Color.green);
					approach.useWorldSpace = true;
					approach.SetVertexCount(2);
					approach.SetWidth(10, 10);  //was 15, 5

					approach.material = ((MapView)GameObject.FindObjectOfType(typeof(MapView))).orbitLinesMaterial;
				}			
			}







			_logger.Info( "Protractor: Load DONE" );
        }



		private void Unload( )
		{
			_logger.Info( "Protractor: Unload" );
			_logger.Info( "Removing Buttons" );
			RemoveButtons( );
			_logger.Info( "Removing Callbacks" );

			ApplicationLauncher.Instance.RemoveOnShowCallback( Launcher_Show );
			ApplicationLauncher.Instance.RemoveOnHideCallback( Launcher_Hide );
			_launcherVisible = false;
			_logger.Info( "Protractor: Unload DONE" );
		}



		// F2 support
		void OnHideUI( )
		{
			_UiHidden = true;
		}
		void OnShowUI( )
		{
			_UiHidden = false;
		}



		// Called when the KSP toolbar is shown.
		private void Launcher_Show( )
		{
//			if( !_active )
//				return;

//			_logger.Trace("Launcher_Show");
			_launcherVisible = true;
		}



		// Called when the KSP toolbar is hidden.
		private void Launcher_Hide( )
		{
//			if( !_active )
//				return;
//			_logger.Trace( "Launcher_Hide" );
			_launcherVisible = false;
		}
		#endregion





		
 









        private void initGUI()
        {
            boldstyle = new GUIStyle(GUI.skin.label);
            boldstyle.normal.textColor = Color.yellow;
            boldstyle.fontStyle = FontStyle.Bold;
            boldstyle.alignment = TextAnchor.LowerCenter;

            datastyle = new GUIStyle(GUI.skin.label);
            datastyle.alignment = TextAnchor.MiddleLeft;
            datastyle.fontStyle = FontStyle.Normal;

            tooltipstyle = new GUIStyle(GUI.skin.box);
            tooltipstyle.normal.background = GUI.skin.window.normal.background;
            tooltipstyle.fontStyle = FontStyle.Bold;
            tooltipstyle.normal.textColor = Color.yellow;

            databox = new GUIStyle(GUI.skin.box);
            databox.margin.top = databox.margin.bottom = -5;
            databox.border.top = databox.border.bottom = 0;
            databox.wordWrap = false;

            datatitle = new GUIStyle(GUI.skin.label);
            datatitle.alignment = TextAnchor.MiddleLeft;
            datatitle.fontStyle = FontStyle.Bold;

            dataclose = new GUIStyle(GUI.skin.label);
            dataclose.alignment = TextAnchor.MiddleLeft;
            dataclose.fontStyle = FontStyle.Bold;

            dataintercept = new GUIStyle(GUI.skin.label);
            dataintercept.alignment = TextAnchor.MiddleLeft;
            dataintercept.fontStyle = FontStyle.BoldAndItalic;

            // Figure out the width of the fields in the GUI with a little font metrics
            // from sample strings that should be as wide as the field can be.
            for (int i = 1; i < colheaders.Length; ++i)
            {
                colwidths[i] = Mathf.CeilToInt(datastyle.CalcSize(new GUIContent(colsamples[i])).x);
            }

            isGUIInitialized = true;
        }




        public void drawGUI( )
        {
			_logger.Info( "Protractor: drawGUI" );
			if( !UiActive( ) ) return;
			if( !isVisible ) return;
_logger.Trace( "drawGUI 1" );
            if( !isGUIInitialized )
                initGUI( );
_logger.Trace( "drawGUI 2" );
			_settingsWindow.DrawWindow( );
_logger.Trace( "drawGUI 3" );
			_helpWindow.DrawWindow( );
_logger.Trace( "drawGUI 4" );






//			if( !FlightGlobals.ready ) return;
			if( !FlightGlobals.fetch.isActiveAndEnabled ) return;
			if( FlightGlobals.fetch.activeVessel == null ) return;


            Vessel vessel = FlightGlobals.fetch.activeVessel;


            if (vessel == FlightGlobals.ActiveVessel)
            {
                if (isVisible)
                {
                    windowPos = GUILayout.Window(556, windowPos, mainGUI, "Protractor", GUILayout.Width(1), GUILayout.Height(1)); //367
                }
                else
                {
                    approach.enabled = false;
                }
            }
			_logger.Info( "Protractor: drawGUI DONE" );
        }




        public void mainGUI(int windowID)
        {
			if( !FlightGlobals.ready ) return;
			if( !FlightGlobals.fetch.isActiveAndEnabled ) return;
			if( !FlightGlobals.fetch.activeVessel ) return;




            Vessel vessel = FlightGlobals.fetch.activeVessel;
            if (vessel.mainBody != lastknownmainbody)
            {
                drawApproachToBody = null;
                pdata.initialize();
                lastknownmainbody = vessel.mainBody;
                focusbody = null;
            } //resets bodies, lines and collapse

            bodytip = focusbody == null ? "Click to focus" : "Click to unfocus";
            linetip = "Click to toggle approach line";
            phase_angle_time = "Toggle between angle and ESTIMATED time";
            psi_time = "Toggle between angle and ESTIMATED time";
            dv_time = "Toggle between ΔV and ESTIMATED\nburn time at full thrust";

            printheaders();
            if( Config.ShowPlanets )
            {
                printplanetdata();
            }
            if( Config.ShowMoons )
            {
                printmoondata();
            }
            printvesseldata();
            drawApproach();

            if (GUI.tooltip != "")
            {
                // How many lines, and what's the longest line in the tooltip text?
                int num_lines = 1;
                int max_width = 0;
                int line_width = 0;
                for (int i = 0; i < GUI.tooltip.Length; ++i)
                {
                    if (GUI.tooltip[i] == '\n')
                    {
                        num_lines++;
                        line_width = 0;
                    }
                    else
                    {
                        line_width++;
                        if (line_width > max_width)
                        {
                            max_width = line_width;
                        }
                    }
                }
                int w = 10 * max_width;
                float x = (Event.current.mousePosition.x < windowPos.width / 2) ? Event.current.mousePosition.x + 10 : Event.current.mousePosition.x - 10 - w;
                GUI.Box(new Rect(x, Event.current.mousePosition.y, w, 30*num_lines), GUI.tooltip, tooltipstyle); //resize
            }
            GUI.DragWindow();
        }


      
        public void switchcolor( CelestialBody body )
        {
			Color col;
			col = ( body.orbitDriver.Renderer.orbitColor * 2 ).A( 1 );

            datastyle.normal.textColor = col;
            datatitle.normal.textColor = col;
		}


        public void printheaders()
        {
            // Begin column headers
            GUILayout.BeginHorizontal();
            {
                for (int i = 0; i <= 5; i++)
                {
                    if (i == 5 && (!Config.ShowAdvanced || pdata.getorbitbodytype() != ProtractorData.orbitbodytype.moon))
                    {
                        GUILayout.Label(new GUIContent("", ""), boldstyle);
                        continue;
                    }
                    GUILayout.BeginVertical(GUILayout.Width(colwidths[i]));
                    {
                        if (colheaders[i] == "θ")
                        {
                            GUILayout.Label(new GUIContent(colheaders[i], phase_angle_time), boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                            if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                            {
                                thetatotime = !thetatotime;
                            }
                        }
                        else if (colheaders[i] == "Ψ")
                        {
                            GUILayout.Label(new GUIContent(colheaders[i], psi_time), boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                            if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                            {
                                psitotime = !psitotime;
                            }
                        }
                        else if (colheaders[i] == "Δv")
                        {
                            GUILayout.Label(new GUIContent(colheaders[i], dv_time), boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                            if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                            {
                                dvtotime = !dvtotime;
                            }
                            
                        }
                        else
                        {
                            GUILayout.Label(colheaders[i], boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                    }
                    GUILayout.EndVertical();
                }
                // End column headers
                GUILayout.EndHorizontal();
            }
        }

        // margin: Time before 'ut' we wish to trigger
        // ut: Time of event. Time of alarm will be ut - margin.
        public void AddAlarm(CelestialBody origin, CelestialBody destination, double margin, double ut)
        {
            // Add KAC alarm
            if (KACWrapper.APIReady)
            {
                String tmpID = KACWrapper.KAC.CreateAlarm(KACWrapper.KACAPI.AlarmTypeEnum.TransferModelled,
                    String.Format("{0} -> {1}", origin.name, destination.name),
                    ut - margin);

                KACWrapper.KACAPI.KACAlarm alarmNew = KACWrapper.KAC.Alarms.First(a => a.ID == tmpID);
                alarmNew.Notes = "Alarm created by Protractor.";
                alarmNew.AlarmMargin = margin;
                alarmNew.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
                alarmNew.XferOriginBodyName = origin.name;
                alarmNew.XferTargetBodyName = destination.name;
            }
        }

        public void printplanetdata()
        {
            foreach (CelestialBody planet in pdata.planets)
            {
                CelestialData planetdata = pdata.celestials[planet.name];
                if (!(planet.Equals(focusbody)) && (focusbody != null))
                {
                    continue; //focus body defined and it isn't this one
                }
               	switchcolor(planet);
                // Starts a row of planet data
                GUILayout.BeginHorizontal(databox);
                for (int i = 0; i <= 5; i++)
                {
                    GUILayout.BeginVertical(GUILayout.MinWidth(colwidths[i])); //begin data cell
                    switch (i)
                    {
                    //******printing names******
                    case 0:
                        GUILayout.Label(new GUIContent(planetdata.name, bodytip), datatitle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                        if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                        {
                            if (planet.Equals(focusbody))
                            {
                                focusbody = null;
                            }
                            else
                            {
                                focusbody = planet;
                            }
                        }
                        break;
                    //******printing phase angles******
                    case 1:
                        string datastring;
                        if (thetatotime)    //convert to time or leave as angle
                        {
                            datastring = planetdata.theta_time_str;
                        }
                        else
                        {
                            datastring = String.Format("{0:0.00}°", planetdata.theta_angle);
                        }

                        GUI.skin.label.alignment = TextAnchor.MiddleRight;
                        GUILayout.Label(datastring, datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        GUI.skin.label.alignment = TextAnchor.MiddleLeft;

                        if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                        {
                            // Add KAC alarm on click
                            AddAlarm(FlightGlobals.fetch.activeVessel.mainBody, planet, Config.PlanetAlarmMargin, Planetarium.GetUniversalTime() + planetdata.theta_time);
                        }

                        break;
                    //******printing psi angles******
                    case 2:
                        if (pdata.getorbitbodytype() != ProtractorData.orbitbodytype.planet)
                        {
                            GUILayout.Label("----", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        } else {
                            double psidata;
                            string psidisplay;

                            if (adjustejectangle)
                            {
                                psidata = planetdata.psi_angle_adjusted;
                            } else {
                                psidata = planetdata.psi_angle;
                            }

                            if (psidata == -1)
                            {
                                psidisplay = "0 TMR";
                            } else if (psitotime) {
                                if (adjustejectangle)
                                {
                                    psidisplay = planetdata.psi_time_adjusted_str;
                                } else {
                                    psidisplay = planetdata.psi_time_str;
                                }
                            } else {
                                psidisplay = String.Format("{0:0.00}°", psidata);
                            }

                            GUILayout.Label(psidisplay, datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        break;
                    //******delta-v******
                    case 3:
                        if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon)
                        {
                            GUILayout.Label("----", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            double dv = planetdata.deltaV;
                            if (!dvtotime)
                            {
                                GUILayout.Label(String.Format("{0:0.0} m/s", dv), datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                            }
                            else
                            {
                                GUILayout.Label(planetdata.deltaV_time.ToString("F1") + "s", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                            }
                        }
                        break;
                        //******closest approach******
                    case 4:
                        double distance = planetdata.closest_approach;
                        GUIStyle diststyle = datastyle;
                        if (distance <= 5 * planet.sphereOfInfluence && distance >= 0)
                        {
                            diststyle = dataclose;
                        }

                        if (distance <= planet.sphereOfInfluence && distance >= 0)
                        {
                            diststyle = dataintercept;
                        }

                        if (planet.Equals(drawApproachToBody))
                        {
                            GUILayout.Label(new GUIContent("*" + ProtractorCalcs.ToSI(distance) + "m" + "*", linetip), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            GUILayout.Label(new GUIContent(ProtractorCalcs.ToSI(distance) + "m", linetip), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }

                        if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                        {
                            if (planet.Equals(drawApproachToBody))
                            {
                                drawApproachToBody = null;
                            }
                            else
                            {
                                drawApproachToBody = planet;
                            }
                        }
                        break;
                    case 5:
                        if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon && Config.ShowAdvanced)
                        {
                            GUILayout.Label(String.Format("{0:0.00}°", planetdata.adv_ejection_angle), datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        } else
                        {
                            GUILayout.Label(new GUIContent("", ""), datastyle);
                        }
                        break;
                    }
                    GUILayout.EndVertical();    //end data cell
                }
                GUILayout.EndHorizontal(); //end planet row
            }
        }


        public void printmoondata()
        {
            foreach (CelestialBody moon in pdata.moons)
            {
                CelestialData moondata = pdata.celestials[moon.name];
                if (!(moon.Equals (focusbody)) && (focusbody != null))
                {
                    continue;
                }
                switchcolor(moon);
                GUILayout.BeginHorizontal(databox);    //starts row of moon data

                for (int i = 0; i <= 4; i++)
                {
                    GUILayout.BeginVertical(GUILayout.Width(colwidths[i])); //begin data cell
                    switch (i)
                    {
                    //******printing names******
                    case 0:
                        GUILayout.Label(new GUIContent(moondata.name, bodytip), datatitle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        if ((Event.current.type == EventType.repaint) &&
                             GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) &&
                             Input.GetMouseButtonDown(0))
                        {
                            if (moon.Equals(focusbody))
                            {
                                focusbody = null;
                            }
                            else
                            {
                                focusbody = moon;
                            }
                        }
                        break;
                    //******phase angles******
                    case 1:
                        string datastring;
                        if (thetatotime)    //convert to time or leave as angle
                        {
                            datastring = moondata.theta_time_str;
                        } else
                        {
                            datastring = String.Format("{0:0.00}°", moondata.theta_angle);
                        }

                        GUI.skin.label.alignment = TextAnchor.MiddleRight;
                        GUILayout.Label(datastring, datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        GUI.skin.label.alignment = TextAnchor.MiddleLeft;

                        // Add KAC Alarm on click
                        if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                        {
                            AddAlarm(FlightGlobals.fetch.activeVessel.mainBody, moon, Config.MoonAlarmMargin, Planetarium.GetUniversalTime() + moondata.theta_time);
                        }
                        
                        break;
                    //******eject angles******
                    case 2:
                        if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.planet)
                        {
                            GUILayout.Label("----", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        } else {
                            double psidata;
                            string psidisplay;

                            if (adjustejectangle)
                            {
                                psidata = moondata.psi_angle_adjusted;
                            } else {
                                psidata = moondata.psi_angle;
                            }

                            if (psidata == -1)
                            {
                                psidisplay = "0 TMR";
                            } else if (psitotime) {
                                if (adjustejectangle)
                                {
                                    psidisplay = moondata.psi_time_adjusted_str;
                                } else
                                {
                                    psidisplay = moondata.psi_time_str;
                                }
                            } else {
                                psidisplay = String.Format("{0:0.00}°", psidata);
                            }

                            GUILayout.Label(psidisplay, datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        break;
                        //******delta V******
                    case 3:
                        double dv = moondata.deltaV;
                        if (!dvtotime)
                        {
                            GUILayout.Label(String.Format("{0:0.0} m/s", dv), datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            GUILayout.Label(moondata.deltaV_time.ToString("F1") + "s", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        break;
                        //******closest approach******
                    case 4:
                        double distance = moondata.closest_approach;
                        GUIStyle diststyle = datastyle;

                        if (distance <= 2 * moon.sphereOfInfluence && distance >= 0)
                        {
                            diststyle = dataclose;
                        }
                        if (distance <= moon.sphereOfInfluence && distance >= 0)
                        {
                            diststyle = dataintercept;
                        }

                        if (moon.Equals(drawApproachToBody))
                        {
                            GUILayout.Label(new GUIContent("*" + ProtractorCalcs.ToSI(distance) + "m" + "*", linetip), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            GUILayout.Label(new GUIContent(ProtractorCalcs.ToSI(distance) + "m", linetip), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }

                        if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                        {
                            if (moon.Equals(drawApproachToBody))
                            {
                                drawApproachToBody = null;
                            }
                            else
                            {
                                drawApproachToBody = moon;
                            }
                        }
                        break;
                    }
                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
            }
        }

        public void printvesseldata()
        {
            Vessel vessel = FlightGlobals.fetch.activeVessel;
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    adjustejectangle = GUILayout.Toggle(adjustejectangle, "Adjust Ψ", new GUIStyle(GUI.skin.button));
                    GUILayout.EndVertical();
                }

                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    Config.ShowPlanets = GUILayout.Toggle(Config.ShowPlanets, "Planets", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    Config.ShowMoons = GUILayout.Toggle(Config.ShowMoons, "Moons", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    Config.ShowDv = GUILayout.Toggle(Config.ShowDv, "Show dV", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon)
                {
                    GUILayout.BeginVertical(GUILayout.Width(50));
                    {
                        Config.ShowAdvanced = GUILayout.Toggle(Config.ShowAdvanced, "Adv", new GUIStyle(GUI.skin.button));
                    }
                    GUILayout.EndVertical();
                }

                GUILayout.FlexibleSpace();

                if (focusbody != null && pcalcs.getclosestapproach(focusbody) <= focusbody.sphereOfInfluence)
                {
                    Orbit o = pcalcs.getclosestorbit(focusbody);
                    if (o.referenceBody == focusbody)
                    {
                        GUILayout.Label(String.Format("Inc: {0:0.00}°", o.inclination));
                    }
                    else
                    {
                        GUILayout.Label("Inc: ---");
                    }
                }

                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical(GUILayout.Width(10));
                {
					if (GUILayout.Button( "Settings", new GUIStyle( GUI.skin.button )))
					{
						_settingsWindow.ToggleVisible( );
					}


                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(10));
                {
					if (GUILayout.Button( "?", new GUIStyle( GUI.skin.button )))
					{
						_helpWindow.ToggleVisible( );
					}
                }
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }

            if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon && Config.ShowAdvanced)
            {
                GUILayout.BeginHorizontal(databox);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label(String.Format("Ejection from " + vessel.mainBody.name + ": {0:0.00}°", (pcalcs.MoonAngle() - pcalcs.CurrentEjectAngle(null) + 360) % 360),
                            boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label("Alt above " + vessel.mainBody.orbit.referenceBody.name + ": " +
                            ProtractorCalcs.ToSI(1.05 * vessel.mainBody.orbit.referenceBody.atmosphereDepth) + "m", boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }   //allow advanced menu

            if (Config.ShowDv)
            {
                int w = 80;
                boldstyle.alignment = TextAnchor.MiddleLeft;
                GUILayout.BeginHorizontal(databox);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(string.Format("Sum Δv: {0:#,#}", totaldv), boldstyle, GUILayout.ExpandWidth(true));
                            Config.TrackDv = GUILayout.Toggle(Config.TrackDv, "Track", new GUIStyle(GUI.skin.button), GUILayout.Width(w));
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(string.Format("Tracked Δv: {0:#,#}", trackeddv), boldstyle, GUILayout.ExpandWidth(true));
                            if (GUILayout.Button("Reset", new GUIStyle(GUI.skin.button), GUILayout.Width(w)))
                            {
                                trackeddv = 0;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
                boldstyle.alignment = TextAnchor.LowerCenter;
            }
        }

        public void drawApproach( )
        {
            if (drawApproachToBody != null && MapView.MapIsEnabled && pdata.closestApproachTime > 0)
            {
                //approach.enabled = true;
                Orbit closeorbit = pcalcs.getclosestorbit(drawApproachToBody);
                double distance = pcalcs.getclosestapproach(drawApproachToBody);
                // Only draw when not on intercept course already. Was a bug where
                // we would draw a line to nowhere when intercepting. This is better.
                if (distance > drawApproachToBody.sphereOfInfluence)
                {
                    approach.enabled = true;
                    if (closeorbit.referenceBody == drawApproachToBody)
                    {
                        approach.SetPosition(0, ScaledSpace.LocalToScaledSpace(closeorbit.getTruePositionAtUT(pdata.closestApproachTime)));
                    }
					else
                    {
                        approach.SetPosition(0, ScaledSpace.LocalToScaledSpace(closeorbit.getPositionAtUT(pdata.closestApproachTime)));
                    }
                    approach.SetPosition(1, ScaledSpace.LocalToScaledSpace(drawApproachToBody.orbit.getPositionAtUT(pdata.closestApproachTime)));
                    float scale = (float)(0.004 * cam.Distance);
                    approach.SetWidth(scale, scale);
                }
                else
                {
					if( approach != null )
						approach.enabled = false;
                }
            }
            else
            {
				if( approach != null )
					approach.enabled = false;
            }

        }





		#region METHODS Window helper functions
		// Teeny-tiny helper function.  Are we drawing windows or not
		private bool UiActive( )
		{
			if( ( !_UiHidden ) && /*_active &&*/ _launcherVisible )
				return true;
			return false;
		}
		#endregion







		#region METHODS General Toolbar functions

		// Initializes the toolbar button.
		private void InitButtons( )
		{
			_logger.Info( "InitButtons" );
			RemoveButtons( );
			AddButtons( );
			_logger.Info( "InitButtons Done" );
		}



		// Add the buttons
		private void AddButtons( )
		{
			Texture2D StockTexture;



			_protractorMainButton = new UnifiedButton( );



			if( ZKeyButtons.BlizzysToolbarButton.IsAvailable )
			{
				_protractorMainButton.UseBlizzyIfPossible = true; //Config.UseBlizzysToolbar;




				var texturePath = "protractor/protractor_small.png";
				if( !GameDatabase.Instance.ExistsTexture( texturePath ) )
				{
					var texture = ZKeyLib.TextureHelper.FromResource( "protractor.icons.icon-small.png", 24, 24 );
					var ti = new GameDatabase.TextureInfo( null, texture, false, true, true );
					ti.name = texturePath;
					GameDatabase.Instance.databaseTexture.Add( ti );
				}
				_logger.Info( "Load : Blizzy texture" );



				_protractorMainButton.BlizzyNamespace = BLIZZY_NAMESPACE;
				_protractorMainButton.BlizzyButtonId = "protractorButton";
				_protractorMainButton.BlizzyToolTip = "Protractor";
				_protractorMainButton.BlizzyText = "Protractor Transfer Calculator";
				_protractorMainButton.BlizzyTexturePath = texturePath;
				_protractorMainButton.BlizzyVisibility = new GameScenesVisibility( GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION );
				_logger.Info( "Load : Set Blizzy Stuff" );
			}
			else
				_logger.Info( "NoBlizzy!" );



			StockTexture = ZKeyLib.TextureHelper.FromResource( "protractor.icons.icon.png", 38, 38 );
			if( StockTexture != null )
				_logger.Info( "Load : Stock texture" );
			else
				_logger.Info( "Load : cant load texture" );
			_protractorMainButton.LauncherTexture = StockTexture;
			_protractorMainButton.LauncherVisibility =
				ApplicationLauncher.AppScenes.SPACECENTER |
				ApplicationLauncher.AppScenes.FLIGHT |
				ApplicationLauncher.AppScenes.MAPVIEW |
				ApplicationLauncher.AppScenes.TRACKSTATION;
			_logger.Info( "Load : Set Stock Stuff" );


			_protractorMainButton.ButtonOn += Window_Open;
			_protractorMainButton.ButtonOff += Window_Close;
			_protractorMainButton.Add( );
		}



		private void RemoveButtons( )
		{
			if( _protractorMainButton != null )
			{
				_protractorMainButton.ButtonOn -= Window_Open;
				_protractorMainButton.ButtonOff -= Window_Close;
				_protractorMainButton.Remove( );
				_protractorMainButton = null;
			}
		}
		#endregion






		#region METHODS Checklist window callbacks
		// Registered with the button
		// Called when the toolbar button for the checklist window is toggled on.
		private void Window_Open( object sender, EventArgs e )
		{
//			if( !_active )
//				return;
			isVisible = true;
			_logger.Info( "Window_Open" );
//			UpdateChecklistVisibility( true );
		}



		// Registered with the button
		// Called when the toolbar button for the checklist window is toggled off.
		private void Window_Close( object sender, EventArgs e )
		{
//			if( !_active )
//				return;
			isVisible = false;
			_logger.Info( "Window_Close" );
//			UpdateChecklistVisibility( false );
		}
		#endregion




		// We register this with the settings window.
		// When the blizzy toolbar setting changes this gets popped so we can recreate the buttons
		private void Settings_UseBlizzysToolbarChanged( object sender, EventArgs e )
		{
			InitButtons( );


			// Need to set this
			if( isVisible )
				_protractorMainButton.SetOn( );
			else
				_protractorMainButton.SetOff( );

		}




/*
        // from http://wiki.unity3d.com/index.php?title=PopupList
        public static bool List(Rect position, ref bool showList, ref int listEntry,
            GUIContent buttonContent, string[] list, GUIStyle listStyle)
        {
            return List(position, ref showList, ref listEntry, buttonContent, list, "button", "box", listStyle);
        }
        public static bool List(Rect position, ref bool showList, ref int listEntry, GUIContent buttonContent, string[] list,
            GUIStyle buttonStyle, GUIStyle boxStyle, GUIStyle listStyle)
        {
            int controlID = GUIUtility.GetControlID(865645, FocusType.Passive);
            bool done = false;
            switch (Event.current.GetTypeForControl(controlID))
            {
            case EventType.mouseDown:
                if (position.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = controlID;
                    showList = true;
                }
                break;
            case EventType.mouseUp:
                if (showList)
                {
                    done = true;
                }
                break;
            }
            GUI.Label(position, buttonContent, buttonStyle);
            if (showList)
            {
                Rect listRect = new Rect(position.x, position.y, position.width, list.Length * 20);
                GUI.Box(listRect, "", boxStyle);
                listEntry = GUI.SelectionGrid(listRect, listEntry, list, 1, listStyle);
            }
            if (done)
            {
                showList = false;
            }
            return done;
        }



		*/



    } // end of class

} //end of namespace
