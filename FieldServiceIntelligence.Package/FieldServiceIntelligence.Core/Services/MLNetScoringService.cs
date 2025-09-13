using Microsoft.ML;
using Microsoft.Extensions.Logging;
using FieldServiceIntelligence.Core.Interfaces;
using FieldServiceIntelligence.Core.Models;

namespace FieldServiceIntelligence.Core.Services
{
    public class MLNetScoringService : IMLNetScoringService
    {
        private readonly ILogger<MLNetScoringService> _logger;
        private MLContext _mlContext;
        private ITransformer? _model;
        private PredictionEngine<MLNetInput, MLNetOutput>? _predictionEngine;

        public bool IsModelLoaded => _model != null && _predictionEngine != null;

        public MLNetScoringService(ILogger<MLNetScoringService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mlContext = new MLContext(seed: 0);
        }

        public async Task InitializeModelAsync(string modelPath)
        {
            try
            {
                if (string.IsNullOrEmpty(modelPath))
                {
                    _logger.LogWarning("Model path is null or empty. Using fallback scoring.");
                    return;
                }

                if (!File.Exists(modelPath))
                {
                    _logger.LogWarning("Model file not found at {ModelPath}. Using fallback scoring.", modelPath);
                    return;
                }

                _model = _mlContext.Model.Load(modelPath, out var modelInputSchema);
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<MLNetInput, MLNetOutput>(_model);

                _logger.LogInformation("ML.NET model loaded successfully from {ModelPath}", modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ML.NET model from {ModelPath}", modelPath);
                _model = null;
                _predictionEngine = null;
            }
        }

        public async Task<float> PredictTechnicianScoreAsync(MLNetInput input)
        {
            try
            {
                if (input == null)
                {
                    _logger.LogWarning("MLNetInput is null");
                    return 0.0f;
                }

                if (!IsModelLoaded)
                {
                    return await CalculateFallbackScoreAsync(input);
                }

                var prediction = _predictionEngine!.Predict(input);
                var score = Math.Max(0.0f, Math.Min(1.0f, prediction.Score));

                _logger.LogDebug("ML.NET prediction: {Score} for input skills={Skills}, distance={Distance}, workload={Workload}",
                    score, input.SkillsMatch, input.Distance, input.Workload);

                return score;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making ML.NET prediction");
                return await CalculateFallbackScoreAsync(input);
            }
        }

        private async Task<float> CalculateFallbackScoreAsync(MLNetInput input)
        {
            try
            {
                // Fallback scoring algorithm when ML.NET model is not available
                var skillsWeight = 0.4f;
                var distanceWeight = 0.3f;
                var workloadWeight = 0.15f;
                var slaWeight = 0.1f;
                var travelTimeWeight = 0.05f;

                // Normalize distance score (inverse relationship - closer is better)
                var normalizedDistance = Math.Max(0.0f, 1.0f - (input.Distance / 100.0f));

                // Normalize workload score (inverse relationship - less workload is better)
                var normalizedWorkload = Math.Max(0.0f, 1.0f - (input.Workload / 10.0f));

                // Normalize travel time score (inverse relationship - less time is better)
                var normalizedTravelTime = Math.Max(0.0f, 1.0f - (input.TravelTime / 180.0f));

                var fallbackScore =
                    (input.SkillsMatch * skillsWeight) +
                    (normalizedDistance * distanceWeight) +
                    (normalizedWorkload * workloadWeight) +
                    (input.SlaHistory * slaWeight) +
                    (normalizedTravelTime * travelTimeWeight);

                // Apply priority multiplier
                var priorityMultiplier = input.Priority switch
                {
                    >= 0.9f => 1.2f,  // Emergency
                    >= 0.7f => 1.1f,  // High
                    >= 0.3f => 1.0f,  // Normal
                    _ => 0.9f          // Low
                };

                var finalScore = Math.Max(0.0f, Math.Min(1.0f, fallbackScore * priorityMultiplier));

                _logger.LogDebug("Fallback prediction: {Score} for input skills={Skills}, distance={Distance}, workload={Workload}",
                    finalScore, input.SkillsMatch, input.Distance, input.Workload);

                return await Task.FromResult(finalScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fallback scoring");
                return 0.5f; // Default safe score
            }
        }

        public async Task TrainModelAsync(IEnumerable<MLNetInput> trainingData, string outputModelPath)
        {
            try
            {
                if (trainingData?.Any() != true)
                {
                    _logger.LogWarning("No training data provided");
                    return;
                }

                if (string.IsNullOrEmpty(outputModelPath))
                {
                    _logger.LogWarning("Output model path is null or empty");
                    return;
                }

                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

                var pipeline = _mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "Score")
                    .Append(_mlContext.Transforms.Concatenate("Features",
                        nameof(MLNetInput.SkillsMatch),
                        nameof(MLNetInput.Distance),
                        nameof(MLNetInput.Workload),
                        nameof(MLNetInput.SlaHistory),
                        nameof(MLNetInput.TravelTime),
                        nameof(MLNetInput.Priority)))
                    .Append(_mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", featureColumnName: "Features"));

                _logger.LogInformation("Training ML.NET model with {Count} samples", trainingData.Count());

                _model = pipeline.Fit(dataView);
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<MLNetInput, MLNetOutput>(_model);

                _mlContext.Model.Save(_model, dataView.Schema, outputModelPath);

                _logger.LogInformation("ML.NET model trained and saved to {ModelPath}", outputModelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training ML.NET model");
            }
        }

        public void Dispose()
        {
            _predictionEngine?.Dispose();
            _predictionEngine = null;
            _model = null;
        }
    }
}