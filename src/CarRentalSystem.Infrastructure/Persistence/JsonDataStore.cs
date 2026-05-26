using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CarRentalSystem.Application.Contracts;

namespace CarRentalSystem.Infrastructure.Persistence
{
    public class JsonDataStore<T> : IDataStore<T>
    {
        private readonly string _filePath;

        public JsonDataStore(string fileName)
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, fileName);
        }

        public async Task<IReadOnlyCollection<T>> LoadAsync(CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_filePath)) return Array.Empty<T>();
            try
            {
                using var stream = File.OpenRead(_filePath);
                var items = await JsonSerializer.DeserializeAsync<List<T>>(stream, cancellationToken: cancellationToken);
                return items ?? new List<T>();
            }
            catch (JsonException) { return Array.Empty<T>(); }
            catch (IOException) { return Array.Empty<T>(); }
        }

        public async Task SaveAsync(IReadOnlyCollection<T> items, CancellationToken cancellationToken = default)
        {
            try
            {
                using var stream = File.Create(_filePath);
                await JsonSerializer.SerializeAsync(stream, items, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
            }
            catch (IOException ex)
            {
                throw new Exception($"Помилка запису у файл: {_filePath}", ex);
            }
        }
    }
}