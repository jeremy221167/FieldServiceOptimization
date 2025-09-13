using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;

namespace ML.Services.Implementation
{
    public class FaultDiagnosisService : IFaultDiagnosisService
    {
        private readonly IEquipmentHistoryService _historyService;
        private readonly IPartsRecommendationService _partsService;
        private readonly ITechnicianKPIService _kpiService;
        private readonly IFaultMLService _faultMLService;
        private readonly ILogger<FaultDiagnosisService> _logger;

        public FaultDiagnosisService(
            IEquipmentHistoryService historyService,
            IPartsRecommendationService partsService,
            ITechnicianKPIService kpiService,
            IFaultMLService faultMLService,
            ILogger<FaultDiagnosisService> logger)
        {
            _historyService = historyService;
            _partsService = partsService;
            _kpiService = kpiService;
            _faultMLService = faultMLService;
            _logger = logger;
        }

        public async Task<FaultDiagnosisResponse> DiagnoseFaultAsync(FaultDiagnosisRequest request)
        {
            try
            {
                _logger.LogInformation("Starting fault diagnosis for equipment {EquipmentId}, tenant {TenantId}",
                    request.EquipmentId, request.TenantId);

                var response = new FaultDiagnosisResponse
                {
                    DiagnosisId = Guid.NewGuid().ToString(),
                    TenantId = request.TenantId,
                    EquipmentId = request.EquipmentId,
                    ModelVersion = "1.0.0"
                };

                // Predict possible faults
                response.PossibleFaults = await PredictPossibleFaultsAsync(
                    request.TenantId,
                    request.EquipmentId,
                    request.ReportedSymptoms);

                // Get parts recommendations if requested
                if (request.IncludePartsRecommendations && response.PossibleFaults.Any())
                {
                    response.RecommendedParts = await RecommendPartsAsync(request.TenantId, response.PossibleFaults);
                }

                // Get technician recommendations if requested
                if (request.IncludeTechnicianRecommendations && response.PossibleFaults.Any())
                {
                    response.RecommendedTechnicians = await RecommendTechniciansForFaultAsync(request.TenantId, response.PossibleFaults);
                }

                // Calculate estimates
                if (response.PossibleFaults.Any())
                {
                    response.EstimatedResolutionTimeMinutes = (int)response.PossibleFaults
                        .Take(3)
                        .Average(f => f.EstimatedResolutionTime);

                    response.EstimatedCost = response.RecommendedParts?.Sum(p => p.TotalCost) ?? 0;

                    response.RecommendedActions = GenerateRecommendedActions(response.PossibleFaults.Take(3).ToList());
                }

                _logger.LogInformation("Completed fault diagnosis for equipment {EquipmentId}. Found {FaultCount} possible faults",
                    request.EquipmentId, response.PossibleFaults.Count);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during fault diagnosis for equipment {EquipmentId}, tenant {TenantId}",
                    request.EquipmentId, request.TenantId);
                throw;
            }
        }

