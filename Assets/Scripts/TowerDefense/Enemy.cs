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
public sealed class Enemy : MonoBehaviour
    {
        private TowerDefenseGame game;
        private EnemyDefinition definition;
        private SpriteRenderer body;
        private Transform healthBar;
        private int waypointIndex;
        private float health;
        private float slowTimer;
        private float slowPercent;

        public float Progress
        {
            get
            {
                if (game == null || waypointIndex <= 0)
                    return 0f;

                var targetIndex = Mathf.Min(waypointIndex, game.Waypoints.Count - 1);
                var previous = game.Waypoints[targetIndex - 1];
                var targetPoint = game.Waypoints[targetIndex];
                var segmentLength = Mathf.Max(0.001f, Vector3.Distance(previous, targetPoint));
                var remaining = Vector3.Distance(transform.position, targetPoint);
                return targetIndex + (1f - Mathf.Clamp01(remaining / segmentLength));
            }
        }

        public void Spawn(EnemyDefinition enemyDefinition, TowerDefenseGame owner)
        {
            game = owner;
            definition = enemyDefinition;
            health = definition.maxHealth;
            slowTimer = 0f;
            slowPercent = 0f;
            gameObject.SetActive(true);
            transform.position = game.Waypoints[0];
            waypointIndex = game.Waypoints.Count > 1 ? 1 : 0;
            transform.localScale = Vector3.one * GetEnemyVisualScale(definition);

            if (body == null)
            {
                body = gameObject.AddComponent<SpriteRenderer>();
                body.sortingOrder = 3;
                var barObject = new GameObject("Health");
                barObject.transform.SetParent(transform, false);
                barObject.transform.localPosition = new Vector3(0f, 0.48f, 0f);
                healthBar = barObject.transform;
                var barRenderer = barObject.AddComponent<SpriteRenderer>();
                barRenderer.sprite = SpriteFactory.Square;
                barRenderer.color = new Color(0.9f, 0.18f, 0.16f);
                barRenderer.sortingOrder = 4;
            }

            body.sprite = definition.sprite != null ? definition.sprite : SpriteFactory.Circle;
            body.color = definition.sprite != null ? Color.white : definition.color;
            UpdateHealthBar();
            game.RegisterEnemy(this);
        }

        private void Update()
        {
            if (game == null || definition == null)
                return;

            slowTimer -= Time.deltaTime;
            var speedMultiplier = slowTimer > 0f ? 1f - slowPercent : 1f;
            var target = game.Waypoints[Mathf.Min(waypointIndex, game.Waypoints.Count - 1)];
            transform.position = Vector3.MoveTowards(transform.position, target, definition.speed * speedMultiplier * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) <= 0.02f)
            {
                waypointIndex++;
                if (waypointIndex >= game.Waypoints.Count)
                    game.EnemyReachedBase(this);
            }
        }

        public void TakeDamage(float amount, TowerDefinition towerDefinition)
        {
            health -= amount;
            if (towerDefinition.slowPercent > 0f && !definition.ignoresSlow)
            {
                slowPercent = towerDefinition.slowPercent;
                slowTimer = towerDefinition.slowDuration;
            }

            if (health <= 0f)
                game.EnemyKilled(this, definition.goldReward);
            else
                UpdateHealthBar();
        }

        private void UpdateHealthBar()
        {
            if (healthBar == null || definition == null)
                return;

            var ratio = Mathf.Clamp01(health / definition.maxHealth);
            healthBar.localScale = new Vector3(0.7f * ratio, 0.08f, 1f);
        }

        private static float GetEnemyVisualScale(EnemyDefinition definition)
        {
            if (definition.sprite == null)
                return 0.52f;

            var targetWorldSize = definition.kind switch
            {
                EnemyKind.Goblin => 0.58f,
                EnemyKind.Orc => 0.9f,
                EnemyKind.Ghost => 0.88f,
                _ => 0.68f,
            };

            var largestSpriteAxis = Mathf.Max(definition.sprite.bounds.size.x, definition.sprite.bounds.size.y);
            return largestSpriteAxis > 0f ? targetWorldSize / largestSpriteAxis : 0.6f;
        }
    }
}

