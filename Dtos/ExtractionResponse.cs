namespace DebtOptimizer.Dtos
{
    public class ExtractionResponse
    {
        public ExtractionResult Extraction { get; set; } = new();
        public List<string> FollowUpQuestions { get; set; } = [];
        public bool IsComplete { get; set; }
    }
}
