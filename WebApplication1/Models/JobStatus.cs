namespace WebApplication1.Models
{
    public class JobStatus
    {
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public List<string> Paths { get; set; }
        public string ReviewerFilePath { get; set; }
        public List<string> Reviewers { get; set; }
    }
}
