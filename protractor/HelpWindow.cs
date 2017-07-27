using System;
using UnityEngine;




/*


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
                "  Click on the θ angle or time display to create a KAC alarm, if present. Keep in mind\n" +
                "    the time calculation assumes a circular orbit and may be off by varying degrees.\n" +
                "- Click on θ in the column headers to toggle between displaying an angle and an \n" +
                " approximate time until the next launch window.\n" +
                "- Click on Ψ in the column headers to toggle between displaying and angle and an\n" +
                " approximate time until the next ejection burn.\n" +
                "- Click on Δv in the column headers to toggle between displaying estimated transfer\n" +
                " delta V and an approximate burn time for that delta V in seconds. When engines are\n" +
                " off, uses maximum thrust for current stage. When firing engines, uses the thrust at\n" +
                " current throttle levels.\n" +
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

*/





namespace Protractor
{
	class HelpWindow : ZKeyLib.Window<Protractor>
	{
		private GUIStyle labelStyle;
		private GUIStyle sectionStyle;
		private Vector2 scrollPosition;
		private readonly Protractor	_parent;



		public HelpWindow( Protractor Parent )
			: base("[x] Science! Help", 500, Screen.height * 0.75f )
		{
			_parent = Parent;
			UiScale = 1;
			scrollPosition = Vector2.zero;

		}



		protected override void ConfigureStyles( )
		{
			base.ConfigureStyles();

			if( labelStyle == null )
			{
				labelStyle = new GUIStyle( _skin.label );
				labelStyle.wordWrap = true;
				labelStyle.fontStyle = FontStyle.Normal;
				labelStyle.normal.textColor = Color.white;
				labelStyle.stretchWidth = true;
				labelStyle.stretchHeight = false;
				labelStyle.margin.bottom -= wScale( 2 );
				labelStyle.padding.bottom -= wScale( 2 );
			}

			if( sectionStyle == null )
			{
				sectionStyle = new GUIStyle( labelStyle );
				sectionStyle.fontStyle = FontStyle.Bold;
			}
		}





		protected override void DrawWindowContents( int windowID )
		{
			scrollPosition = GUILayout.BeginScrollView( scrollPosition );
			GUILayout.BeginVertical( GUILayout.ExpandWidth( true ) );

			GUILayout.Label( "[x] Science! by Z-Key Aerospace and Bodrick.", sectionStyle, GUILayout.ExpandWidth( true ) );

			GUILayout.Space( wScale( 30 ) );
			GUILayout.Label("About", sectionStyle, GUILayout.ExpandWidth(true));
			GUILayout.Label( "[x] Science! creates a list of all possible science.  Use the list to find what is possible, to see what is left to accomplish, to decide where your Kerbals are going next.", labelStyle, GUILayout.ExpandWidth( true ) );

			GUILayout.Space( wScale( 20 ) );
			GUILayout.Label( "The four filter buttons at the bottom of the window are", sectionStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Show experiments available right now – based on you current ship and its situation", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Show experiments available on this vessel – based on your ship but including all known biomes", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Show all unlocked experiments – based on instruments you have unlocked and celestial bodies you have visited.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Show all experiments – shows everything.  You can hide this button", labelStyle, GUILayout.ExpandWidth( true ) );

			GUILayout.Space( wScale( 20 ) );
			GUILayout.Label( "The text filter", sectionStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "To narrow your search, you may enter text into the filter eg \"kerbin’s shores\"", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "Use – to mean NOT eg \"mun space -near\"", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "Use | to mean OR eg \"mun|minmus space\"", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "Hover the mouse over the \"123/456 completed\" text.  A pop-up will show more infromation.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "Press the X button to clear your text filter.", labelStyle, GUILayout.ExpandWidth( true ) );

			GUILayout.Space( wScale( 20 ) );
			GUILayout.Label( "The settings are", sectionStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Hide complete experiments – Any science with a full green bar is hidden.  It just makes it easier to see what is left to do.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Complete without recovery – Consider science in your spaceships as if it has been recovered.  You still need to recover to get the points.  It just makes it easier to see what is left to do.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Check debris – Science that survived a crash will be visible.  You may still be able to recover it.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Allow all filter – The \"All\" filter button shows science on planets you have never visited using instruments you have not invented yet.  Some people may consider it overpowered.  If you feel like a cheat, turn it off.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Filter difficult science – Hide science that is practically impossible.  Flying at stars, that kinda thing.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Use blizzy78's toolbar – If you have blizzy78’s toolbar installed then place the [x] Science! button on that instead of the stock \"Launcher\" toolbar.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Right click [x] icon – Choose to open the Here and Now window by right clicking.  Hides the second window.  Otherwise mute music.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Music starts muted – Music is muted on load.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Adjust UI Size – Change the scaling of the UI.", labelStyle, GUILayout.ExpandWidth( true ) );

			GUILayout.Space( wScale( 20 ) );
			GUILayout.Label( "Here and Now Window", sectionStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "The Here and Now Window will stop time-warp, display an alert message and play a noise when you enter a new situation.  To prevent this, close the window.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "The Here and Now Window will show all outstanding experiments for the current situation that are possible with the current ship.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "To run an experiment, click the button.  If the button is greyed-out then you may need to reset the experiment or recover or transmit the science.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "To perform an EVA report or surface sample, first EVA your Kerbal.  The window will react, allowing those buttons to be clicked.", labelStyle, GUILayout.ExpandWidth( true ) );

			GUILayout.Space( wScale( 20 ) );
			GUILayout.Label( "Did you know? (includes spoilers)", sectionStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* In the VAB editor you can use the filter \"Show experiments available on this vessel\" to see what your vessel could collect before you launch it.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Does the filter \"mun space high\" show mun’s highlands?  – use \"mun space –near\" instead.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Need more science?  Go to Minmus.  It’s a little harder to get to but your fuel will last longer.  A single mission can collect thousands of science points before you have to come back.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Generally moons are easier - it is more efficient to collect science from the surface of Ike or Gilly than from Duna or Eve.  That said - beware Tylo, it's big and you can't aerobrake.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Most of Kerbin’s biomes include both splashed and landed situations.  Landed at Kerbin’s water?  First build an aircraft carrier.", labelStyle, GUILayout.ExpandWidth( true ) );

			GUILayout.EndVertical( );
			GUILayout.EndScrollView( );

			GUILayout.Space( wScale( 8 ) );
		}
	}
}
