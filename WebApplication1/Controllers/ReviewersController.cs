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

// Пример использования можно посмотреть в файле Readme по ссылку:
// https://github.com/Tri0nic/KasperskyTask
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
            Guid jobId = Guid.NewGuid();
            try
            {
                // Сохраняем статус задачи в JSON при начале выполнения задачи
                // для "минимизации вероятности потерять обрабатываемый в момент перезагрузки запрос"
                await SaveJobStatusToFileAsync(jobId, "Running", request.Paths);

                // Загружаем правила ревью
                string projectRoot = GetProjectRootDirectory();
                var rules = await LoadReviewRulesAsync(Path.Combine(projectRoot, "reviewers.yaml"));

                // Выполняем обработку путей и находим ревьюеров
                var reviewers = await Task.Run(() => GetReviewersForPaths(request.Paths, rules));

                // Обновляем статус задачи в JSON как завершенный
                await SaveJobStatusToFileAsync(jobId, "Completed", request.Paths, "reviewers.yaml", reviewers.Distinct().ToList());

                // Возвращаем результат клиенту
                // Также можно было бы сделать отправку результата на почту,
                // чтобы "не заставлять пользователя API синхронно ждать завершения операции, а дать ему возможность через какое-то время уточнить ее результат" 
                return Ok(new { jobId, reviewers = reviewers.Distinct().ToList() });
            }
            catch (Exception ex)
            {
                // Обновляем статус задачи в JSON как ошибочный
                await SaveJobStatusToFileAsync(jobId, "Failed");

                _logger.LogError(ex, "Error getting reviewers");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task SaveJobStatusToFileAsync(Guid jobId, string status, List<string> paths = null, string reviewerFilePath = null, List<string> reviewers = null)
        {
            string filePath = Path.Combine(GetProjectRootDirectory(), "job_status.json");

            Dictionary<Guid, JobStatus> jobStatuses = new Dictionary<Guid, JobStatus>();

            // Чтение существующих данных из файла, если файл существует
            if (System.IO.File.Exists(filePath))
            {
                string jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                if (!string.IsNullOrWhiteSpace(jsonContent))
                {
                    jobStatuses = System.Text.Json.JsonSerializer.Deserialize<Dictionary<Guid, JobStatus>>(jsonContent);
                }
            }

            // Обновляем или добавляем статус задачи
            if (!jobStatuses.ContainsKey(jobId))
            {
                jobStatuses[jobId] = new JobStatus();
            }

            jobStatuses[jobId].Status = status;
            jobStatuses[jobId].Timestamp = DateTime.UtcNow;
            if (paths != null) jobStatuses[jobId].Paths = paths;
            if (reviewerFilePath != null) jobStatuses[jobId].ReviewerFilePath = reviewerFilePath;
            if (reviewers != null) jobStatuses[jobId].Reviewers = reviewers;

            // Сохраняем данные обратно в файл
            string newJsonContent = System.Text.Json.JsonSerializer.Serialize(jobStatuses, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, newJsonContent);
        }

        private string GetProjectRootDirectory()
        {
            string baseDirectory = AppContext.BaseDirectory;
            string projectRoot = Directory.GetParent(baseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName;
            return projectRoot;
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