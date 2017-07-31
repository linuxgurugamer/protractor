using System;

using System.Linq;
//using System.Collections.Generic;
//using System.Reflection;
using UnityEngine;



namespace Protractor
{
	class MainWindow : ZKeyLib.Window<Protractor>
	{
		private ZKeyLib.Logger							_logger;



		// Sample strings for GUI fields for font metrics
		private string[] colheaders = new string[6] { "", "θ", "Ψ", "Δv", "Closest", "Moon Ω" };
		private string[] colsamples = new string[6] { "XXXXXXXX", "Xy XXXd 00:00:00XX", "00:00:00XX", "0000.0 m/sXX", "000.00 XXXX", "Moon Ω" };
		private int[] colwidths = new int[6] { 70, 120, 63, 71, 100, 71 };



		private GUIContent			settingsContent;
		private GUIContent			helpContent;

		private SettingsWindow		_settingsWindow;
		private HelpWindow			_helpWindow;

		private CelestialBody		focusbody = null,
									drawApproachToBody = null,
									lastknownmainbody = null;

		private GameObject			approach_obj;
		private LineRenderer		approach;
		private PlanetariumCamera	cam;

		private GUIStyle
			boldstyle,
			datastyle,
			databoxstyle,
			datatitlestyle,
			dataclosestyle,
			datainterceptstyle;

		private readonly Protractor	_parent;

		private bool
			psitotime = false,
			thetatotime = false,
			dvtotime = false;




		public MainWindow( Protractor Parent, SettingsWindow settingsWindow, HelpWindow helpWindow )
			: base( "Protractor", 500, 300 )
		{
			_logger = new ZKeyLib.Logger( this );
			_parent = Parent;
			_settingsWindow = settingsWindow;
			_helpWindow = helpWindow;
			UiScale = 1;
			base.Resizable = false;


			var settingstexture = ZKeyLib.TextureHelper.FromResource( "protractor.icons.settings.png", 16, 16 );
			settingsContent = ( settingstexture != null ) ? new GUIContent( settingstexture, "Settings window" ) : new GUIContent( "S", "Settings window" );

			var helptexture = ZKeyLib.TextureHelper.FromResource( "protractor.icons.help.png", 16, 16 );
			helpContent = ( helptexture != null ) ? new GUIContent( helptexture, "Help window" ) : new GUIContent( "?", "Help window" );
		}



		protected override void ConfigureStyles( )
		{
			_logger.Info( "ConfigureStyles" );
			base.ConfigureStyles( );
            boldstyle = new GUIStyle( GUI.skin.label );
            boldstyle.normal.textColor = Color.yellow;
            boldstyle.fontStyle = FontStyle.Bold;
            boldstyle.alignment = TextAnchor.LowerCenter;

            datastyle = new GUIStyle( GUI.skin.label );
            datastyle.alignment = TextAnchor.MiddleLeft;
            datastyle.fontStyle = FontStyle.Normal;

			databoxstyle = new GUIStyle( GUI.skin.box );
            databoxstyle.margin.top = databoxstyle.margin.bottom = -5;
            databoxstyle.border.top = databoxstyle.border.bottom = 0;
            databoxstyle.wordWrap = false;

			datatitlestyle = new GUIStyle( GUI.skin.label );
            datatitlestyle.alignment = TextAnchor.MiddleLeft;
            datatitlestyle.fontStyle = FontStyle.Bold;

            dataclosestyle = new GUIStyle(GUI.skin.label);
            dataclosestyle.alignment = TextAnchor.MiddleLeft;
            dataclosestyle.fontStyle = FontStyle.Bold;

            datainterceptstyle = new GUIStyle(GUI.skin.label);
            datainterceptstyle.alignment = TextAnchor.MiddleLeft;
            datainterceptstyle.fontStyle = FontStyle.BoldAndItalic;


            // Figure out the width of the fields in the GUI with a little font metrics
            // from sample strings that should be as wide as the field can be.
            for( int i = 1; i < colheaders.Length; ++i )
                colwidths[i] = Mathf.CeilToInt( datastyle.CalcSize( new GUIContent( colsamples[ i ] ) ).x );
			_logger.Info( "ConfigureStyles DONE" );
		}



