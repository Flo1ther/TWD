using System;
using UnityEngine;

namespace TowerDefense
{
    [Serializable]
    public sealed class EnemyDefinition
    {
        public EnemyKind kind;
        public float maxHealth;
        public float speed;
        public int attackCost;
        public int goldReward;
        public Color color;
        public Sprite sprite;
        public bool ignoresSlow;
    }

    [Serializable]
    public sealed class TowerDefinition
    {
        public TowerKind kind;
        public int price;
        public float range;
        public float fireRate;
        public float damage;
        public float splashRadius;
        public float slowPercent;
        public float slowDuration;
        public Color color;
        public Sprite sprite;
        public Sprite projectileSprite;
    }

    [CreateAssetMenu(menuName = "Tower Defense/Tower Data", fileName = "TowerData")]
    public sealed class TowerData : ScriptableObject
    {
        public TowerDefinition definition = new();
    }

    [CreateAssetMenu(menuName = "Tower Defense/Enemy Data", fileName = "EnemyData")]
    public sealed class EnemyData : ScriptableObject
    {
        public EnemyDefinition definition = new();
    }
}
