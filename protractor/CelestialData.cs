using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Protractor {

    public class CelestialData
    {
        public CelestialBody body;
        public string name;
        public double theta_angle;
        public string theta_time;
        public double psi_angle;
        public string psi_time;
        public double psi_angle_adjusted;
        public string psi_time_adjusted;
        public double deltaV;
        public double deltaV_time;
        public double adv_ejection_angle;
        public double closest_approach;
        public double closest_approach_time;

        public CelestialData(CelestialBody body)
        {
            this.body = body;
            this.name = body.name;
        }

        public void print()
        {
            Debug.Log("Protractor: CelestialData: Name: " + name);
        }
    }
}

