using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
namespace TowerDefense
{
public sealed class Tower : MonoBehaviour
    {
        private const int MaxLevel = 3;
        private const float DamageBonusPerLevel = 0.30f;
        private const float RangeBonusPerLevel = 0.20f;
        private const float FireRateBonusPerLevel = 0.15f;

        private float cooldown;
        private SpriteRenderer rangeRenderer;

        public TowerDefinition Definition { get; private set; }
        public bool HasCompletedBattleCycle { get; private set; }
        public int DamageLevel { get; private set; } = 1;
        public int RangeLevel { get; private set; } = 1;
        public int SpeedLevel { get; private set; } = 1;
        public int Level => Mathf.Max(DamageLevel, RangeLevel, SpeedLevel);
        public int GoldInvested { get; private set; }
        public bool CanUpgrade => CanUpgradeDamage || CanUpgradeRange || CanUpgradeSpeed;
        public bool CanUpgradeDamage => DamageLevel < MaxLevel;
        public bool CanUpgradeRange => RangeLevel < MaxLevel;
        public bool CanUpgradeSpeed => SpeedLevel < MaxLevel;
        public int UpgradeCost => DamageUpgradeCost;
        public int DamageUpgradeCost => CanUpgradeDamage ? CalculateUpgradeCost(DamageLevel, 0.82f) : 0;
        public int RangeUpgradeCost => CanUpgradeRange ? CalculateUpgradeCost(RangeLevel, 0.72f) : 0;
        public int SpeedUpgradeCost => CanUpgradeSpeed ? CalculateUpgradeCost(SpeedLevel, 0.76f) : 0;
        public int SellRefund => HasCompletedBattleCycle ? Mathf.RoundToInt(GoldInvested * 0.6f) : GoldInvested;
        public float DamageMultiplier => 1f + (DamageLevel - 1) * DamageBonusPerLevel;
        public float CurrentDamage => Definition.damage * DamageMultiplier;
        public float CurrentRange => Definition.range + (RangeLevel - 1) * RangeBonusPerLevel;
        public float CurrentFireRate => Definition.fireRate * (1f + (SpeedLevel - 1) * FireRateBonusPerLevel);
        public Vector3 PopupAnchorPosition => transform.position + GetTowerVisualOffset(Definition) * transform.localScale.y;

        public void Initialize(TowerDefinition definition)
        {
            Definition = definition;
            HasCompletedBattleCycle = false;
            DamageLevel = 1;
            RangeLevel = 1;
            SpeedLevel = 1;
            GoldInvested = definition.price;
            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(transform, false);
            bodyObject.transform.localPosition = GetTowerVisualOffset(definition);
            var body = bodyObject.AddComponent<SpriteRenderer>();
            body.sprite = definition.sprite != null ? definition.sprite : SpriteFactory.Circle;
            body.color = definition.sprite != null ? Color.white : definition.color;
            body.sortingOrder = 2;
            transform.localScale = Vector3.one * CalculateTowerVisualScale(definition, body.sprite);

            var rangeObject = new GameObject("Range");
            rangeObject.transform.SetParent(transform, false);
            rangeRenderer = rangeObject.AddComponent<SpriteRenderer>();
            rangeRenderer.sprite = SpriteFactory.Circle;
            rangeRenderer.color = new Color(definition.color.r, definition.color.g, definition.color.b, 0.12f);
            rangeRenderer.sortingOrder = -1;
            rangeRenderer.enabled = false;
            UpdateRangeVisual();
        }

        private static float CalculateTowerVisualScale(TowerDefinition definition, Sprite sprite)
        {
            if (definition.sprite == null || sprite == null)
                return 0.62f;

            var maxWorldSize = definition.kind switch
            {
                TowerKind.Archer => 0.92f,
                TowerKind.Mage => 1.22f,
                TowerKind.Freezer => 1.20f,
                TowerKind.Cannon => 1.30f,
                _ => 0.88f,
            };
            var largestSpriteAxis = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            return largestSpriteAxis > 0f ? maxWorldSize / largestSpriteAxis : 0.7f;
        }

        private static Vector3 GetTowerVisualOffset(TowerDefinition definition)
        {
            if (definition.sprite == null)
                return Vector3.zero;

            return definition.kind switch
            {
                TowerKind.Archer => new Vector3(0f, 0.32f, 0f),
                TowerKind.Mage => new Vector3(0f, 0.28f, 0f),
                TowerKind.Freezer => new Vector3(0f, 0.26f, 0f),
                TowerKind.Cannon => new Vector3(0f, 0.15f, 0f),
                _ => new Vector3(0f, 0.26f, 0f),
            };
        }

        public void SetRangeVisible(bool isVisible)
        {
            if (rangeRenderer != null)
                rangeRenderer.enabled = isVisible;
        }

        public void MarkCompletedBattleCycle()
        {
            HasCompletedBattleCycle = true;
        }

        public void Upgrade()
        {
            UpgradeDamage();
            UpgradeRange();
            UpgradeSpeed();
        }

        public void UpgradeDamage()
        {
            if (!CanUpgradeDamage)
                return;

            var cost = DamageUpgradeCost;
            DamageLevel++;
            GoldInvested += cost;
        }

        public void UpgradeRange()
        {
            if (!CanUpgradeRange)
                return;

            var cost = RangeUpgradeCost;
            RangeLevel++;
            GoldInvested += cost;
            UpdateRangeVisual();
        }

        public void UpgradeSpeed()
        {
            if (!CanUpgradeSpeed)
                return;

            var cost = SpeedUpgradeCost;
            SpeedLevel++;
            GoldInvested += cost;
        }

        public void Tick(float deltaTime, TowerDefenseGame game)
        {
            if (Definition == null)
                return;

            cooldown -= deltaTime;
            if (cooldown > 0f)
                return;

            var target = FindTarget(game.ActiveEnemies);
            if (target == null)
                return;

            cooldown = 1f / CurrentFireRate;
            game.FireProjectile(this, target);
        }

        private void UpdateRangeVisual()
        {
            if (rangeRenderer != null)
                rangeRenderer.transform.localScale = Vector3.one * (CurrentRange / 0.31f / transform.localScale.x);
        }

        private int CalculateUpgradeCost(int level, float multiplier)
        {
            return Mathf.RoundToInt(Definition.price * (multiplier + 0.35f * (level - 1)));
        }

        private Enemy FindTarget(IReadOnlyList<Enemy> enemies)
        {
            Enemy best = null;
            var bestProgress = -1f;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!enemy.isActiveAndEnabled)
                    continue;

                if (Vector3.Distance(transform.position, enemy.transform.position) > CurrentRange)
                    continue;

                if (enemy.Progress > bestProgress)
                {
                    best = enemy;
                    bestProgress = enemy.Progress;
                }
            }

            return best;
        }
    }
}

