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
        private readonly int returnAmount = 10;
        private static readonly object fileLock = new object();
        private static readonly Dictionary<AchievementResults, string> requestResults = new Dictionary<AchievementResults, string> 
        {
            
            { AchievementResults.AddSuccess, "Achievement added" },
            { AchievementResults.InvalidUserInfo, "Bad request" },
            { AchievementResults.NoAchievementsEarned, "No achievements found" },
            { AchievementResults.InvalidData, "Invalid achievement data" },
            { AchievementResults.AlreadyEarned, "Achievement already earned for user" }

        };

        public AchievementListing(ILogger<AchievementListing> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Endpoint for requesting 10 last earned achievements published to server
        /// </summary>
        /// <returns>List of 10 last earned achievements added to data storage</returns>
        [HttpGet("earned")]
        public IEnumerable<Achievement> GetLastTen()
        {

            string json;

            lock (fileLock)
                json = System.IO.File.ReadAllText(achievementFile);
            List<Achievement> achievements = JsonSerializer.Deserialize<List<Achievement>>(json) ?? new List<Achievement>();

            return achievements.GetRange(Math.Max(achievements.Count - returnAmount, 0), Math.Min(returnAmount, achievements.Count));

        }

        /// <summary>
        /// Endpoint for user to check own already earned achievements
        /// </summary>
        /// <param name="userInfo">Users info to compare against storage</param>
        /// <returns>Response/list of own achievements</returns>
        [HttpPost("getown")]
        public IActionResult GetOwn([FromBody] LoginDTO userInfo)
        {

            if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Email))
                return BadRequest(requestResults[AchievementResults.InvalidUserInfo]);

            string json;

            lock (fileLock)
                json = System.IO.File.ReadAllText(achievementFile);
            List<Achievement> achievements = JsonSerializer.Deserialize<List<Achievement>>(json) ?? new List<Achievement>();

            if (achievements.Count == 0)
                return Conflict(requestResults[AchievementResults.NoAchievementsEarned]);

            return Ok(achievements.FindAll(x => x.UserEmail == userInfo.Email));

        }

        /// <summary>
        /// Endpoint for publishing earned achievement
        /// </summary>
        /// <param name="achievement">Achievement to publish</param>
        /// <returns>Response</returns>
        [HttpPost("add")]
        public IActionResult AddEarnedAchievement([FromBody] Achievement achievement)
        {

            if (achievement == null || string.IsNullOrWhiteSpace(achievement.UserEmail) || string.IsNullOrWhiteSpace(achievement.UserName))
                return BadRequest(requestResults[AchievementResults.InvalidData]);

            lock (fileLock)
            {

                string json = System.IO.File.ReadAllText(achievementFile);
                List<Achievement> achievements = JsonSerializer.Deserialize<List<Achievement>>(json) ?? new List<Achievement>();

                if (achievements.Any(x => x.UserEmail == achievement.UserEmail && x.AchievementID == achievement.AchievementID))
                    return Conflict(requestResults[AchievementResults.AlreadyEarned]);

                achievements.Add(achievement);

                var updatedAchievements = JsonSerializer.Serialize(achievements, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(achievementFile, updatedAchievements);

            }

            return Ok(requestResults[AchievementResults.AddSuccess]);

        }

        /// <summary>
        /// Endpoint for clearing achievement "memory"
        /// </summary>
        /// <returns>Response</returns>
        [HttpDelete("clear")]
        public IActionResult ClearAchievements()
        {

            lock (fileLock)
                System.IO.File.WriteAllText(achievementFile, "[]");

            return NoContent();

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
