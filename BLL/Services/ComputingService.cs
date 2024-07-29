using BLL.Shared;
using BLL.Shared.Cash;
using BLL.Shared.RabbitMessages;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace BLL.Services
{
    public class ComputingService : IComputingService
    {
        IMemoryCache _cache;
        public ComputingService(IMemoryCache memoryCach) {
            _cache = memoryCach;
        }

        public double? CountDistance(AeroportsJob job)
        {
            if (job != null && job.Distance == null) //если выражение еще не вычислено
            {
                _cache.TryGetValue(job.FromIATACode, out AeroportsTask fromAeroportTask);
                _cache.TryGetValue(job.ToIATACode, out AeroportsTask toAeroportTask);
                if (fromAeroportTask.Location != null && toAeroportTask.Location != null)
                {
                    var distance = CountDistance(job, fromAeroportTask.Location, toAeroportTask.Location);
                    return distance; 
                }
            }

            return null;
        }

        private double CountDistance(AeroportsJob job, Location fromLocation, Location toLocation) //из сервиса
        {                            
            var distance = getDistanceFromLatLonInKm(fromLocation.lat, fromLocation.lon,
            toLocation.lat, toLocation.lon);
            job.Distance = distance;

            _cache.Set(job.Id, job,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1)));
            return distance;                                       
        }

        private double getDistanceFromLatLonInKm(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Radius of the earth in km
            var dLat = deg2rad(lat2 - lat1);  // deg2rad below
            var dLon = deg2rad(lon2 - lon1);
            var a =
              Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
              Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) *
              Math.Sin(dLon / 2) * Math.Sin(dLon / 2)
              ;
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = R * c; // Distance in km
            return d;
        }

        private double deg2rad(double deg)
        {
            return deg * (Math.PI / 180);
        }
    }
}
