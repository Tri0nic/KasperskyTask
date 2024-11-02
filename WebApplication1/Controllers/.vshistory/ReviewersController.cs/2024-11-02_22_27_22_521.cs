using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CodeReviewAPI
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewersController : ControllerBase
    {
        private readonly ILogger<ReviewersController> _logger;

        public ReviewersController(ILogger<ReviewersController> logger)
        {
            _logger = logger;
        }

        [HttpPost("get-reviewers")]
        public async Task<IActionResult> GetReviewers([FromBody] ReviewRequest request)
        {
            try
            {
                var rules = await LoadReviewRulesAsync(request.ReviewerFilePath);
                var reviewers = await Task.Run(() => GetReviewersForPaths(request.Paths, rules));
                return Ok(new { reviewers = reviewers.Distinct().ToList() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviewers");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<List<ReviewRule>> LoadReviewRulesAsync(string filePath)
        {
            using var reader = new StreamReader(filePath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var yamlContent = await reader.ReadToEndAsync();
            var rules = deserializer.Deserialize<Dictionary<string, ReviewRule>>(yamlContent);
            return rules.Select(r => r.Value).ToList();
        }

        private List<string> GetReviewersForPaths(List<string> paths, List<ReviewRule> rules)
        {
            var reviewers = new List<string>();

            foreach (var path in paths)
            {
                foreach (var rule in rules)
                {
                    if (IsPathIncluded(path, rule) && (!rule.ExcludedPaths?.Any() ?? true || !IsPathExcluded(path, rule)))
                    {
                        reviewers.AddRange(rule.Reviewers);
                    }
                }
            }

            return reviewers;
        }

        private bool IsPathIncluded(string path, ReviewRule rule)
        {
            return rule.IncludedPaths.Any(includedPath => IsPathMatch(path, includedPath));
        }

        private bool IsPathExcluded(string path, ReviewRule rule)
        {
            return rule.ExcludedPaths != null && rule.ExcludedPaths.Any(excludedPath => IsPathMatch(path, excludedPath));
        }

        private bool IsPathMatch(string path, string pattern)
        {
            if (pattern.EndsWith("/*"))
            {
                var basePath = pattern.Substring(0, pattern.Length - 2);
                return path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
            }
            else if (pattern.StartsWith("*"))
            {
                var extension = pattern.Substring(1);
                return path.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return path.Equals(pattern, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public class ReviewRequest
    {
        public string ReviewerFilePath { get; set; }
        public List<string> Paths { get; set; }
    }

    public class ReviewRule
    {
        public string RuleName { get; set; }
        public List<string> IncludedPaths { get; set; }
        public List<string> ExcludedPaths { get; set; }
        public List<string> Reviewers { get; set; }
    }
}
