//Original author: Enigma
//Development has been continued by: Addle
//Distributed according to GNU General Public License version 3, available at http://www.gnu.org/copyleft/gpl.html. All other rights reserved.
//no warrantees of any kind are made with distribution, including but not limited to warranty of merchantability and warranty for a particular purpose.

/*
 Changes in 2.4.7
 * Added (optional) support for blizzy78's Toolbar.

 Changes in 2.4.6
 * Fixed support for ModuleEngineFX
 * Added support for using Kerbin time (for 0.23.5)

 Changes in 2.4.5
 * added support for ModuleEngineFX
 
 Todo list:
 * fix for disabled engines counting toward dv
 * usable amount for engine modules
 * warp-to-angle button?
 * toggle delta-v and target v?
 * debris/vessel tracking?
 * "lap" timer for dv?
 * closest approach on maneuver nodes
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Protractor {

    public class ProtractorModule : PartModule
    {
        //private ProtractorModule primary = null;
        public static ProtractorModule primary = null;
        private GameObject approach_obj = new GameObject("Line");
        private KSP.IO.PluginConfiguration cfg = KSP.IO.PluginConfiguration.CreateForType<ProtractorModule>();
        private static Texture2D
            protractoriconOFF = new Texture2D(32, 32, TextureFormat.ARGB32, false),
            protractoriconON = new Texture2D(32, 32, TextureFormat.ARGB32, false),
            protractoricon = new Texture2D(30, 30, TextureFormat.ARGB32, false);
        public Dictionary<string, Color> bodycolorlist = new Dictionary<string, Color>();
        private List<CelestialBody>
            planets,
            moons,
            bodyList;
        public CelestialBody
            Sun = Planetarium.fetch.Sun,
            drawApproachToBody = null,
            focusbody = null,
            lastknownmainbody;
        protected Rect
            manualwindowPos,
            settingswindowPos,
            windowPos;
        private Vector2 scrollposition;
        private bool
            phitotime = false,
            thetatotime = false,
            adjustejectangle = false,
            showmanual = true,
            showsettings = false,
            init = false,
            loaded = false,
            showplanets = true,
            showadvanced = false,
            showdv = false,
            trackdv = false,
            showmoons = true;
        private GUIStyle
            boldstyle,
            datastyle,
            databox,
            datatitle,
            dataclose,
            dataintercept,
            tooltipstyle,
            iconstyle = new GUIStyle();
        private LineRenderer approach;
        private PlanetariumCamera cam;
        private double
            throttle = 0,
            totaldv = 0,
            maxthrustaccel = 0,
            minthrustaccel = 0,
            trackeddv = 0,
            closestApproachTime = -1;
        public enum orbitbodytype { sun, planet, moon };

        // Sample strings for GUI fields for font metrics
        private string[] colheaders = new string[6] { "", "θ", "Ψ", "Δv", "Closest", "Moon Ω" };
        private string[] colsamples = new string[6] { "XXXXXXXX", "Xy XXXd 00:00:00XX", "00:00:00XX", "0000.0 m/sXX", "000.00 XXXX", "Moon Ω" };
        private int[] colwidths = new int[6] { 70, 120, 63, 71, 100, 71 };

        public ProtractorModule.orbitbodytype orbiting;
        private string
            phi_time,
            bodytip,
            phase_angle_time,
            linetip;
        private string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        // The Id of the currently selected GUI skin
        public int skinId = 0;

        public enum SkinType { Default, KSP, Compact }
        public static GUISkin defaultSkin;
        public static GUISkin compactSkin;

        // Main GUI visibility
        public static bool isVisible = true;

        // Button for Toolbar
        private IButton button;

        // Initializes lists of bodies, planets, and parameters
        internal void initialize()
        {
            if (init)
            {
                return;
            }
            ProtractorModule.primary = this;
            getbodies();
            getplanets();
            getmoons();
            lastknownmainbody = vessel.mainBody;

            getorbitbodytype();

            init = true;

            LoadSkin((SkinType)skinId);

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

            bodycolorlist.Add("Kerbin", Utils.hextorgb("a3ede4"));
            bodycolorlist.Add("Moho", Utils.hextorgb("c46a4b"));
            bodycolorlist.Add("Eve", Utils.hextorgb("d3adff"));
            bodycolorlist.Add("Duna", Utils.hextorgb("edb4a6"));
            bodycolorlist.Add("Jool", Utils.hextorgb("8cf068"));
            bodycolorlist.Add("Vall", Utils.hextorgb("969ebf"));
            bodycolorlist.Add("Laythe", Utils.hextorgb("90caeb"));
            bodycolorlist.Add("Tylo", Utils.hextorgb("fedede"));
            bodycolorlist.Add("Bop", Utils.hextorgb("b8a58b"));
            bodycolorlist.Add("Ike", Utils.hextorgb("aeb5ca"));
            bodycolorlist.Add("Gilly", Utils.hextorgb("b8a58b"));
            bodycolorlist.Add("Mun", Utils.hextorgb("aeb5ca"));
            bodycolorlist.Add("Minmus", Utils.hextorgb("a68db8"));
            bodycolorlist.Add("Eeloo", Utils.hextorgb("929292"));
            bodycolorlist.Add("Dres", Utils.hextorgb("917552"));
            bodycolorlist.Add("Pol", Utils.hextorgb("929d6d"));
            Debug.Log("-------------Protractor Initialized-------------");
        }

        public void loadicons()
        {
            protractoriconON.LoadImage(KSP.IO.File.ReadAllBytes<ProtractorModule>("protractor-on.png"));
            protractoriconOFF.LoadImage(KSP.IO.File.ReadAllBytes<ProtractorModule>("protractor-off.png"));
        }

        public void getmoons()  //gets a list of moons
        {
            // In interstellar space
            if (vessel.mainBody == Planetarium.fetch.Sun)
            {
                if (init)
                {
                    moons.Clear();
                }
                else
                {
                    moons = new List<CelestialBody>();
                }
            }
            // Orbiting a planet
            else if (vessel.mainBody.referenceBody == Planetarium.fetch.Sun)
            {
                moons = new List<CelestialBody>(vessel.mainBody.orbitingBodies);
            }
            // Orbiting a moon, gets all moons in planetary system
            else
            {
                moons = new List<CelestialBody>();
                List<CelestialBody> allmoons = new List<CelestialBody>(vessel.mainBody.referenceBody.orbitingBodies);
                foreach (CelestialBody moon in allmoons)
                {
                    if (vessel.mainBody != moon)
                    {
                        moons.Add(moon);
                    }
                }
            }
        }

        // Gets a list of celestial bodies orbiting the star
        public void getplanets()
        {
            planets = new List<CelestialBody>();
            foreach (CelestialBody body in bodyList)
            {
                if (body.referenceBody == Planetarium.fetch.Sun)
                {
                    if (body == Planetarium.fetch.Sun || body == vessel.mainBody || body == vessel.mainBody.referenceBody)
                    {
                        continue;
                    }
                    planets.Add(body);
                }
            }
        }

        // Gets a list of all celestial bodies in the system
        public void getbodies()
        {
            bodyList = new List<CelestialBody>(FlightGlobals.Bodies);
        }

        public void getorbitbodytype()
        {
            if (vessel.mainBody == Planetarium.fetch.Sun)
            {
                orbiting = orbitbodytype.sun;
            }
            else if (vessel.mainBody.referenceBody != Planetarium.fetch.Sun)
            {
                orbiting = orbitbodytype.moon;
            }
            else
            {
                orbiting = orbitbodytype.planet;
            }
        }

        public void switchcolor(string key)
        {
            Color col;
            if (bodycolorlist.TryGetValue(key, out col))
            {
                datastyle.normal.textColor = col;
                datatitle.normal.textColor = col;
            }
            else
            {
                datastyle.normal.textColor = Color.white;
                datatitle.normal.textColor = Color.white;
            }
        }

        public void settingsGUI(int windowID)
        {
            GUILayout.Label("Current skin: " + (SkinType)skinId );
            if (GUI.skin == null || skinId != 1)
            {
                if (GUILayout.Button("KSP skin"))
                {
                    LoadSkin(SkinType.KSP);
                    skinId = 1;
                }
            }
            if (GUI.skin == null || skinId != 0)
            {
                if (GUILayout.Button("Unity Smoke skin"))
                {
                    LoadSkin(SkinType.Default);
                    skinId = 0;
                }
            }
            if (GUI.skin == null || skinId != 2)
            {
                if (GUILayout.Button("Compact skin"))
                {
                    LoadSkin(SkinType.Compact);
                    skinId = 2;
                }
            }
            GUI.DragWindow();
        }

        public void manualGUI(int windowID)
        {
            GUIStyle wordWrapLabelStyle;
            wordWrapLabelStyle = new GUIStyle();
            wordWrapLabelStyle.wordWrap = true;
            wordWrapLabelStyle.normal.textColor = Color.white;

            scrollposition = GUILayout.BeginScrollView(scrollposition, false, true,
                                                       GUILayout.Width(600), GUILayout.Height(600));
            GUILayout.Label(
                "*****Tips*****\n\n" +
                "- Click on the icon in the bottom left to hide Protractor and its windows\n" +
                "- Click on the number in \"Closest\" column to toggle the closest approach line on the map\n" +
                "- Click on the name of a celestial body in the list to hide other bodies.\n" +
                "- Click on θ in the column headers to toggle between displaying an angle and an \n" +
                " approximate time until the next launch window in the format D.HH:MM.\n" +
                "- When a body is focused and an intercept is detected, your predicted inclination is \n" +
                " displayed below the closest approach.\n\n" +
                "*****Column Key*****\n\n" +
                "θ: Difference in the current angle between bodies and the desired angle between \n" +
                "   them for transfer. Launch your ship when this is 0.\n\n" +
                "Ψ: Point in vessel's current orbit (relative to orbited body's prograde) where you \n" +
                "   should start your ejection burn. Burn when this is 0.\n\n" +
                "Δv: Amount your current velocity needs to be changed to accomplish maneuver.\n\n" +
                "Adjust Ψ: Used to time escape. Toggle to adjust your escape angle based on your\n" +
                "          craft's thrust capabilities.\n\n" +
                "Closest: The closest approach between your craft and the target during one revolution.\n\n" +
                "*****Instructions*****\n\n" +
                "To use this guide, time warp until \"θ\" is 0. IT IS STRONGLY SUGGESTED TO DO \n" +
                "THIS BEFORE LAUNCHING YOUR SHIP. This means the planets are in the right \n" +
                "position relative to each other. \n" +
                "Launch into a low orbit, then time warp until \"Ψ\" is 0. This means your vessel is \n" +
                "in the right place in it's orbit. \n" +
                "For best results, click \"Adjust Ψ\" or start your ejection burn before \n" +
                "the Ψ hits 0 so that it does so when your burn is exactly 2/3 complete. \n" +
                "Burn in direction of vessel's prograde until \"Δv\" is approximately 0.\"\n\n" +
                "This mod assumes your craft is in a 0-inclination, circular orbit. Target is also \n" +
                "assumed to be in 0-inclination, circular orbit. Either a 90° or 270° heading \n" +
                "will work, though launching to 90° is more efficient. \n" +
                "YOU WILL HAVE TO MAKE ADJUSTMENTS TO RENDEZVOUS. THIS MOD ONLY \n" +
                "GETS YOU IN THE NEIGHBORHOOD. To close the gap, try burning at 90° angles \n" +
                "(pro/retro, norm/antinorm, +rad/-rad).\n" +
                "Eventually, you'll know which way to burn to correct an orbit.\n\n" +
                "*****Advanced*****\n\n" +
                "Only works when orbiting a moon. This data is designed to aid in travelling from \n" +
                "a moon, to the moon's planet, and then to another moon. (e.g. Tylo -> Jool -> Kerbin). \n" +
                "Adds \"Moon Ω\" column representing angle from moon to the prograde of the \n" + 
                "planet that moon orbits. \n" +
                "\"Alt\" above represents your target periapsis around the moon's planet where you \n" +
                "should begin your ejection burn. \"Eject from [moon]\" " + "indicates where to \n" +
                "leave your moon's orbit. To use this mode, wait until \"θ\" is 0, \"Moon Ω\" is 0, \n" +
                "and \"Eject from [moon]\" is 0. Burn to create an orbit with an apoapsis at your \n" +
                "current moon and a periapsis at \"Alt\". When you reach periapsis, burn for target \n" +
                "planet.\n",
                wordWrapLabelStyle
                );
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        public void printheaders()
        {
            // Begin column headers
            GUILayout.BeginHorizontal();
            {
                for (int i = 0; i <= 5; i++)
                {
                    if (i == 5 && (!showadvanced || orbiting != orbitbodytype.moon))
                    {
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
                            GUILayout.Label(new GUIContent(colheaders[i], phi_time), boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                            if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                            {
                                phitotime = !phitotime;
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

        public void printplanetdata()
        {
            foreach (CelestialBody planet in planets)
            {
                if (!(planet.Equals(focusbody)) && (focusbody != null))
                {
                    continue; //focus body defined and it isn't this one
                }
                switchcolor(planet.name);
                // Starts a row of planet data
                GUILayout.BeginHorizontal(databox);
                for (int i = 0; i <= 5; i++)
                {
                    GUILayout.BeginVertical(GUILayout.MinWidth(colwidths[i])); //begin data cell
                    switch (i)
                    {
                    //******printing names******
                    case 0:
                        GUILayout.Label(new GUIContent(planet.name, bodytip), datatitle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

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
                        double data;
                        string datastring;
                        if (orbiting == orbitbodytype.moon) //get the data
                        {
                            //Debug.Log("Protractor: Orbiting bodytype moon");
                            data = (CurrentPhase(planet) - OberthDesiredPhase(planet) + 360) % 360;
                        } else
                        {
                            data = (CurrentPhase(planet) - DesiredPhase(planet) + 360) % 360;
                        }
                        //Debug.Log("Protractor: planet: " + planet.name + "; data = " + data);

                        if (thetatotime)    //convert to time or leave as angle
                        {
                            double delta_theta;
                            if (orbiting == orbitbodytype.moon)
                            {
                                // Theta = moon angle relative to dest planet
                                CelestialBody o = vessel.orbit.referenceBody.orbit.referenceBody;
                                delta_theta = (360.0 / o.orbit.period) - (360.0 / planet.orbit.period);
                                //Debug.Log("moon orbit: delta_theta = " + delta_theta +
                                //    "; o.orbital.period = " + o.orbit.period +
                                //    "; planet.orbit.period = " + planet.orbit.period);
                            }
                            else if (orbiting == orbitbodytype.planet)
                            {
                                CelestialBody o = vessel.orbit.referenceBody;
                                delta_theta = (360.0 / o.orbit.period) - (360.0 / planet.orbit.period);
                            }
                            else
                            {
                                delta_theta = (360.0 / vessel.orbit.period) - (360.0 / planet.orbit.period);
                            }
                            
                            if (delta_theta > 0)
                            {
                                //Debug.Log("data: delta_theta > 0 so " + data + " / " + delta_theta + " = " + data / delta_theta);
                                data /= delta_theta;
                            }
                            else
                            {
                                //Debug.Log("data: delta_theta <= 0 so Math.abs(" + (360.0 - data) + " / " + delta_theta + " = " + Math.Abs((360.0 - data) / delta_theta));
                                data = Math.Abs((360.0 - data) / delta_theta);
                            }
                            datastring = TimeToDHMS(data);
                        }
                        else
                        {
                            datastring = String.Format("{0:0.00}°", data);
                        }

                        GUI.skin.label.alignment = TextAnchor.MiddleRight;
                        GUILayout.Label(datastring, datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        GUI.skin.label.alignment = TextAnchor.MiddleLeft;

                        break;
                    //******printing psi angles******
                    case 2:
                        //if (orbiting == orbitbodytype.planet)
                        //{
                        //    if (!adjustejectangle)
                        //    {
                        //        GUILayout.Label(String.Format("{0:0.00}°", (CalculateDesiredEjectionAngle(vessel.mainBody, planet) - CurrentEjectAngle(null) + 360) % 360), datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        //    }
                        //    else if (adjustejectangle && tmr() > 0)
                        //    {
                        //        GUILayout.Label(String.Format("{0:0.00}°", (AdjustEjectAngle(vessel.mainBody, planet) - CurrentEjectAngle(null) + 360) % 360), datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        //    }
                        //    else
                        //    {
                        //        GUILayout.Label("0 TMR", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        //    }
                        //}
                        //else
                        //{
                        //    GUILayout.Label("----", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                        //}

                        if (orbiting != orbitbodytype.planet)
                        {
                            GUILayout.Label("----", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            double phidata;
                            string phidisplay;

                            if (!adjustejectangle)
                            {
                                phidata = (CalculateDesiredEjectionAngle(vessel.mainBody, planet) - CurrentEjectAngle(null) + 360) % 360;
                            }
                            else if (adjustejectangle && tmr() > 0)
                            {
                                phidata = (AdjustEjectAngle(vessel.mainBody, planet) - CurrentEjectAngle(null) + 360) % 360;
                            }
                            else
                            {
                                phidata = -1;
                            }

                            if (phidata == -1)
                            {
                                phidisplay = "0 TMR";
                            }
                            else if (phitotime)
                            {
                                phidata /= (360 / vessel.orbit.period);
                                phidisplay = TimeToDHMS(phidata);
                            }
                            else
                            {
                                phidisplay = String.Format("{0:0.00}°", phidata);
                            }

                            GUILayout.Label(phidisplay, datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        break;
                    //******delta-v******
                    case 3:
                        if (orbiting == orbitbodytype.moon)
                        {
                            GUILayout.Label("----", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            GUILayout.Label(String.Format("{0:0.0} m/s", CalculateDeltaV(planet)), datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        break;
                    //******closest******
                    case 4:
                        double distance = getclosestapproach(planet);
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
                            GUILayout.Label(new GUIContent("*" + ToSI(distance) + "m" + "*", linetip), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            GUILayout.Label(new GUIContent(ToSI(distance) + "m", linetip), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
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
                        if (orbiting == orbitbodytype.moon && showadvanced)
                        {
                            GUILayout.Label(String.Format("{0:0.00}°", (CalculateDesiredEjectionAngle(vessel.mainBody.orbit.referenceBody, planet) + 180 - CurrentEjectAngle(vessel.mainBody) + 360) % 360), datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
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
            foreach (CelestialBody moon in moons)
            {
                if (!(moon.Equals (focusbody)) && (focusbody != null))
                {
                    continue;
                }
                switchcolor(moon.name);
                GUILayout.BeginHorizontal(databox);    //starts row of moon data

                for (int i = 0; i <= 4; i++)
                {
                    GUILayout.BeginVertical(GUILayout.Width(colwidths[i])); //begin data cell
                    switch (i)
                    {
                    //******printing names******
                    case 0:
                        GUILayout.Label(new GUIContent(moon.name, bodytip), datatitle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
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
                    //******printing phase angles******
                    case 1:
                        double data = (CurrentPhase(moon) - DesiredPhase(moon) + 360) % 360;
                        string datastring;
                        if (thetatotime)
                        {
                            double delta_theta;
                            if (vessel.Landed && orbiting == orbitbodytype.planet)  //ship is landed on a planet, use rotation of the planet
                            {
                                //double ves_vel = vessel.horizontalSrfSpeed;
                                double ves_vel = vessel.orbit.getOrbitalSpeedAtPos(vessel.CoM);
                                double radius = vessel.altitude + vessel.mainBody.Radius;
                                double circumference = Math.PI * 2 * radius;
                                double rot = circumference / ves_vel;
                                delta_theta = (360 / rot)-(360 / moon.orbit.period);
                            }
                            else if (orbiting == orbitbodytype.planet)   //ship orbiting a planet, but is not landed
                            {
                                delta_theta = (360 / vessel.orbit.period) - (360 / moon.orbit.period);
                            }
                            else     //ship orbiting a moon
                            {
                                CelestialBody o = vessel.mainBody;
                                delta_theta = (360 / o.orbit.period) - (360 / moon.orbit.period);
                            }
                            
                            //double delta_theta = (360 / vessel.orbit.referenceBody.orbit.period) - (360 / moon.orbit.period);   //FIX THIS - COMPARE ROTATION OF PLANET TO MOON OR USE TWO PLANET MODEL FOR TWO MOONS
                            if (delta_theta > 0)
                            {
                                data/= delta_theta;
                            }
                            else
                            {
                                data = Math.Abs((360 - data) / (delta_theta));
                            }
                            datastring = TimeToDHMS(data);
                        }
                        else
                        {
                            datastring = String.Format("{0:0.00}°", data);
                        }
                        GUI.skin.label.alignment = TextAnchor.MiddleRight;
                        GUILayout.Label(datastring, datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                        
                        break;
                    //******printing eject angleS******
                    case 2:
                        if (orbiting == orbitbodytype.planet) //vessel and moon share planet
                        {
                            GUILayout.Label("----", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else //vessel orbiting moon
                        {
                            double phidata;
                            string phidisplay;

                            if (!adjustejectangle)
                            {
                                phidata = (CalculateDesiredEjectionAngle(vessel.mainBody, moon) - CurrentEjectAngle(null) + 360) % 360;
                            }
                            else if (adjustejectangle && tmr() > 0)
                            {
                                phidata = (AdjustEjectAngle(vessel.mainBody, moon) - CurrentEjectAngle(null) + 360) % 360;
                            }
                            else
                            {
                                phidata = -1;
                            }

                            if(phidata == -1)
                            {
                                phidisplay = "0 TMR";
                            }
                            else if (phitotime)
                            {
                                phidata /= (360/vessel.orbit.period);
                                phidisplay = TimeToDHMS(phidata);
                            }
                            else
                            {
                                phidisplay = String.Format("{0:0.00}°", phidata);
                            }
                            
                            GUILayout.Label(phidisplay, datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        break;
                    case 3:
                        GUILayout.Label(String.Format("{0:0.0} m/s", CalculateDeltaV(moon)), datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        break;
                    case 4:
                        double distance = getclosestapproach(moon);
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
                            GUILayout.Label(new GUIContent("*" + ToSI(distance) + "m" + "*", linetip), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            GUILayout.Label(new GUIContent(ToSI(distance) + "m", linetip), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
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
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    adjustejectangle = GUILayout.Toggle(adjustejectangle, "Adjust Ψ", new GUIStyle(GUI.skin.button));
                    GUILayout.EndVertical();
                }

                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    showplanets = GUILayout.Toggle(showplanets, "Planets", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    showmoons = GUILayout.Toggle(showmoons, "Moons", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    showdv = GUILayout.Toggle(showdv, "Show dV", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                if (orbiting == orbitbodytype.moon)
                {
                    GUILayout.BeginVertical(GUILayout.Width(50));
                    {
                        showadvanced = GUILayout.Toggle(showadvanced, "Adv", new GUIStyle(GUI.skin.button));
                    }
                    GUILayout.EndVertical();
                }

                GUILayout.FlexibleSpace();

                if (focusbody != null && getclosestapproach(focusbody) <= focusbody.sphereOfInfluence)
                {
                    Orbit o = getclosestorbit(focusbody);
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
                    showsettings = GUILayout.Toggle(showsettings, "Settings", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(10));
                {
                    showmanual = GUILayout.Toggle(showmanual, "?", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }

            if (orbiting == orbitbodytype.moon && showadvanced)
            {
                GUILayout.BeginHorizontal(databox);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label(String.Format("Ejection from " + vessel.mainBody.name + ": {0:0.00}°", (MoonAngle() - CurrentEjectAngle(null) + 360) % 360),
                            boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label("Alt above " + vessel.mainBody.orbit.referenceBody.name + ": " +
                            ToSI(1.05 * vessel.mainBody.orbit.referenceBody.maxAtmosphereAltitude) + "m", boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }   //allow advanced menu

            if (showdv)
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
                            trackdv = GUILayout.Toggle(trackdv, "Track", new GUIStyle(GUI.skin.button), GUILayout.Width(w));
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

        // GUI functions
        public void mainGUI(int windowID)
        {
            if (!init)
            {
                initialize();
            }
            if (vessel.mainBody != lastknownmainbody)
            {
                drawApproachToBody = null;
                getmoons();
                getplanets();
                lastknownmainbody = vessel.mainBody;
                focusbody = null;
                getorbitbodytype();
            } //resets bodies, lines and collapse

            bodytip = focusbody == null ? "Click to focus" : "Click to unfocus";
            linetip = "Click to toggle approach line";
            phase_angle_time = "Toggle between angle and ESTIMATED time";
            phi_time = "Toggle between angle and ESTIMTED time";

            printheaders();
            if (showplanets)
            {
                printplanetdata ();
            }
            if (showmoons)
            {
                printmoondata ();
            }
            printvesseldata();
            drawApproach();

            if (GUI.tooltip != "")
            {
                int w = 7 * GUI.tooltip.Length;
                float x = (Event.current.mousePosition.x < windowPos.width / 2) ? Event.current.mousePosition.x + 10 : Event.current.mousePosition.x - 10 - w;
                GUI.Box(new Rect(x, Event.current.mousePosition.y, w, 30), GUI.tooltip, tooltipstyle); //resize
            }
            GUI.DragWindow();
        }

        public void drawApproach()
        {
            if (drawApproachToBody != null && MapView.MapIsEnabled && closestApproachTime > 0)
            {
                approach.enabled = true;
                Orbit closeorbit = getclosestorbit(drawApproachToBody);

                if (closeorbit.referenceBody == drawApproachToBody)
                {
                    approach.SetPosition(0, ScaledSpace.LocalToScaledSpace(closeorbit.getTruePositionAtUT(closestApproachTime)));
                }
                else
                {
                    approach.SetPosition(0, ScaledSpace.LocalToScaledSpace(closeorbit.getPositionAtUT(closestApproachTime)));
                }

                approach.SetPosition(1, ScaledSpace.LocalToScaledSpace(drawApproachToBody.orbit.getPositionAtUT(closestApproachTime)));


                float scale = (float)(0.004 * cam.Distance);
                approach.SetWidth(scale, scale);
            }
            else
            {
                approach.enabled = false;
            }

        }

        public void drawGUI()
        {
            primary = this;

            foreach (Part p in vessel.parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.GetInstanceID() < this.GetInstanceID() && pm is ProtractorModule)
                    {
                        primary = (ProtractorModule)pm;
                    }
                }
            }

            if (vessel == FlightGlobals.ActiveVessel && primary == this)
            {
                GUI.skin = null;
                LoadSkin((SkinType)skinId);

                if (!ToolbarManager.ToolbarAvailable && HighLogic.LoadedSceneIsFlight && !FlightDriver.Pause)
                {
                    if (GUI.Button(new Rect(Screen.width / 6, Screen.height - 34, 32, 32), protractoricon, iconstyle))
                    {
                        if (isVisible == false)
                        {
                            isVisible = true;
                        }
                        else
                        {
                            isVisible = false;
                            approach.enabled = false;
                        }
                    }
                }

                if (isVisible)
                {
                    if (showmanual)
                    {
                        manualwindowPos = GUILayout.Window(555, manualwindowPos, manualGUI,
                            "Protractor v." + version, GUILayout.Width(400), GUILayout.Height(500));
                    }
                    if (showsettings)
                    {
                        settingswindowPos = GUILayout.Window(557, settingswindowPos, settingsGUI,
                            "Settings", GUILayout.Width(200), GUILayout.Height(100));
                    }

                    windowPos = GUILayout.Window(556, windowPos, mainGUI, "Protractor v." + version, GUILayout.Width(1), GUILayout.Height(1)); //367
                }
                else
                {
                    approach.enabled = false;
                }
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            // MMD
            ProtractorModule.primary = this;
            if (state != StartState.Editor)
            {
                loadsettings();
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

                if (ToolbarManager.ToolbarAvailable)
                {
                    Debug.Log("Protractor: Blizzy's toolbar present");

                    button = ToolbarManager.Instance.add("Protractor", "protractorButton");
                    button.TexturePath = "Protractor/icon";
                    button.ToolTip = "Toggle Protractor UI";
                    button.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
                    button.OnClick += (e) =>
                    {
                        isVisible = !isVisible;
                    };
                }
                else
                {
                    Debug.Log("Protractor: Blizzy's toolbar NOT present");
                    loadicons();
                }
                RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));
                vessel.OnFlyByWire += new FlightInputCallback(fly);
            }
        }

        // If using Blizzy78's Toolbar, the button *must* be destroyed OnDestroy
        public void OnDestroy()
        {
            if (button != null)
            {
                button.Destroy();
            }
        }

        public void Update()
        {
            if (isVisible)
            {
                protractoricon = protractoriconON;
            }
            else
            {
                protractoricon = protractoriconOFF;
            }
        }

        public override void OnSave(ConfigNode node)
        {
            savesettings();
            base.OnSave(node);
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }
            if (vessel.situation != Vessel.Situations.PRELAUNCH)
            {
                totaldv += TimeWarp.fixedDeltaTime * thrustAccel();
                if (trackdv)
                {
                    trackeddv += TimeWarp.fixedDeltaTime * thrustAccel();
                }
            }
            base.OnFixedUpdate();
        }

        public void fly(FlightCtrlState s)
        {
            throttle = s.mainThrottle;
        }

        public void savesettings()
        {
            if (!loaded)
            {
                return;
            }
            cfg["config_version"] = version;
            cfg["mainpos"] = windowPos;
            cfg["manualpos"] = manualwindowPos;
            cfg["settingspos"] = settingswindowPos;
            cfg["showadvanced"] = showadvanced;
            cfg["adjustejectangle"] = adjustejectangle;
            cfg["showmanual"] = showmanual;
            cfg["showsettings"] = showsettings;
            cfg["isvisible"] = isVisible;
            cfg["showplanets"] = showplanets;
            cfg["showmoons"] = showmoons;
            cfg["showadvanced"] = showadvanced;
            cfg["showdv"] = showdv;
            cfg["trackdv"] = trackdv;
            cfg["skinid"] = skinId;

            Debug.Log("-------------Saved Protractor Settings-------------");
            cfg.save();
        }

        public void loadsettings()
        {
            Debug.Log("-------------Loading settings...-------------");

            cfg.load();
            Debug.Log("-------------Settings Opened-------------");
            windowPos = cfg.GetValue<Rect>("mainpos", new Rect(0, 0, 0, 0));
            manualwindowPos = cfg.GetValue<Rect>("manualpos", new Rect(0, 0, 0, 0));
            settingswindowPos = cfg.GetValue<Rect>("settingspos", new Rect(0, 0, 0, 0));
            showadvanced = cfg.GetValue<bool>("showadvanced", true);
            adjustejectangle = cfg.GetValue<bool>("adjustejectangle", false);
            showmanual = cfg.GetValue<bool>("showmanual", false);
            showsettings = cfg.GetValue<bool>("showsettings", false);
            isVisible = cfg.GetValue<bool>("isvisible", true);
            showplanets = cfg.GetValue<bool>("showplanets", true);
            showmoons = cfg.GetValue<bool>("showmoons", true);
            showdv = cfg.GetValue<bool>("showdv", true);
            trackdv = cfg.GetValue<bool>("trackdv", true);

            skinId = cfg.GetValue<int>("skinid", (int)ProtractorModule.SkinType.Default);

            loaded = true;  //loaded

            Debug.Log("-------------Loaded Protractor Settings-------------");
        }

        public double getclosestapproach(CelestialBody target)
        {
            Orbit closestorbit = new Orbit();
            closestorbit = getclosestorbit(target);
            if (closestorbit.referenceBody == target)
            {
                closestApproachTime = closestorbit.StartUT + closestorbit.timeToPe;
                return closestorbit.PeA;
            }
            else if (closestorbit.referenceBody == target.referenceBody)
            {
                return mindistance(target, closestorbit.StartUT, closestorbit.period / 10, closestorbit) - target.Radius;
            }
            else
            {
                return mindistance(target, Planetarium.GetUniversalTime(), closestorbit.period / 10, closestorbit) - target.Radius;
            }
        }

        public Orbit getclosestorbit(CelestialBody target)
        {
            Orbit checkorbit = vessel.orbit;
            int orbitcount = 0;

            // Search for target
            while (checkorbit.nextPatch != null && checkorbit.patchEndTransition != Orbit.PatchTransitionType.FINAL &&
                   orbitcount < 3)
            {
                checkorbit = checkorbit.nextPatch;
                orbitcount += 1;
                if (checkorbit.referenceBody == target)
                {
                    return checkorbit;
                }

            }
            checkorbit = vessel.orbit;
            orbitcount = 0;

            // Search for target's referencebody
            while (checkorbit.nextPatch != null && checkorbit.patchEndTransition != Orbit.PatchTransitionType.FINAL &&
                   orbitcount < 3)
            {
                checkorbit = checkorbit.nextPatch;
                orbitcount += 1;
                if (checkorbit.referenceBody == target.orbit.referenceBody)
                {
                    return checkorbit;
                }
            }

            return vessel.orbit;
        }

        public double mindistance(CelestialBody target, double time, double dt, Orbit vesselorbit)
        {
            double[] dist_at_int = new double[11];
            for (int i = 0; i <= 10; i++)
            {
                double step = time + i * dt;
                dist_at_int[i] = (target.getPositionAtUT(step) - vesselorbit.getPositionAtUT(step)).magnitude;
            }
            double mindist = dist_at_int.Min();
            double maxdist = dist_at_int.Max();
            int minindex = Array.IndexOf(dist_at_int, mindist);

            if (drawApproachToBody == target)
            {
                closestApproachTime = time + minindex * dt;
            }

            if ((maxdist - mindist) / maxdist >= 0.00001)
            {
                mindist = mindistance(target, time + ((minindex - 1) * dt), dt / 5, vesselorbit);
            }

            return mindist;
        }

        // Projects two vectors to 2D plane and returns angle between them
        public double Angle2d(Vector3d vector1, Vector3d vector2)
        {
            Vector3d v1 = Vector3d.Project(new Vector3d(vector1.x, 0, vector1.z), vector1);
            Vector3d v2 = Vector3d.Project(new Vector3d(vector2.x, 0, vector2.z), vector2);
            return Vector3d.Angle(v1, v2);
        }

        // Calculates phase angle between the current body and target body
        public double CurrentPhase(CelestialBody target)
        {
            Vector3d vecthis = new Vector3d();
            Vector3d vectarget = new Vector3d();
            vectarget = target.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());

            // Vessel orbits a moon, target is a planet (going down)
            if (target.referenceBody == Sun && orbiting == orbitbodytype.moon)
            {
                vecthis = vessel.mainBody.referenceBody.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());
            }
            //vessel and target orbit same body (going parallel)
            else if (vessel.mainBody == target.referenceBody)
            {
                vecthis = vessel.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()); //going up
            }
            else
            {
                vecthis = vessel.mainBody.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());
            }

            double phase = Angle2d(vecthis, vectarget);

            vecthis = Quaternion.AngleAxis(90, Vector3d.forward) * vecthis;

            if (Angle2d(vecthis, vectarget) > 90)
            {
                phase = 360 - phase;
            }

            return (phase + 360) % 360;
        }

        // Calculates angle between vessel's position and prograde of orbited body
        public double CurrentEjectAngle(CelestialBody check)
        {
            Vector3d vesselvec = new Vector3d();
            vesselvec = check == null ?
                vessel.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()) :
                check.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());

            Vector3d bodyvec = new Vector3d();
            bodyvec = orbiting == orbitbodytype.moon &&
                check != null ?
                  bodyvec = vessel.mainBody.orbit.referenceBody.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()) :
                  vessel.mainBody.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()); //get planet's position relative to universe

            double eject = Angle2d(vesselvec, Quaternion.AngleAxis(90, Vector3d.forward) * bodyvec);

            if (Angle2d(vesselvec, Quaternion.AngleAxis(180, Vector3d.forward) * bodyvec) > Angle2d(vesselvec, bodyvec))
            {
                eject = 360 - eject; //use cross vector to determine up or down
            }

            return eject;
        }

        // Calculates phase angle for rendezvous between two bodies orbiting same parent
        public double DesiredPhase(CelestialBody dest)
        {
            CelestialBody orig = vessel.mainBody;
            double o_alt =
                (vessel.mainBody == dest.orbit.referenceBody) ?
                (vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass())) + dest.referenceBody.Radius : //going "up" from sun -> planet or planet -> moon
                calcmeanalt(orig);  //going lateral from moon -> moon or planet -> planet
            double d_alt = calcmeanalt(dest);
            double u = dest.referenceBody.gravParameter;
            double th = Math.PI * Math.Sqrt(Math.Pow(o_alt + d_alt, 3) / (8 * u));
            double phase = (180 - Math.Sqrt(u / d_alt) * (th / d_alt) * (180 / Math.PI));
            while (phase < 0)
            {
                phase += 360;
            }
            return phase % 360;
        }

        // For going from a moon to another planet exploiting oberth effect
        public double OberthDesiredPhase(CelestialBody dest)
        {
            CelestialBody moon = vessel.mainBody;
            CelestialBody planet = vessel.mainBody.referenceBody;
            double planetalt = calcmeanalt(planet);
            double destalt = calcmeanalt(dest);
            double moonalt = calcmeanalt(moon);
            double usun = dest.referenceBody.gravParameter;
            double uplanet = planet.gravParameter;
            double oberthalt = (planet.Radius + planet.maxAtmosphereAltitude) * 1.05;

            double th1 = Math.PI * Math.Sqrt(Math.Pow(moonalt + oberthalt, 3) / (8 * uplanet));
            double th2 = Math.PI * Math.Sqrt(Math.Pow(planetalt + destalt, 3) / (8 * usun));

            double phase = (180 - Math.Sqrt(usun / destalt) * ((th1 + th2) / destalt) * (180 / Math.PI));
            while (phase < 0)
            {
                phase += 360;
            }
            return phase % 360;
        }

        // Calculates ejection v to reach destination
        public double CalculateDeltaV(CelestialBody dest)
        {
            if (vessel.mainBody == dest.orbit.referenceBody)
            {
                double radius = dest.referenceBody.Radius;
                double u = dest.referenceBody.gravParameter;
                double d_alt = calcmeanalt(dest);
                double alt = (vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass())) + radius;
                double v = Math.Sqrt(u / alt) * (Math.Sqrt((2 * d_alt) / (alt + d_alt)) - 1);
                return Math.Abs((Math.Sqrt(u / alt) + v) - vessel.orbit.GetVel().magnitude);
            }
            else
            {
                CelestialBody orig = vessel.mainBody;
                double d_alt = calcmeanalt(dest);
                double o_radius = orig.Radius;
                double u = orig.referenceBody.gravParameter;
                double o_mu = orig.gravParameter;
                double o_soi = orig.sphereOfInfluence;
                double o_alt = calcmeanalt(orig);
                double exitalt = o_alt + o_soi;
                double v2 = Math.Sqrt(u / exitalt) * (Math.Sqrt((2 * d_alt) / (exitalt + d_alt)) - 1);
                double r = o_radius + (vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass()));
                double v = Math.Sqrt((r * (o_soi * v2 * v2 - 2 * o_mu) + 2 * o_soi * o_mu) / (r * o_soi));
                return Math.Abs(v - vessel.orbit.GetVel().magnitude);
            }
        }

        // Calculates ejection angle to reach destination body from origin body
        public double CalculateDesiredEjectionAngle(CelestialBody orig, CelestialBody dest)
        {
            double o_alt = calcmeanalt(orig);
            double d_alt = calcmeanalt(dest);
            double o_soi = orig.sphereOfInfluence;
            double o_radius = orig.Radius;
            double o_mu = orig.gravParameter;
            double u = orig.referenceBody.gravParameter;
            double exitalt = o_alt + o_soi;
            double v2 = Math.Sqrt(u / exitalt) * (Math.Sqrt((2 * d_alt) / (exitalt + d_alt)) - 1);
            double r = o_radius + (vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass()));
            double v = Math.Sqrt((r * (o_soi * v2 * v2 - 2 * o_mu) + 2 * o_soi * o_mu) / (r * o_soi));
            double eta = Math.Abs(v * v / 2 - o_mu / r);
            double h = r * v;
            double e = Math.Sqrt(1 + ((2 * eta * h * h) / (o_mu * o_mu)));
            double eject = (180 - (Math.Acos(1 / e) * (180 / Math.PI))) % 360;

            eject = o_alt > d_alt ? 180 - eject : 360 - eject;

            return vessel.orbit.inclination > 90 && !(vessel.Landed) ? 360 - eject : eject;
        }

        // Calculates eject angle for moon -> planet in preparation for planet -> planet transfer
        public double MoonAngle()
        {
            CelestialBody orig = vessel.mainBody;
            double o_alt = calcmeanalt(orig);
            double d_alt = (vessel.mainBody.orbit.referenceBody.Radius + vessel.mainBody.orbit.referenceBody.maxAtmosphereAltitude) * 1.05;
            double o_soi = orig.sphereOfInfluence;
            double o_radius = orig.Radius;
            double o_mu = orig.gravParameter;
            double u = orig.referenceBody.gravParameter;
            double exitalt = o_alt + o_soi;
            double v2 = Math.Sqrt(u / exitalt) * (Math.Sqrt((2 * d_alt) / (exitalt + d_alt)) - 1);
            double r = o_radius + (vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass()));
            double v = Math.Sqrt((r * (o_soi * v2 * v2 - 2 * o_mu) + 2 * o_soi * o_mu) / (r * o_soi));
            double eta = Math.Abs(v * v / 2 - o_mu / r);
            double h = r * v;
            double e = Math.Sqrt(1 + ((2 * eta * h * h) / (o_mu * o_mu)));
            double eject = (180 - (Math.Acos(1 / e) * (180 / Math.PI))) % 360;

            eject = o_alt > d_alt ? 180 - eject : 360 - eject;

            return vessel.orbit.inclination > 90 && !(vessel.Landed) ? 360 - eject : eject;
        }

        public double AdjustEjectAngle(CelestialBody orig, CelestialBody dest)
        {
            double ang = CalculateDesiredEjectionAngle(orig, dest);
            double adj = 0;
            double time = (0.2 / 0.3) * burnlength(CalculateDeltaV(dest));
            adj = ang - (360 * (time / vessel.orbit.period));
            adj = adj < 0 ? adj += 360 : adj;
            return adj;
        }

        public double calcmeanalt(CelestialBody body)
        {
            return body.orbit.semiMajorAxis * (1 + body.orbit.eccentricity * body.orbit.eccentricity / 2);
        }

        public double tmr()
        {
            Vector3d forward = vessel.transform.up;
            double totalmass, thrustmax, thrustmin;
            totalmass = thrustmax = thrustmin = 0;
            foreach (Part p in vessel.parts)
            {
                if (p.physicalSignificance != Part.PhysicalSignificance.NONE)
                {
                    totalmass += p.mass;

                    foreach (PartResource pr in p.Resources)
                    {
                        totalmass += pr.amount * PartResourceLibrary.Instance.GetDefinition(pr.info.id).density;
                    }
                }
                if ((p.State == PartStates.ACTIVE) || (Staging.CurrentStage > Staging.lastStage && p.inverseStage == Staging.lastStage))
                {
                    if (p is LiquidEngine && p.RequestFuel(p, 0, Part.getFuelReqId()))
                    {
                        LiquidEngine le = (LiquidEngine)p;
                        double amountforward = Vector3d.Dot(le.transform.rotation * le.thrustVector.normalized, forward);
                        thrustmax += le.maxThrust * amountforward;
                        thrustmin += (le.minThrust * amountforward);
                    }
                    else if (p is LiquidFuelEngine && p.RequestFuel(p, 0, Part.getFuelReqId()))
                    {
                        LiquidFuelEngine lfe = (LiquidFuelEngine)p;
                        double amountforward = Vector3d.Dot(lfe.transform.rotation * lfe.thrustVector.normalized, forward);
                        thrustmax += lfe.maxThrust * amountforward;
                        thrustmin += (lfe.minThrust * amountforward);
                    }
                    else if (p is SolidRocket && !p.ActivatesEvenIfDisconnected)
                    {
                        SolidRocket sr = (SolidRocket)p;
                        double amountforward = Vector3d.Dot(sr.transform.rotation * sr.thrustVector.normalized, forward);
                        thrustmax += sr.thrust * amountforward;
                        thrustmin += (sr.thrust * amountforward);
                    }
                    else if (p is AtmosphericEngine && p.RequestFuel(p, 0, Part.getFuelReqId()))
                    {
                        AtmosphericEngine ae = (AtmosphericEngine)p;
                        double amountforward = Vector3d.Dot(ae.transform.rotation * ae.thrustVector.normalized, forward);
                        thrustmax += (ae.maximumEnginePower * ae.totalEfficiency * amountforward);
                    }
                    else if (p.Modules.Contains("ModuleEngines"))
                    {
                        foreach (PartModule pm in p.Modules)
                        {
                            if (pm is ModuleEngines && pm.isEnabled)
                            {
                                ModuleEngines me = (ModuleEngines)pm;
                                //double amountforward = Vector3d.Dot(me.thrustTransform.rotation * me.thrust, forward);
                                if (!me.getFlameoutState)
                                {
                                    thrustmax += me.maxThrust;
                                    thrustmin += me.minThrust;
                                }
                            }
                        }
                    }
                    else if (p.Modules.Contains("ModuleEnginesFX"))
                    {
                        foreach (PartModule pm in p.Modules)
                        {
                            if (pm is ModuleEnginesFX && pm.isEnabled)
                            {
                                ModuleEnginesFX me = (ModuleEnginesFX)pm;
                                //double amountforward = Vector3d.Dot(me.thrustTransform.rotation * me.thrust, forward);
                                if (!me.getFlameoutState)
                                {
                                    thrustmax += me.maxThrust;
                                    thrustmin += me.minThrust;
                                }
                            }
                        }
                    }
                }
            }
            maxthrustaccel = thrustmax / totalmass;
            minthrustaccel = thrustmin / totalmass;

            return thrustmax / totalmass;
        }
        /*
        public string toSI(double d)
        {
            if (d >= 0)
            {
                int i = 0;
                string[] units = { "m", "km", "Mm", "Gm", "Tm" };
                for (i = 0; i <= 4; i++)
                {
                    if (d > 1000) { d /= 1000; } else { break; }
                }
                string result = string.Format("{0:0.000}", d);
                int p = result.IndexOf(".") + 3;
                return (result.Substring(0, p) + " " + units[i]);
            }
            else
            {
                return "----";
            }
        }
        */

        public double burnlength(double dv)
        {
            return dv / tmr();
        }

        public double thrustAccel()
        {
            tmr();
            return (1.0 - throttle) * minthrustaccel + throttle * maxthrustaccel;
        }

        // More code from MechJeb2 for skin selection. Yay GPL3 licensing!
        public static void CopyDefaultSkin()
        {
            GUI.skin = null;
            defaultSkin = (GUISkin)GameObject.Instantiate(GUI.skin);
        }

        public static void CopyCompactSkin()
        {
            GUI.skin = null;
            compactSkin = (GUISkin)GameObject.Instantiate(GUI.skin);
            GUI.skin.name = "KSP Compact";
            compactSkin.label.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.label.padding = new RectOffset(0, 0, 2, 2);
            compactSkin.button.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.button.padding = new RectOffset(4, 4, 2, 2);
            compactSkin.toggle.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.toggle.padding = new RectOffset(15, 0, 2, 0);
            compactSkin.textField.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.textField.padding = new RectOffset(2, 2, 2, 2);
            compactSkin.textArea.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.textArea.padding = new RectOffset(2, 2, 2, 2);
            compactSkin.window.margin = new RectOffset(0, 0, 0, 0);
            compactSkin.window.padding = new RectOffset(5, 5, 20, 5);
        }

        public static void LoadSkin(SkinType skinType)
        {
            GUI.skin = null;
            switch (skinType)
            {
            case SkinType.Default:
                if (defaultSkin == null) CopyDefaultSkin();
                GUI.skin = defaultSkin;
                break;
            case SkinType.KSP:
                GUI.skin = AssetBase.GetGUISkin("KSP window 2");
                break;
            case SkinType.Compact:
                if (compactSkin == null) CopyCompactSkin();
                GUI.skin = compactSkin;
                break;
            }

        }

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


        public static int HoursPerDay { get { return GameSettings.KERBIN_TIME ? 6 : 24; } }
        public static int DaysPerYear { get { return GameSettings.KERBIN_TIME ? 426 : 365; } }

        public static string TimeToDHMS(double seconds, int decimalPlaces = 0)
        {
            if (double.IsInfinity(seconds) || double.IsNaN(seconds))
            {
                return "Inf";
            }

            string ret = "";
            bool showSecondsDecimals = decimalPlaces > 0;

            try
            {
                string[] postfixes = { "y ", "d ", ":", ":", "" };
                long[] intervals = { DaysPerYear * HoursPerDay * 3600, HoursPerDay * 3600, 3600, 60, 1 };

                if (seconds < 0)
                {
                    ret += "-";
                    seconds *= -1;
                }

                for (int i = 0; i < postfixes.Length; i++)
                {
                    long n = (long)(seconds / intervals[i]);
                    bool first = ret.Length < 2;
                    if (!first || n != 0 || i >= 2 || (i == postfixes.Length - 1 && ret == ""))
                    {
                        if (showSecondsDecimals && seconds < 60 && i == postfixes.Length -1)
                        {
                            ret += seconds.ToString("0." + new string('0', decimalPlaces));
                        }
                        else if (first)
                        {
                            ret += n.ToString();
                        }
                        else
                        {
                            ret += n.ToString("00");
                        }

                        ret += postfixes[i];
                    }
                    seconds -= n * intervals[i];
                }

            }
            catch (Exception)
            {
                return "NaN";
            }
            return ret;
        }

        //Puts numbers into SI format, e.g. 1234 -> "1.234 k", 0.0045678 -> "4.568 m"
        //maxPrecision is the exponent of the smallest place value that will be shown; for example
        //if maxPrecision = -1 and digitsAfterDecimal = 3 then 12.345 will be formatted as "12.3"
        //while 56789 will be formated as "56.789 k"
        public static string ToSI(double d, int maxPrecision = -99, int sigFigs = 4)
        {
            if (d == 0 || double.IsInfinity(d) || double.IsNaN(d))
            {
                return d.ToString() + " ";
            }

            int exponent = (int)Math.Floor(Math.Log10(Math.Abs(d))); //exponent of d if it were expressed in scientific notation

            string[] units = new string[] { "y", "z", "a", "f", "p", "n", "μ", "m", "", "k", "M", "G", "T", "P", "E", "Z", "Y" };
            const int unitIndexOffset = 8; //index of "" in the units array
            int unitIndex = (int)Math.Floor(exponent / 3.0) + unitIndexOffset;
            if (unitIndex < 0)
            {
                unitIndex = 0;
            }
            if (unitIndex >= units.Length)
            {
                unitIndex = units.Length - 1;
            }
            string unit = units[unitIndex];

            int actualExponent = (unitIndex - unitIndexOffset) * 3; //exponent of the unit we will us, e.g. 3 for k.
            d /= Math.Pow(10, actualExponent);

            int digitsAfterDecimal = sigFigs - (int)(Math.Ceiling(Math.Log10(Math.Abs(d))));

            if (digitsAfterDecimal > actualExponent - maxPrecision)
            {
                digitsAfterDecimal = actualExponent - maxPrecision;
            }
            if (digitsAfterDecimal < 0)
            {
                digitsAfterDecimal = 0;
            }

            string ret = d.ToString("F" + digitsAfterDecimal) + " " + unit;

            return ret;
        }

        public List<CelestialBody> Planets { get { return planets; } }
        public List<CelestialBody> Moons { get { return moons; } }
    } // end of class

} //end of namespace
