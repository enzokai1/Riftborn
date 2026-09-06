using System;
using UnityEngine;

[DisallowMultipleComponent]
public class MatchStats : MonoBehaviour
{
    public int KillCount { get; private set; } = 0;
    public event Action KillsChanged;

    public void RegisterKill()
    {
        KillCount++;
        KillsChanged?.Invoke();
    }
}