		protected override void DrawWindowContents( int windowID )
		{

			//if( !FlightGlobals.ready ) return;
			if( FlightGlobals.fetch.isActiveAndEnabled )
			{
				if( FlightGlobals.fetch.activeVessel )
				{
					lastknownmainbody = FlightGlobals.fetch.activeVessel.mainBody;
					approach_obj = new GameObject("Line");
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



            Vessel vessel = FlightGlobals.fetch.activeVessel;
            if (vessel.mainBody != lastknownmainbody)
            {
                drawApproachToBody = null;
                _parent.pdata.initialize( );
                lastknownmainbody = vessel.mainBody;
                focusbody = null;
            } //resets bodies, lines and collapse




           

			GUILayout.BeginVertical( );
            printheaders( );
            if( _parent.Config.ShowPlanets )
                printplanetdata( );
            if( _parent.Config.ShowMoons )
                printmoondata( );
			printvesseldata( );
			drawApproach( );
			GUILayout.EndVertical( );


			// Extra title bar buttons
			if( GUI.Button( new Rect( windowPos.width - 48, 4, 20, 20 ), settingsContent, closeButtonStyle ) )
            {
                _settingsWindow.ToggleVisible( );
            }
            if( GUI.Button( new Rect( windowPos.width - 24, 4, 20, 20 ), helpContent, closeButtonStyle ) )
            {
				_helpWindow.ToggleVisible( );
            }
		}



        public void printheaders( )
        {
			string	psi_time_tip_text,
					phase_angle_time_tip_text,
					dv_time_tip_text;

            phase_angle_time_tip_text = "Toggle between angle and ESTIMATED time";
            psi_time_tip_text =			"Toggle between angle and ESTIMATED time";
            dv_time_tip_text =			"Toggle between ΔV and ESTIMATED\nburn time at full thrust";



            // Begin column headers
            GUILayout.BeginHorizontal( ); // "", "θ", "Ψ", "Δv", "Closest", "Moon Ω"

			// First column
			GUILayout.BeginVertical( GUILayout.Width( colwidths[ 0 ] ) );
			GUILayout.Label( colheaders[ 0 ], boldstyle, GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( true ) );
			GUILayout.EndVertical( );

			// θ
			GUILayout.BeginVertical( GUILayout.Width( colwidths[ 1 ] ) );
            GUILayout.Label( new GUIContent( colheaders[ 1 ], phase_angle_time_tip_text ), boldstyle, GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( true ) );
            if( ( Event.current.type == EventType.repaint ) && GUILayoutUtility.GetLastRect( ).Contains( Event.current.mousePosition ) && Input.GetMouseButtonDown( 0 ) )
                thetatotime = !thetatotime;
			GUILayout.EndVertical( );

			// Ψ
			GUILayout.BeginVertical( GUILayout.Width( colwidths[ 2 ] ) );
            GUILayout.Label( new GUIContent( colheaders[ 2 ], psi_time_tip_text ), boldstyle, GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( true ) );
            if( ( Event.current.type == EventType.repaint ) && GUILayoutUtility.GetLastRect( ).Contains( Event.current.mousePosition ) && Input.GetMouseButtonDown( 0 ) )
                psitotime = !psitotime;
			GUILayout.EndVertical( );

			// Δv
			GUILayout.BeginVertical( GUILayout.Width( colwidths[ 3 ] ) );
            GUILayout.Label( new GUIContent( colheaders[ 3 ], dv_time_tip_text ), boldstyle, GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( true ) );
            if( ( Event.current.type == EventType.repaint ) && GUILayoutUtility.GetLastRect( ).Contains( Event.current.mousePosition ) && Input.GetMouseButtonDown( 0 ) )
                dvtotime = !dvtotime;
			GUILayout.EndVertical( );

			// Closest
			GUILayout.BeginVertical( GUILayout.Width( colwidths[ 4 ] ) );
			GUILayout.Label( colheaders[ 4 ], boldstyle, GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( true ) );
			GUILayout.EndVertical( );

			// Moon Ω
			if( !_parent.Config.ShowAdvanced || _parent.pdata.getorbitbodytype( ) != ProtractorData.orbitbodytype.moon )
				GUILayout.Label( new GUIContent( "", "" ), boldstyle );

            // End column headers
            GUILayout.EndHorizontal( );
        }


		
        public void printplanetdata()
        {
			string	body_tip_text,
					line_tip_text;


            body_tip_text =				focusbody == null ? "Click to focus" : "Click to unfocus";
            line_tip_text =				"Click to toggle approach line";


            foreach (CelestialBody planet in _parent.pdata.planets)
            {
                CelestialData planetdata = _parent.pdata.celestials[planet.name];
                if (!(planet.Equals(focusbody)) && (focusbody != null))
                {
                    continue; //focus body defined and it isn't this one
                }
               	switchcolor(planet);
                // Starts a row of planet data
                GUILayout.BeginHorizontal(databoxstyle);
                for (int i = 0; i <= 5; i++)
                {
                    GUILayout.BeginVertical(GUILayout.MinWidth(colwidths[i])); //begin data cell
                    switch (i)
                    {
                    //******printing names******
                    case 0:
                        GUILayout.Label(new GUIContent(planetdata.name, body_tip_text), datatitlestyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

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
                            AddAlarm(FlightGlobals.fetch.activeVessel.mainBody, planet, _parent.Config.PlanetAlarmMargin, Planetarium.GetUniversalTime() + planetdata.theta_time);
                        }

                        break;
                    //******printing psi angles******
                    case 2:
                        if (_parent.pdata.getorbitbodytype() != ProtractorData.orbitbodytype.planet)
                        {
                            GUILayout.Label("----", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        } else {
                            double psidata;
                            string psidisplay;

                            if (_parent.Config.AdjustEjectAngle)
                            {
                                psidata = planetdata.psi_angle_adjusted;
                            } else {
                                psidata = planetdata.psi_angle;
                            }

                            if (psidata == -1)
                            {
                                psidisplay = "0 TMR";
                            } else if (psitotime) {
                                if (_parent.Config.AdjustEjectAngle)
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
                        if (_parent.pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon)
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
                            diststyle = dataclosestyle;
                        }

                        if (distance <= planet.sphereOfInfluence && distance >= 0)
                        {
                            diststyle = datainterceptstyle;
                        }

                        if (planet.Equals(drawApproachToBody))
                        {
                            GUILayout.Label(new GUIContent("*" + ProtractorCalcs.ToSI(distance) + "m" + "*", line_tip_text), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            GUILayout.Label(new GUIContent(ProtractorCalcs.ToSI(distance) + "m", line_tip_text), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
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
                        if (_parent.pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon && _parent.Config.ShowAdvanced)
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
 			string	body_tip_text,
					line_tip_text;


            body_tip_text =				focusbody == null ? "Click to focus" : "Click to unfocus";
			line_tip_text =				"Click to toggle approach line";



            foreach (CelestialBody moon in _parent.pdata.moons)
            {
                CelestialData moondata = _parent.pdata.celestials[moon.name];
                if (!(moon.Equals (focusbody)) && (focusbody != null))
                {
                    continue;
                }
                switchcolor(moon);
                GUILayout.BeginHorizontal(databoxstyle);    //starts row of moon data

                for (int i = 0; i <= 4; i++)
                {
                    GUILayout.BeginVertical(GUILayout.Width(colwidths[i])); //begin data cell
                    switch (i)
                    {
                    //******printing names******
                    case 0:
                        GUILayout.Label(new GUIContent(moondata.name, body_tip_text), datatitlestyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
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
                            AddAlarm(FlightGlobals.fetch.activeVessel.mainBody, moon, _parent.Config.MoonAlarmMargin, Planetarium.GetUniversalTime() + moondata.theta_time);
                        }
                        
                        break;
                    //******eject angles******
                    case 2:
                        if (_parent.pdata.getorbitbodytype() == ProtractorData.orbitbodytype.planet)
                        {
                            GUILayout.Label("----", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        } else {
                            double psidata;
                            string psidisplay;

                            if( _parent.Config.AdjustEjectAngle )
                            {
                                psidata = moondata.psi_angle_adjusted;
                            } else {
                                psidata = moondata.psi_angle;
                            }

                            if (psidata == -1)
                            {
                                psidisplay = "0 TMR";
                            } else if (psitotime) {
                                if( _parent.Config.AdjustEjectAngle )
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
                            diststyle = dataclosestyle;
                        }
                        if (distance <= moon.sphereOfInfluence && distance >= 0)
                        {
                            diststyle = datainterceptstyle;
                        }

                        if (moon.Equals(drawApproachToBody))
                        {
                            GUILayout.Label(new GUIContent("*" + ProtractorCalcs.ToSI(distance) + "m" + "*", line_tip_text), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            GUILayout.Label(new GUIContent(ProtractorCalcs.ToSI(distance) + "m", line_tip_text), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
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



        public void switchcolor( CelestialBody body )
        {
			Color col;
			col = ( body.orbitDriver.Renderer.orbitColor * 2 ).A( 1 );

            datastyle.normal.textColor = col;
            datatitlestyle.normal.textColor = col;
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







        public void printvesseldata()
        {
            Vessel vessel = FlightGlobals.fetch.activeVessel;
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    _parent.Config.AdjustEjectAngle = GUILayout.Toggle(_parent.Config.AdjustEjectAngle, "Adjust Ψ", new GUIStyle(GUI.skin.button));
                    GUILayout.EndVertical();
                }

                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    _parent.Config.ShowPlanets = GUILayout.Toggle(_parent.Config.ShowPlanets, "Planets", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    _parent.Config.ShowMoons = GUILayout.Toggle(_parent.Config.ShowMoons, "Moons", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    _parent.Config.ShowDv = GUILayout.Toggle(_parent.Config.ShowDv, "Show dV", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                if (_parent.pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon)
                {
                    GUILayout.BeginVertical(GUILayout.Width(50));
                    {
                        _parent.Config.ShowAdvanced = GUILayout.Toggle(_parent.Config.ShowAdvanced, "Adv", new GUIStyle(GUI.skin.button));
                    }
                    GUILayout.EndVertical();
                }

                GUILayout.FlexibleSpace();

                if (focusbody != null && _parent.pcalcs.getclosestapproach(focusbody) <= focusbody.sphereOfInfluence)
                {
                    Orbit o = _parent.pcalcs.getclosestorbit(focusbody);
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

            if (_parent.pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon && _parent.Config.ShowAdvanced)
            {
                GUILayout.BeginHorizontal(databoxstyle);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label(String.Format("Ejection from " + vessel.mainBody.name + ": {0:0.00}°", (_parent.pcalcs.MoonAngle() - _parent.pcalcs.CurrentEjectAngle(null) + 360) % 360),
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

            if (_parent.Config.ShowDv)
            {
                int w = 80;
                boldstyle.alignment = TextAnchor.MiddleLeft;
                GUILayout.BeginHorizontal(databoxstyle);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(string.Format("Sum Δv: {0:#,#}", _parent.totaldv), boldstyle, GUILayout.ExpandWidth(true));
                            _parent.Config.TrackDv = GUILayout.Toggle(_parent.Config.TrackDv, "Track", new GUIStyle(GUI.skin.button), GUILayout.Width(w));
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(string.Format("Tracked Δv: {0:#,#}", _parent.trackeddv), boldstyle, GUILayout.ExpandWidth(true));
                            if (GUILayout.Button("Reset", new GUIStyle(GUI.skin.button), GUILayout.Width(w)))
                            {
                                _parent.trackeddv = 0;
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
            if (drawApproachToBody != null && MapView.MapIsEnabled && _parent.pdata.closestApproachTime > 0)
            {
                //approach.enabled = true;
                Orbit closeorbit = _parent.pcalcs.getclosestorbit(drawApproachToBody);
                double distance = _parent.pcalcs.getclosestapproach(drawApproachToBody);
                // Only draw when not on intercept course already. Was a bug where
                // we would draw a line to nowhere when intercepting. This is better.
                if (distance > drawApproachToBody.sphereOfInfluence)
                {
                    approach.enabled = true;
                    if (closeorbit.referenceBody == drawApproachToBody)
                    {
                        approach.SetPosition(0, ScaledSpace.LocalToScaledSpace(closeorbit.getTruePositionAtUT(_parent.pdata.closestApproachTime)));
                    }
					else
                    {
                        approach.SetPosition(0, ScaledSpace.LocalToScaledSpace(closeorbit.getPositionAtUT(_parent.pdata.closestApproachTime)));
                    }
                    approach.SetPosition(1, ScaledSpace.LocalToScaledSpace(drawApproachToBody.orbit.getPositionAtUT(_parent.pdata.closestApproachTime)));
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
	}
}


/*



			// Main GUI visibility
			public static bool isVisible = true;

					if ((windowPos.x == 0) && (windowPos.y == 0))//windowPos is used to position the GUI window, lets set it in the center of the screen
					{
						windowPos = new Rect(Screen.width / 2, Screen.height / 2, 10, 10);
					}



/*
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

*/

