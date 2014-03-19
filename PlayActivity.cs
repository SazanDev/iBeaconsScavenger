using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ScavengerLibrary;
using RadiusNetworks.IBeaconAndroid;

namespace Scavenger
{
	[Activity (Label = "Play")]			
	public class PlayActivity : Activity, IBeaconConsumer
	{
		private const string REGION_ID = "Scavenger";
		private const int DISTANCE_BUCKET = 12;
		IBeaconManager beaconManager;
		BeaconNotifier beaconNotifier;
		Region beaconRegion;
		GameManager gameManager;
		TextView console;
		TextView beaconInfoText;
		Dictionary<string, IBeacon> beacons;
		Dictionary<string, double[]> distances;
		bool scanning;
		GameState gameState;
		bool beaconsAdded;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.Play);

			// Create your application here
			beaconManager = IBeaconManager.GetInstanceForApplication (this);
			beaconNotifier = new BeaconNotifier ();
			beaconRegion = new Region (REGION_ID, null, null, null);

			gameManager = new GameManager ();
			beacons = new Dictionary<string, IBeacon> ();
			distances = new Dictionary<string, double[]> ();
			scanning = false;
			gameState = GameState.Off;
			beaconsAdded = false;

			FindViewById (Resource.Id.scanButton).Click += (sender, e) => {
				ScanForBeacons ();
			};

			FindViewById (Resource.Id.addAllBeaconsButton).Click += (sender, e) => {
				AddAllBeacons ();
			};

			FindViewById (Resource.Id.backButton).Click += (sender, e) => {
				OnBackPressed ();
			};

			FindViewById (Resource.Id.startGameButton).Click += (sender, e) => {
				StartGame ();
			};

			console = FindViewById<TextView> (Resource.Id.consoleText);
			beaconInfoText = FindViewById<TextView> (Resource.Id.beaconInfoText);

			beaconNotifier.EnterRegionComplete += HandleEnterRegion;
			beaconNotifier.ExitRegionComplete += HandleExitRegion;
			beaconNotifier.DidRangeBeaconsInRegionComplete += HandleBeaconInfo;

			gameManager.AllTokensFound += (sender, e) => {
				HandleAllTokensFound ();
			};

