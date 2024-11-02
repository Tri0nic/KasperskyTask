namespace WebApplication1.Models
{
    public class ReviewRule
    {
        public List<string> IncludedPaths { get; set; }
        public List<string> ExcludedPaths { get; set; }
        public List<string> Reviewers { get; set; }
    }
}
