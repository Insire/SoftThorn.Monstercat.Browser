using Serilog;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public abstract class FileStorageService<TModel>
        where TModel : class, new()
    {
        private readonly ILogger _logger;
        private readonly JsonSerializerOptions _serializerOptions;

        protected FileStorageService(ILogger? logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General)
            {
                AllowTrailingCommas = true,
                WriteIndented = true,
            };
        }

        public async Task Write(string filePath, TModel model, bool truncate, CancellationToken token)
        {
            try
            {
                using (var fileStream = File.Open(filePath, truncate ? FileMode.Truncate : FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    await JsonSerializer.SerializeAsync<TModel>(fileStream, model, _serializerOptions, token);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "");
            }
        }

        public async Task<TModel> Read(string filePath, CancellationToken token)
        {
            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var result = await JsonSerializer.DeserializeAsync<TModel>(stream, _serializerOptions, token);

                    return result ?? new TModel();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "");

                return new TModel();
            }
        }
    }
}
