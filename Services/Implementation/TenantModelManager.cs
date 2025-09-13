using ML.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ML.Services.Implementation
{
    public class TenantModelManager : ITenantModelManager
    {
        private readonly string _modelsBasePath;
        private readonly ILogger<TenantModelManager> _logger;

        public TenantModelManager(IConfiguration configuration, ILogger<TenantModelManager> logger)
        {
            _modelsBasePath = configuration.GetValue<string>("MLModels:BasePath") ?? "Models";
            _logger = logger;

            if (!Directory.Exists(_modelsBasePath))
            {
                Directory.CreateDirectory(_modelsBasePath);
                _logger.LogInformation("Created models directory at: {BasePath}", _modelsBasePath);
            }
        }

        public async Task<string> GetModelPathAsync(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentException("TenantId cannot be null or empty", nameof(tenantId));

            await Task.CompletedTask;
            return Path.Combine(_modelsBasePath, $"{tenantId}_model.zip");
        }

        public async Task<bool> ModelExistsAsync(string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(tenantId))
                    return false;

                var modelPath = await GetModelPathAsync(tenantId);
                return File.Exists(modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if model exists for tenant {TenantId}", tenantId);
                return false;
            }
        }

        public async Task<DateTime> GetModelLastUpdatedAsync(string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(tenantId))
                    return DateTime.MinValue;

                var modelPath = await GetModelPathAsync(tenantId);
                if (!File.Exists(modelPath))
                    return DateTime.MinValue;

                return File.GetLastWriteTimeUtc(modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model last updated time for tenant {TenantId}", tenantId);
                return DateTime.MinValue;
            }
        }

        public async Task<string> GetModelVersionAsync(string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(tenantId))
                    return string.Empty;

                var lastUpdated = await GetModelLastUpdatedAsync(tenantId);
                if (lastUpdated == DateTime.MinValue)
                    return string.Empty;

                return $"{tenantId}_v{lastUpdated:yyyyMMdd_HHmmss}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model version for tenant {TenantId}", tenantId);
                return string.Empty;
            }
        }

        public async Task UpdateModelAsync(string tenantId, byte[] modelData)
        {
            try
            {
                if (string.IsNullOrEmpty(tenantId))
                    throw new ArgumentException("TenantId cannot be null or empty", nameof(tenantId));

                if (modelData == null || modelData.Length == 0)
                    throw new ArgumentException("Model data cannot be null or empty", nameof(modelData));

                var modelPath = await GetModelPathAsync(tenantId);

                var directory = Path.GetDirectoryName(modelPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var tempPath = $"{modelPath}.tmp";
                await File.WriteAllBytesAsync(tempPath, modelData);

                if (File.Exists(modelPath))
                {
                    var backupPath = $"{modelPath}.backup";
                    File.Move(modelPath, backupPath);

                    try
                    {
                        File.Move(tempPath, modelPath);
                        File.Delete(backupPath);
                    }
                    catch
                    {
                        File.Move(backupPath, modelPath);
                        throw;
                    }
                }
                else
                {
                    File.Move(tempPath, modelPath);
                }

                _logger.LogInformation("Successfully updated model for tenant {TenantId}", tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update model for tenant {TenantId}", tenantId);
                throw;
            }
        }
    }
}