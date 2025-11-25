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

        public AchievementListing(ILogger<AchievementListing> logger)
        {
            _logger = logger;
        }

        [HttpGet("earned")]
        public IEnumerable<Achievement> GetLastTen()
        {

            string json;

            lock (fileLock)
                json = System.IO.File.ReadAllText(achievementFile);
            List<Achievement> achievements = JsonSerializer.Deserialize<List<Achievement>>(json) ?? new List<Achievement>();

            return achievements.GetRange(Math.Max(achievements.Count - returnAmount, 0), Math.Min(returnAmount, achievements.Count)).ToArray();

        }

        [HttpPost("getown")]
        public IActionResult GetOwn([FromBody] LoginDTO userInfo)
        {

            if (userInfo == null)
                return BadRequest("Bad request");

            if (string.IsNullOrWhiteSpace(userInfo.Email))
                return BadRequest("Invalid user email");

            string json;

            lock (fileLock)
                json = System.IO.File.ReadAllText(achievementFile);
            List<Achievement> achievements = JsonSerializer.Deserialize<List<Achievement>>(json) ?? new List<Achievement>();

            if (achievements.Count == 0)
                return Conflict("No achievements found");

            return Ok(achievements.FindAll(x => x.UserEmail == userInfo.Email).ToArray());

        }

        [HttpPost("add")]
        public IActionResult AddEarnedAchievement([FromBody] Achievement achievement)
        {

            if (achievement == null)
                return BadRequest("Achievement data incomplete");

            if (string.IsNullOrWhiteSpace(achievement.UserEmail) || string.IsNullOrWhiteSpace(achievement.UserName))
                return BadRequest("Invalid user email or username");

            lock (fileLock)
            {

                string json = System.IO.File.ReadAllText(achievementFile);
                List<Achievement> achievements = JsonSerializer.Deserialize<List<Achievement>>(json) ?? new List<Achievement>();

                if (achievements.Any(x => x.UserEmail == achievement.UserEmail && x.AchievementID == achievement.AchievementID))
                    return Conflict("Achievement already earned for user");

                achievements.Add(achievement);

                var updatedAchievements = JsonSerializer.Serialize(achievements, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(achievementFile, updatedAchievements);

            }

            return Ok("Achievement added");

        }

        [HttpDelete("clear")]
        public IActionResult ClearAchievements()
        {

            lock (fileLock)
                System.IO.File.WriteAllText(achievementFile, "[]");

            return NoContent();

        }

    }
}
