namespace InsectWars.RTS
{
    public static class MatchStats
    {
        public static float ElapsedTime { get; set; }
        public static int PlayerUnitsLost { get; private set; }
        public static int EnemyUnitsKilled { get; private set; }
        public static int CaloriesGathered { get; private set; }

        public static void RecordKill(Team deadUnitTeam)
        {
            if (deadUnitTeam == Team.Player)
                PlayerUnitsLost++;
            else if (deadUnitTeam == Team.Enemy)
                EnemyUnitsKilled++;
        }

        public static void RecordCaloriesGathered(int amount)
        {
            if (amount > 0)
                CaloriesGathered += amount;
        }

        public static void Reset()
        {
            ElapsedTime = 0f;
            PlayerUnitsLost = 0;
            EnemyUnitsKilled = 0;
            CaloriesGathered = 0;
        }
    }
}
