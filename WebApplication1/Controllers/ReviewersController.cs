using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApplication1.Models;
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
                string projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName;
                var rules = await LoadReviewRulesAsync(Path.Combine(projectRoot, "reviewers.yaml"));
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

        private HashSet<string> GetReviewersForPaths(List<string> paths, List<ReviewRule> rules)
        {
            var reviewers = new HashSet<string>();

            Parallel.ForEach(paths, path =>
            {
                foreach (var rule in rules)
                {
                    if (IsPathIncluded(path, rule) && !IsPathExcluded(path, rule))
                    {
                        lock (reviewers)
                        {
                            reviewers.UnionWith(rule.Reviewers);
                        }
                    }
                }
            });

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
                var basePath = (pattern.Substring(0, pattern.Length - 2)).Replace("/", @"\");
                return path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
            }
            else if (pattern.Contains("/*"))
            {
                var patternExtension = pattern.Substring(pattern.IndexOf("/*") + 2);
                return path.EndsWith(patternExtension, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return false;
            }
        }
    }
}