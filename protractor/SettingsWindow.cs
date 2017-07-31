using System;
using UnityEngine;
using ZKeyButtons;



namespace Protractor
{
	class SettingsWindow : ZKeyLib.Window<Protractor>
	{
		private readonly string version;
		private GUIStyle labelStyle;
		private GUIStyle toggleStyle;
		private GUIStyle sliderStyle;
		private GUIStyle editStyle;
		private GUIStyle versionStyle;
		private GUIStyle selectionStyle;

		private readonly ZKeyLib.Logger _logger;
		private readonly Protractor _parent;

		public static readonly float updateInterval_def = 0.2f;
		public string updateIntervalString = "0.20";

		public static readonly double planetAlarmMargin_def = 60 * 60;
		public string planetAlarmMargin_str = "3600.00";

		public static readonly double moonAlarmMargin_def = 60 * 5;
		public string moonAlarmMargin_str = "300.00";



		// Constructor
		public SettingsWindow( Protractor Parent )
			: base( "Protractor Settings", 240, 240 )
		{
			_logger = new ZKeyLib.Logger( this );
			_parent = Parent;
			UiScale = 1; // Don't let this change
			version = ZKeyLib.Utilities.GetDllVersion( this );

			updateIntervalString = _parent.Config.UpdateInterval.ToString( "0.##" );
			planetAlarmMargin_str = _parent.Config.PlanetAlarmMargin.ToString( "0.##" );
			moonAlarmMargin_str = _parent.Config.MoonAlarmMargin.ToString( "0.##" );
		}



		// For our Window base class
		protected override void ConfigureStyles( )
		{
			base.ConfigureStyles( );

			if( labelStyle == null )
			{
				labelStyle = new GUIStyle( _skin.label );
				labelStyle.wordWrap = false;
				labelStyle.fontStyle = FontStyle.Normal;
				labelStyle.normal.textColor = Color.white;

				toggleStyle = new GUIStyle( _skin.toggle );
				sliderStyle = new GUIStyle( _skin.horizontalSlider );
				
				editStyle = new GUIStyle( _skin.textField );
				editStyle.fontStyle = FontStyle.Normal;
				
				versionStyle = ZKeyLib.Utilities.GetVersionStyle( );

				selectionStyle = new GUIStyle( _skin.button );
				selectionStyle.margin = new RectOffset( 30, 0, 0, 0 );
			}
		}



		// For our Window base class
		protected override void DrawWindowContents( int windowID )
		{
			bool save = false;
            GUILayout.BeginVertical();
            
			
			
			GUILayout.BeginHorizontal();
            GUILayout.Label( "Update interval (secs): ", labelStyle );
            updateIntervalString = GUILayout.TextField( updateIntervalString, 10, editStyle );
            try {
                _parent.Config.UpdateInterval = float.Parse(updateIntervalString);
            } catch {
                _parent.Config.UpdateInterval = updateInterval_def;
            }
            if (_parent.Config.UpdateInterval < 0.001f || _parent.Config.UpdateInterval > 10.0f)
            {
                _parent.Config.UpdateInterval = updateInterval_def;
            }
            GUILayout.EndHorizontal();



            GUILayout.BeginHorizontal();
            GUILayout.Label("KAC Alarm Margin (planets): ", labelStyle);
            planetAlarmMargin_str = GUILayout.TextField( planetAlarmMargin_str, 10, editStyle );
            try {
                _parent.Config.PlanetAlarmMargin = float.Parse(planetAlarmMargin_str);
            } catch {
                _parent.Config.PlanetAlarmMargin = planetAlarmMargin_def;
            }
            if (_parent.Config.PlanetAlarmMargin < 0.0 || _parent.Config.PlanetAlarmMargin > 60*60*ProtractorCalcs.HoursPerDay*5)
            {
                _parent.Config.PlanetAlarmMargin = planetAlarmMargin_def;
            }
            GUILayout.Label("s");
            GUILayout.EndHorizontal();



            GUILayout.BeginHorizontal( );
            GUILayout.Label( "KAC Alarm Margin (moons): ", labelStyle );
            moonAlarmMargin_str = GUILayout.TextField( moonAlarmMargin_str, 10, editStyle );
            try {
                _parent.Config.MoonAlarmMargin = float.Parse(moonAlarmMargin_str);
            } catch {
                _parent.Config.MoonAlarmMargin = moonAlarmMargin_def;
            }
            if (_parent.Config.MoonAlarmMargin < 0.0 || _parent.Config.MoonAlarmMargin > 60*60*ProtractorCalcs.HoursPerDay)
            {
                _parent.Config.MoonAlarmMargin = moonAlarmMargin_def;
            }
            GUILayout.Label("s");
            GUILayout.EndHorizontal( );



			if( BlizzysToolbarButton.IsAvailable )
			{
				GUILayout.BeginHorizontal( );
				var toggle = GUILayout.Toggle( _parent.Config.UseBlizzysToolbar, new GUIContent( "Use blizzy78's toolbar", "Remove Protractor button from stock toolbar and add to blizzy78 toolbar." ), toggleStyle );
				if( toggle != _parent.Config.UseBlizzysToolbar )
				{
					_parent.Config.UseBlizzysToolbar = toggle;
					save = true;
				}
				GUILayout.EndHorizontal( );
			}



            GUILayout.EndVertical();
			GUILayout.Space(10);
			GUI.Label( new Rect( 4, windowPos.height - 13, windowPos.width - 20, 12 ), "Protractor V" + version, versionStyle );

			if( save )
				_parent.Config.Save( );
		}
	}
}
