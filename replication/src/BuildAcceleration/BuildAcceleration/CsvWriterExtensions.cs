using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;

namespace ForecastBuildTime
{
    public static class CsvWriterExtensions
    {
#nullable disable
        public static void WriteDictionaryRecords(this CsvWriter csvWriter, IEnumerable<IEnumerable<KeyValuePair<string, object>>> records)
        {
            var headings = records.SelectMany(d => d.Select(p => p.Key)).Distinct().ToList();
            foreach (var heading in headings)
            {
                csvWriter.WriteField(heading);
            }
            csvWriter.NextRecord();
            foreach (var item in records)
            {
                foreach (var heading in headings)
                {
                    csvWriter.WriteField(item.FirstOrDefault(p => p.Key == heading).Value);
                }
                csvWriter.NextRecord();
            }
        }

        public static async Task WriteDictionaryRecordsAsync(this CsvWriter csvWriter, IEnumerable<IEnumerable<KeyValuePair<string, object>>> records)
        {
            var headings = records.SelectMany(d => d.Select(p => p.Key)).Distinct().ToList();
            foreach (var heading in headings)
            {
                csvWriter.WriteField(heading);
            }
            await csvWriter.NextRecordAsync().ConfigureAwait(false);
            foreach (var item in records)
            {
                foreach (var heading in headings)
                {
                    csvWriter.WriteField(item.FirstOrDefault(p => p.Key == heading).Value);
                }
                await csvWriter.NextRecordAsync().ConfigureAwait(false);
            }
        }
#nullable restore
    }
}
