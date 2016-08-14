﻿using System;
using System.IO;
using System.Threading.Tasks;

using Android.App;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;

using Android.Support.Design.Widget;
using Android.Support.V4.View;

using ServiceStack;

using SettingsStudio;

using XWeather.Clients;
using XWeather.Domain;

using Toolbar = Android.Support.V7.Widget.Toolbar;


namespace XWeather.Droid
{
	[Activity (Label = "XWeather", MainLauncher = true,
			   Icon = "@mipmap/icon", LaunchMode = Android.Content.PM.LaunchMode.SingleTop,
			   ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	public class WeatherActivity : BaseActivity, FloatingActionButton.IOnClickListener
	{
		ViewPager viewPager;

		WeatherPagerAdapter PagerAdapter;

		LocationProvider LocationProvider;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			Bootstrap.Run (this, Application);

			base.OnCreate (savedInstanceState);

			SetContentView (Resource.Layout.WeatherActivity);

			LocationProvider = new LocationProvider (this);


			//var toolbar = FindViewById<Toolbar> (Resource.Id.toolbar);

			//SetSupportActionBar (toolbar);


			var fab = FindViewById<FloatingActionButton> (Resource.Id.floatingButton);

			fab.SetOnClickListener (this);


			setupViewPager ();

			getData ();
		}


		protected override void OnStart ()
		{
			base.OnStart ();

			reloadData ();
		}


		public void OnClick (View v) => StartActivity (typeof (LocationsActivity));


		protected override void HandleUpdatedSelectedLocation (object sender, EventArgs e)
		{
			RunOnUiThread (() => {
				updateToolbarButtons (true);
				reloadData ();
				Settings.LocationsJson = WuClient.Shared.Locations.GetLocationsJson ();
			});
		}


		void updateToolbarButtons (bool dismissing)
		{
			//foreach (var button in toolbarButtons)
			//button.Hidden = dismissing ? button.Tag > 1 : button.Tag < 2;

			//pageIndicator.Hidden = !dismissing;
		}


		void reloadData ()
		{
			for (int i = 0; i < 3; i++) {

				var fragment = PagerAdapter.GetFragmentAtPosition (i) as IRecyclerViewFragment;

				fragment?.Adapter?.NotifyDataSetChanged ();
			}

			//updateBackground ();

			//if (WuClient.Shared.HasCurrent) removeEmptyView ();

			//foreach (var controller in Controllers) controller?.TableView?.ReloadData ();
		}


		void setupViewPager ()
		{
			PagerAdapter = new WeatherPagerAdapter (SupportFragmentManager);

			viewPager = (ViewPager)FindViewById (Resource.Id.viewPager);
			viewPager.Adapter = PagerAdapter;

			updateBackground ();

			//var tabLayout = (TabLayout)FindViewById (Resource.Id.tabLayout);
			//tabLayout.SetupWithViewPager (viewPager);

			viewPager.PageSelected += (sender, e) => {

				//System.Diagnostics.Debug.WriteLine ("PageSelected");

				updateBackground ();

				//update the query listener
				//var fragment = PagerAdapter.GetFragmentAtPosition (e.Position);
			};
		}


		void updateBackground ()
		{
			var location = WuClient.Shared.Selected;

			//var random = location == null || Settings.RandomBackgrounds;
			var random = true;

			var gradients = location.GetTimeOfDayGradient (random);

			using (var gd = new GradientDrawable (GradientDrawable.Orientation.TopBottom, gradients.Item1.ToArray ())) {

				gd.SetCornerRadius (0f);

				if (viewPager.Background == null) {

					viewPager.Background = gd;

				} else {

					var backgrounds = new Drawable [2];

					backgrounds [0] = viewPager.Background;
					backgrounds [1] = gd;

					var crossfader = new TransitionDrawable (backgrounds);

					viewPager.Background = crossfader;

					crossfader.StartTransition (1000);
				}
			}
		}


		void getData ()
		{
#if DEBUG

			Task.Run (async () => {

				await Task.Delay (10);

				foreach (var location in TestData.Locations) {

					var name = location.name.Split (',') [0].Replace (' ', '_');

					var path = $"{name}.json";

					using (var sr = new StreamReader (Assets.Open (path))) {

						var json = sr.ReadToEnd ();

						var weather = json?.FromJson<WuWeather> ();

						WuClient.Shared.Locations.Add (new WuLocation (location, weather));
					}
				}

				var i = new Random ().Next (5);

				WuClient.Shared.Selected = WuClient.Shared.Locations [i];
			});

#else

			Task.Run (async () => {

				var location = await LocationProvider.GetCurrentLocationAsync ();

				if (location != null) {

					await WuClient.Shared.GetLocations (Settings.LocationsJson, location.Latitude, location.Longitude);
				}
			});

#endif
		}
	}
}