        public async Task<List<FaultPrediction>> PredictPossibleFaultsAsync(string tenantId, string equipmentId, string symptoms)
        {
            try
            {
                // Get historical fault data for this equipment
                var faultHistory = await _historyService.GetEquipmentFaultHistoryAsync(tenantId, equipmentId);

                // Create sample fault predictions based on symptoms and history
                var predictions = new List<FaultPrediction>();

                // This would normally use ML.NET models, but for demo we'll simulate with logic
                var symptomKeywords = symptoms.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Sample fault types with their typical keywords
                var faultPatterns = new Dictionary<string, (string FaultCode, string FaultName, string Category, string Severity, int ResolutionTime, List<string> Keywords)>
                {
                    ["HVAC_001"] = ("H001", "Compressor Failure", "Mechanical", "Critical", 240, new[] { "compressor", "noise", "vibration", "overheating" }.ToList()),
                    ["HVAC_002"] = ("H002", "Refrigerant Leak", "System", "High", 180, new[] { "leak", "cooling", "ice", "pressure" }.ToList()),
                    ["HVAC_003"] = ("H003", "Filter Blockage", "Maintenance", "Medium", 60, new[] { "airflow", "filter", "blocked", "reduced" }.ToList()),
                    ["ELEC_001"] = ("E001", "Circuit Breaker Trip", "Electrical", "High", 90, new[] { "power", "breaker", "trip", "electrical" }.ToList()),
                    ["ELEC_002"] = ("E002", "Wiring Fault", "Electrical", "Critical", 300, new[] { "wiring", "sparks", "burning", "smell" }.ToList()),
                    ["MECH_001"] = ("M001", "Motor Bearing Wear", "Mechanical", "High", 120, new[] { "motor", "bearing", "grinding", "noise" }.ToList())
                };

                foreach (var pattern in faultPatterns)
                {
                    var matchCount = pattern.Value.Keywords.Count(keyword =>
                        symptomKeywords.Any(symptom => symptom.Contains(keyword)));

                    if (matchCount > 0)
                    {
                        var probability = Math.Min(0.95, (double)matchCount / pattern.Value.Keywords.Count * 0.8 + 0.1);

                        // Adjust probability based on historical data
                        var historicalOccurrences = faultHistory.Count(h => h.FaultId == pattern.Key);
                        if (historicalOccurrences > 0)
                        {
                            probability *= 1.2; // Increase probability if this fault occurred before
                        }

                        predictions.Add(new FaultPrediction
                        {
                            FaultId = pattern.Key,
                            FaultCode = pattern.Value.FaultCode,
                            FaultName = pattern.Value.FaultName,
                            ProbabilityScore = Math.Min(0.98, probability),
                            Category = pattern.Value.Category,
                            Severity = pattern.Value.Severity,
                            EstimatedResolutionTime = pattern.Value.ResolutionTime,
                            PredictedCause = GeneratePredictedCause(pattern.Value.FaultName, symptoms),
                            DiagnosisExplanation = $"Based on symptoms '{symptoms}' and historical patterns",
                            RequiredSkills = GetRequiredSkills(pattern.Value.Category),
                            HistoricalSuccessRate = CalculateHistoricalSuccessRate(faultHistory, pattern.Key)
                        });
                    }
                }

                return predictions.OrderByDescending(p => p.ProbabilityScore).Take(5).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting faults for equipment {EquipmentId}", equipmentId);
                return new List<FaultPrediction>();
            }
        }

        public async Task<List<PartRecommendation>> RecommendPartsAsync(string tenantId, List<FaultPrediction> faultPredictions)
        {
            try
            {
                var allRecommendations = new List<PartRecommendation>();

                foreach (var fault in faultPredictions.Take(3)) // Top 3 faults only
                {
                    var partsForFault = await _partsService.GetPartsForFaultAsync(tenantId, fault.FaultId, "");

                    // Weight recommendations by fault probability
                    foreach (var part in partsForFault)
                    {
                        part.RecommendationScore *= fault.ProbabilityScore;
                    }

                    allRecommendations.AddRange(partsForFault);
                }

                // Consolidate duplicate parts
                var consolidatedParts = allRecommendations
                    .GroupBy(p => p.PartId)
                    .Select(g => new PartRecommendation
                    {
                        PartId = g.Key,
                        PartNumber = g.First().PartNumber,
                        PartName = g.First().PartName,
                        RecommendationScore = g.Max(p => p.RecommendationScore),
                        ReasonForRecommendation = string.Join("; ", g.Select(p => p.ReasonForRecommendation).Distinct()),
                        Quantity = g.Sum(p => p.Quantity),
                        UnitCost = g.First().UnitCost,
                        TotalCost = g.Sum(p => p.TotalCost),
                        InStock = g.First().InStock,
                        StockLevel = g.First().StockLevel,
                        EstimatedDelivery = g.First().EstimatedDelivery,
                        HistoricalUsageRate = g.Average(p => p.HistoricalUsageRate)
                    })
                    .OrderByDescending(p => p.RecommendationScore)
                    .Take(10)
                    .ToList();

                return consolidatedParts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recommending parts for tenant {TenantId}", tenantId);
                return new List<PartRecommendation>();
            }
        }

