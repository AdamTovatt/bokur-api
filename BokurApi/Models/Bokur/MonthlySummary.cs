namespace BokurApi.Models.Bokur
{
    public class MonthlySummary
    {
        public List<SimplifiedSummaryForAccountPerMonth> SimplifiedSummaryForAccount { get; set; }
        public List<SummaryPerMonth> DetailedSummary { get; set; }

        public MonthlySummary(List<SummaryPerMonth> detailedSummary)
        {
            DetailedSummary = detailedSummary;
            SimplifiedSummaryForAccount = new List<SimplifiedSummaryForAccountPerMonth>();

            foreach (SummaryPerMonth summaryPerMonth in detailedSummary)
            {
                SimplifiedSummaryForAccount.AddRange(summaryPerMonth.GetSimplifiedSummary());
            }
        }
    }
}
