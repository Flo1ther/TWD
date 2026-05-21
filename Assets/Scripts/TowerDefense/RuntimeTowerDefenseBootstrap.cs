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
public sealed class RuntimeTowerDefenseBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePrototypeExists()
        {
            if (FindFirstObjectByType<TowerDefenseGame>() != null)
                return;

            var root = new GameObject("Tower Defense Prototype");
            root.AddComponent<RuntimeTowerDefenseBootstrap>().Build(root);
        }

        private void Build(GameObject root)
        {
            Camera.main?.gameObject.SetActive(false);
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.8f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.12f);
            cameraObject.AddComponent<AudioListener>();

            var game = root.AddComponent<TowerDefenseGame>();
            game.Initialize(LoadTowerDefinitions(), LoadEnemyDefinitions());
        }

        private static List<TowerDefinition> LoadTowerDefinitions()
        {
            var result = DefaultTowers();
            AttachRuntimeTowerSprites(result);
            return result;
        }

        private static List<EnemyDefinition> LoadEnemyDefinitions()
        {
            var result = DefaultEnemies();
            AttachRuntimeEnemySprites(result);
            return result;
        }

        public static List<EnemyDefinition> DefaultEnemyDefinitionsForRuntime()
        {
            var definitions = DefaultEnemies();
            AttachRuntimeEnemySprites(definitions);
            return definitions;
        }

        private static void AttachRuntimeTowerSprites(List<TowerDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                switch (definition.kind)
                {
                    case TowerKind.Archer:
                        definition.sprite = SpriteLoader.Load("ArcherTowerAlt") ?? definition.sprite ?? SpriteLoader.Load("ArcherTower");
                        definition.projectileSprite ??= SpriteLoader.Load("ArrowProjectile");
                        break;
                    case TowerKind.Mage:
                        definition.sprite ??= SpriteLoader.Load("MageTower");
                        definition.projectileSprite ??= SpriteLoader.Load("MagicOrbProjectile");
                        break;
                    case TowerKind.Freezer:
                        definition.sprite ??= SpriteLoader.Load("FreezerTower");
                        definition.projectileSprite ??= SpriteLoader.Load("IceShardProjectile");
                        break;
                    case TowerKind.Cannon:
                        definition.sprite ??= SpriteLoader.Load("CannonTower");
                        definition.projectileSprite ??= SpriteLoader.Load("CannonballProjectile");
                        break;
                }
            }
        }

        private static void AttachRuntimeEnemySprites(List<EnemyDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                switch (definition.kind)
                {
                    case EnemyKind.Goblin:
                        definition.sprite ??= SpriteLoader.Load("Goblin");
                        break;
                    case EnemyKind.Orc:
                        definition.sprite ??= SpriteLoader.Load("Orc");
                        break;
                    case EnemyKind.Ghost:
                        definition.sprite ??= SpriteLoader.Load("Ghost");
                        break;
                }
            }
        }

        private static bool AreTowerDefinitionsValid(List<TowerDefinition> definitions)
        {
            if (definitions.Count == 0)
                return false;

            foreach (var kind in Enum.GetValues(typeof(TowerKind)))
            {
                if (!definitions.Exists(t => t.kind == (TowerKind)kind && t.price > 0 && t.range > 0f && t.fireRate > 0f))
                    return false;
            }

            return true;
        }

        private static bool AreEnemyDefinitionsValid(List<EnemyDefinition> definitions)
        {
            if (definitions.Count == 0)
                return false;

            foreach (var kind in Enum.GetValues(typeof(EnemyKind)))
            {
                if (!definitions.Exists(e => e.kind == (EnemyKind)kind && e.attackCost > 0 && e.maxHealth > 0f && e.speed > 0f))
                    return false;
            }

            return true;
        }

        private static List<TowerDefinition> DefaultTowers()
        {
            return new List<TowerDefinition>
            {
                new TowerDefinition { kind = TowerKind.Archer, price = 100, range = 2.75f, fireRate = 1.35f, damage = 18f, color = new Color(0.36f, 0.76f, 0.42f) },
                new TowerDefinition { kind = TowerKind.Mage, price = 150, range = 1.9f, fireRate = 0.7f, damage = 18f, splashRadius = 0.85f, color = new Color(0.55f, 0.4f, 0.95f) },
                new TowerDefinition { kind = TowerKind.Freezer, price = 120, range = 2.4f, fireRate = 0.95f, damage = 4f, slowPercent = 0.45f, slowDuration = 1.8f, color = new Color(0.32f, 0.78f, 0.94f) },
                new TowerDefinition { kind = TowerKind.Cannon, price = 200, range = 3.25f, fireRate = 0.45f, damage = 55f, color = new Color(0.93f, 0.56f, 0.26f) },
            };
        }

        private static List<EnemyDefinition> DefaultEnemies()
        {
            return new List<EnemyDefinition>
            {
                new EnemyDefinition { kind = EnemyKind.Goblin, maxHealth = 50f, speed = 1.72f, attackCost = 10, goldReward = 7, color = new Color(0.44f, 0.86f, 0.34f) },
                new EnemyDefinition { kind = EnemyKind.Orc, maxHealth = 160f, speed = 0.76f, attackCost = 25, goldReward = 16, color = new Color(0.74f, 0.47f, 0.31f) },
                new EnemyDefinition { kind = EnemyKind.Ghost, maxHealth = 95f, speed = 1.12f, attackCost = 20, goldReward = 12, color = new Color(0.78f, 0.86f, 1f), ignoresSlow = true },
            };
        }
    }
}

