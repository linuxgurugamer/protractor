//Original author: Enigma
//Development has been continued by: Addle
//Distributed according to GNU General Public License version 3, available at http://www.gnu.org/copyleft/gpl.html. All other rights reserved.
//no warrantees of any kind are made with distribution, including but not limited to warranty of merchantability and warranty for a particular purpose.


using System;
using System.Collections.Generic;
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

			private Dictionary<string, Color>				bodycolorlist = new Dictionary<string, Color>();
			public Config									Config { get; private set; }

			private UnifiedButton							_protractorMainButton;
			private SettingsWindow							_settingsWindow;
			private HelpWindow								_helpWindow;
			private MainWindow								_mainWindow;
			private bool									_launcherVisible;	// If the toolbar is shown
			private bool									_UiHidden;			// If the user hit F2 

			public ProtractorData							pdata { get; private set; }
			public ProtractorCalcs							pcalcs { get; private set; }

			// Used by FixedUpdate to keep track of the current vessel
			public double	totaldv = 0.0f;
			public double	trackeddv = 0.0f;
			public float	t_lastUpdate = 0.0f;
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

			// Main window
			_mainWindow = new MainWindow( this, _settingsWindow, _helpWindow );
			_logger.Info( "Made Windows" );


			



            pdata = new ProtractorData( );
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
                    if( _mainWindow.IsVisible( ) )
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
			_logger.Info( "OnGUI" );
			if( !UiActive( ) ) return;
			if( !_mainWindow.IsVisible( ) )
			{
//				approach.enabled = false; DO SOMETHING ABOUT THIS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
				return;
			}

			_settingsWindow.DrawWindow( );
			_helpWindow.DrawWindow( );
			_mainWindow.DrawWindow( );

			_logger.Info( "OnGUI DONE" );
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
				_protractorMainButton.UseBlizzyIfPossible = Config.UseBlizzysToolbar;




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
			
			_mainWindow.SetVisible( true );
			_logger.Info( "Window_Open" );
//			UpdateChecklistVisibility( true );
		}



		// Registered with the button
		// Called when the toolbar button for the checklist window is toggled off.
		private void Window_Close( object sender, EventArgs e )
		{
//			if( !_active )
//				return;
			_mainWindow.SetVisible( false );
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
			if( _mainWindow.IsVisible( ) )
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
