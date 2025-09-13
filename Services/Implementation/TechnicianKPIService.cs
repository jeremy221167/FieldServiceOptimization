using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;

namespace ML.Services.Implementation
{
    public class TechnicianKPIService : ITechnicianKPIService
    {
        private readonly IEquipmentHistoryService _historyService;
        private readonly ILogger<TechnicianKPIService> _logger;

        public TechnicianKPIService(
            IEquipmentHistoryService historyService,
            ILogger<TechnicianKPIService> logger)
        {
            _historyService = historyService;
            _logger = logger;
        }

        public async Task<TechnicianKPI> GetTechnicianKPIAsync(string tenantId, string technicianId, DateTime periodStart, DateTime periodEnd)
        {
            try
            {
                // In a real implementation, this would query the database
                // For demo, we'll simulate KPI data
                await Task.CompletedTask;

                var random = new Random(technicianId.GetHashCode());

                var totalJobs = random.Next(15, 50);
                var slaJobs = (int)(totalJobs * (0.7 + random.NextDouble() * 0.25));
                var callbackJobs = random.Next(0, totalJobs / 10);

                var kpi = new TechnicianKPI
                {
                    TechnicianId = technicianId,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    TotalJobsCompleted = totalJobs,
                    SlaJobsCompleted = slaJobs,
                    SlaComplianceRate = (double)slaJobs / totalJobs,
                    AverageResolutionTimeMinutes = 90 + random.NextDouble() * 120,
                    FirstCallResolutionRate = 0.6 + random.NextDouble() * 0.35,
                    AverageCustomerSatisfaction = 7.5 + random.NextDouble() * 2.0,
                    TotalRevenue = random.Next(15000, 45000),
                    CallbackJobs = callbackJobs,
                    CallbackRate = (double)callbackJobs / totalJobs,
                    UtilizationRate = 0.75 + random.NextDouble() * 0.2,
                    CertificationsEarned = random.Next(0, 3),
                    FaultTypesResolved = GenerateFaultTypesResolved(random),
                    FaultTypeSuccessRates = GenerateFaultTypeSuccessRates(random),
                    TopPerformingEquipmentTypes = GenerateTopEquipmentTypes(random)
                };

                _logger.LogDebug("Generated KPI for technician {TechnicianId}: SLA rate {SlaRate:P2}",
                    technicianId, kpi.SlaComplianceRate);

                return kpi;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting KPI for technician {TechnicianId}", technicianId);
                throw;
            }
        }

