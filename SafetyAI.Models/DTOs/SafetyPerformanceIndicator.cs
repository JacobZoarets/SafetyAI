namespace SafetyAI.Models.DTOs
{
    public class SafetyPerformanceIndicator
    {
        public string Name { get; set; }
        public double CurrentValue { get; set; }
        public double PreviousValue { get; set; }
        public double Target { get; set; }
        public string Unit { get; set; }
        public double PercentageChange { get; set; }
        public bool IsHigherBetter { get; set; }
        public string Status => GetStatus();
        public string Trend => GetTrend();

        private string GetStatus()
        {
            if (IsHigherBetter)
            {
                return CurrentValue >= Target ? "Good" : "Needs Improvement";
            }
            else
            {
                return CurrentValue <= Target ? "Good" : "Needs Improvement";
            }
        }

        private string GetTrend()
        {
            if (PercentageChange > 5)
                return IsHigherBetter ? "Improving" : "Worsening";
            else if (PercentageChange < -5)
                return IsHigherBetter ? "Worsening" : "Improving";
            else
                return "Stable";
        }
    }
}