using System.Text.Json;

namespace OpenProductivity.Web.Helpers
{
    public class Utf8JsonStream
    {
        private readonly StreamReader _reader;

        public Utf8JsonStream(StreamReader reader)
        {
            _reader = reader;
        }

        public async IAsyncEnumerable<JsonElement> EnumerateWorkPackagesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            // Read entire file in chunks
            string jsonContent = await _reader.ReadToEndAsync(ct);
            using var doc = JsonDocument.Parse(jsonContent);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    yield return element;
                }
            }
            else
            {
                yield return doc.RootElement;
            }
        }
    }
}
