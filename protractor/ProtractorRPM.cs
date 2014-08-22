using System;
using System.Collections.Generic;
using UnityEngine;
using KSP;

namespace Protractor
{
    public class ProtractorRPM : InternalModule
    {
        //globalButtons = button_UP,button_DOWN,button_ENTER,button_ESC,button_HOME,button_RIGHT,button_LEFT,buttonR9,buttonR10
        // On this particular model, R10 is marked prev, R9 marked next.
        [KSPField(isPersistant = false)]
        int btnUp = 0;
        [KSPField(isPersistant = false)]
        int btnDown = 1;
        [KSPField(isPersistant = false)]
        int btnEnter = 2;
        [KSPField(isPersistant = false)]
        int btnEscape = 3;
        //[KSPField(isPersistant = false)]
        //int btnHome = 4;
        [KSPField(isPersistant = false)]
        int btnRight = 5;
        [KSPField(isPersistant = false)]
        int btnLeft = 6;
        //[KSPField(isPersistant = false)]
        //int btnNext = 7;
        //[KSPField(isPersistant = false)]
        //int btnPrev = 8;

        bool thetatotime = true;
        bool phitotime = true;
        bool adjustejectangle = true;

        int activePage = 0;

        int selection = 0;

        public void pageActiveProcessor(bool pageActive, int pageNumber)
        {
            Debug.Log("ProtractorRPM: pageNumber = " + pageNumber);
        }

        public string pageAuthor(int screenWidth, int screenHeight)
        {
            switch (activePage)
            {
            case 0:
                return planetPage(screenWidth, screenHeight);
            case 1:
                return moonPage(screenWidth, screenHeight);
            case 2:
                return moonReturnPage(screenWidth, screenHeight);
            default:
                return planetPage(screenWidth, screenHeight);
            }
        }

        private string getMenu(int screenWidth, int screenHeight)
        {
            string output = "";

            if (selection == 0)
            {
                output += "*";
            }
            if (thetatotime)
            {
                output += "Theta(Time)";
            }
            else
            {
                output += "Theta(Angle)";
            }
            output += "  ";

            if (selection == 1)
            {
                output += "*";
            }
            if (phitotime)
            {
                output += "Phi(Time)";
            } else
            {
                output += "Phi(Angle)";
            }
            output += "  ";

            if (selection == 2)
            {
                output += "*";
            }
            if (adjustejectangle)
            {
                output += "Adj. ejection";
            } else
            {
                output += "No adj. eject";
            }

            output += Environment.NewLine;

            return output;
        }

        public Dictionary<string, string> bodycolorlist = new Dictionary<string, string>();

        private string getBodyColorTag(string name)
        {
            string val;
            if (bodycolorlist.TryGetValue(name, out val))
            {
                return val;
            } else
            {
                return "[#ffffff]";
            }
        }

        //"XXXXXXXX  9y 999d 99:99:99  00:00:00  ",
        //"               99999.9 m/s  99.99 Gm  "
        string[] fieldHeaders = new string[] { "", "Theta", "Phi", "Δv", "Closest" };
        string[] fieldPrototypes = new string[] {"XXXXXXXX  ", "  9y 999d 99:99:99", "  00:00:00",
            "             99999.9 m/s", "  99.99 Gm"};
        int[] fieldWidths = new int[5];

