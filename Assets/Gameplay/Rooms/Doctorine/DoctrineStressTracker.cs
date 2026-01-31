public class DoctrineStressTracker
{
    private int initialEnemyCount;
    private int deaths;
    private bool triggered;

    public void Init(int totalEnemies)
    {
        initialEnemyCount = totalEnemies;
        deaths = 0;
        triggered = false;
    }

    // Returns true ONCE when doctrine should break
    public bool RegisterDeath()
    {
        if (triggered || initialEnemyCount <= 0)
            return false;

        deaths++;

        float lossRatio = (float)deaths / initialEnemyCount;

        if (lossRatio >= 0.4f)
        {
            triggered = true;
            return true;
        }

        return false;
    }
}