using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;
using System.Collections.Concurrent;

namespace ML.Services.Implementation
{
    public class TechnicianTrackingService : ITechnicianTrackingService
    {
        private readonly ConcurrentDictionary<string, TechnicianStatus> _technicianStatuses;
        private readonly ConcurrentDictionary<string, List<TrackingUpdate>> _trackingHistory;
        private readonly IRecommendationScoring _scoringService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<TechnicianTrackingService> _logger;

        public TechnicianTrackingService(
            IRecommendationScoring scoringService,
            INotificationService notificationService,
            ILogger<TechnicianTrackingService> logger)
        {
            _technicianStatuses = new ConcurrentDictionary<string, TechnicianStatus>();
            _trackingHistory = new ConcurrentDictionary<string, List<TrackingUpdate>>();
            _scoringService = scoringService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task UpdateTechnicianLocationAsync(TrackingUpdate update)
        {
            try
            {
                _logger.LogDebug("Updating location for technician {TechnicianId} at {Timestamp}",
                    update.TechnicianId, update.Timestamp);

                // Update current status
                var status = _technicianStatuses.GetOrAdd(update.TechnicianId, new TechnicianStatus());

                var previousLat = status.CurrentLatitude;
                var previousLng = status.CurrentLongitude;
                var previousUpdate = status.LastLocationUpdate;

                status.CurrentLatitude = update.Latitude;
                status.CurrentLongitude = update.Longitude;
                status.LastLocationUpdate = update.Timestamp;
                status.Status = update.Status;
                status.EstimatedArrivalMinutes = update.EstimatedArrivalMinutes;

                // Calculate speed if we have previous location
                if (previousLat.HasValue && previousLng.HasValue)
                {
                    var distance = _scoringService.CalculateDistance(
                        previousLat.Value, previousLng.Value,
                        update.Latitude, update.Longitude);

                    var timeElapsed = (update.Timestamp - previousUpdate).TotalHours;
                    if (timeElapsed > 0)
                    {
                        update.SpeedKmh = distance / timeElapsed;
                    }
                }

                // Store tracking history
                _trackingHistory.AddOrUpdate(
                    update.TechnicianId,
                    new List<TrackingUpdate> { update },
                    (key, existing) =>
                    {
                        existing.Add(update);
                        // Keep only last 100 updates per technician
                        return existing.TakeLast(100).ToList();
                    });

                // Auto-update ETA if technician is en route
                if (status.Status == "EnRoute" && !string.IsNullOrEmpty(status.CurrentJobLocation))
                {
                    await UpdateETAEstimateAsync(update.TechnicianId, status);
                }

                _logger.LogDebug("Location updated for technician {TechnicianId}: {Status}, ETA: {ETA} min",
                    update.TechnicianId, status.Status, status.EstimatedArrivalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating technician location for {TechnicianId}",
                    update.TechnicianId);
                throw;
            }
        }

        public async Task<TechnicianStatus> GetTechnicianStatusAsync(string technicianId)
        {
            try
            {
                if (_technicianStatuses.TryGetValue(technicianId, out var status))
                {
                    return status;
                }

                _logger.LogWarning("Status not found for technician {TechnicianId}", technicianId);
                return new TechnicianStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting technician status for {TechnicianId}", technicianId);
                return new TechnicianStatus();
            }
        }

        public async Task<double> GetEstimatedArrivalTimeAsync(string technicianId, double destLat, double destLng)
        {
            try
            {
                if (!_technicianStatuses.TryGetValue(technicianId, out var status) ||
                    !status.CurrentLatitude.HasValue || !status.CurrentLongitude.HasValue)
                {
                    _logger.LogWarning("Cannot calculate ETA for technician {TechnicianId} - no current location",
                        technicianId);
                    return 60.0; // Default 60 minutes if no location data
                }

                var distance = _scoringService.CalculateDistance(
                    status.CurrentLatitude.Value, status.CurrentLongitude.Value,
                    destLat, destLng);

                var baseETA = _scoringService.EstimateTravelTime(distance);

                // Adjust ETA based on recent speed if available
                if (_trackingHistory.TryGetValue(technicianId, out var history) && history.Any())
                {
                    var recentUpdates = history.TakeLast(5).Where(u => u.SpeedKmh > 0).ToList();
                    if (recentUpdates.Any())
                    {
                        var avgSpeed = recentUpdates.Average(u => u.SpeedKmh);
                        if (avgSpeed > 0)
                        {
                            var adjustedETA = (distance / avgSpeed) * 60; // Convert to minutes
                            baseETA = (baseETA + adjustedETA) / 2; // Average of estimates
                        }
                    }
                }

                // Add buffer based on traffic and status
                var bufferFactor = status.Status switch
                {
                    "EnRoute" => 1.1, // 10% buffer
                    "OnSite" => 1.5,  // 50% buffer (need to finish current job)
                    "Busy" => 2.0,    // 100% buffer
                    _ => 1.2          // 20% default buffer
                };

                var finalETA = baseETA * bufferFactor;

                _logger.LogDebug("Calculated ETA for technician {TechnicianId}: {ETA} minutes (distance: {Distance} km)",
                    technicianId, finalETA, distance);

                return finalETA;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating ETA for technician {TechnicianId}", technicianId);
                return 60.0; // Default fallback
            }
        }

        public async Task StartTrackingAsync(string technicianId, string jobId)
        {
            try
            {
                var status = _technicianStatuses.GetOrAdd(technicianId, new TechnicianStatus());
                status.IsTracking = true;
                status.CurrentJobLocation = jobId;
                status.Status = "EnRoute";

                _logger.LogInformation("Started tracking technician {TechnicianId} for job {JobId}",
                    technicianId, jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting tracking for technician {TechnicianId}", technicianId);
                throw;
            }
        }

        public async Task StopTrackingAsync(string technicianId)
        {
            try
            {
                if (_technicianStatuses.TryGetValue(technicianId, out var status))
                {
                    status.IsTracking = false;
                    status.Status = "Available";
                    status.CurrentJobLocation = string.Empty;
                    status.EstimatedArrivalMinutes = 0;

                    _logger.LogInformation("Stopped tracking technician {TechnicianId}", technicianId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping tracking for technician {TechnicianId}", technicianId);
                throw;
            }
        }

        private async Task UpdateETAEstimateAsync(string technicianId, TechnicianStatus status)
        {
            try
            {
                // This would typically parse job location to get coordinates
                // For demo purposes, we'll simulate coordinate extraction
                var jobCoordinates = await ParseJobLocationAsync(status.CurrentJobLocation);
                if (jobCoordinates.HasValue)
                {
                    var newETA = await GetEstimatedArrivalTimeAsync(
                        technicianId, jobCoordinates.Value.lat, jobCoordinates.Value.lng);

                    var previousETA = status.EstimatedArrivalMinutes;
                    status.EstimatedArrivalMinutes = newETA;

                    // Notify customer if ETA changed significantly (more than 5 minutes)
                    if (Math.Abs(newETA - previousETA) > 5)
                    {
                        _logger.LogInformation("ETA changed significantly for technician {TechnicianId}: {OldETA} -> {NewETA}",
                            technicianId, previousETA, newETA);

                        // This would trigger customer notification
                        // await _notificationService.SendETAUpdateAsync(customerPhone, technicianName, newETA);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ETA estimate for technician {TechnicianId}", technicianId);
            }
        }

        private async Task<(double lat, double lng)?> ParseJobLocationAsync(string jobLocation)
        {
            // In a real implementation, this would:
            // 1. Look up job details from database
            // 2. Parse address to coordinates using geocoding service
            // 3. Return actual coordinates

            // For demo purposes, return sample NYC coordinates
            await Task.CompletedTask;
            return (40.7128, -74.0060);
        }

        public async Task<List<TrackingUpdate>> GetTrackingHistoryAsync(string technicianId, int maxRecords = 50)
        {
            try
            {
                if (_trackingHistory.TryGetValue(technicianId, out var history))
                {
                    return history.TakeLast(maxRecords).ToList();
                }
                return new List<TrackingUpdate>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracking history for technician {TechnicianId}", technicianId);
                return new List<TrackingUpdate>();
            }
        }

        public async Task<Dictionary<string, TechnicianStatus>> GetAllTechnicianStatusesAsync()
        {
            try
            {
                return _technicianStatuses.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all technician statuses");
                return new Dictionary<string, TechnicianStatus>();
            }
        }
    }
}