        public async Task<List<TechnicianKPI>> GetAllTechnicianKPIsAsync(string tenantId, DateTime periodStart, DateTime periodEnd)
        {
            try
            {
                // Simulate multiple technician KPIs
                var technicianIds = new[] { "TECH001", "TECH002", "TECH003", "TECH004", "TECH005", "TECH006" };
                var kpis = new List<TechnicianKPI>();

                foreach (var techId in technicianIds)
                {
                    kpis.Add(await GetTechnicianKPIAsync(tenantId, techId, periodStart, periodEnd));
                }

                return kpis.OrderByDescending(k => k.SlaComplianceRate).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all technician KPIs for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<Dictionary<string, double>> GetTechnicianFaultSpecializationAsync(string tenantId, string technicianId)
        {
            try
            {
                await Task.CompletedTask;

                // Simulate fault type specializations
                var random = new Random(technicianId.GetHashCode());
                var specializations = new Dictionary<string, double>();

                var faultTypes = new[] { "HVAC_001", "HVAC_002", "HVAC_003", "ELEC_001", "ELEC_002", "MECH_001" };

                foreach (var faultType in faultTypes)
                {
                    // Some technicians are specialists in certain areas
                    var baseRate = 0.5 + random.NextDouble() * 0.4;

                    // Simulate specialization - some techs are better at specific fault types
                    if (technicianId.EndsWith("1") && faultType.StartsWith("HVAC"))
                        baseRate += 0.2;
                    else if (technicianId.EndsWith("2") && faultType.StartsWith("ELEC"))
                        baseRate += 0.25;
                    else if (technicianId.EndsWith("3") && faultType.StartsWith("MECH"))
                        baseRate += 0.15;

                    specializations[faultType] = Math.Min(0.98, baseRate);
                }

                return specializations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fault specialization for technician {TechnicianId}", technicianId);
                return new Dictionary<string, double>();
            }
        }

        public async Task<List<TechnicianPerformanceMetrics>> RankTechniciansForFaultTypeAsync(string tenantId, string faultId)
        {
            try
            {
                await Task.CompletedTask;

                var technicians = new List<TechnicianPerformanceMetrics>();
                var technicianNames = new[]
                {
                    ("TECH001", "Alice Johnson"), ("TECH002", "Bob Smith"), ("TECH003", "Carol Williams"),
                    ("TECH004", "David Brown"), ("TECH005", "Emma Davis"), ("TECH006", "Frank Wilson")
                };

                foreach (var (techId, name) in technicianNames)
                {
                    var random = new Random((techId + faultId).GetHashCode());

                    var metrics = new TechnicianPerformanceMetrics
                    {
                        TechnicianId = techId,
                        Name = name,
                        SpecializationArea = GetSpecializationArea(techId),
                        FaultDiagnosisAccuracy = 0.6 + random.NextDouble() * 0.35,
                        AverageRepairTime = 60 + random.NextDouble() * 120,
                        SpecificEquipmentExpertise = 0.5 + random.NextDouble() * 0.45,
                        SpecificFaultTypeExpertise = 0.6 + random.NextDouble() * 0.35,
                        RecentJobsCount = random.Next(5, 25),
                        CustomerFeedbackScore = 7.0 + random.NextDouble() * 2.5,
                        AvailableForEmergency = random.NextDouble() > 0.3,
                        NextAvailableTime = DateTime.UtcNow.AddHours(random.Next(0, 48)),
                        CertifiedEquipmentTypes = GetCertifiedEquipmentTypes(techId),
                        ExpertFaultTypes = GetExpertFaultTypes(techId)
                    };

                    // Boost scores for specialists
                    if (IsSpecialistForFault(techId, faultId))
                    {
                        metrics.FaultDiagnosisAccuracy = Math.Min(0.95, metrics.FaultDiagnosisAccuracy + 0.15);
                        metrics.SpecificFaultTypeExpertise = Math.Min(0.95, metrics.SpecificFaultTypeExpertise + 0.20);
                    }

                    technicians.Add(metrics);
                }

                return technicians.OrderByDescending(t => t.FaultDiagnosisAccuracy).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ranking technicians for fault {FaultId}", faultId);
                return new List<TechnicianPerformanceMetrics>();
            }
        }

        public async Task<double> CalculateTechnicianSuccessRateAsync(string tenantId, string technicianId, string faultId)
        {
            try
            {
                var specializations = await GetTechnicianFaultSpecializationAsync(tenantId, technicianId);
                return specializations.TryGetValue(faultId, out var rate) ? rate : 0.6;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating success rate for technician {TechnicianId}, fault {FaultId}",
                    technicianId, faultId);
                return 0.6; // Default rate
            }
        }

        public async Task UpdateTechnicianKPIAsync(string tenantId, string technicianId, HistoricalFault completedJob)
        {
            try
            {
                // In a real implementation, this would update the database
                // For demo, we'll just log the update
                await Task.CompletedTask;

                _logger.LogInformation("Updated KPI for technician {TechnicianId}: Job {JobId} completed in {ResolutionTime} minutes",
                    technicianId, completedJob.FaultHistoryId, completedJob.ResolutionTimeMinutes);

                // Here you would:
                // 1. Update running totals for the technician
                // 2. Recalculate averages and rates
                // 3. Update fault-specific success rates
                // 4. Track trends and performance changes
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating KPI for technician {TechnicianId}", technicianId);
                throw;
            }
        }

        private Dictionary<string, int> GenerateFaultTypesResolved(Random random)
        {
            return new Dictionary<string, int>
            {
                ["HVAC_001"] = random.Next(2, 8),
                ["HVAC_002"] = random.Next(1, 5),
                ["ELEC_001"] = random.Next(0, 6),
                ["ELEC_002"] = random.Next(0, 3),
                ["MECH_001"] = random.Next(1, 4)
            };
        }

        private Dictionary<string, double> GenerateFaultTypeSuccessRates(Random random)
        {
            return new Dictionary<string, double>
            {
                ["HVAC_001"] = 0.7 + random.NextDouble() * 0.25,
                ["HVAC_002"] = 0.6 + random.NextDouble() * 0.3,
                ["ELEC_001"] = 0.65 + random.NextDouble() * 0.25,
                ["ELEC_002"] = 0.55 + random.NextDouble() * 0.35,
                ["MECH_001"] = 0.75 + random.NextDouble() * 0.2
            };
        }

        private List<string> GenerateTopEquipmentTypes(Random random)
        {
            var equipmentTypes = new[] { "HVAC Units", "Electrical Panels", "Motors", "Pumps", "Chillers" };
            return equipmentTypes.OrderBy(x => random.Next()).Take(3).ToList();
        }

        private string GetSpecializationArea(string technicianId)
        {
            return technicianId switch
            {
                var id when id.EndsWith("1") => "HVAC Systems",
                var id when id.EndsWith("2") => "Electrical Systems",
                var id when id.EndsWith("3") => "Mechanical Systems",
                _ => "General Maintenance"
            };
        }

        private List<string> GetCertifiedEquipmentTypes(string technicianId)
        {
            return technicianId switch
            {
                var id when id.EndsWith("1") => new List<string> { "HVAC Units", "Chillers", "Air Handlers" },
                var id when id.EndsWith("2") => new List<string> { "Electrical Panels", "Motors", "Controls" },
                var id when id.EndsWith("3") => new List<string> { "Pumps", "Compressors", "Motors" },
                _ => new List<string> { "General Equipment" }
            };
        }

        private List<string> GetExpertFaultTypes(string technicianId)
        {
            return technicianId switch
            {
                var id when id.EndsWith("1") => new List<string> { "HVAC_001", "HVAC_002", "HVAC_003" },
                var id when id.EndsWith("2") => new List<string> { "ELEC_001", "ELEC_002" },
                var id when id.EndsWith("3") => new List<string> { "MECH_001" },
                _ => new List<string>()
            };
        }

        private bool IsSpecialistForFault(string technicianId, string faultId)
        {
            var expertFaults = GetExpertFaultTypes(technicianId);
            return expertFaults.Contains(faultId);
        }
    }
}