        private string planetPage(int screenWidth, int screenHeight)
        {
            string output = "";
            ProtractorModule pm = ProtractorModule.primary;

            List<CelestialBody> planets = pm.Planets;

            output += "[#ffffff]";
            output += "[#00ff00]      --- Protractor (planets) ---      " + Environment.NewLine +
                Environment.NewLine;
            output += "[#CC2EFA]";
            for (int i = 0; i < fieldHeaders.Length; ++i)
            {
                output += fieldHeaders[i].PadLeft(fieldWidths[i]);
                if (i == 2)
                {
                    output += Environment.NewLine;
                    output += "[#CC2EFA]";
                }
            }
            output += Environment.NewLine;
            output += "[#ffffff]";

            foreach (CelestialBody planet in planets)
            {
                double data;

                if (!(planet.Equals(pm.focusbody)) && (pm.focusbody != null))
                {
                    continue; //focus body defined and it isn't this one
                }

                // NAME
                output += getBodyColorTag(planet.name);
                output += planet.name.PadRight(fieldWidths[0]);
                output += "[#ffffff]";

                // THETA
                if (pm.orbiting == ProtractorModule.orbitbodytype.moon) //get the data
                {
                    data = (pm.CurrentPhase(planet) - pm.OberthDesiredPhase(planet) + 360) % 360;
                }
                else
                {
                    data = (pm.CurrentPhase(planet) - pm.DesiredPhase(planet) + 360) % 360;
                }

                if (thetatotime)    //convert to time or leave as angle
                {
                    double delta_theta;
                    if (pm.orbiting == ProtractorModule.orbitbodytype.moon)
                    {
                        CelestialBody o = vessel.orbit.referenceBody.orbit.referenceBody;
                        delta_theta = (360 / o.orbit.period) - (360 / planet.orbit.period);
                    }
                    else if (pm.orbiting == ProtractorModule.orbitbodytype.planet)
                    {
                        CelestialBody o = vessel.orbit.referenceBody;
                        delta_theta = (360 / o.orbit.period) - (360 / planet.orbit.period);
                    }
                    else
                    {
                        delta_theta = (360 / vessel.orbit.period) - (360 / planet.orbit.period);
                    }

                    if (delta_theta > 0)
                    {
                        data /= delta_theta;
                    }
                    else
                    {
                        data = Math.Abs((360 - data) / (delta_theta));
                    }
                    output += ProtractorModule.TimeToDHMS(data).PadLeft(fieldWidths[1]);
                }
                else
                {
                    output += String.Format("{0:0.00}°", data).PadLeft(fieldWidths[1]);;
                }

                // PHI
                if (pm.orbiting != ProtractorModule.orbitbodytype.planet)
                {
                    output += "----".PadLeft(fieldWidths[2]);;
                }
                else
                {
                    double phidata;
                    string phidisplay;

                    if (!adjustejectangle)
                    {
                        phidata = (pm.CalculateDesiredEjectionAngle(vessel.mainBody, planet) - pm.CurrentEjectAngle(null) + 360) % 360;
                    }
                    else if (adjustejectangle && pm.tmr() > 0)
                    {
                        phidata = (pm.AdjustEjectAngle(vessel.mainBody, planet) - pm.CurrentEjectAngle(null) + 360) % 360;
                    }
                    else
                    {
                        phidata = -1;
                    }

                    if (phidata == -1)
                    {
                        phidisplay = "[#ff0000]0 TMR[#ffffff]";
                    }
                    else if (phitotime)
                    {
                        phidata /= (360 / vessel.orbit.period);
                        phidisplay = ProtractorModule.TimeToDHMS(phidata);
                    }
                    else
                    {
                        phidisplay = String.Format("{0:0.00}°", phidata);
                    }

                    output += phidisplay.PadLeft(fieldWidths[2]);
                }

                output += Environment.NewLine + "    ";

                // dV
                if (pm.orbiting == ProtractorModule.orbitbodytype.moon)
                {
                    output += "----".PadLeft(fieldWidths[3]);
                }
                else
                {
                    output += String.Format("{0:0.0} m/s", pm.CalculateDeltaV(planet)).PadLeft(fieldWidths[3]);
                }

                // Closest
                double distance = pm.getclosestapproach(planet);

                if (distance <= 5 * planet.sphereOfInfluence && distance >= 0)
                {
                    output += "[#00ff00]";
                }
                if (distance <= planet.sphereOfInfluence && distance >= 0)
                {
                    output += "[#ff0000]";
                }

                output += (ProtractorModule.ToSI(distance) + "m").PadLeft(fieldWidths[4]);

                output += Environment.NewLine;
                output += "[#ffffff]";
            }

            output += Environment.NewLine;

            output += getMenu(screenWidth, screenHeight);

            return output;
        }

