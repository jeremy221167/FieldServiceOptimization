using Microsoft.ML;
using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;
using System.Collections.Concurrent;

namespace ML.Services.Implementation
{
    public class MLNetPredictionService : IMLNetPredictionService
    {
        private readonly MLContext _mlContext;
        private readonly ConcurrentDictionary<string, ITransformer> _tenantModels;
        private readonly ConcurrentDictionary<string, PredictionEngine<MLNetInput, MLNetOutput>> _predictionEngines;
        private readonly ITenantModelManager _modelManager;
        private readonly ILogger<MLNetPredictionService> _logger;

        public MLNetPredictionService(
            ITenantModelManager modelManager,
            ILogger<MLNetPredictionService> logger)
        {
            _mlContext = new MLContext(seed: 0);
            _tenantModels = new ConcurrentDictionary<string, ITransformer>();
            _predictionEngines = new ConcurrentDictionary<string, PredictionEngine<MLNetInput, MLNetOutput>>();
            _modelManager = modelManager;
            _logger = logger;
        }

        public async Task<float> PredictScoreAsync(string tenantId, MLNetInput input)
        {
            try
            {
                if (string.IsNullOrEmpty(tenantId))
                    throw new ArgumentException("TenantId cannot be null or empty", nameof(tenantId));

                if (tenantId == "demo-tenant")
                {
                    var mockScore = GenerateMockScore(input);
                    _logger.LogDebug("Generated mock prediction for demo tenant: {Score}", mockScore);
                    return mockScore;
                }

                if (!await IsModelLoadedAsync(tenantId))
                {
                    var loaded = await LoadTenantModelAsync(tenantId);
                    if (!loaded)
                    {
                        _logger.LogError("Failed to load model for tenant {TenantId}", tenantId);
                        return 0f;
                    }
                }

                if (!_predictionEngines.TryGetValue(tenantId, out var predictionEngine))
                {
                    _logger.LogError("Prediction engine not found for tenant {TenantId}", tenantId);
                    return 0f;
                }

                var prediction = predictionEngine.Predict(input);

                _logger.LogDebug("Generated prediction for tenant {TenantId}: {Score}", tenantId, prediction.Score);

                return Math.Max(0f, Math.Min(1f, prediction.Score));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting score for tenant {TenantId}", tenantId);
                return 0f;
            }
        }

        public async Task<bool> LoadTenantModelAsync(string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(tenantId))
                    throw new ArgumentException("TenantId cannot be null or empty", nameof(tenantId));

                if (!await _modelManager.ModelExistsAsync(tenantId))
                {
                    _logger.LogWarning("Model does not exist for tenant {TenantId}", tenantId);
                    return false;
                }

                var modelPath = await _modelManager.GetModelPathAsync(tenantId);
                if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
                {
                    _logger.LogError("Model file not found at path: {ModelPath} for tenant {TenantId}", modelPath, tenantId);
                    return false;
                }

                var model = _mlContext.Model.Load(modelPath, out var modelInputSchema);

                _tenantModels.AddOrUpdate(tenantId, model, (key, oldValue) => model);

                var predictionEngine = _mlContext.Model.CreatePredictionEngine<MLNetInput, MLNetOutput>(model);
                _predictionEngines.AddOrUpdate(tenantId, predictionEngine, (key, oldValue) =>
                {
                    oldValue?.Dispose();
                    return predictionEngine;
                });

                _logger.LogInformation("Successfully loaded model for tenant {TenantId}", tenantId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load model for tenant {TenantId}", tenantId);
                return false;
            }
        }

        public async Task<string> GetModelVersionAsync(string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(tenantId))
                    return string.Empty;

                return await _modelManager.GetModelVersionAsync(tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model version for tenant {TenantId}", tenantId);
                return string.Empty;
            }
        }

        public async Task<bool> IsModelLoadedAsync(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
                return false;

            var isLoaded = _tenantModels.ContainsKey(tenantId) && _predictionEngines.ContainsKey(tenantId);

            if (isLoaded)
            {
                var lastUpdated = await _modelManager.GetModelLastUpdatedAsync(tenantId);
                if (lastUpdated > DateTime.UtcNow.AddHours(-24))
                {
                    return true;
                }

                _tenantModels.TryRemove(tenantId, out _);
                if (_predictionEngines.TryRemove(tenantId, out var engine))
                {
                    engine?.Dispose();
                }
                return false;
            }

            return false;
        }

        private static float GenerateMockScore(MLNetInput input)
        {
            var baseScore = 0.5f;

            baseScore += input.SkillsMatch * 0.3f;
            baseScore += Math.Max(0f, (1000f - input.Distance) / 1000f) * 0.2f;
            baseScore += (10f - input.Workload) / 10f * 0.1f;
            baseScore += input.SlaHistory * 0.2f;
            baseScore += Math.Max(0f, (120f - input.TravelTime) / 120f) * 0.1f;
            baseScore += input.Priority * 0.1f;

            return Math.Max(0f, Math.Min(1f, baseScore));
        }

        public void Dispose()
        {
            foreach (var engine in _predictionEngines.Values)
            {
                engine?.Dispose();
            }
            _predictionEngines.Clear();
            _tenantModels.Clear();
        }
    }
}