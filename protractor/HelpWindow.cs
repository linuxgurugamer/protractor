using System;
using UnityEngine;



namespace Protractor
{
	class HelpWindow : ZKeyLib.Window<Protractor>
	{
		private GUIStyle labelStyle;
		private GUIStyle sectionStyle;
		private Vector2 scrollPosition;
		private readonly Protractor	_parent;



		public HelpWindow( Protractor Parent )
			: base( "Protractor Help", 500, Screen.height * 0.75f )
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



			GUILayout.Label( "Protractor Protracted by Z-Key Aerospace.", sectionStyle, GUILayout.ExpandWidth( true ) );

			GUILayout.Space( wScale( 30 ) );
			GUILayout.Label( "Tips", sectionStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Click on the icon in the bottom left to hide Protractor and its windows.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Click on the number in \"Closest\" column to toggle the closest approach line on the map.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Click on the name of a celestial body in the list to hide other bodies.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Click on the θ angle or time display to create a KAC alarm, if present. Keep in mind the time calculation assumes a circular orbit and may be off by varying degrees.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Click on θ in the column headers to toggle between displaying an angle and an approximate time until the next launch window.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Click on Ψ in the column headers to toggle between displaying and angle and an approximate time until the next ejection burn.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* Click on Δv in the column headers to toggle between displaying estimated transfer delta V and an approximate burn time for that delta V in seconds.  When engines are off, uses maximum thrust for current stage. When firing engines, uses the thrust at current throttle levels.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "* When a body is focused and an intercept is detected, your predicted inclination is displayed below the closest approach.", labelStyle, GUILayout.ExpandWidth( true ) );

			GUILayout.Space( wScale( 20 ) );
			GUILayout.Label( "Column Key", sectionStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "θ - Difference in the current angle between bodies and the desired angle between them for transfer. Launch your ship when this is 0.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "Ψ - Point in vessel's current orbit (relative to orbited body's prograde) where you should start your ejection burn. Burn when this is 0.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "Δv - Amount your current velocity needs to be changed to accomplish maneuver.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "Adjust Ψ - Used to time escape. Toggle to adjust your escape angle based on your craft's thrust capabilities.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "Closest - The closest approach between your craft and the target during one revolution.", labelStyle, GUILayout.ExpandWidth( true ) );

			GUILayout.Space( wScale( 20 ) );
			GUILayout.Label( "Instructions", sectionStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "To use this guide, time warp until \"θ\" is 0. IT IS STRONGLY SUGGESTED TO DO THIS BEFORE LAUNCHING YOUR SHIP. This means the planets are in the right position relative to each other.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "Launch into a low orbit, then time warp until \"Ψ\" is 0. This means your vessel is in the right place in it's orbit.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "For best results, click \"Adjust Ψ\" or start your ejection burn before the Ψ hits 0 so that it does so when your burn is exactly 2/3 complete.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "Burn in direction of vessel's prograde until \"Δv\" is approximately 0.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "This mod assumes your craft is in a 0-inclination, circular orbit. Target is also assumed to be in 0-inclination, circular orbit. Either a 90° or 270° heading will work, though launching to 90° is more efficient.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "YOU WILL HAVE TO MAKE ADJUSTMENTS TO RENDEZVOUS. THIS MOD ONLY GETS YOU IN THE NEIGHBORHOOD. To close the gap, try burning at 90° angles (pro/retro, norm/antinorm, +rad/-rad).", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "Eventually, you'll know which way to burn to correct an orbit.", labelStyle, GUILayout.ExpandWidth( true ) );

			GUILayout.Space( wScale( 20 ) );
			GUILayout.Label( "Advanced", sectionStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "Only works when orbiting a moon. This data is designed to aid in travelling from a moon, to the moon's planet, and then to another moon. (e.g. Tylo -> Jool -> Vall). Adds \"Moon Ω\" column representing angle from moon to the prograde of the planet that moon orbits.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "\"Alt\" above represents your target periapsis around the moon's planet where you should begin your ejection burn. \"Eject from [moon]\" indicates where to leave your moon's orbit.", labelStyle, GUILayout.ExpandWidth( true ) );
			GUILayout.Label( "To use this mode, wait until \"θ\" is 0, \"Moon Ω\" is 0, and \"Eject from [moon]\" is 0. Burn to create an orbit with an apoapsis at your current moon and a periapsis at \"Alt\". When you reach periapsis, burn for target planet.", labelStyle, GUILayout.ExpandWidth( true ) );





			GUILayout.EndVertical( );
			GUILayout.EndScrollView( );

			GUILayout.Space( wScale( 8 ) );
		}
	}
}
