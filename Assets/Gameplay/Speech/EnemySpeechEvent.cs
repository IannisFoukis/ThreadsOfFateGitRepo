public enum EnemySpeechEvent
{
    DoctrineEngaged,     // room start
    Advance,             // March → HoldFire
    HoldLine,            // HoldFire stable
    EncircleCall,        // HoldFire → Encircle
    FormationBreak,      // Chaos fracture
    FanaticLock,         // Fanatic refusal to break
    RetreatCall          // Optional (canRetreat)
}