        private string moonPage(int screenWidth, int screenHeight)
        {
            string output = "";
            ProtractorModule pm = ProtractorModule.primary;

            List<CelestialBody> moons = pm.Moons;

            output += "[#00ff00]";
            output += "      --- Protractor (moons) ---        " + Environment.NewLine +
                Environment.NewLine;

            output += "[#CC2EFA]";
            for (int i = 0; i < fieldHeaders.Length; ++i)
            {
                output += fieldHeaders[i].PadLeft(fieldWidths[i]);
                if (i == 2)
                {
                    output += Environment.NewLine;
                    output += "[#CC2EFA]";
                }
            }
            output += Environment.NewLine;

            //output += "[#ff0000]";

            foreach (CelestialBody moon in moons)
            {
                if (!(moon.Equals (pm.focusbody)) && (pm.focusbody != null))
                {
                    continue;
                }
                // NAME
                output += getBodyColorTag(moon.name);
                output += moon.name.PadRight(fieldWidths[0]);
                output += "[#ffffff]";

                // THETA
                double data = (pm.CurrentPhase(moon) - pm.DesiredPhase(moon) + 360) % 360;
                if (thetatotime)
                {
                    double delta_theta;
                    if (vessel.Landed && pm.orbiting == ProtractorModule.orbitbodytype.planet)  //ship is landed on a planet, use rotation of the planet
                    {
                        //double ves_vel = vessel.horizontalSrfSpeed;
                        double ves_vel = vessel.orbit.getOrbitalSpeedAtPos(vessel.CoM);
                        double radius = vessel.altitude + vessel.mainBody.Radius;
                        double circumference = Math.PI * 2 * radius;
                        double rot = circumference / ves_vel;
                        delta_theta = (360 / rot)-(360 / moon.orbit.period);
                    }
                    else if (pm.orbiting == ProtractorModule.orbitbodytype.planet)   //ship orbiting a planet, but is not landed
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
                    output += ProtractorModule.TimeToDHMS(data).PadLeft(fieldWidths[1]);
                }
                else
                {
                    output += String.Format("{0:0.00}°", data).PadLeft(fieldWidths[1]);
                }

                // PHI
                if (pm.orbiting == ProtractorModule.orbitbodytype.planet) //vessel and moon share planet
                {
                    output += "----".PadLeft(fieldWidths[2]);
                }
                else //vessel orbiting moon
                {
                    double phidata;
                    string phidisplay;

                    if (!adjustejectangle)
                    {
                        phidata = (pm.CalculateDesiredEjectionAngle(vessel.mainBody, moon) - pm.CurrentEjectAngle(null) + 360) % 360;
                    }
                    else if (adjustejectangle && pm.tmr() > 0)
                    {
                        phidata = (pm.AdjustEjectAngle(vessel.mainBody, moon) - pm.CurrentEjectAngle(null) + 360) % 360;
                    }
                    else
                    {
                        phidata = -1;
                    }

                    if(phidata == -1)
                    {
                        phidisplay = "[#ff0000]0 TMR[#ffffff]";
                    }
                    else if (phitotime)
                    {
                        phidata /= (360/vessel.orbit.period);
                        phidisplay = ProtractorModule.TimeToDHMS(phidata);
                    }
                    else
                    {
                        phidisplay = String.Format("{0:0.00}°", phidata);
                    }

                    output += phidisplay.PadLeft(fieldWidths[2]);
                }

                output += Environment.NewLine + "    ";

                // dV
                output += String.Format("{0:0.0} m/s", pm.CalculateDeltaV(moon)).PadLeft(fieldWidths[3]);

                // Closest
                double distance = pm.getclosestapproach(moon);
                if (distance <= 2 * moon.sphereOfInfluence && distance >= 0)
                {
                    output += "[#00ff00]";
                }
                if (distance <= moon.sphereOfInfluence && distance >= 0)
                {
                    output += "[#ff0000]";
                }

                output += (ProtractorModule.ToSI(distance) + "m").PadLeft(fieldWidths[4]);

                output += Environment.NewLine;
            }

            output += Environment.NewLine;

            output += getMenu(screenWidth, screenHeight);

            return output;
        }

