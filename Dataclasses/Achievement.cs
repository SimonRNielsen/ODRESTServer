namespace ODRESTServer.Dataclasses
{

    /// <summary>
    /// Data storage class for saving information pertinent for a earned achievement
    /// </summary>
    public class Achievement
    {


        public required DateTime Date { get; set; }


        public required string UserName { get; set; }


        public required string UserEmail { get; set; }


        public required int AchievementID { get; set; }

    }
}
