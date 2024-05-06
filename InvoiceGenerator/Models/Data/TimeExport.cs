using InvoiceGenerator.Helpers;
using InvoiceGenerator.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceGenerator.Models.Data
{
    public class TimeExport
    {
        public List<Time>? CollapsedTimes { get; private set; }
        public List<Time>? Times { get; private set; }

        private Dictionary<string, int> headerIndexes;

        public TimeExport(Dictionary<string, int> headerIndexes)
        {
            this.headerIndexes = headerIndexes;
        }

        public static TimeExport FromCsv(string csv)
        {
            List<Time> times = new List<Time>();
            string[] lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            TimeExport exportedTimes = new TimeExport(GetHeaderIndexes(lines[0]));

            int? userHeaderIndex = exportedTimes.GetHeaderIndex("User");
            int? projectHeaderIndex = exportedTimes.GetHeaderIndex("Project");
            int? clientHeaderIndex = exportedTimes.GetHeaderIndex("Client");
            int? amountHeaderIndex = exportedTimes.GetHeaderIndex("Duration (decimal)", "Time (decimal)");
            int? descriptionHeaderIndex = exportedTimes.GetHeaderIndex("Description");

            foreach (string line in lines.Skip(1))
            {
                string[] values = line.Split("\",");

                string? name = null;
                string? project = null;
                string? client = null;
                string? description = null;
                decimal? amount = null;

                if (userHeaderIndex != null) name = values[userHeaderIndex.Value].GetUnescapedValue();
                if(projectHeaderIndex != null) project = values[projectHeaderIndex.Value].GetUnescapedValue();
                if(clientHeaderIndex != null) client = values[clientHeaderIndex.Value].GetUnescapedValue();
                if(amountHeaderIndex != null) amount = GetDecimal(values[amountHeaderIndex.Value].GetUnescapedValue());
                if(descriptionHeaderIndex != null) description = values[descriptionHeaderIndex.Value].GetUnescapedValue();
                
                name = GetNullInsteadOfEmptyString(name); // if the string is empty we make it null instead
                project = string.IsNullOrEmpty(project) ? null : project;
                client = string.IsNullOrEmpty(client) ? null : client;

                if (amount == null)
                    throw new GenerationException("Amount was missing for a time entry, this is not allowed");

                if (client == "(Without client)") // Clockify exports null as "(Without client)"
                    client = null;

                Time time = new Time(name, project, client, amount.Value) { Description = description };

                times.Add(time);
            }

            exportedTimes.CollapsedTimes = Time.CollapseTimes(times);
            exportedTimes.Times = times;

            return exportedTimes;
        }

        private static decimal GetDecimal(string value)
        {
            try
            {
                return decimal.Parse(value, CultureInfo.InvariantCulture);
            }
            catch(Exception exception)
            {
                throw new Exception($"Error when parsing {value} to a decimal", exception);
            }
        }

        private static string? GetNullInsteadOfEmptyString(string? text)
        {
            if (text == null) return null;
            if(text.Length == 0) return null;
            return text;
        }

        private int? GetHeaderIndex(string header, params string[] additionalHeaders)
        {
            if (headerIndexes.ContainsKey(header))
                return headerIndexes[header];

            foreach (string additionalHeader in additionalHeaders)
            {
                if (headerIndexes.ContainsKey(additionalHeader))
                    return headerIndexes[additionalHeader];
            }

            return null;
        }

        private static Dictionary<string, int> GetHeaderIndexes(string firstLine)
        {
            Dictionary<string, int> indexes = new Dictionary<string, int>();

            string[] headers = firstLine.Split(',');

            for (int i = 0; i < headers.Length; i++)
            {
                indexes.Add(headers[i].Substring(1, headers[i].Length - 2).Trim(), i);
            }

            return indexes;
        }

        public int GetTotalCost(GeneralInformation configuration)
        {
            if (CollapsedTimes == null)
                throw new GenerationException("Trying to get total cost but there is no time data");

            if (CollapsedTimes.Any(time => time.Name == null))
                throw new GenerationException("Trying to get total cost but there is a time entry without a name");

            return CollapsedTimes.Sum(time => time.Amount * configuration.GetUnitPrice(time.Name!));
        }
    }
}
