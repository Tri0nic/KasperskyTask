namespace WebApplication1.Models
{
    public class ReviewRequest
    {
        public string ReviewerFilePath { get; set; }
        public List<string> Paths { get; set; }
    }
}
