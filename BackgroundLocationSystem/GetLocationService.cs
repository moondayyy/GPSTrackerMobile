﻿
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BackgroundLocationSystem
{
    public class GetLocationService
    {
        
        readonly bool stopping = false;
        public GetLocationService()
        {

        }
        public async Task Run(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                while (!stopping)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        await Task.Delay(2000);
                        var request = new GeolocationRequest(GeolocationAccuracy.High);
                        var location = await Geolocation.GetLocationAsync(request);
                        if (location != null)
                        {
                            var message = new LocationMessage
                            {
                                Latitude = location.Latitude,
                                Longitude = location.Longitude
                            };

                            Device.BeginInvokeOnMainThread(() =>
                            {
                                MessagingCenter.Send(message, "Location");
                            });
                        }
                    }
                    catch (Exception ex)
                    {

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            var errorMessage = new LocationErrorMessage();
                            MessagingCenter.Send(errorMessage, "LocationError");
                        });
                    }
                }
                return;
            }, token);
        }
    }
}
