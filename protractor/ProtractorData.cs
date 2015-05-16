using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Protractor {

    public class ProtractorData
    {
        public Vessel vessel = null;
        public List<CelestialBody>
            planets = null,
            moons = null,
            bodyList = null;
        public CelestialBody Sun = null;

        public Dictionary<string, CelestialData> celestials = new Dictionary<string, CelestialData>();

        public enum orbitbodytype { sun, planet, moon };


        public double closestApproachTime;
        public double maxthrustaccel;
        public double minthrustaccel;

        public ProtractorData(Vessel vessel)
        {
            initialize(vessel);
        }

        public void initialize(Vessel vessel)
        {
            this.vessel = vessel;
            Sun = Planetarium.fetch.Sun;
            getbodies();
            getplanets();
            getmoons();
            //Debug.Log("Protractor: ProtractorData initialized.");
            //print();
        }
        public void getmoons()  //gets a list of moons
        {
            // In interstellar space
            if (vessel.mainBody == Sun)
            {
                if (moons != null)
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
            if (planets != null)
            {
                planets.Clear();
            } else {
                planets = new List<CelestialBody>();
            }

            foreach (CelestialBody body in bodyList)
            {
                if (body.referenceBody == Planetarium.fetch.Sun)
                {
                    if (body == Sun || body == vessel.mainBody || body == vessel.mainBody.referenceBody)
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
            bodyList = null;
            bodyList = new List<CelestialBody>(FlightGlobals.Bodies);
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                celestials.Add(body.name, new CelestialData(body));
            }
        }

        public orbitbodytype getorbitbodytype()
        {
            if (vessel.mainBody == Sun)
            {
                return orbitbodytype.sun;
            }
            else if (vessel.mainBody.referenceBody != Sun)
            {
                return orbitbodytype.moon;
            }
            else
            {
                return orbitbodytype.planet;
            }
        }

        public void print()
        {
            foreach (KeyValuePair<string, CelestialData> data in celestials)
            {
                data.Value.print();
            }
        }

    }
}

