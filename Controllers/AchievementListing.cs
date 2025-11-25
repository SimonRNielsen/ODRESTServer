using Microsoft.AspNetCore.Mvc;
using ODRESTServer.Dataclasses;
using System.Text.Json;

namespace ODRESTServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AchievementListing : ControllerBase
    {

        private readonly ILogger<AchievementListing> _logger;
        private readonly string achievementFile = "app_data/achievements.json";
        private readonly string scoreFile = "app_data/highscore.json";
        private readonly int returnAmount = 10;
        private static readonly object achievementFileLock = new object();
        private static readonly object scoreFileLock = new object();
        private static readonly Dictionary<AchievementResults, string> achievementResults = new Dictionary<AchievementResults, string>
        {

            { AchievementResults.AddSuccess, "Data added" },
            { AchievementResults.InvalidUserInfo, "Bad request" },
            { AchievementResults.NoAchievementsEarned, "No data found" },
            { AchievementResults.InvalidData, "Invalid data" },
            { AchievementResults.AlreadyEarned, "Entry already earned or better is stored" }

        };

        public AchievementListing(ILogger<AchievementListing> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Endpoint for requesting 10 last earned achievements published to server
        /// </summary>
        /// <returns>List of 10 last earned achievements added to data storage</returns>
        [HttpGet("achievementsearned")]
        public IEnumerable<Achievement> GetLastTen()
        {

            string json;

            lock (achievementFileLock)
                json = System.IO.File.ReadAllText(achievementFile);
            List<Achievement> achievements = JsonSerializer.Deserialize<List<Achievement>>(json) ?? new List<Achievement>();

            return achievements.GetRange(Math.Max(achievements.Count - returnAmount, 0), Math.Min(returnAmount, achievements.Count));

        }

        /// <summary>
        /// Endpoint for user to check own already earned achievements
        /// </summary>
        /// <param name="userInfo">Users info to compare against storage</param>
        /// <returns>Response/list of own achievements</returns>
        [HttpPost("getownachievements")]
        public IActionResult GetOwn([FromBody] LoginDTO userInfo)
        {

            if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Email))
                return BadRequest(achievementResults[AchievementResults.InvalidUserInfo]);

            string json;

            lock (achievementFileLock)
                json = System.IO.File.ReadAllText(achievementFile);
            List<Achievement> achievements = JsonSerializer.Deserialize<List<Achievement>>(json) ?? new List<Achievement>();

            if (achievements.Count == 0)
                return Conflict(achievementResults[AchievementResults.NoAchievementsEarned]);

            return Ok(achievements.FindAll(x => x.UserEmail == userInfo.Email));

        }

        /// <summary>
        /// Endpoint for publishing earned achievement
        /// </summary>
        /// <param name="achievement">Achievement to publish</param>
        /// <returns>Response</returns>
        [HttpPost("addachievement")]
        public IActionResult AddEarnedAchievement([FromBody] Achievement achievement)
        {

            if (achievement == null || string.IsNullOrWhiteSpace(achievement.UserEmail) || string.IsNullOrWhiteSpace(achievement.UserName))
                return BadRequest(achievementResults[AchievementResults.InvalidData]);

            lock (achievementFileLock)
            {

                string json = System.IO.File.ReadAllText(achievementFile);
                List<Achievement> achievements = JsonSerializer.Deserialize<List<Achievement>>(json) ?? new List<Achievement>();

                if (achievements.Any(x => x.UserEmail == achievement.UserEmail && x.AchievementID == achievement.AchievementID))
                    return Conflict(achievementResults[AchievementResults.AlreadyEarned]);

                achievements.Add(achievement);

                var updatedAchievements = JsonSerializer.Serialize(achievements, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(achievementFile, updatedAchievements);

            }

            return Ok(achievementResults[AchievementResults.AddSuccess]);

        }

        /// <summary>
        /// Endpoint for clearing achievement "memory" and highscores
        /// </summary>
        /// <returns>Response</returns>
        [HttpDelete("clear")]
        public IActionResult ClearAchievementsAndScore()
        {

            lock (achievementFileLock)
                System.IO.File.WriteAllText(achievementFile, "[]");
            lock (scoreFileLock)
                System.IO.File.WriteAllText(scoreFile, "[]");

            return NoContent();

        }

        /// <summary>
        /// Endpoint for adding a highscore or modifying an existing to a better
        /// </summary>
        /// <param name="score">Score to check</param>
        /// <returns>Response</returns>
        [HttpPost("addscore")]
        public IActionResult PublishScore([FromBody] HighScore score)
        {

            if (score == null || score.Score <= 0)
                return BadRequest(achievementResults[AchievementResults.InvalidUserInfo]);

            lock (scoreFileLock)
            {

                string json = System.IO.File.ReadAllText(scoreFile);
                List<HighScore> scores = JsonSerializer.Deserialize<List<HighScore>>(json) ?? new List<HighScore>();

                if (scores.Any(x => x.UserEmail == score.UserEmail && x.Score >= score.Score))
                    return Conflict(achievementResults[AchievementResults.AlreadyEarned]);

                HighScore highScore = scores.Find(x => x.UserEmail == score.UserEmail);

                if (highScore != null)
                {

                    highScore.Date = DateTime.UtcNow;
                    highScore.Score = score.Score;

                }
                else
                {

                    score.Date = DateTime.UtcNow;
                    scores.Add(score);

                }

                var updatedScores = JsonSerializer.Serialize(scores, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(scoreFile, updatedScores);

            }

            return Ok(achievementResults[AchievementResults.AddSuccess]);

        }

        /// <summary>
        /// Endpoint to get own highscore (if any)
        /// </summary>
        /// <param name="userInfo">Data needed to locate score</param>
        /// <returns>Own highscore</returns>
        [HttpPost("getownscore")]
        public IActionResult GetOwnScore([FromBody] LoginDTO userInfo)
        {

            if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Email))
                return BadRequest(achievementResults[AchievementResults.InvalidUserInfo]);

            string json;

            lock (scoreFileLock)
                json = System.IO.File.ReadAllText(scoreFile);
            List<HighScore> scores = JsonSerializer.Deserialize<List<HighScore>>(json) ?? new List<HighScore>();

            if (scores.Count == 0 || !scores.Any(x => x.UserEmail == userInfo.Email))
                return Conflict(achievementResults[AchievementResults.NoAchievementsEarned]);

            HighScore score = scores.Find(x => x.UserEmail == userInfo.Email);

            return Ok(score);

        }

        /// <summary>
        /// Endpoint to get (up to) top 10 highscores
        /// </summary>
        /// <returns>Top 10 highscores</returns>
        [HttpGet("getleaderboard")]
        public IEnumerable<HighScore> GetTopTen()
        {

            string json;

            lock (scoreFileLock)
                json = System.IO.File.ReadAllText(scoreFile);
            List<HighScore> scores = JsonSerializer.Deserialize<List<HighScore>>(json) ?? new List<HighScore>();

            return scores.OrderByDescending(x => x.Score).Take(returnAmount).ToList();

        }

    }

    public enum AchievementResults
    {

        AddSuccess,
        InvalidUserInfo,
        NoAchievementsEarned,
        InvalidData,
        AlreadyEarned

    }

}