        private string moonReturnPage(int screenWidth, int screenHeight)
        {
            ProtractorModule pm = ProtractorModule.primary;
            string output = "";

            // TODO: Won't work around sun
            output += "[#00ff00]";
            output += " --- Protractor (satellite return) ---" + Environment.NewLine +
                Environment.NewLine;

            string mainBodyName = pm.vessel.mainBody.name;
            string referenceBodyName = vessel.mainBody.orbit.referenceBody.name;

            output += String.Format("Ejection from " +
                getBodyColorTag(mainBodyName) + mainBodyName + "[#ffffff]" +
                ": {0:0.00}°", (pm.MoonAngle() - pm.CurrentEjectAngle(null) + 360) % 360) +
                Environment.NewLine +
                "Alt above " + getBodyColorTag(referenceBodyName) + referenceBodyName + "[#ffffff]" +
                ": " +
                ProtractorModule.ToSI(1.05 * vessel.mainBody.orbit.referenceBody.maxAtmosphereAltitude) + "m" +
                Environment.NewLine;

            double distance = pm.getclosestapproach(vessel.mainBody.orbit.referenceBody);
            output += "Closest approach to " +
                getBodyColorTag(referenceBodyName) + referenceBodyName + "[#ffffff]" +
                ": " + ProtractorModule.ToSI(distance) + "m" + Environment.NewLine;

            return output;
        }

        public void buttonProcessor(int buttonID)
        {
            //Debug.Log("ProtractorRPM: Button " + buttonID + " pressed.");

            if (buttonID == btnUp)
            {
                activePage = activePage == 0 ? 2 : (activePage - 1);
                //Debug.Log("ProtractorRPM: UP button pressed. Page = " + activePage);
            }
            if (buttonID == btnDown)
            {
                activePage = (activePage + 1) % 3;
                //Debug.Log("ProtractorRPM: DOWN button pressed. Page = " + activePage);
            }
            if (buttonID == btnEnter)
            {
                //Debug.Log("ProtractorRPM: ENTER button pressed. Selection = " + selection);
                switch (selection)
                {
                case 0:
                    thetatotime = !thetatotime;
                    break;
                case 1:
                    phitotime = !phitotime;
                    break;
                case 2:
                    adjustejectangle = !adjustejectangle;
                    break;
                default:
                    Debug.LogError("Selection integer is out of range. This should never happen!");
                    break;
                }
            }
            if (buttonID == btnEscape)
            {
                //Debug.Log("ProtractorRPM: ESC button pressed.");
                thetatotime = false;
                phitotime = false;
                adjustejectangle = true;
            }
            //if (buttonID == btnHome)
            //{
            //    Debug.Log("ProtractorRPM: HOME button pressed.");
            //}
            if (buttonID == btnRight)
            {
                selection = (selection + 1) % 3;
                //Debug.Log("ProtractorRPM: RIGHT button pressed. Selection = " + selection);
            }
            if (buttonID == btnLeft)
            {
                selection = selection == 0 ? 2 : (selection - 1);
                //Debug.Log("ProtractorRPM: LEFT button pressed. Selection = " + selection);
            }
            //if (buttonID == btnNext)
            //{
            //    Debug.Log("ProtractorRPM: NEXT button pressed.");
            //}
            //if (buttonID == btnPrev)
            //{
            //    Debug.Log("ProtractorRPM: PREV button pressed.");
            //}
        }


        public void Start()
        {
            Debug.Log("--- ProtractorRPM: starting systems...");

            //ProtractorModule pm = ProtractorModule.primary;
            //pm.initialize();

            for (int i = 0; i < fieldPrototypes.Length; ++i)
            {
                fieldWidths[i] = fieldPrototypes[i].Length;
            }

            bodycolorlist.Add("Kerbin", "[#a3ede4]");
            bodycolorlist.Add("Moho", "[#c46a4b]");
            bodycolorlist.Add("Eve", "[#d3adff]");
            bodycolorlist.Add("Duna", "[#edb4a6]");
            bodycolorlist.Add("Jool", "[#8cf068]");
            bodycolorlist.Add("Vall", "[#969ebf]");
            bodycolorlist.Add("Laythe", "[#90caeb]");
            bodycolorlist.Add("Tylo", "[#fedede]");
            bodycolorlist.Add("Bop", "[#b8a58b]");
            bodycolorlist.Add("Ike", "[#aeb5ca]");
            bodycolorlist.Add("Gilly", "[#b8a58b]");
            bodycolorlist.Add("Mun", "[#aeb5ca]");
            bodycolorlist.Add("Minmus", "[#a68db8]");
            bodycolorlist.Add("Eeloo", "[#929292]");
            bodycolorlist.Add("Dres", "[#917552]");
            bodycolorlist.Add("Pol", "[#929d6d]");
        }
    }
}

