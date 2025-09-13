using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;

namespace ML.Services.Implementation
{
    public class PartsRecommendationService : IPartsRecommendationService
    {
        private readonly ILogger<PartsRecommendationService> _logger;
        private static readonly Dictionary<string, List<PartRecommendation>> _faultPartsMapping;

        static PartsRecommendationService()
        {
            _faultPartsMapping = InitializeFaultPartsMapping();
        }

        public PartsRecommendationService(ILogger<PartsRecommendationService> logger)
        {
            _logger = logger;
        }

        public async Task<List<PartRecommendation>> GetPartsForFaultAsync(string tenantId, string faultId, string equipmentId)
        {
            try
            {
                await Task.CompletedTask;

                if (_faultPartsMapping.TryGetValue(faultId, out var parts))
                {
                    // Clone the parts and adjust scores based on equipment specifics
                    return parts.Select(p => new PartRecommendation
                    {
                        PartId = p.PartId,
                        PartNumber = p.PartNumber,
                        PartName = p.PartName,
                        RecommendationScore = p.RecommendationScore * GetEquipmentSpecificMultiplier(equipmentId),
                        ReasonForRecommendation = p.ReasonForRecommendation,
                        Quantity = p.Quantity,
                        UnitCost = p.UnitCost,
                        TotalCost = p.UnitCost * p.Quantity,
                        InStock = p.InStock,
                        StockLevel = p.StockLevel,
                        EstimatedDelivery = p.InStock ? "Same Day" : "2-3 Business Days",
                        HistoricalUsageRate = p.HistoricalUsageRate
                    }).OrderByDescending(p => p.RecommendationScore).ToList();
                }

                _logger.LogWarning("No parts found for fault {FaultId}", faultId);
                return new List<PartRecommendation>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parts for fault {FaultId}", faultId);
                return new List<PartRecommendation>();
            }
        }

        public async Task<List<PartRecommendation>> PredictPartsNeededAsync(string tenantId, List<FaultPrediction> faultPredictions)
        {
            try
            {
                var allParts = new List<PartRecommendation>();

                foreach (var fault in faultPredictions)
                {
                    var partsForFault = await GetPartsForFaultAsync(tenantId, fault.FaultId, "");

                    // Weight by fault probability
                    foreach (var part in partsForFault)
                    {
                        part.RecommendationScore *= fault.ProbabilityScore;
                        part.ReasonForRecommendation = $"For {fault.FaultName} ({fault.ProbabilityScore:P1} probability)";
                    }

                    allParts.AddRange(partsForFault);
                }

                // Consolidate duplicate parts and sum quantities
                var consolidatedParts = allParts
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
                        TotalCost = g.First().UnitCost * g.Sum(p => p.Quantity),
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
                _logger.LogError(ex, "Error predicting parts needed for tenant {TenantId}", tenantId);
                return new List<PartRecommendation>();
            }
        }

