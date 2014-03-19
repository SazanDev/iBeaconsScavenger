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
using RadiusNetworks.IBeaconAndroid;

namespace Scavenger
{
	public class MonitorEventArgs : EventArgs
	{
		public Region Region { get; set; }
		public int State { get; set; }
	}

	public class RangeEventArgs : EventArgs
	{
		public Region Region { get; set; }
		public ICollection<IBeacon> Beacons { get; set; }
	}

	public class BeaconNotifier : Java.Lang.Object, IMonitorNotifier, IRangeNotifier
	{
		public event EventHandler<MonitorEventArgs> DetermineStateForRegionComplete;
		public event EventHandler<MonitorEventArgs> EnterRegionComplete;
		public event EventHandler<MonitorEventArgs> ExitRegionComplete;
		public event EventHandler<RangeEventArgs> DidRangeBeaconsInRegionComplete;

		public void DidDetermineStateForRegion (int state, Region region)
		{
			if (DetermineStateForRegionComplete != null) {
				DetermineStateForRegionComplete (this, new MonitorEventArgs { Region = region, State = state });
			}
		}

		public void DidEnterRegion (Region region)
		{
			if (EnterRegionComplete != null) {
				EnterRegionComplete (this, new MonitorEventArgs { Region = region });
			}
		}

		public void DidExitRegion (Region region)
		{
			if (ExitRegionComplete != null) {
				ExitRegionComplete (this, new MonitorEventArgs { Region = region });
			}
		}

		public void DidRangeBeaconsInRegion (ICollection<IBeacon> beacons, Region region)
		{
			if (DidRangeBeaconsInRegionComplete != null) {
				DidRangeBeaconsInRegionComplete (this, new RangeEventArgs { Beacons = beacons, Region = region });
			}
		}
	}
}

