using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace Protractor
{
	public class Config
	{
			private ZKeyLib.Logger							_logger;
			private readonly string _assemblyPath = Path.GetDirectoryName( typeof( Protractor ).Assembly.Location );
			private readonly string _file = KSP.IO.IOUtils.GetFilePathFor( typeof( Protractor ), "settings.cfg" );
//			private Dictionary<GameScenes, Dictionary<string, WindowSettings>> _windowSettings = new Dictionary<GameScenes, Dictionary<string, WindowSettings>>( );

			private double _updateInterval;
			private double _planetAlarmMargin;
			private double _moonAlarmMargin;
			private bool _showPlanets;
			private bool _showMoons;
			private bool _showAdvanced;
			private bool _showDv;
			private bool _trackDv;
			private bool _useBlizzysToolbar;



		// Members
			public double UpdateInterval				{ get { return _updateInterval; }				set { if (_updateInterval != value) { _updateInterval = value; } } }
			public double PlanetAlarmMargin				{ get { return _planetAlarmMargin; }			set { if (_planetAlarmMargin != value) { _planetAlarmMargin = value; } } }
			public double MoonAlarmMargin				{ get { return _moonAlarmMargin; }				set { if (_moonAlarmMargin != value) { _moonAlarmMargin = value; } } }
			
			public bool ShowPlanets						{ get { return _showPlanets; }					set { if( _showPlanets != value ) { _showPlanets = value; } } }
			public bool ShowMoons						{ get { return _showMoons; }					set { if( _showMoons != value ) { _showMoons = value; } } }
			public bool ShowAdvanced					{ get { return _showAdvanced; }					set { if( _showAdvanced != value ) { _showAdvanced = value; } } }
			public bool ShowDv							{ get { return _showDv; }						set { if( _showDv != value ) { _showDv = value; } } }
			public bool TrackDv							{ get { return _trackDv; }						set { if( _trackDv != value ) { _trackDv = value; } } }
			public bool UseBlizzysToolbar				{ get { return _useBlizzysToolbar; }			set { if( _useBlizzysToolbar != value ) { _useBlizzysToolbar = value; OnUseBlizzysToolbarChanged( ); } } }



		// Get notified when settings change
			public event EventHandler UseBlizzysToolbarChanged;

			


		// For triggering events
			private void OnUseBlizzysToolbarChanged( )
			{
				if( UseBlizzysToolbarChanged != null )
				{
					UseBlizzysToolbarChanged( this, EventArgs.Empty );
				}
			}





















		public Config( )
		{
			_logger = new ZKeyLib.Logger( this );
		}

//
// SETTINGS.
//
/*        public void savesettings()
        {
            if (!loaded)
            {
                return;
            }
            KSP.IO.PluginConfiguration cfg = KSP.IO.PluginConfiguration.CreateForType<Protractor>();
            cfg["mainpos"] = windowPos;
            cfg["showadvanced"] = showadvanced;
            cfg["adjustejectangle"] = adjustejectangle;
            cfg["isvisible"] = isVisible;
            cfg["showplanets"] = showplanets;
            cfg["showmoons"] = showmoons;
            cfg["showadvanced"] = showadvanced;
            cfg["showdv"] = showdv;
            cfg["trackdv"] = trackdv;
            cfg["updateinterval"] = updateInterval;
            updateIntervalString = updateInterval.ToString("F2");
            cfg["updateinterval"] = updateIntervalString;
            planetAlarmMargin_str = planetAlarmMargin.ToString("F2");
            cfg["planetalarmmargin"] = planetAlarmMargin_str;
            moonAlarmMargin_str = moonAlarmMargin.ToString("F2");
            cfg["moonalarmmargin"] = moonAlarmMargin_str;

            _logger.Info("-------------Saved Protractor Settings-------------");
            cfg.save();
        }

        public void loadsettings()
        {
            _logger.Info("-------------Loading settings...-------------");
            KSP.IO.PluginConfiguration cfg = KSP.IO.PluginConfiguration.CreateForType<Protractor>();
            cfg.load();
            _logger.Info("-------------Settings Opened-------------");
            windowPos = cfg.GetValue<Rect>("mainpos", new Rect(0, 0, 0, 0));
            showadvanced = cfg.GetValue<bool>("showadvanced", true);
            adjustejectangle = cfg.GetValue<bool>("adjustejectangle", false);
            isVisible = cfg.GetValue<bool>("isvisible", true);
            showplanets = cfg.GetValue<bool>("showplanets", true);
            showmoons = cfg.GetValue<bool>("showmoons", true);
            showdv = cfg.GetValue<bool>("showdv", true);
            trackdv = cfg.GetValue<bool>("trackdv", true);

            updateIntervalString = cfg.GetValue<string>("updateinterval", "0.20");
            try {
                updateInterval = Single.Parse(updateIntervalString);
            } catch {
                updateInterval = updateInterval_def;
            }

            planetAlarmMargin_str = cfg.GetValue<string>("planetalarmmargin", "3600");
            try {
                planetAlarmMargin = Single.Parse(planetAlarmMargin_str);
            } catch {
                planetAlarmMargin = planetAlarmMargin_def;
            }

            moonAlarmMargin_str = cfg.GetValue<string>("moonalarmmargin", "300");
            try {
                moonAlarmMargin = Single.Parse(moonAlarmMargin_str);
            } catch {
                moonAlarmMargin = moonAlarmMargin_def;
            }

            loaded = true;  //loaded

            _logger.Info("-------------Loaded Protractor Settings-------------");
        }
*/








		public void Save( )
		{
			_logger.Trace( "Save" );
			var node = new ConfigNode( );
/*				var root = node.AddNode( "ScienceChecklist" );
			var settings = root.AddNode( "Config" );
			var windowSettings = root.AddNode( "Windows" );


			
			settings.AddValue( "HideCompleteExperiments",		_hideCompleteExperiments );
			settings.AddValue( "UseBlizzysToolbar",				_useBlizzysToolbar );
			settings.AddValue( "CompleteWithoutRecovery",		_completeWithoutRecovery );
			settings.AddValue( "CheckDebris",					_checkDebris );
			settings.AddValue( "AllFilter",						_allFilter );
			settings.AddValue( "StopTimeWarp",					_stopTimeWarp );
			settings.AddValue( "PlayNoise",						_playNoise );
			settings.AddValue( "ShowResultsWindow",				_showResultsWindow );
			settings.AddValue( "FilterDifficultScience",		_filterDifficultScience );
			settings.AddValue( "UiScale",						_uiScale );
			settings.AddValue( "MusicStartsMuted",				_musicStartsMuted );
			settings.AddValue( "RighClickMutesMusic",			_righClickMutesMusic );
			settings.AddValue( "SelectedObjectWindow",			_selectedObjectWindow );



			foreach( var V in _windowSettings )
			{
				var SceneNode = windowSettings.AddNode( V.Key.ToString( ) );
				foreach( var W in V.Value )
				{
					var WindowNode = SceneNode.AddNode( W.Key );
					foreach( var S in W.Value._settings )
					{
						WindowNode.AddValue( S.Key,	S.Value );
					}
				}
			}
*/


//			_logger.Trace( "Saving to" + _file );
			node.Save( _file );
		}



		public void Load( )
		{
/*			_hideCompleteExperiments =		false;
			_useBlizzysToolbar =			false;
			_completeWithoutRecovery =		false;
			_checkDebris =					false;
			_allFilter =					true;
			_stopTimeWarp =					true;
			_playNoise =					true;
			_showResultsWindow =			true;
			_filterDifficultScience =		true;
			_uiScale =						1f;
			_musicStartsMuted =				false;
			_righClickMutesMusic =			true;
			_selectedObjectWindow =			true;



			try
			{
				if( File.Exists( _file ) )
				{
					var node = ConfigNode.Load( _file );
					if( node == null ) return;
					var root = node.GetNode( "ScienceChecklist" );
					if( root == null ) return;
					var settings = root.GetNode( "Config" );
					if( settings == null ) return;



					var V = settings.GetValue( "HideCompleteExperiments" );
					if( V != null )
						_hideCompleteExperiments = bool.Parse( V );

					V = settings.GetValue( "UseBlizzysToolbar" );
					if( V != null )
						_useBlizzysToolbar = bool.Parse( V );

					V = settings.GetValue( "CompleteWithoutRecovery" );
					if( V != null )
						_completeWithoutRecovery = bool.Parse( V );

					V = settings.GetValue( "CheckDebris" );
					if( V != null )
						_checkDebris = bool.Parse( V );

					V = settings.GetValue( "AllFilter" );
					if( V != null )
						_allFilter = bool.Parse( V );

					V = settings.GetValue( "StopTimeWarp" );
					if( V != null )
						_stopTimeWarp = bool.Parse( V );

					V = settings.GetValue( "PlayNoise" );
					if( V != null )
						_playNoise = bool.Parse( V );

					V = settings.GetValue( "ShowResultsWindow" );
					if( V != null )
						_showResultsWindow = bool.Parse( V );

					V = settings.GetValue( "FilterDifficultScience" );
					if( V != null )
						_filterDifficultScience = bool.Parse( V );

					V = settings.GetValue( "UiScale" );
					if (V != null)
						_uiScale = float.Parse(V);

					V = settings.GetValue( "MusicStartsMuted" );
					if( V != null )
						_musicStartsMuted = bool.Parse( V );

					V = settings.GetValue( "RighClickMutesMusic" );
					if( V != null )
						_righClickMutesMusic = bool.Parse( V );

					V = settings.GetValue( "SelectedObjectWindow" );
					if( V != null )
						_selectedObjectWindow = bool.Parse( V );



					var windowSettings = root.GetNode( "Windows" );
					if( windowSettings == null ) return;
					foreach( var N in windowSettings.nodes )
					{
//						_logger.Trace( "Window Node" );
						if( N.GetType( ) == typeof( ConfigNode ) )
						{
							ConfigNode SceneNode = (ConfigNode)N;
							GameScenes Scene = (GameScenes)Enum.Parse( typeof( GameScenes ), SceneNode.name, true );

							if( !_windowSettings.ContainsKey( Scene ) )
								_windowSettings.Add( Scene, new Dictionary<string, WindowSettings>( ) );

							foreach( var W in SceneNode.nodes )
							{
								if( W.GetType( ) == typeof( ConfigNode ) )
								{
									ConfigNode WindowNode = (ConfigNode)W;
									string WindowName = WindowNode.name;

//									_logger.Trace( "Loading " + WindowName + " For " + Scene.ToString( ) );

									WindowSettings NewWindowSetting = new WindowSettings( WindowName );


									for( int x = 0; x < WindowNode.CountValues; x++ )
									{
										NewWindowSetting._settings[ WindowNode.values[ x ].name ] = WindowNode.values[ x ].value;
									}


									_windowSettings[ Scene ][ NewWindowSetting._windowName ] = NewWindowSetting;
								}
							}
						}
					}

//					_logger.Info( "Loaded successfully." );
					return; // <--- Return from here --------------------------------------
				}
			}
			catch( Exception e )
			{
				_logger.Info( "Unable to load config: " + e.ToString( ) );
			}*/
		}

	}














	}