        public async Task<Dictionary<string, double>> GetPartUsageFrequencyAsync(string tenantId, string faultId)
        {
            try
            {
                await Task.CompletedTask;

                // Simulate historical usage frequency for parts related to this fault
                var parts = await GetPartsForFaultAsync(tenantId, faultId, "");
                return parts.ToDictionary(p => p.PartId, p => p.HistoricalUsageRate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting part usage frequency for fault {FaultId}", faultId);
                return new Dictionary<string, double>();
            }
        }

        public async Task<List<Part>> GetLowStockPartsAsync(string tenantId)
        {
            try
            {
                await Task.CompletedTask;

                // Simulate low stock parts
                var lowStockParts = new List<Part>
                {
                    new Part
                    {
                        PartId = "P001",
                        PartNumber = "COMP-001",
                        PartName = "HVAC Compressor",
                        Category = "Compressor",
                        Cost = 1250.00m,
                        StockLevel = 2,
                        ReorderLevel = 5,
                        Supplier = "HVAC Supply Co"
                    },
                    new Part
                    {
                        PartId = "P005",
                        PartNumber = "FILT-001",
                        PartName = "Air Filter - Standard",
                        Category = "Filter",
                        Cost = 25.00m,
                        StockLevel = 8,
                        ReorderLevel = 20,
                        Supplier = "Filter Direct"
                    },
                    new Part
                    {
                        PartId = "P009",
                        PartNumber = "BEAR-001",
                        PartName = "Motor Bearing Set",
                        Category = "Bearing",
                        Cost = 180.00m,
                        StockLevel = 1,
                        ReorderLevel = 3,
                        Supplier = "Industrial Parts Inc"
                    }
                };

                return lowStockParts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock parts for tenant {TenantId}", tenantId);
                return new List<Part>();
            }
        }

        public async Task<decimal> EstimateRepairCostAsync(string tenantId, List<PartRecommendation> parts)
        {
            try
            {
                await Task.CompletedTask;

                var partsCost = parts.Sum(p => p.TotalCost);
                var laborCost = parts.Count * 75m; // $75 per part for installation
                var overhead = (partsCost + laborCost) * 0.15m; // 15% overhead

                return partsCost + laborCost + overhead;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error estimating repair cost for tenant {TenantId}", tenantId);
                return 0;
            }
        }

        private double GetEquipmentSpecificMultiplier(string equipmentId)
        {
            // In a real system, this would consider equipment age, model, location, etc.
            var random = new Random(equipmentId.GetHashCode());
            return 0.8 + random.NextDouble() * 0.4; // 0.8 to 1.2 multiplier
        }

        private static Dictionary<string, List<PartRecommendation>> InitializeFaultPartsMapping()
        {
            return new Dictionary<string, List<PartRecommendation>>
            {
                ["HVAC_001"] = new List<PartRecommendation>
                {
                    new PartRecommendation
                    {
                        PartId = "P001", PartNumber = "COMP-001", PartName = "HVAC Compressor",
                        RecommendationScore = 0.95, ReasonForRecommendation = "Primary component failure",
                        Quantity = 1, UnitCost = 1250.00m, InStock = true, StockLevel = 3, HistoricalUsageRate = 0.85
                    },
                    new PartRecommendation
                    {
                        PartId = "P002", PartNumber = "CAP-001", PartName = "Start Capacitor",
                        RecommendationScore = 0.80, ReasonForRecommendation = "Commonly fails with compressor",
                        Quantity = 1, UnitCost = 45.00m, InStock = true, StockLevel = 15, HistoricalUsageRate = 0.70
                    },
                    new PartRecommendation
                    {
                        PartId = "P003", PartNumber = "REL-001", PartName = "Compressor Relay",
                        RecommendationScore = 0.65, ReasonForRecommendation = "Preventive replacement",
                        Quantity = 1, UnitCost = 25.00m, InStock = true, StockLevel = 10, HistoricalUsageRate = 0.45
                    }
                },
                ["HVAC_002"] = new List<PartRecommendation>
                {
                    new PartRecommendation
                    {
                        PartId = "P004", PartNumber = "SEAL-001", PartName = "Refrigerant Line Seal Kit",
                        RecommendationScore = 0.90, ReasonForRecommendation = "Direct repair for leak",
                        Quantity = 1, UnitCost = 35.00m, InStock = true, StockLevel = 20, HistoricalUsageRate = 0.90
                    },
                    new PartRecommendation
                    {
                        PartId = "P010", PartNumber = "REF-001", PartName = "R-410A Refrigerant",
                        RecommendationScore = 0.85, ReasonForRecommendation = "System recharge required",
                        Quantity = 2, UnitCost = 85.00m, InStock = true, StockLevel = 8, HistoricalUsageRate = 0.85
                    }
                },
                ["HVAC_003"] = new List<PartRecommendation>
                {
                    new PartRecommendation
                    {
                        PartId = "P005", PartNumber = "FILT-001", PartName = "Air Filter - Standard",
                        RecommendationScore = 0.98, ReasonForRecommendation = "Direct replacement for blockage",
                        Quantity = 2, UnitCost = 25.00m, InStock = false, StockLevel = 0, HistoricalUsageRate = 0.95
                    },
                    new PartRecommendation
                    {
                        PartId = "P006", PartNumber = "FILT-002", PartName = "Air Filter - High Efficiency",
                        RecommendationScore = 0.75, ReasonForRecommendation = "Upgrade option",
                        Quantity = 2, UnitCost = 45.00m, InStock = true, StockLevel = 12, HistoricalUsageRate = 0.60
                    }
                },
                ["ELEC_001"] = new List<PartRecommendation>
                {
                    new PartRecommendation
                    {
                        PartId = "P007", PartNumber = "BRK-001", PartName = "Circuit Breaker 20A",
                        RecommendationScore = 0.85, ReasonForRecommendation = "Replace tripped breaker",
                        Quantity = 1, UnitCost = 35.00m, InStock = true, StockLevel = 25, HistoricalUsageRate = 0.75
                    },
                    new PartRecommendation
                    {
                        PartId = "P008", PartNumber = "WIRE-001", PartName = "Electrical Wire 12 AWG",
                        RecommendationScore = 0.60, ReasonForRecommendation = "May need rewiring",
                        Quantity = 50, UnitCost = 1.50m, InStock = true, StockLevel = 500, HistoricalUsageRate = 0.40
                    }
                },
                ["ELEC_002"] = new List<PartRecommendation>
                {
                    new PartRecommendation
                    {
                        PartId = "P008", PartNumber = "WIRE-001", PartName = "Electrical Wire 12 AWG",
                        RecommendationScore = 0.95, ReasonForRecommendation = "Faulty wiring replacement",
                        Quantity = 100, UnitCost = 1.50m, InStock = true, StockLevel = 500, HistoricalUsageRate = 0.90
                    },
                    new PartRecommendation
                    {
                        PartId = "P011", PartNumber = "CONN-001", PartName = "Wire Connectors",
                        RecommendationScore = 0.80, ReasonForRecommendation = "New connections required",
                        Quantity = 10, UnitCost = 3.00m, InStock = true, StockLevel = 100, HistoricalUsageRate = 0.80
                    }
                },
                ["MECH_001"] = new List<PartRecommendation>
                {
                    new PartRecommendation
                    {
                        PartId = "P009", PartNumber = "BEAR-001", PartName = "Motor Bearing Set",
                        RecommendationScore = 0.90, ReasonForRecommendation = "Bearing wear replacement",
                        Quantity = 1, UnitCost = 180.00m, InStock = false, StockLevel = 0, HistoricalUsageRate = 0.85
                    },
                    new PartRecommendation
                    {
                        PartId = "P012", PartNumber = "LUBR-001", PartName = "High-Grade Bearing Grease",
                        RecommendationScore = 0.70, ReasonForRecommendation = "Lubrication maintenance",
                        Quantity = 1, UnitCost = 35.00m, InStock = true, StockLevel = 15, HistoricalUsageRate = 0.65
                    }
                }
            };
        }
    }
}