        public async Task<List<TechnicianRecommendation>> RecommendTechniciansForFaultAsync(string tenantId, List<FaultPrediction> faultPredictions)
        {
            try
            {
                var topFault = faultPredictions.FirstOrDefault();
                if (topFault == null) return new List<TechnicianRecommendation>();

                var expertTechnicians = await _kpiService.RankTechniciansForFaultTypeAsync(tenantId, topFault.FaultId);

                return expertTechnicians.Take(5).Select(tech => new TechnicianRecommendation
                {
                    TechnicianId = tech.TechnicianId,
                    Name = tech.Name,
                    Score = tech.FaultDiagnosisAccuracy,
                    SkillsScore = tech.SpecificFaultTypeExpertise,
                    SlaScore = tech.CustomerFeedbackScore / 10.0, // Convert 1-10 to 0-1
                    Distance = CalculateDistance(tech.TechnicianId), // Would be calculated based on location
                    EstimatedTravelTime = 30, // Placeholder
                    Explanation = $"Expert in {topFault.FaultName} with {tech.FaultDiagnosisAccuracy:P1} success rate"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recommending technicians for tenant {TenantId}", tenantId);
                return new List<TechnicianRecommendation>();
            }
        }

        public async Task<double> CalculateFaultProbabilityAsync(string tenantId, string equipmentId, string faultId, string symptoms)
        {
            try
            {
                // This would normally use the ML.NET model
                var faultHistory = await _historyService.GetEquipmentFaultHistoryAsync(tenantId, equipmentId);
                var historicalOccurrences = faultHistory.Count(h => h.FaultId == faultId);

                var baseProbability = historicalOccurrences > 0 ? 0.3 : 0.1;

                // Simple symptom matching for demo
                var symptomMatch = symptoms.ToLower().Contains(faultId.ToLower().Substring(0, 4)) ? 0.4 : 0.1;

                return Math.Min(0.95, baseProbability + symptomMatch + (historicalOccurrences * 0.05));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating fault probability for {FaultId}", faultId);
                return 0.1;
            }
        }

        private string GeneratePredictedCause(string faultName, string symptoms)
        {
            return $"Likely cause of {faultName} based on reported symptoms: {symptoms.Substring(0, Math.Min(50, symptoms.Length))}...";
        }

        private List<string> GetRequiredSkills(string category)
        {
            return category switch
            {
                "Electrical" => new List<string> { "Electrical Systems", "Troubleshooting", "Safety Protocols" },
                "Mechanical" => new List<string> { "Mechanical Repair", "Motor Systems", "Precision Tools" },
                "System" => new List<string> { "System Diagnostics", "HVAC Systems", "Refrigeration" },
                _ => new List<string> { "General Maintenance", "Problem Solving" }
            };
        }

        private double CalculateHistoricalSuccessRate(List<HistoricalFault> faultHistory, string faultId)
        {
            var faultOccurrences = faultHistory.Where(h => h.FaultId == faultId).ToList();
            if (!faultOccurrences.Any()) return 0.75; // Default rate

            return (double)faultOccurrences.Count(f => f.SlaMetric) / faultOccurrences.Count;
        }

        private double CalculateDistance(string technicianId)
        {
            // Placeholder - would normally calculate based on technician and equipment locations
            var random = new Random(technicianId.GetHashCode());
            return random.NextDouble() * 50 + 5; // 5-55 km
        }

        private string GenerateRecommendedActions(List<FaultPrediction> topFaults)
        {
            if (!topFaults.Any()) return "No specific actions recommended.";

            var topFault = topFaults.First();
            return $"1. Verify {topFault.FaultName} symptoms\n2. Check {topFault.Category.ToLower()} components\n3. Prepare required tools and parts\n4. Follow safety protocols for {topFault.Severity.ToLower()} priority repairs";
        }
    }
}