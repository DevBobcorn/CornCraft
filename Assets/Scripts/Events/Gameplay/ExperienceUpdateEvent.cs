namespace CraftSharp.Event
{
    public record ExperienceUpdateEvent : BaseEvent
    {
        public int Level { get; }
        public int TotalExperience { get; }
        public float LevelUpProgress { get; }
        
        public ExperienceUpdateEvent(int level, int totalExperience, float levelUpProgress)
        {
            Level = level;
            TotalExperience = totalExperience;
            LevelUpProgress = levelUpProgress;
        }
    }
}