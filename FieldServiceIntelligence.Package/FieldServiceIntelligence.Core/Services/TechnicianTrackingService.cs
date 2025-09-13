using Microsoft.Extensions.Logging;
using FieldServiceIntelligence.Core.Interfaces;
using FieldServiceIntelligence.Core.Models;

namespace FieldServiceIntelligence.Core.Services
{
    public class TechnicianTrackingService : ITechnicianTrackingService
    {
        private readonly ILogger<TechnicianTrackingService> _logger;
        private static readonly Dictionary<string, List<TrackingUpdate>> _trackingHistory = new();
        private static readonly Dictionary<string, TechnicianStatus> _currentStatus = new();

        public TechnicianTrackingService(ILogger<TechnicianTrackingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> UpdateTechnicianLocationAsync(TrackingUpdate update)
        {
            try
            {
                if (update == null || string.IsNullOrEmpty(update.TechnicianId))
                {
                    return false;
                }

                // Store tracking history
                if (!_trackingHistory.ContainsKey(update.TechnicianId))
                {
                    _trackingHistory[update.TechnicianId] = new List<TrackingUpdate>();
                }

                _trackingHistory[update.TechnicianId].Add(update);

                // Keep only last 100 updates per technician
                if (_trackingHistory[update.TechnicianId].Count > 100)
                {
                    _trackingHistory[update.TechnicianId] = _trackingHistory[update.TechnicianId]
                        .OrderByDescending(t => t.Timestamp)
                        .Take(100)
                        .ToList();
                }

                // Update current status
                var status = _currentStatus.GetValueOrDefault(update.TechnicianId, new TechnicianStatus());
                status.CurrentLatitude = update.Latitude;
                status.CurrentLongitude = update.Longitude;
                status.LastLocationUpdate = update.Timestamp;
                status.Status = update.Status;
                status.EstimatedArrivalMinutes = update.EstimatedArrivalMinutes;
                status.IsTracking = true;

                _currentStatus[update.TechnicianId] = status;

                _logger.LogDebug("Updated location for technician {TechnicianId} at {Timestamp}",
                    update.TechnicianId, update.Timestamp);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating technician location for {TechnicianId}", update?.TechnicianId);
                return false;
            }
        }

        public async Task<TechnicianStatus> GetTechnicianStatusAsync(string technicianId)
        {
            try
            {
                if (string.IsNullOrEmpty(technicianId))
                {
                    return new TechnicianStatus();
                }

                var status = _currentStatus.GetValueOrDefault(technicianId, new TechnicianStatus
                {
                    Status = "Unknown",
                    IsTracking = false
                });

                return await Task.FromResult(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting technician status for {TechnicianId}", technicianId);
                return new TechnicianStatus
                {
                    Status = "Error",
                    IsTracking = false
                };
            }
        }

        public async Task<List<TrackingUpdate>> GetRecentTrackingHistoryAsync(string technicianId, int hours = 24)
        {
            try
            {
                if (string.IsNullOrEmpty(technicianId))
                {
                    return new List<TrackingUpdate>();
                }

                if (!_trackingHistory.ContainsKey(technicianId))
                {
                    return new List<TrackingUpdate>();
                }

                var cutoffTime = DateTime.UtcNow.AddHours(-hours);
                var recentUpdates = _trackingHistory[technicianId]
                    .Where(update => update.Timestamp >= cutoffTime)
                    .OrderByDescending(update => update.Timestamp)
                    .ToList();

                return await Task.FromResult(recentUpdates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracking history for {TechnicianId}", technicianId);
                return new List<TrackingUpdate>();
            }
        }

        public async Task<bool> StartTrackingAsync(string technicianId)
        {
            try
            {
                if (string.IsNullOrEmpty(technicianId))
                    return false;

                var status = _currentStatus.GetValueOrDefault(technicianId, new TechnicianStatus());
                status.IsTracking = true;
                status.LastLocationUpdate = DateTime.UtcNow;
                _currentStatus[technicianId] = status;

                _logger.LogInformation("Started tracking for technician {TechnicianId}", technicianId);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting tracking for {TechnicianId}", technicianId);
                return false;
            }
        }

        public async Task<bool> StopTrackingAsync(string technicianId)
        {
            try
            {
                if (string.IsNullOrEmpty(technicianId))
                    return false;

                if (_currentStatus.ContainsKey(technicianId))
                {
                    _currentStatus[technicianId].IsTracking = false;
                }

                _logger.LogInformation("Stopped tracking for technician {TechnicianId}", technicianId);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping tracking for {TechnicianId}", technicianId);
                return false;
            }
        }

        public async Task<Dictionary<string, TechnicianStatus>> GetAllTechnicianStatusAsync()
        {
            try
            {
                return await Task.FromResult(new Dictionary<string, TechnicianStatus>(_currentStatus));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all technician status");
                return new Dictionary<string, TechnicianStatus>();
            }
        }

        public async Task<double> CalculateDistanceTraveledAsync(string technicianId, DateTime since)
        {
            try
            {
                if (string.IsNullOrEmpty(technicianId) || !_trackingHistory.ContainsKey(technicianId))
                    return 0.0;

                var relevantUpdates = _trackingHistory[technicianId]
                    .Where(u => u.Timestamp >= since)
                    .OrderBy(u => u.Timestamp)
                    .ToList();

                if (relevantUpdates.Count < 2)
                    return 0.0;

                double totalDistance = 0.0;
                for (int i = 1; i < relevantUpdates.Count; i++)
                {
                    var prev = relevantUpdates[i - 1];
                    var current = relevantUpdates[i];

                    var distance = CalculateDistance(prev.Latitude, prev.Longitude, current.Latitude, current.Longitude);
                    totalDistance += distance;
                }

                return await Task.FromResult(totalDistance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating distance traveled for {TechnicianId}", technicianId);
                return 0.0;
            }
        }

        private static double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusKm = 6371.0;

            var lat1Rad = DegreesToRadians(lat1);
            var lng1Rad = DegreesToRadians(lng1);
            var lat2Rad = DegreesToRadians(lat2);
            var lng2Rad = DegreesToRadians(lng2);

            var deltaLat = lat2Rad - lat1Rad;
            var deltaLng = lng2Rad - lng1Rad;

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}