			gameManager.GameStateChange += HandleGameStateChange;
			gameManager.TokenFound += HandleTokenFound;
		}

		public void OnIBeaconServiceConnect ()
		{
			beaconManager.SetMonitorNotifier (beaconNotifier);
			beaconManager.SetRangeNotifier (beaconNotifier);

			beaconManager.StartMonitoringBeaconsInRegion (beaconRegion);
			beaconManager.StartRangingBeaconsInRegion (beaconRegion);

			scanning = true;
		}

		protected override void OnDestroy ()
		{
			base.OnDestroy ();

			beaconNotifier.EnterRegionComplete -= HandleEnterRegion;
			beaconNotifier.ExitRegionComplete -= HandleExitRegion;
			beaconNotifier.DidRangeBeaconsInRegionComplete -= HandleBeaconInfo;
			if (scanning) {
				beaconManager.StopMonitoringBeaconsInRegion (beaconRegion);
				beaconManager.StopRangingBeaconsInRegion (beaconRegion);
				beaconManager.UnBind (this);
			}
		}

		void ScanForBeacons ()
		{
			if (!scanning) {
				gameState = GameState.Off;
				Log ("Scanning for beacons");

				try {
					if (beaconManager.CheckAvailability ()) {
						beaconManager.Bind (this);
					} else {
						Log ("Bluetooth LE is not turned on");
					}
				} catch {
					Log ("Bluetooth LE is not supported on this device");
				}
			} else {
				Log ("Already scanning");
			}
		}

		void AddAllBeacons ()
		{
			if (gameState != GameState.Start) {
				beaconsAdded = true;
				Log (string.Format ("Adding {0} beacons", beacons.Count));
				foreach (KeyValuePair<string, IBeacon> beacon in beacons) {
					gameManager.AddToken (beacon.Key);
					distances.Add (beacon.Key, new double[DISTANCE_BUCKET]);
					for (int i = 0; i < DISTANCE_BUCKET; i++) {
						distances [beacon.Key] [i] = beacon.Value.Accuracy;
					}
				}
			} else {
				Log ("Can't add beacons while game is running or over");
			}
		}

		void StartGame ()
		{
			if (gameState != GameState.Start) {
				gameManager.StartGame ();
				UpdateBeaconInfo ();
			} else {
				Log ("Game already started");
			}
		}

		void HandleEnterRegion (object sender, MonitorEventArgs e)
		{
			Log ("Entered beacon region");
		}

		void HandleExitRegion (object sender, MonitorEventArgs e)
		{
			Log ("Exited beacon region");
		}

		void HandleBeaconInfo (object sender, RangeEventArgs e)
		{
			if (e.Beacons.Count > 0) {
				if (gameState == GameState.Start) {
					foreach (IBeacon beacon in e.Beacons) {
						if (!gameManager.IsTokenFound (beacon.Minor.ToString ())) {
							if ((ProximityType)beacon.Proximity == ProximityType.Immediate) {
								gameManager.MarkTokenFound (beacon.Minor.ToString ());
							} else if (beacons.ContainsKey (beacon.Minor.ToString ())) {
								beacons [beacon.Minor.ToString ()] = beacon;
								for (int i = 0; i < DISTANCE_BUCKET - 1; i++) {
									distances [beacon.Minor.ToString ()] [i] = distances [beacon.Minor.ToString ()] [i + 1];
								}
								distances [beacon.Minor.ToString ()] [DISTANCE_BUCKET - 1] = beacon.Accuracy;
							}
						}
					}
					UpdateBeaconInfo ();
				} else if (gameState == GameState.Off && !beaconsAdded && e.Beacons.Count != beacons.Count) {
					beacons.Clear ();
					foreach (IBeacon beacon in e.Beacons) {
						beacons.Add (beacon.Minor.ToString (), beacon);
					}
					Log (string.Format ("{0} beacons detected", beacons.Count));
				}
			}
		}

		void HandleAllTokensFound ()
		{
			Log ("All beacons found. You win!");
			gameManager.RemoveAllTokens ();
			beacons.Clear ();
			UpdateBeaconInfo ();
			beaconsAdded = false;
			beaconManager.UnBind (this);
			scanning = false;
		}

		void HandleGameStateChange (object sender, GameStateEventArgs e)
		{
			gameState = e.GameState;
		}

		void HandleTokenFound (object sender, TokenEventArgs token)
		{
			Log (string.Format ("Beacon with id {0} found", token.ID));
			UpdateBeaconInfo ();
		}

		void UpdateBeaconInfo ()
		{
			string beaconInfo = string.Empty;
			foreach (KeyValuePair<string, IBeacon> beacon in beacons) {
				string distance = string.Empty;

				if ((ProximityType)beacon.Value.Proximity == ProximityType.Near) {
					distance = "Near";
				} else if ((ProximityType)beacon.Value.Proximity == ProximityType.Far) {
					distance = "Far";
				} else {
					distance = "Unknown";
				}

				if (gameManager.IsTokenFound (beacon.Key)) {
					beaconInfo += string.Format ("Beacon id {0} FOUND\n", beacon.Key);
				} else {
					beaconInfo += string.Format ("Beacon id {0} is {1} with distance: {2}\n", beacon.Key, distance, distances [beacon.Key].Average ());
				}
			}

			RunOnUiThread (() => {
				beaconInfoText.Text = beaconInfo;
			});
		}

		void Log (string message)
		{
			RunOnUiThread (() => {
				console.Text = message + "\n" + console.Text;
			});
		}
	}
}

