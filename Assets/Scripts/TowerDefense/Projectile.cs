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
public sealed class Projectile : MonoBehaviour
    {
        private const float Speed = 8f;

        private Enemy target;
        private TowerDefinition towerDefinition;
        private TowerDefenseGame game;
        private SpriteRenderer body;
        private float damageMultiplier = 1f;

        public void Launch(Vector3 origin, Enemy enemyTarget, Tower tower, TowerDefenseGame owner)
        {
            target = enemyTarget;
            towerDefinition = tower.Definition;
            damageMultiplier = tower.DamageMultiplier;
            game = owner;
            gameObject.SetActive(true);
            transform.position = origin;
            transform.localScale = Vector3.one * GetProjectileVisualScale(towerDefinition);
            if (body == null)
            {
                body = gameObject.AddComponent<SpriteRenderer>();
                body.sortingOrder = 5;
            }

            body.sprite = towerDefinition.projectileSprite != null ? towerDefinition.projectileSprite : SpriteFactory.Circle;
            body.color = towerDefinition.projectileSprite != null ? Color.white : towerDefinition.color;
        }

        private static float GetProjectileVisualScale(TowerDefinition definition)
        {
            if (definition.projectileSprite == null)
                return 0.18f;

            return definition.kind switch
            {
                TowerKind.Archer => 0.55f,
                TowerKind.Mage => 0.42f,
                TowerKind.Freezer => 0.42f,
                TowerKind.Cannon => 0.4f,
                _ => 0.45f,
            };
        }

        private void Update()
        {
            if (target == null || !target.isActiveAndEnabled)
            {
                game?.ReleaseProjectile(this);
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, Speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, target.transform.position) <= 0.08f)
            {
                game.ApplyDamage(transform.position, towerDefinition, target, damageMultiplier);
                game.ReleaseProjectile(this);
            }
        }
    }
}

