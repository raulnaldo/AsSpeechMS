using System;

namespace AsAsrMicrosoft
{
    public class LuisOutputResponse
    {
        public string query { get; set; }
        public string topScoringIntent { get; set; }
        public decimal topScoringScore { get; set; }
        public string EntityFrom { get; set; }
        public string EntityFromType { get; set; }
        public decimal EntityFromScore { get; set; }
        public string EntityTo { get; set; }
        public string EntityType { get; set; }
        public decimal EntityToScore { get; set; }        
    }
}
