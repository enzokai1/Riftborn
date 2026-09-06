using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maximumHealth = 10;

    public int MaximumHealth => maximumHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    // A lifecycle component can later handle destruction or pooling independently.
    public event Action Died;

    private void Awake()
    {
        maximumHealth = Mathf.Max(1, maximumHealth);
        CurrentHealth = maximumHealth;
    }

    public void TakeDamage(int damage)
    {
        int previousHealth = CurrentHealth;

        if (damage <= 0 || IsDead)
        {
            Debug.Log($"{gameObject.name} recebeu {damage} de dano | Vida: {previousHealth} -> {CurrentHealth}", this);
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        Debug.Log($"{gameObject.name} recebeu {damage} de dano | Vida: {previousHealth} -> {CurrentHealth}", this);

        if (IsDead)
        {
            Died?.Invoke();
        }
    }
}
