using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;
using System.Text.Json;

namespace ML.Services.Implementation
{
    public class LlmExplanationService : ILlmExplanationService
    {
        private readonly AzureOpenAIClient? _openAIClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LlmExplanationService> _logger;
        private readonly string _deploymentName;
        private readonly bool _isEnabled;

        public LlmExplanationService(
            IConfiguration configuration,
            ILogger<LlmExplanationService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var endpoint = _configuration.GetValue<string>("AzureOpenAI:Endpoint");
            var apiKey = _configuration.GetValue<string>("AzureOpenAI:ApiKey");
            _deploymentName = _configuration.GetValue<string>("AzureOpenAI:DeploymentName") ?? "gpt-4";
            _isEnabled = _configuration.GetValue<bool>("AzureOpenAI:Enabled");

            if (_isEnabled && !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
            {
                _openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
                _logger.LogInformation("Azure OpenAI client initialized successfully");
            }
            else
            {
                _logger.LogWarning("Azure OpenAI not configured or disabled");
            }
        }

        public async Task<string> GenerateExplanationAsync(
            JobRequest job,
            Technician technician,
            TechnicianRecommendation recommendation)
        {
            await Task.CompletedTask;
            return "LLM explanations are not currently available";
        }

        public async Task<Dictionary<string, string>> GenerateBatchExplanationsAsync(
            JobRequest job,
            List<TechnicianRecommendation> recommendations)
        {
            var explanations = new Dictionary<string, string>();
            foreach (var rec in recommendations)
            {
                explanations[rec.TechnicianId] = "LLM explanations are not currently available";
            }
            await Task.CompletedTask;
            return explanations;
        }

        public async Task<bool> IsServiceAvailableAsync()
        {
            await Task.CompletedTask;
            return false;
        }

        private string BuildExplanationPrompt(JobRequest job, Technician technician, TechnicianRecommendation recommendation)
        {
            return $@"
Explain why {technician.Name} is recommended for this job:

Job Details:
- Service: {job.ServiceType}
- Equipment: {job.Equipment}
- Location: {job.Location}
- Scheduled: {job.ScheduledDate:yyyy-MM-dd HH:mm}
- SLA: {job.SlaHours} hours
- Priority: {job.Priority}

Technician Scores:
- Overall Score: {recommendation.Score:P1}
- Skills Match: {recommendation.SkillsScore:P1}
- Distance: {recommendation.Distance:F1} km
- Travel Time: {recommendation.EstimatedTravelTime:F1} minutes
- SLA History: {recommendation.SlaScore:P1}
- Availability: {recommendation.AvailabilityScore:P1}

Provide a brief, clear explanation in 2-3 sentences focusing on the strongest factors.";
        }
    }
}