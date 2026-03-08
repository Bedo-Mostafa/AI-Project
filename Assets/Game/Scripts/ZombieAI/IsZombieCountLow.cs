using UnityEngine;
using Pada1.BBCore;
using BBUnity.Conditions;
using Pada1.BBCore.Framework;
using System.Diagnostics;

[Condition("FartBoss/IsZombieCountLow")]
public class IsZombieCountLow : GOCondition
{
    [InParam("Balanced Zombie Limit")] public int balancedLimit = 5;
    [InParam("Zombie Tag")] public string zombieTag = "Enemy";

    public override bool Check()
    {
        UnityEngine.Debug.Log($"[Condition] Checking if we need more zombies...");

        GameObject[] activeZombies = GameObject.FindGameObjectsWithTag(zombieTag);
        bool needsMoreZombies = activeZombies.Length < balancedLimit;

        // Explicitly using UnityEngine.Debug
        UnityEngine.Debug.Log($"[Condition] Found {activeZombies.Length} zombies. Needs more? {needsMoreZombies}");

        return needsMoreZombies;
    }
}