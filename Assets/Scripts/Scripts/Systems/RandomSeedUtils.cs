

using System;

public static class RandomSeedUtils {
    public static int Seed;
    public static System.Random Random;

    public static void SetNewSeed(int needSeed) {
        Seed = needSeed;
        Random = new System.Random(Seed);
    }

    public static int RandomWithNumbersCheck(int min, int max) {
        max = Math.Max(min, max);
       return Random.Next(min, max);
    }
}
