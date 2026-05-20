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
    public enum GamePhase { Menu, Preparation, Battle, RoundEnd, GameOver }
    public enum TowerKind { Archer, Mage, Freezer, Cannon }
    public enum EnemyKind { Goblin, Orc, Ghost }

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
                new TowerDefinition { kind = TowerKind.Archer, price = 100, range = 2.9f, fireRate = 1.45f, damage = 20f, color = new Color(0.36f, 0.76f, 0.42f) },
                new TowerDefinition { kind = TowerKind.Mage, price = 150, range = 1.9f, fireRate = 0.7f, damage = 18f, splashRadius = 0.85f, color = new Color(0.55f, 0.4f, 0.95f) },
                new TowerDefinition { kind = TowerKind.Freezer, price = 120, range = 2.4f, fireRate = 0.95f, damage = 4f, slowPercent = 0.45f, slowDuration = 1.8f, color = new Color(0.32f, 0.78f, 0.94f) },
                new TowerDefinition { kind = TowerKind.Cannon, price = 200, range = 3.25f, fireRate = 0.45f, damage = 55f, color = new Color(0.93f, 0.56f, 0.26f) },
            };
        }

        private static List<EnemyDefinition> DefaultEnemies()
        {
            return new List<EnemyDefinition>
            {
                new EnemyDefinition { kind = EnemyKind.Goblin, maxHealth = 45f, speed = 1.65f, attackCost = 10, goldReward = 8, color = new Color(0.44f, 0.86f, 0.34f) },
                new EnemyDefinition { kind = EnemyKind.Orc, maxHealth = 140f, speed = 0.72f, attackCost = 25, goldReward = 18, color = new Color(0.74f, 0.47f, 0.31f) },
                new EnemyDefinition { kind = EnemyKind.Ghost, maxHealth = 80f, speed = 1.05f, attackCost = 20, goldReward = 14, color = new Color(0.78f, 0.86f, 1f), ignoresSlow = true },
            };
        }
    }

    public sealed class TowerDefenseGame : MonoBehaviour
    {
        private const int Width = 12;
        private const int Height = 8;
        private const float CellSize = 1f;
        private const int MaxRounds = 10;
        private const int MaxWaveEnemies = 50;
        private const int PrewarmEnemyCount = 50;
        private const int PrewarmProjectileCount = 90;

        private readonly List<Vector2Int> pathCells = new()
        {
            new Vector2Int(0, 4), new Vector2Int(1, 4), new Vector2Int(2, 4), new Vector2Int(3, 4),
            new Vector2Int(4, 4), new Vector2Int(4, 3), new Vector2Int(5, 3), new Vector2Int(6, 3),
            new Vector2Int(7, 3), new Vector2Int(7, 4), new Vector2Int(8, 4), new Vector2Int(9, 4),
            new Vector2Int(10, 4), new Vector2Int(11, 4)
        };

        private readonly Dictionary<Vector2Int, Tower> towersByCell = new();
        private readonly List<Tower> activeTowers = new();
        private readonly List<Enemy> activeEnemies = new();
        private readonly Queue<Enemy> enemyPool = new();
        private readonly Queue<Projectile> projectilePool = new();
        private readonly Queue<ImpactEffect> impactPool = new();
        private readonly List<Vector3> waypoints = new();

        private List<TowerDefinition> towerDefinitions;
        private List<EnemyDefinition> enemyDefinitions;
        private Transform enemiesRoot;
        private Transform towersRoot;
        private Transform projectilesRoot;
        private Transform effectsRoot;
        private AudioSource audioSource;
        private AudioSource musicSource;
        private AudioClip shotClip;
        private AudioClip arrowShotClip;
        private AudioClip magicShotClip;
        private AudioClip iceShotClip;
        private AudioClip cannonShotClip;
        private AudioClip hitClip;
        private AudioClip explosionImpactClip;
        private AudioClip buildClip;
        private AudioClip sellClip;
        private AudioClip upgradeClip;
        private AudioClip leakClip;
        private AudioClip buttonClickClip;
        private AudioClip menuMusicClip;
        private AudioClip preparationMusicClip;
        private AudioClip battleMusicClip;
        private Sprite entryMarkerSprite;
        private Sprite baseMarkerSprite;
        private Sprite buildableTileSprite;
        private Sprite pathTileSprite;
        private Sprite roadHorizontalSprite;
        private Sprite roadVerticalSprite;
        private Sprite roadCornerSprite;
        private Sprite roadCornerNESprite;
        private Sprite roadCornerBLSprite;
        private Sprite roadCornerESSprite;
        private Sprite roadCornerWNSprite;
        private Sprite roadTJunctionSprite;
        private Sprite forestBorderSprite;
        private Sprite fullBackgroundSprite;
        private TowerKind selectedTower = TowerKind.Archer;
        private GamePhase phase;
        private Text hudText;
        private Text defenderText;
        private Text messageText;
        private Text attackerText;
        private Image leftHudPanel;
        private Image attackerHudPanel;
        private Button menuStartButton;
        private Button pvpStartButton;
        private Button stressTestButton;
        private Button startButton;
        private Button continueButton;
        private Button restartButton;
        private Button menuButton;
        private Button speedButton;
        private Button pauseButton;
        private Button upgradeTowerButton;
        private Button sellTowerButton;
        private Button clearWaveButton;
        private readonly Dictionary<TowerKind, Button> towerButtons = new();
        private readonly Dictionary<EnemyKind, Button> enemyButtons = new();
        private Tower selectedPlacedTower;
        private Vector2Int selectedPlacedTowerCell;
        private bool hasSelectedPlacedTower;
        private int round = 1;
        private int gold = 300;
        private int baseHealth = 20;
        private int attackBudget = 200;
        private int manualAttackBudgetRemaining;
        private int enemiesKilledThisRound;
        private int enemiesLeakedThisRound;
        private int goldEarnedThisRound;
        private int totalEnemiesKilled;
        private int totalEnemiesLeaked;
        private int totalGoldEarned;
        private int totalTowersBuilt;
        private int totalTowersSold;
        private int totalTowerUpgrades;
        private int enemiesToSpawn;
        private float spawnTimer;
        private Queue<EnemyDefinition> pendingWave = new();
        private string waveSummary = "Wave not prepared yet.";
        private string roundSummary = "";
        private string gameOverSummary = "";
        private bool stressTestActive;
        private bool playerAttackerMode;
        private bool fastBattleSpeed;
        private bool battlePaused;

        public IReadOnlyList<Enemy> ActiveEnemies => activeEnemies;
        public IReadOnlyList<Tower> ActiveTowers => activeTowers;
        public IReadOnlyList<Vector3> Waypoints => waypoints;

        public void Initialize(List<TowerDefinition> towers, List<EnemyDefinition> enemies)
        {
            towerDefinitions = towers;
            enemyDefinitions = enemies;
            enemiesRoot = new GameObject("Enemies").transform;
            towersRoot = new GameObject("Towers").transform;
            projectilesRoot = new GameObject("Projectiles").transform;
            effectsRoot = new GameObject("Effects").transform;
            BuildAudio();
            entryMarkerSprite = SpriteLoader.Load("EntryPortal");
            baseMarkerSprite = SpriteLoader.Load("DefenderBase");
            buildableTileSprite = SpriteLoader.Load("BuildableGrassTile") ?? SpriteLoader.Load("GrassTile");
            roadHorizontalSprite = SpriteLoader.Load("RoadHorizontal") ?? SpriteLoader.Load("StonePathTile");
            roadVerticalSprite = SpriteLoader.Load("RoadVertical") ?? roadHorizontalSprite;
            roadCornerSprite = SpriteLoader.Load("RoadCornerTopRight") ?? SpriteLoader.Load("RoadCorner") ?? roadHorizontalSprite;
            roadCornerNESprite = SpriteLoader.Load("RoadCornerTopRight") ?? roadCornerSprite;
            roadCornerBLSprite = SpriteLoader.Load("RoadCornerBottomLeft") ?? roadCornerSprite;
            roadCornerESSprite = SpriteLoader.Load("RoadCornerRightBottom") ?? roadCornerSprite;
            roadCornerWNSprite = SpriteLoader.Load("RoadCornerLeftTop") ?? roadCornerSprite;
            roadTJunctionSprite = SpriteLoader.Load("RoadTJunction") ?? roadHorizontalSprite;
            forestBorderSprite = SpriteLoader.Load("ForestBorderTile");
            fullBackgroundSprite = SpriteLoader.Load("MapBackground") ?? forestBorderSprite;
            pathTileSprite = roadHorizontalSprite ?? SpriteLoader.Load("StonePathTile");
            PrewarmPools();

            BuildWaypoints();
            BuildBackground();
            BuildBoard();
            BuildUi();
            SetPhase(GamePhase.Menu);
        }

        private void Update()
        {
            if (phase == GamePhase.Preparation)
                HandlePlacementInput();

            if (phase == GamePhase.Battle)
            {
                SpawnWave();
                if (pendingWave.Count == 0 && enemiesToSpawn <= 0 && activeEnemies.Count == 0)
                    CompleteRound();
            }

            for (var i = activeTowers.Count - 1; i >= 0; i--)
                activeTowers[i].Tick(Time.deltaTime, this);

            UpdateHud();
        }

        public void FireProjectile(Tower tower, Enemy target)
        {
            var projectile = GetProjectile();
            projectile.Launch(tower.transform.position, target, tower, this);
            PlaySound(GetShotClip(tower.Definition.kind));
        }

        public void ApplyDamage(Vector3 center, TowerDefinition towerDefinition, Enemy mainTarget, float damageMultiplier)
        {
            SpawnImpact(center, towerDefinition.color, towerDefinition.kind);
            var damage = towerDefinition.damage * damageMultiplier;
            if (towerDefinition.splashRadius > 0.01f)
            {
                for (var i = activeEnemies.Count - 1; i >= 0; i--)
                {
                    if (Vector3.Distance(activeEnemies[i].transform.position, center) <= towerDefinition.splashRadius)
                        activeEnemies[i].TakeDamage(damage, towerDefinition);
                }
                return;
            }

            mainTarget.TakeDamage(damage, towerDefinition);
        }

        public void ReleaseProjectile(Projectile projectile)
        {
            projectile.gameObject.SetActive(false);
            projectilePool.Enqueue(projectile);
        }

        public void ReleaseImpact(ImpactEffect impact)
        {
            impact.gameObject.SetActive(false);
            impactPool.Enqueue(impact);
        }

        public void EnemyKilled(Enemy enemy, int reward)
        {
            if (phase == GamePhase.GameOver)
                return;

            gold += reward;
            enemiesKilledThisRound++;
            goldEarnedThisRound += reward;
            totalEnemiesKilled++;
            totalGoldEarned += reward;
            ReleaseEnemy(enemy);
        }

        public void EnemyReachedBase(Enemy enemy)
        {
            if (phase == GamePhase.GameOver)
                return;

            baseHealth = Mathf.Max(0, baseHealth - 1);
            enemiesLeakedThisRound++;
            totalEnemiesLeaked++;
            ReleaseEnemy(enemy);
            if (baseHealth <= 0)
            {
                gameOverSummary = BuildGameOverSummary(false);
                SetPhase(GamePhase.GameOver);
            }
            else
                PlaySound(leakClip);
        }

        private void BuildWaypoints()
        {
            waypoints.Clear();
            foreach (var cell in pathCells)
                waypoints.Add(CellToWorld(cell));
        }

        private void BuildBoard()
        {
            var root = new GameObject("Board").transform;
            var pathSet = new HashSet<Vector2Int>(pathCells);
            CreateBoardBacking(root);
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    var tile = new GameObject($"Tile {x},{y}");
                    tile.transform.SetParent(root);
                    tile.transform.position = CellToWorld(cell);
                    var renderer = tile.AddComponent<SpriteRenderer>();
                    var isPath = pathSet.Contains(cell);
                    renderer.sprite = isPath ? GetPathTileSprite(cell, pathSet) : buildableTileSprite != null ? buildableTileSprite : SpriteFactory.Square;
                    renderer.color = renderer.sprite == SpriteFactory.Square
                        ? isPath ? new Color(0.72f, 0.62f, 0.42f) : new Color(0.18f, 0.24f, 0.24f)
                        : Color.white;
                    tile.transform.rotation = Quaternion.Euler(0f, 0f, isPath ? GetPathTileRotation(cell, pathSet) : 0f);
                    var targetTileSize = isPath ? 1.08f : 1.06f;
                    tile.transform.localScale = renderer.sprite == SpriteFactory.Square ? Vector3.one * 0.94f : Vector3.one * CalculateSpriteScale(renderer.sprite, targetTileSize);
                    renderer.sortingOrder = -5;
                }
            }

            CreateMapMarker(root, "Entry", pathCells[0], new Color(0.24f, 0.88f, 0.48f), "IN", entryMarkerSprite);
            CreateMapMarker(root, "Base", pathCells[^1], new Color(0.95f, 0.25f, 0.22f), "B", baseMarkerSprite);
        }

        private void CreateBoardBacking(Transform parent)
        {
            var backing = new GameObject("Grass Backing");
            backing.transform.SetParent(parent);
            backing.transform.position = new Vector3(0f, 0f, 0.08f);
            backing.transform.localScale = new Vector3(Width * CellSize + 0.2f, Height * CellSize + 0.2f, 1f);
            var renderer = backing.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.Square;
            renderer.color = new Color(0.34f, 0.72f, 0.08f);
            renderer.sortingOrder = -7;
        }

        private Sprite GetPathTileSprite(Vector2Int cell, HashSet<Vector2Int> pathSet)
        {
            var north = pathSet.Contains(cell + Vector2Int.up);
            var east = pathSet.Contains(cell + Vector2Int.right);
            var south = pathSet.Contains(cell + Vector2Int.down);
            var west = pathSet.Contains(cell + Vector2Int.left);
            var connections = (north ? 1 : 0) + (east ? 1 : 0) + (south ? 1 : 0) + (west ? 1 : 0);

            if (connections >= 3 && roadTJunctionSprite != null)
                return roadTJunctionSprite;

            if (north && south && roadVerticalSprite != null)
                return roadVerticalSprite;

            if (east && west && roadHorizontalSprite != null)
                return roadHorizontalSprite;

            if (connections == 1)
            {
                if ((north || south) && roadVerticalSprite != null)
                    return roadVerticalSprite;

                if ((east || west) && roadHorizontalSprite != null)
                    return roadHorizontalSprite;
            }

            if (west && south && roadCornerBLSprite != null)
                return roadCornerBLSprite;

            if (north && east && roadCornerNESprite != null)
                return roadCornerNESprite;

            if (east && south && roadCornerESSprite != null)
                return roadCornerESSprite;

            if (west && north && roadCornerWNSprite != null)
                return roadCornerWNSprite;

            return roadCornerSprite != null ? roadCornerSprite : pathTileSprite != null ? pathTileSprite : SpriteFactory.Square;
        }

        private static float GetPathTileRotation(Vector2Int cell, HashSet<Vector2Int> pathSet)
        {
            return 0f;
        }

        private static float CalculateSpriteScale(Sprite sprite, float targetWorldSize)
        {
            if (sprite == null)
                return 1f;

            var largestSpriteAxis = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            return largestSpriteAxis > 0f ? targetWorldSize / largestSpriteAxis : 1f;
        }

        private void BuildBackground()
        {
            var background = new GameObject("Background");
            background.transform.position = new Vector3(0f, 0f, 0.38f);
            var renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = fullBackgroundSprite != null ? fullBackgroundSprite : SpriteFactory.Square;
            renderer.color = fullBackgroundSprite != null ? new Color(1f, 1f, 1f, 0.86f) : new Color(0.05f, 0.11f, 0.09f);
            renderer.sortingOrder = -22;
            background.transform.localScale = fullBackgroundSprite != null
                ? Vector3.one * CalculateSpriteScale(fullBackgroundSprite, 15.8f)
                : new Vector3(22f, 14f, 1f);

            if (forestBorderSprite != null && fullBackgroundSprite == null)
            {
                CreateBackgroundPatch("Forest Top Left", new Vector3(-5.1f, 4.45f, 0.32f), 2.5f, 0f);
                CreateBackgroundPatch("Forest Top Right", new Vector3(5.1f, 4.45f, 0.32f), 2.5f, 0f);
                CreateBackgroundPatch("Forest Bottom Left", new Vector3(-5.3f, -4.65f, 0.32f), 2.55f, 180f);
                CreateBackgroundPatch("Forest Bottom Right", new Vector3(5.3f, -4.65f, 0.32f), 2.55f, 180f);
            }
        }

        private void CreateBackgroundPatch(string name, Vector3 position, float targetWorldSize, float rotation)
        {
            var patch = new GameObject(name);
            patch.transform.position = position;
            patch.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            patch.transform.localScale = Vector3.one * CalculateSpriteScale(forestBorderSprite, targetWorldSize);
            var renderer = patch.AddComponent<SpriteRenderer>();
            renderer.sprite = forestBorderSprite;
            renderer.color = new Color(1f, 1f, 1f, 0.92f);
            renderer.sortingOrder = -19;
        }

        private void CreateMapMarker(Transform parent, string name, Vector2Int cell, Color color, string label, Sprite sprite)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent);
            var isBase = name == "Base";
            var xOffset = sprite != null ? isBase ? 0.92f : -0.9f : 0f;
            marker.transform.position = CellToWorld(cell) + new Vector3(xOffset, 0f, -0.1f);
            var spriteTargetSize = isBase ? 1.55f : 1.35f;
            marker.transform.localScale = sprite != null ? Vector3.one * CalculateSpriteScale(sprite, spriteTargetSize) : Vector3.one * 0.72f;
            var renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : SpriteFactory.Circle;
            renderer.color = sprite != null ? Color.white : color;
            renderer.sortingOrder = 1;

            if (sprite != null)
                return;

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(marker.transform, false);
            textObject.transform.localPosition = Vector3.zero;
            var textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = label;
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.08f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            var meshRenderer = textObject.GetComponent<MeshRenderer>() ?? textObject.AddComponent<MeshRenderer>();
            meshRenderer.sortingOrder = 2;
        }

        private void BuildUi()
        {
            EnsureEventSystem();

            var canvasObject = new GameObject("HUD");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            leftHudPanel = CreatePanel(canvas.transform, "Left HUD Panel", new Vector2(18, -18), new Vector2(220f, 214f), TextAnchor.UpperLeft);
            hudText = CreateText(canvas.transform, "HUD Text", new Vector2(30, -28), TextAnchor.UpperLeft, 15);
            hudText.GetComponent<RectTransform>().sizeDelta = new Vector2(204f, 58f);
            defenderText = CreateText(canvas.transform, "Defender Text", new Vector2(30, -86), TextAnchor.UpperLeft, 15);
            defenderText.GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 126f);
            messageText = CreateText(canvas.transform, "Message Text", new Vector2(0, -18), TextAnchor.UpperCenter, 15);
            messageText.GetComponent<RectTransform>().sizeDelta = new Vector2(540f, 76f);
            attackerHudPanel = CreatePanel(canvas.transform, "Attacker HUD Panel", new Vector2(-18, -74), new Vector2(224f, 132f), TextAnchor.UpperRight);
            attackerText = CreateText(canvas.transform, "Attacker Text", new Vector2(-30, -86), TextAnchor.UpperRight, 15);
            var attackerRect = attackerText.GetComponent<RectTransform>();
            attackerRect.anchorMin = new Vector2(1f, 1f);
            attackerRect.anchorMax = new Vector2(1f, 1f);
            attackerRect.pivot = new Vector2(1f, 1f);
            attackerRect.sizeDelta = new Vector2(248f, 150f);
            attackerHudPanel.gameObject.SetActive(false);
            attackerText.gameObject.SetActive(false);

            var x = 16f;
            foreach (var tower in towerDefinitions)
            {
                var captured = tower;
                var button = CreateButton(canvas.transform, $"{tower.kind} {tower.price}", new Vector2(x, 18), () => SelectTower(captured.kind));
                towerButtons[captured.kind] = button;
                x += 116f;
            }

            menuStartButton = CreateButton(canvas.transform, "Start PvE", new Vector2(0, -54), StartNewGame);
            var menuRect = menuStartButton.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0.5f, 0.5f);
            menuRect.anchorMax = new Vector2(0.5f, 0.5f);
            menuRect.pivot = new Vector2(0.5f, 0.5f);
            menuRect.sizeDelta = new Vector2(136f, 40f);

            pvpStartButton = CreateButton(canvas.transform, "Start PvP", new Vector2(0, -102), StartPvpGame);
            var pvpRect = pvpStartButton.GetComponent<RectTransform>();
            pvpRect.anchorMin = new Vector2(0.5f, 0.5f);
            pvpRect.anchorMax = new Vector2(0.5f, 0.5f);
            pvpRect.pivot = new Vector2(0.5f, 0.5f);
            pvpRect.sizeDelta = new Vector2(136f, 40f);

            stressTestButton = CreateButton(canvas.transform, "Stress Test", new Vector2(0, -150), StartStressTest);
            var stressRect = stressTestButton.GetComponent<RectTransform>();
            stressRect.anchorMin = new Vector2(0.5f, 0.5f);
            stressRect.anchorMax = new Vector2(0.5f, 0.5f);
            stressRect.pivot = new Vector2(0.5f, 0.5f);
            stressRect.sizeDelta = new Vector2(136f, 40f);

            var enemyX = 16f;
            foreach (var enemy in enemyDefinitions)
            {
                var captured = enemy;
                var button = CreateButton(canvas.transform, $"{enemy.kind} {enemy.attackCost}", new Vector2(enemyX, 62), () => AddEnemyToManualWave(captured.kind));
                enemyButtons[captured.kind] = button;
                enemyX += 116f;
            }

            clearWaveButton = CreateButton(canvas.transform, "Clear Wave", new Vector2(enemyX, 62), ClearManualWave);
            clearWaveButton.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 36f);

            startButton = CreateButton(canvas.transform, "Start Battle", new Vector2(-136, 18), StartBattle);
            var rect = startButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);

            continueButton = CreateButton(canvas.transform, "Continue", new Vector2(-136, 18), ContinueAfterRound);
            var continueRect = continueButton.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(1, 0);
            continueRect.anchorMax = new Vector2(1, 0);
            continueButton.gameObject.SetActive(false);

            restartButton = CreateButton(canvas.transform, "Restart", new Vector2(-136, 62), RestartGame);
            var restartRect = restartButton.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(1, 0);
            restartRect.anchorMax = new Vector2(1, 0);
            restartButton.gameObject.SetActive(false);

            menuButton = CreateButton(canvas.transform, "Main Menu", new Vector2(-136, 62), ReturnToMenu);
            var menuButtonRect = menuButton.GetComponent<RectTransform>();
            menuButtonRect.anchorMin = new Vector2(1, 0);
            menuButtonRect.anchorMax = new Vector2(1, 0);
            menuButtonRect.sizeDelta = new Vector2(120f, 36f);
            menuButton.gameObject.SetActive(false);

            speedButton = CreateButton(canvas.transform, "Speed x1", new Vector2(-136, 106), ToggleBattleSpeed);
            var speedRect = speedButton.GetComponent<RectTransform>();
            speedRect.anchorMin = new Vector2(1, 0);
            speedRect.anchorMax = new Vector2(1, 0);
            speedRect.sizeDelta = new Vector2(120f, 36f);
            speedButton.gameObject.SetActive(false);

            pauseButton = CreateButton(canvas.transform, "Pause", new Vector2(-136, 150), ToggleBattlePause);
            var pauseRect = pauseButton.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(1, 0);
            pauseRect.anchorMax = new Vector2(1, 0);
            pauseRect.sizeDelta = new Vector2(120f, 36f);
            pauseButton.gameObject.SetActive(false);

            upgradeTowerButton = CreateButton(canvas.transform, "Upgrade", new Vector2(-268, 62), UpgradeSelectedTower);
            var upgradeRect = upgradeTowerButton.GetComponent<RectTransform>();
            upgradeRect.anchorMin = new Vector2(1, 0);
            upgradeRect.anchorMax = new Vector2(1, 0);
            upgradeRect.sizeDelta = new Vector2(120f, 36f);
            upgradeTowerButton.gameObject.SetActive(false);

            sellTowerButton = CreateButton(canvas.transform, "Sell", new Vector2(-268, 18), SellSelectedTower);
            var sellRect = sellTowerButton.GetComponent<RectTransform>();
            sellRect.anchorMin = new Vector2(1, 0);
            sellRect.anchorMax = new Vector2(1, 0);
            sellRect.sizeDelta = new Vector2(120f, 36f);
            sellTowerButton.gameObject.SetActive(false);
            SelectTower(selectedTower);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private void SelectTower(TowerKind kind)
        {
            selectedTower = kind;
            foreach (var pair in towerButtons)
            {
                var image = pair.Value.GetComponent<Image>();
                image.color = pair.Key == selectedTower
                    ? new Color(0.22f, 0.36f, 0.3f, 0.98f)
                    : new Color(0.13f, 0.16f, 0.18f, 0.94f);
            }
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, TextAnchor anchor, int size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = Color.white;
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = anchor == TextAnchor.UpperCenter ? new Vector2(0.5f, 1f) : anchor == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = anchor == TextAnchor.UpperCenter ? new Vector2(0.5f, 1f) : anchor == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(520f, 96f);
            return text;
        }

        private static Image CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor)
        {
            var panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            var image = panelObject.AddComponent<Image>();
            image.color = new Color(0.04f, 0.07f, 0.07f, 0.58f);
            image.raycastTarget = false;

            var rect = image.GetComponent<RectTransform>();
            rect.anchorMin = anchor == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = anchor == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return image;
        }

        private Button CreateButton(Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.13f, 0.16f, 0.18f, 0.94f);
            var button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                PlaySound(buttonClickClip);
                action?.Invoke();
            });

            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(108f, 36f);

            var text = CreateText(buttonObject.transform, "Label", Vector2.zero, TextAnchor.MiddleCenter, 14);
            text.text = label;
            text.raycastTarget = false;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            return button;
        }

        private void HandlePlacementInput()
        {
            if (!WasPrimaryClickPressed())
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var world = Camera.main.ScreenToWorldPoint(GetPointerPosition());
            var cell = WorldToCell(world);
            if (!IsInside(cell) || pathCells.Contains(cell))
                return;

            if (towersByCell.TryGetValue(cell, out var existingTower))
            {
                SelectPlacedTower(cell, existingTower);
                return;
            }

            ClearPlacedTowerSelection();
            var definition = towerDefinitions.Find(t => t.kind == selectedTower);
            if (definition == null || gold < definition.price)
                return;

            gold -= definition.price;
            SelectPlacedTower(cell, PlaceTower(cell, definition));
            totalTowersBuilt++;
            PlaySound(buildClip);
        }

        private Tower PlaceTower(Vector2Int cell, TowerDefinition definition)
        {
            var towerObject = new GameObject(definition.kind.ToString());
            towerObject.transform.SetParent(towersRoot);
            towerObject.transform.position = CellToWorld(cell);
            var tower = towerObject.AddComponent<Tower>();
            tower.Initialize(definition);
            activeTowers.Add(tower);
            towersByCell[cell] = tower;
            return tower;
        }

        private void SellTower(Vector2Int cell, Tower tower)
        {
            var refund = tower.SellRefund;
            gold += refund;
            activeTowers.Remove(tower);
            towersByCell.Remove(cell);
            totalTowersSold++;
            Destroy(tower.gameObject);
            messageText.text = $"Sold {tower.Definition.kind} for {refund} gold.";
            PlaySound(sellClip);
            UpdateTowerActionButtons();
        }

        private void SelectPlacedTower(Vector2Int cell, Tower tower)
        {
            if (phase != GamePhase.Preparation || tower == null)
                return;

            selectedPlacedTower = tower;
            selectedPlacedTowerCell = cell;
            hasSelectedPlacedTower = true;
            messageText.text = BuildSelectedTowerMessage(tower);
            UpdateTowerActionButtons();
        }

        private void ClearPlacedTowerSelection()
        {
            selectedPlacedTower = null;
            hasSelectedPlacedTower = false;
            UpdateTowerActionButtons();
        }

        private void UpgradeSelectedTower()
        {
            if (phase != GamePhase.Preparation || !hasSelectedPlacedTower || selectedPlacedTower == null)
                return;

            if (!selectedPlacedTower.CanUpgrade)
            {
                messageText.text = $"{selectedPlacedTower.Definition.kind} is already max level.";
                UpdateTowerActionButtons();
                return;
            }

            var cost = selectedPlacedTower.UpgradeCost;
            if (gold < cost)
            {
                messageText.text = $"Need {cost} gold to upgrade {selectedPlacedTower.Definition.kind}.";
                UpdateTowerActionButtons();
                return;
            }

            gold -= cost;
            selectedPlacedTower.Upgrade();
            totalTowerUpgrades++;
            messageText.text = BuildSelectedTowerMessage(selectedPlacedTower);
            PlaySound(upgradeClip);
            UpdateTowerActionButtons();
        }

        private void SellSelectedTower()
        {
            if (phase != GamePhase.Preparation || !hasSelectedPlacedTower || selectedPlacedTower == null)
                return;

            var tower = selectedPlacedTower;
            var cell = selectedPlacedTowerCell;
            ClearPlacedTowerSelection();
            SellTower(cell, tower);
        }

        private static string BuildSelectedTowerMessage(Tower tower)
        {
            var upgradeText = tower.CanUpgrade ? $"Upgrade: {tower.UpgradeCost}" : "Max level";
            return $"Selected {tower.Definition.kind} L{tower.Level}. Damage: {tower.CurrentDamage:0}. Range: {tower.CurrentRange:0.0}. Fire rate: {tower.CurrentFireRate:0.0}. {upgradeText}. Sell: {tower.SellRefund}.";
        }

        private void StartBattle()
        {
            if (phase != GamePhase.Preparation)
                return;

            enemiesKilledThisRound = 0;
            enemiesLeakedThisRound = 0;
            goldEarnedThisRound = 0;
            gameOverSummary = "";
            roundSummary = "";
            if (pendingWave.Count == 0)
                PrepareNextWave();

            if (pendingWave.Count == 0)
            {
                if (playerAttackerMode)
                {
                    messageText.text = "Attacker must add at least one enemy before battle.";
                    return;
                }

                Debug.LogWarning("AI wave was empty. Rebuilding enemy definitions from fallback data.");
                enemyDefinitions = RuntimeTowerDefenseBootstrap.DefaultEnemyDefinitionsForRuntime();
                pendingWave = BuildAiWave();
            }

            waveSummary = DescribeWave(pendingWave);
            enemiesToSpawn = pendingWave.Count;
            spawnTimer = 0f;
            ClearPlacedTowerSelection();
            SetTowerRangeVisible(false);
            SetPhase(GamePhase.Battle);
        }

        private void StartNewGame()
        {
            ResetRun();
            stressTestActive = false;
            playerAttackerMode = false;
            SetPhase(GamePhase.Preparation);
        }

        private void StartPvpGame()
        {
            ResetRun();
            playerAttackerMode = true;
            manualAttackBudgetRemaining = attackBudget;
            UpdateManualWaveSummary();
            SetPhase(GamePhase.Preparation);
        }

        private void StartStressTest()
        {
            if (towerDefinitions.Count == 0)
                return;

            ResetRun();
            stressTestActive = true;
            playerAttackerMode = false;
            gold = 9999;
            attackBudget = 500;
            enemiesKilledThisRound = 0;
            enemiesLeakedThisRound = 0;
            goldEarnedThisRound = 0;

            var buildableCells = GetBuildableCells();
            for (var i = 0; i < 20 && i < buildableCells.Count; i++)
            {
                var definition = towerDefinitions[i % towerDefinitions.Count];
                PlaceTower(buildableCells[i], definition);
            }

            pendingWave = BuildFixedStressWave();
            waveSummary = DescribeWave(pendingWave);
            enemiesToSpawn = pendingWave.Count;
            spawnTimer = 0f;
            roundSummary = "";
            SetPhase(GamePhase.Battle);
        }

        private void RestartGame()
        {
            var restartAsPvp = playerAttackerMode;
            ResetRun();
            stressTestActive = false;
            playerAttackerMode = restartAsPvp;
            if (playerAttackerMode)
            {
                manualAttackBudgetRemaining = attackBudget;
                waveSummary = "Attacker: add enemies to form a wave.";
            }

            SetPhase(GamePhase.Preparation);
        }

        private void ContinueAfterRound()
        {
            if (phase != GamePhase.RoundEnd)
                return;

            roundSummary = "";
            if (playerAttackerMode)
            {
                pendingWave.Clear();
                manualAttackBudgetRemaining = attackBudget;
                UpdateManualWaveSummary();
            }
            else
            {
                PrepareNextWave();
            }

            SetPhase(GamePhase.Preparation);
        }

        private void ReturnToMenu()
        {
            ResetRun();
            SetPhase(GamePhase.Menu);
        }

        private void ToggleBattleSpeed()
        {
            if (battlePaused)
                battlePaused = false;

            fastBattleSpeed = !fastBattleSpeed;
            Time.timeScale = fastBattleSpeed ? 2f : 1f;
            UpdateBattleSpeedButton();
            UpdateBattlePauseButton();
        }

        private void ToggleBattlePause()
        {
            if (phase != GamePhase.Battle)
                return;

            battlePaused = !battlePaused;
            Time.timeScale = battlePaused ? 0f : fastBattleSpeed ? 2f : 1f;
            messageText.text = battlePaused ? "Battle paused." : "Battle in progress.";
            UpdateBattlePauseButton();
        }

        private void ResetRun()
        {
            pendingWave.Clear();
            enemiesToSpawn = 0;
            spawnTimer = 0f;
            ClearPlacedTowerSelection();

            for (var i = activeEnemies.Count - 1; i >= 0; i--)
                ReleaseEnemy(activeEnemies[i]);

            foreach (var projectile in projectilesRoot.GetComponentsInChildren<Projectile>(true))
            {
                if (projectile.gameObject.activeSelf)
                    ReleaseProjectile(projectile);
            }

            foreach (var tower in activeTowers)
            {
                if (tower != null)
                    Destroy(tower.gameObject);
            }

            activeTowers.Clear();
            towersByCell.Clear();
            round = 1;
            gold = 300;
            baseHealth = 20;
            attackBudget = 200;
            manualAttackBudgetRemaining = attackBudget;
            enemiesKilledThisRound = 0;
            enemiesLeakedThisRound = 0;
            goldEarnedThisRound = 0;
            totalEnemiesKilled = 0;
            totalEnemiesLeaked = 0;
            totalGoldEarned = 0;
            totalTowersBuilt = 0;
            totalTowersSold = 0;
            totalTowerUpgrades = 0;
            waveSummary = "Wave not prepared yet.";
            roundSummary = "";
            gameOverSummary = "";
            stressTestActive = false;
            playerAttackerMode = false;
            fastBattleSpeed = false;
            battlePaused = false;
            Time.timeScale = 1f;
        }

        private Queue<EnemyDefinition> BuildAiWave()
        {
            var budget = GetAiWaveBudget();
            var result = new Queue<EnemyDefinition>();
            var roundBias = Mathf.Clamp(round - 1, 0, 9);
            var cheapestEnemy = FindCheapestEnemy();
            if (cheapestEnemy == null || cheapestEnemy.attackCost <= 0)
                return result;

            while (budget >= cheapestEnemy.attackCost && result.Count < MaxWaveEnemies)
            {
                var pick = PickAiEnemy(roundBias, budget);

                if (pick == null || pick.attackCost <= 0 || pick.attackCost > budget)
                    pick = cheapestEnemy;

                budget -= pick.attackCost;
                result.Enqueue(pick);
            }

            return result;
        }

        private int GetAiWaveBudget()
        {
            var baseBudget = 130 + (round - 1) * 36;
            var towerPressure = Mathf.Min(80, activeTowers.Count * 8 + totalTowerUpgrades * 5);
            return Mathf.Min(MaxWaveEnemies * 10, baseBudget + towerPressure);
        }

        private EnemyDefinition PickAiEnemy(int roundBias, int budget)
        {
            var goblin = enemyDefinitions.Find(e => e.kind == EnemyKind.Goblin);
            var orc = enemyDefinitions.Find(e => e.kind == EnemyKind.Orc);
            var ghost = enemyDefinitions.Find(e => e.kind == EnemyKind.Ghost);
            var roll = UnityEngine.Random.value;

            if (roundBias >= 7 && orc != null && budget >= orc.attackCost && roll < 0.42f)
                return orc;

            if (roundBias >= 5 && ghost != null && budget >= ghost.attackCost && roll < 0.36f)
                return ghost;

            if (roundBias >= 3 && orc != null && budget >= orc.attackCost && roll < 0.24f)
                return orc;

            return goblin;
        }

        private void PrepareNextWave()
        {
            if (playerAttackerMode)
            {
                UpdateManualWaveSummary();
                return;
            }

            pendingWave = BuildAiWave();
            waveSummary = DescribeWave(pendingWave);
        }

        private void AddEnemyToManualWave(EnemyKind kind)
        {
            if (phase != GamePhase.Preparation || !playerAttackerMode)
                return;

            if (pendingWave.Count >= MaxWaveEnemies)
            {
                messageText.text = $"Wave limit reached ({MaxWaveEnemies}).";
                UpdateManualWaveSummary();
                return;
            }

            var definition = enemyDefinitions.Find(e => e.kind == kind);
            if (definition == null || definition.attackCost <= 0)
                return;

            if (definition.attackCost > manualAttackBudgetRemaining)
            {
                messageText.text = "Not enough attack budget for that enemy.";
                UpdateManualWaveSummary();
                return;
            }

            manualAttackBudgetRemaining -= definition.attackCost;
            pendingWave.Enqueue(definition);
            UpdateManualWaveSummary();
            UpdateManualEnemyButtons();
        }

        private void ClearManualWave()
        {
            if (phase != GamePhase.Preparation || !playerAttackerMode)
                return;

            pendingWave.Clear();
            manualAttackBudgetRemaining = attackBudget;
            UpdateManualWaveSummary();
            UpdateManualEnemyButtons();
            messageText.text = "Attacker wave cleared.";
        }

        private void UpdateManualWaveSummary()
        {
            waveSummary = $"{DescribeWave(pendingWave)} | Budget left: {manualAttackBudgetRemaining} | Count: {pendingWave.Count}/{MaxWaveEnemies}";
        }

        private void UpdateManualEnemyButtons()
        {
            foreach (var pair in enemyButtons)
            {
                var definition = enemyDefinitions.Find(e => e.kind == pair.Key);
                var affordable = definition != null && definition.attackCost <= manualAttackBudgetRemaining;
                pair.Value.interactable = phase == GamePhase.Preparation && playerAttackerMode && affordable && pendingWave.Count < MaxWaveEnemies;
                pair.Value.gameObject.SetActive(phase == GamePhase.Preparation && playerAttackerMode);
            }

            if (clearWaveButton != null)
            {
                clearWaveButton.interactable = phase == GamePhase.Preparation && playerAttackerMode && pendingWave.Count > 0;
                clearWaveButton.gameObject.SetActive(phase == GamePhase.Preparation && playerAttackerMode);
            }
        }

        private Queue<EnemyDefinition> BuildFixedStressWave()
        {
            var result = new Queue<EnemyDefinition>();
            var goblin = enemyDefinitions.Find(e => e.kind == EnemyKind.Goblin);
            var fallback = FindCheapestEnemy();
            var enemy = goblin != null && goblin.attackCost > 0 ? goblin : fallback;
            if (enemy == null)
                return result;

            for (var i = 0; i < MaxWaveEnemies; i++)
                result.Enqueue(enemy);

            return result;
        }

        private List<Vector2Int> GetBuildableCells()
        {
            var result = new List<Vector2Int>();
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!pathCells.Contains(cell) && !towersByCell.ContainsKey(cell))
                        result.Add(cell);
                }
            }

            return result;
        }

        private EnemyDefinition FindCheapestEnemy()
        {
            EnemyDefinition cheapest = null;
            foreach (var enemy in enemyDefinitions)
            {
                if (enemy.attackCost <= 0)
                    continue;

                if (cheapest == null || enemy.attackCost < cheapest.attackCost)
                    cheapest = enemy;
            }

            return cheapest;
        }

        private static string DescribeWave(IEnumerable<EnemyDefinition> wave)
        {
            var counts = new Dictionary<EnemyKind, int>();
            var total = 0;
            foreach (var enemy in wave)
            {
                if (!counts.ContainsKey(enemy.kind))
                    counts[enemy.kind] = 0;
                counts[enemy.kind]++;
                total++;
            }

            if (total == 0)
                return "Wave: empty.";

            var parts = new List<string>();
            foreach (var pair in counts)
                parts.Add($"{pair.Key} x{pair.Value}");
            return $"Wave: {string.Join(", ", parts)}";
        }

        private void SpawnWave()
        {
            if (pendingWave.Count == 0)
                return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
                return;

            spawnTimer = 0.9f;
            var enemy = GetEnemy();
            enemy.Spawn(pendingWave.Dequeue(), this);
            enemiesToSpawn = pendingWave.Count;
        }

        private Enemy GetEnemy()
        {
            if (enemyPool.Count > 0)
                return enemyPool.Dequeue();

            var enemyObject = new GameObject("Enemy");
            enemyObject.transform.SetParent(enemiesRoot);
            return enemyObject.AddComponent<Enemy>();
        }

        private Projectile GetProjectile()
        {
            if (projectilePool.Count > 0)
                return projectilePool.Dequeue();

            var projectileObject = new GameObject("Projectile");
            projectileObject.transform.SetParent(projectilesRoot);
            return projectileObject.AddComponent<Projectile>();
        }

        private void PrewarmPools()
        {
            for (var i = 0; i < PrewarmEnemyCount; i++)
            {
                var enemyObject = new GameObject($"Enemy Pooled {i + 1}");
                enemyObject.transform.SetParent(enemiesRoot);
                var enemy = enemyObject.AddComponent<Enemy>();
                enemyObject.SetActive(false);
                enemyPool.Enqueue(enemy);
            }

            for (var i = 0; i < PrewarmProjectileCount; i++)
            {
                var projectileObject = new GameObject($"Projectile Pooled {i + 1}");
                projectileObject.transform.SetParent(projectilesRoot);
                var projectile = projectileObject.AddComponent<Projectile>();
                projectileObject.SetActive(false);
                projectilePool.Enqueue(projectile);
            }
        }

        private void SpawnImpact(Vector3 position, Color color, TowerKind towerKind)
        {
            var impact = impactPool.Count > 0 ? impactPool.Dequeue() : CreateImpact();
            impact.Play(position, color, this);
            PlaySound(towerKind == TowerKind.Cannon ? explosionImpactClip ?? hitClip : hitClip);
        }

        private ImpactEffect CreateImpact()
        {
            var impactObject = new GameObject("Impact");
            impactObject.transform.SetParent(effectsRoot);
            impactObject.SetActive(false);
            return impactObject.AddComponent<ImpactEffect>();
        }

        private void BuildAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.28f;
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0.18f;

            arrowShotClip = LoadAudioClip("ArrowShot") ?? CreateToneClip("TD Arrow Shot", 720f, 0.045f, 0.18f);
            magicShotClip = LoadAudioClip("MagicShot") ?? CreateToneClip("TD Magic Shot", 620f, 0.08f, 0.18f);
            iceShotClip = LoadAudioClip("IceShot") ?? CreateToneClip("TD Ice Shot", 940f, 0.07f, 0.16f);
            cannonShotClip = LoadAudioClip("CannonShot") ?? CreateToneClip("TD Cannon Shot", 120f, 0.16f, 0.24f);
            shotClip = arrowShotClip;
            hitClip = LoadAudioClip("EnemyHit") ?? CreateToneClip("TD Hit", 210f, 0.055f, 0.2f);
            explosionImpactClip = LoadAudioClip("ExplosionImpact") ?? hitClip;
            buildClip = LoadAudioClip("BuildTower") ?? CreateToneClip("TD Build", 520f, 0.08f, 0.22f);
            sellClip = LoadAudioClip("SellTower") ?? CreateToneClip("TD Sell", 360f, 0.08f, 0.18f);
            upgradeClip = LoadAudioClip("UpgradeTower") ?? CreateToneClip("TD Upgrade", 840f, 0.11f, 0.22f);
            leakClip = CreateToneClip("TD Leak", 120f, 0.18f, 0.2f);
            buttonClickClip = LoadAudioClip("ButtonClick") ?? CreateToneClip("TD Button", 680f, 0.035f, 0.12f);
            menuMusicClip = LoadAudioClip("MenuMusic");
            preparationMusicClip = LoadAudioClip("PreparationMusic");
            battleMusicClip = LoadAudioClip("BattleMusic");
        }

        private static AudioClip LoadAudioClip(string name)
        {
            return Resources.Load<AudioClip>($"TowerDefense/Audio/{name}");
        }

        private AudioClip GetShotClip(TowerKind kind)
        {
            return kind switch
            {
                TowerKind.Archer => arrowShotClip ?? shotClip,
                TowerKind.Mage => magicShotClip ?? shotClip,
                TowerKind.Freezer => iceShotClip ?? shotClip,
                TowerKind.Cannon => cannonShotClip ?? shotClip,
                _ => shotClip,
            };
        }

        private static AudioClip CreateToneClip(string name, float frequency, float duration, float volume)
        {
            const int sampleRate = 22050;
            var samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = 1f - i / (float)samples;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        private void PlayMusic(AudioClip clip)
        {
            if (musicSource == null)
                return;

            if (clip == null)
            {
                musicSource.Stop();
                musicSource.clip = null;
                return;
            }

            if (musicSource.clip == clip && musicSource.isPlaying)
                return;

            musicSource.clip = clip;
            musicSource.Play();
        }

        private void ReleaseEnemy(Enemy enemy)
        {
            activeEnemies.Remove(enemy);
            enemy.gameObject.SetActive(false);
            enemyPool.Enqueue(enemy);
        }

        private void CompleteRound()
        {
            if (stressTestActive)
            {
                MarkTowersCompletedBattleCycle();
                roundSummary = $"Stress test complete. Towers: {activeTowers.Count}. Enemies tested: {enemiesKilledThisRound + enemiesLeakedThisRound}. Reached base: {enemiesLeakedThisRound}.";
                SetPhase(GamePhase.RoundEnd);
                return;
            }

            MarkTowersCompletedBattleCycle();
            var completedRound = round;
            var roundBonus = 50 + round * 10;
            gold += roundBonus;
            totalGoldEarned += roundBonus;
            round++;
            attackBudget = GetAiWaveBudget();
            roundSummary = $"Round {completedRound} complete. Killed: {enemiesKilledThisRound}. Reached base: {enemiesLeakedThisRound}. Gold earned: {goldEarnedThisRound + roundBonus}.";

            if (round > MaxRounds)
            {
                gameOverSummary = BuildGameOverSummary(true);
                SetPhase(GamePhase.GameOver);
            }
            else
                SetPhase(GamePhase.RoundEnd);
        }

        private string BuildGameOverSummary(bool defenderWon)
        {
            var title = defenderWon ? "Defender victory: all rounds survived." : "Attacker victory: base destroyed.";
            return $"{title}\nScore: {CalculateScore()}. Killed: {totalEnemiesKilled}. Leaked: {totalEnemiesLeaked}. Gold earned: {totalGoldEarned}.\nTowers built: {totalTowersBuilt}. Upgrades: {totalTowerUpgrades}. Sold: {totalTowersSold}.";
        }

        private int CalculateScore()
        {
            var completedRounds = Mathf.Clamp(round - 1, 0, MaxRounds);
            return completedRounds * 250 + totalEnemiesKilled * 12 + baseHealth * 40 + totalTowerUpgrades * 25 - totalEnemiesLeaked * 30;
        }

        private void SetPhase(GamePhase nextPhase)
        {
            phase = nextPhase;
            UpdateMusicForPhase();
            if (startButton != null)
            {
                startButton.interactable = phase == GamePhase.Preparation;
                startButton.gameObject.SetActive(phase == GamePhase.Preparation);
            }

            if (continueButton != null)
                continueButton.gameObject.SetActive(phase == GamePhase.RoundEnd && !stressTestActive);

            if (menuStartButton != null)
                menuStartButton.gameObject.SetActive(phase == GamePhase.Menu);

            if (pvpStartButton != null)
                pvpStartButton.gameObject.SetActive(phase == GamePhase.Menu);

            if (stressTestButton != null)
                stressTestButton.gameObject.SetActive(phase == GamePhase.Menu);

            if (restartButton != null)
                restartButton.gameObject.SetActive(phase == GamePhase.GameOver);

            if (menuButton != null)
                menuButton.gameObject.SetActive(phase == GamePhase.Preparation || phase == GamePhase.RoundEnd || phase == GamePhase.GameOver);

            if (speedButton != null)
            {
                speedButton.gameObject.SetActive(phase == GamePhase.Battle);
                if (phase != GamePhase.Battle)
                {
                    fastBattleSpeed = false;
                    battlePaused = false;
                    Time.timeScale = 1f;
                }

                UpdateBattleSpeedButton();
            }

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(phase == GamePhase.Battle);
                UpdateBattlePauseButton();
            }

            foreach (var button in towerButtons.Values)
            {
                button.interactable = phase == GamePhase.Preparation;
                button.gameObject.SetActive(phase == GamePhase.Preparation);
            }

            UpdateManualEnemyButtons();
            UpdateTowerActionButtons();

            if (phase == GamePhase.Menu)
            {
                messageText.text = "Tower Defense prototype";
            }
            else if (phase == GamePhase.RoundEnd)
            {
                messageText.text = roundSummary;
            }
            else if (phase == GamePhase.GameOver)
            {
                EndBattleImmediately();
                messageText.text = string.IsNullOrEmpty(gameOverSummary)
                    ? (baseHealth > 0 ? BuildGameOverSummary(true) : BuildGameOverSummary(false))
                    : gameOverSummary;
            }
            else
            {
                if (phase == GamePhase.Preparation)
                {
                    SetTowerRangeVisible(true);
                    if (pendingWave.Count == 0)
                        PrepareNextWave();

                    if (!hasSelectedPlacedTower)
                        messageText.text = playerAttackerMode
                            ? "Defender places towers. Attacker adds enemies, then start battle."
                            : "Click a free tile to place the selected tower.";
                }
                else
                {
                    ClearPlacedTowerSelection();
                    SetTowerRangeVisible(false);
                    messageText.text = "Battle in progress.";
                }
            }
        }

        private void UpdateMusicForPhase()
        {
            var clip = phase switch
            {
                GamePhase.Menu => menuMusicClip,
                GamePhase.Preparation => preparationMusicClip,
                GamePhase.Battle => battleMusicClip,
                GamePhase.RoundEnd => preparationMusicClip,
                GamePhase.GameOver => menuMusicClip,
                _ => null,
            };

            PlayMusic(clip);
        }

        private void UpdateTowerActionButtons()
        {
            var show = phase == GamePhase.Preparation && hasSelectedPlacedTower && selectedPlacedTower != null && activeTowers.Contains(selectedPlacedTower);
            if (upgradeTowerButton != null)
            {
                upgradeTowerButton.gameObject.SetActive(show);
                upgradeTowerButton.interactable = show && selectedPlacedTower.CanUpgrade && gold >= selectedPlacedTower.UpgradeCost;
                SetButtonLabel(upgradeTowerButton, show && selectedPlacedTower.CanUpgrade ? $"Upgrade {selectedPlacedTower.UpgradeCost}" : "Max Level");
            }

            if (sellTowerButton != null)
            {
                sellTowerButton.gameObject.SetActive(show);
                sellTowerButton.interactable = show;
                SetButtonLabel(sellTowerButton, show ? $"Sell {selectedPlacedTower.SellRefund}" : "Sell");
            }
        }

        private void UpdateBattleSpeedButton()
        {
            if (speedButton == null)
                return;

            SetButtonLabel(speedButton, fastBattleSpeed ? "Speed x2" : "Speed x1");
        }

        private void UpdateBattlePauseButton()
        {
            if (pauseButton == null)
                return;

            SetButtonLabel(pauseButton, battlePaused ? "Resume" : "Pause");
        }

        private static void SetButtonLabel(Button button, string label)
        {
            var text = button != null ? button.GetComponentInChildren<Text>() : null;
            if (text != null)
                text.text = label;
        }

        private void SetTowerRangeVisible(bool isVisible)
        {
            foreach (var tower in activeTowers)
                tower.SetRangeVisible(isVisible);
        }

        private void MarkTowersCompletedBattleCycle()
        {
            foreach (var tower in activeTowers)
                tower.MarkCompletedBattleCycle();
        }

        private void EndBattleImmediately()
        {
            pendingWave.Clear();
            enemiesToSpawn = 0;
            spawnTimer = 0f;
            baseHealth = Mathf.Max(0, baseHealth);

            for (var i = activeEnemies.Count - 1; i >= 0; i--)
                ReleaseEnemy(activeEnemies[i]);

            foreach (var projectile in projectilesRoot.GetComponentsInChildren<Projectile>(true))
            {
                if (projectile.gameObject.activeSelf)
                    ReleaseProjectile(projectile);
            }
        }

        private void UpdateHud()
        {
            if (hudText == null)
                return;

            var showDefenderHud = phase != GamePhase.Menu;
            hudText.gameObject.SetActive(showDefenderHud);
            if (defenderText != null)
                defenderText.gameObject.SetActive(showDefenderHud);
            if (leftHudPanel != null)
                leftHudPanel.gameObject.SetActive(showDefenderHud);

            var mode = stressTestActive ? "Stress" : playerAttackerMode ? "PvP Hot-seat" : "PvE";
            hudText.text = $"{mode}\n{phase} | Round {Mathf.Min(round, MaxRounds)}/{MaxRounds}";

            if (defenderText != null)
            {
                var selectedInfo = hasSelectedPlacedTower && selectedPlacedTower != null
                    ? $"\n{selectedPlacedTower.Definition.kind} L{selectedPlacedTower.Level}\nD {selectedPlacedTower.CurrentDamage:0} | R {selectedPlacedTower.CurrentRange:0.0} | F {selectedPlacedTower.CurrentFireRate:0.0}"
                    : "";
                var pveWaveInfo = !playerAttackerMode && phase != GamePhase.Menu
                    ? $"\nWave {CompactWaveSummary(pendingWave)}"
                    : "";
                defenderText.text = $"Gold {gold}\nBase HP {baseHealth}\nTower {selectedTower}\nBuilt {activeTowers.Count}{selectedInfo}{pveWaveInfo}";
            }

            if (attackerText != null)
            {
                var attackBudgetText = playerAttackerMode
                    ? $"{manualAttackBudgetRemaining}/{attackBudget}"
                    : attackBudget.ToString();
                var showAttackerHud = playerAttackerMode && phase != GamePhase.Menu;
                attackerText.gameObject.SetActive(showAttackerHud);
                if (attackerHudPanel != null)
                    attackerHudPanel.gameObject.SetActive(showAttackerHud);

                attackerText.text = $"Attacker\nBudget {attackBudgetText}\nWave {pendingWave.Count}/{MaxWaveEnemies}\nActive {activeEnemies.Count}\n{DescribeWave(pendingWave)}";
            }
        }

        private static string CompactWaveSummary(IEnumerable<EnemyDefinition> wave)
        {
            var goblins = 0;
            var orcs = 0;
            var ghosts = 0;
            foreach (var enemy in wave)
            {
                switch (enemy.kind)
                {
                    case EnemyKind.Goblin:
                        goblins++;
                        break;
                    case EnemyKind.Orc:
                        orcs++;
                        break;
                    case EnemyKind.Ghost:
                        ghosts++;
                        break;
                }
            }

            var total = goblins + orcs + ghosts;
            if (total == 0)
                return "empty";

            return $"G {goblins} | O {orcs} | Gh {ghosts}";
        }

        public void RegisterEnemy(Enemy enemy)
        {
            if (!activeEnemies.Contains(enemy))
                activeEnemies.Add(enemy);
        }

        private Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3((cell.x - Width / 2f + 0.5f) * CellSize, (cell.y - Height / 2f + 0.5f) * CellSize, 0f);
        }

        private Vector2Int WorldToCell(Vector3 world)
        {
            var x = Mathf.FloorToInt(world.x / CellSize + Width / 2f);
            var y = Mathf.FloorToInt(world.y / CellSize + Height / 2f);
            return new Vector2Int(x, y);
        }

        private static bool WasPrimaryClickPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private static Vector3 GetPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector3.zero;
