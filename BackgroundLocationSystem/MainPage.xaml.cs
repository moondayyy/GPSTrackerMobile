﻿using Plugin.Geolocator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;

namespace BackgroundLocationSystem
{
    public partial class MainPage : ContentPage
    {
        private HubConnection connection;
        public MainPage()
        {
            InitializeComponent();
            connection = new HubConnectionBuilder()
            .WithUrl("https://gpstrackertr.herokuapp.com/location")
            .Build();
            connection.StartAsync();
            connection.Closed += Connection_Closed;
            if (Device.RuntimePlatform == Device.Android)
            {
                MessagingCenter.Subscribe<LocationMessage>(this, "Location", message => {
                    if (connection.State == HubConnectionState.Disconnected)
                    {
                        //connection.StopAsync();
                        connection.StartAsync();
                    }
                    connection.InvokeAsync("Connect", tag.Text, message.Latitude.ToString(), message.Longitude.ToString());
                    Device.BeginInvokeOnMainThread(() => {
                        locationLabel.Text += $"{Environment.NewLine}{message.Latitude}, {message.Longitude}, {DateTime.Now.ToLongTimeString()}";

                        

                        Console.WriteLine($"{message.Latitude}, {message.Longitude}, {DateTime.Now.ToLongTimeString()}");
                    });
                });

                MessagingCenter.Subscribe<StopServiceMessage>(this, "ServiceStopped", message => {
                    Device.BeginInvokeOnMainThread(() => {
                        locationLabel.Text = "Location Service has been stopped!";
                        connection.InvokeAsync("DisconnectedC", tag.Text);
                    });
                });

                MessagingCenter.Subscribe<LocationErrorMessage>(this, "LocationError", message => {
                    Device.BeginInvokeOnMainThread(() => {
                        locationLabel.Text = "There was an error updating location!";
                    });
                });

                if (Preferences.Get("LocationServiceRunning", false) == true)
                {
                    StartService();
                }
            }
        }

        private Task Connection_Closed(Exception arg)
        {
            connection.StartAsync();
            throw new NotImplementedException();
        }

        async void Button_Clicked(System.Object sender, System.EventArgs e)
        {
            var permission = await Xamarin.Essentials.Permissions.RequestAsync<Xamarin.Essentials.Permissions.LocationAlways>();

            if (permission == Xamarin.Essentials.PermissionStatus.Denied)
            {
                // TODO Let the user know they need to accept
                return;
            }

            if (Device.RuntimePlatform == Device.iOS)
            {
                if (CrossGeolocator.Current.IsListening)
                {
                    await CrossGeolocator.Current.StopListeningAsync();
                    CrossGeolocator.Current.PositionChanged -= Current_PositionChanged;

                    return;
                }

                await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(1), 10, false, new Plugin.Geolocator.Abstractions.ListenerSettings
                {
                    ActivityType = Plugin.Geolocator.Abstractions.ActivityType.AutomotiveNavigation,
                    AllowBackgroundUpdates = true,
                    DeferLocationUpdates = false,
                    DeferralDistanceMeters = 10,
                    DeferralTime = TimeSpan.FromSeconds(5),
                    ListenForSignificantChanges = true,
                    PauseLocationUpdatesAutomatically = true
                });

                CrossGeolocator.Current.PositionChanged += Current_PositionChanged;
            }
            else if (Device.RuntimePlatform == Device.Android)
            {
                if (Preferences.Get("LocationServiceRunning", false) == false)
                {
                    StartService();
                }
                else
                {
                    StopService();
                }
            }
        }

        private void StartService()
        {
            var startServiceMessage = new StartServiceMessage();
            MessagingCenter.Send(startServiceMessage, "ServiceStarted");
            Preferences.Set("LocationServiceRunning", true);
            locationLabel.Text = "Location Service has been started!";
        }

        private void StopService()
        {
            var stopServiceMessage = new StopServiceMessage();
            MessagingCenter.Send(stopServiceMessage, "ServiceStopped");
            Preferences.Set("LocationServiceRunning", false);
        }

        private void Current_PositionChanged(object sender, Plugin.Geolocator.Abstractions.PositionEventArgs e)
        {
            locationLabel.Text += $"{e.Position.Latitude}, {e.Position.Longitude}, {e.Position.Timestamp.TimeOfDay}{Environment.NewLine}";

            Console.WriteLine($"{e.Position.Latitude}, {e.Position.Longitude}, {e.Position.Timestamp.TimeOfDay}");
        }
    }
}
