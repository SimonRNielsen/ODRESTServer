using Microsoft.AspNetCore.Mvc;
using ODRESTServer.Dataclasses;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ODRESTServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AchievementListing : ControllerBase
    {
        
        private readonly ILogger<AchievementListing> _logger;
        private readonly string achievementFile = "app_data/achievements.json";

        public AchievementListing(ILogger<AchievementListing> logger)
        {
            _logger = logger;
        }

        [HttpGet("GetAchievements")]
        public IEnumerable<Achievement> Get()
        {

            string json = System.IO.File.ReadAllText(achievementFile);
            List<Achievement> achievements = JsonSerializer.Deserialize<List<Achievement>>(json) ?? new List<Achievement>();
            return achievements.ToArray();

        }
    }
}