#else
            return Input.mousePosition;
#endif
        }

        private static bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }
    }

    public sealed class Tower : MonoBehaviour
    {
        private const int MaxLevel = 3;
        private const float DamageBonusPerLevel = 0.35f;
        private const float RangeBonusPerLevel = 0.25f;
        private const float FireRateBonusPerLevel = 0.18f;

        private float cooldown;
        private SpriteRenderer rangeRenderer;

        public TowerDefinition Definition { get; private set; }
        public bool HasCompletedBattleCycle { get; private set; }
        public int Level { get; private set; } = 1;
        public int GoldInvested { get; private set; }
        public bool CanUpgrade => Level < MaxLevel;
        public int UpgradeCost => CanUpgrade ? Mathf.RoundToInt(Definition.price * (0.7f + 0.35f * (Level - 1))) : 0;
        public int SellRefund => HasCompletedBattleCycle ? Mathf.RoundToInt(GoldInvested * 0.6f) : GoldInvested;
        public float DamageMultiplier => 1f + (Level - 1) * DamageBonusPerLevel;
        public float CurrentDamage => Definition.damage * DamageMultiplier;
        public float CurrentRange => Definition.range + (Level - 1) * RangeBonusPerLevel;
        public float CurrentFireRate => Definition.fireRate * (1f + (Level - 1) * FireRateBonusPerLevel);

        public void Initialize(TowerDefinition definition)
        {
            Definition = definition;
            HasCompletedBattleCycle = false;
            Level = 1;
            GoldInvested = definition.price;
            var body = gameObject.AddComponent<SpriteRenderer>();
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
            UpdateRangeVisual();
        }

        private static float CalculateTowerVisualScale(TowerDefinition definition, Sprite sprite)
        {
            if (definition.sprite == null || sprite == null)
                return 0.62f;

            var maxWorldSize = definition.kind switch
            {
                TowerKind.Archer => 0.92f,
                TowerKind.Mage => 0.86f,
                TowerKind.Freezer => 0.88f,
                TowerKind.Cannon => 0.96f,
                _ => 0.88f,
            };
            var largestSpriteAxis = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            return largestSpriteAxis > 0f ? maxWorldSize / largestSpriteAxis : 0.7f;
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
            if (!CanUpgrade)
                return;

            var cost = UpgradeCost;
            Level++;
            GoldInvested += cost;
            UpdateRangeVisual();
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
            waypointIndex = 0;
            slowTimer = 0f;
            slowPercent = 0f;
            gameObject.SetActive(true);
            transform.position = game.Waypoints[0];
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
                EnemyKind.Orc => 0.78f,
                EnemyKind.Ghost => 0.74f,
                _ => 0.68f,
            };

            var largestSpriteAxis = Mathf.Max(definition.sprite.bounds.size.x, definition.sprite.bounds.size.y);
            return largestSpriteAxis > 0f ? targetWorldSize / largestSpriteAxis : 0.6f;
        }
    }

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

    public sealed class ImpactEffect : MonoBehaviour
    {
        private const float Duration = 0.28f;

        private SpriteRenderer body;
        private TowerDefenseGame game;
        private float age;

        public void Play(Vector3 position, Color color, TowerDefenseGame owner)
        {
            game = owner;
            age = 0f;
            gameObject.SetActive(true);
            transform.position = position;
            transform.localScale = Vector3.one * 0.16f;

            if (body == null)
            {
                body = gameObject.AddComponent<SpriteRenderer>();
                body.sprite = SpriteFactory.Circle;
                body.sortingOrder = 6;
            }

            body.color = new Color(color.r, color.g, color.b, 0.65f);
        }

        private void Update()
        {
            age += Time.deltaTime;
            var t = Mathf.Clamp01(age / Duration);
            transform.localScale = Vector3.one * Mathf.Lerp(0.16f, 0.46f, t);
            if (body != null)
                body.color = new Color(body.color.r, body.color.g, body.color.b, 0.65f * (1f - t));

            if (age >= Duration)
                game.ReleaseImpact(this);
        }
    }

    public static class SpriteLoader
    {
        private static readonly Dictionary<string, Sprite> cache = new();

        public static Sprite Load(string spriteName)
        {
            if (cache.TryGetValue(spriteName, out var cached))
                return cached;

            var filterMode = spriteName == "MapBackground" ? FilterMode.Bilinear : FilterMode.Point;
            var resourcePath = $"TowerDefense/Sprites/{spriteName}";
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    texture.filterMode = filterMode;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 512f);
                }
            }

            if (sprite == null)
                sprite = LoadFromProjectFile(spriteName);

            if (sprite != null && sprite.texture != null)
            {
                sprite.texture.filterMode = filterMode;
                sprite.texture.wrapMode = TextureWrapMode.Clamp;
            }

            if (sprite == null)
                Debug.LogWarning($"Tower Defense sprite not found: {spriteName}");

            cache[spriteName] = sprite;
            return sprite;
        }

        private static Sprite LoadFromProjectFile(string spriteName)
        {
            var path = Path.Combine(Application.dataPath, "Resources", "TowerDefense", "Sprites", $"{spriteName}.png");
            if (!File.Exists(path))
                return null;

            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            if (!ImageConversion.LoadImage(texture, bytes))
                return null;

            texture.filterMode = FilterMode.Point;
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 512f);
        }
    }

    public static class SpriteFactory
    {
        private static Sprite square;
        private static Sprite circle;

        public static Sprite Square => square != null ? square : square = CreateSquare();
        public static Sprite Circle => circle != null ? circle : circle = CreateCircle();

        private static Sprite CreateSquare()
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color[16 * 16];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CreateCircle()
        {
            var size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = distance <= size * 0.45f ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
