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
public sealed class TowerDefenseGame : MonoBehaviour
    {
        private const int Width = 12;
        private const int Height = 8;
        private const float CellSize = 1f;
        private const int MaxRounds = 10;
        private const int MaxWaveEnemies = 50;
        private const int StressTestEnemyCount = 250;
        private const float StressTestSpawnInterval = 0.02f;
        private const int PrewarmEnemyCount = StressTestEnemyCount;
        private const int PrewarmProjectileCount = 240;
        private const float BuildSlotClickRadius = 0.48f;

        private readonly List<Vector2Int> pathCells = new()
        {
            new Vector2Int(0, 4), new Vector2Int(1, 4), new Vector2Int(2, 4), new Vector2Int(3, 4),
            new Vector2Int(4, 4), new Vector2Int(4, 5), new Vector2Int(5, 5), new Vector2Int(5, 4),
            new Vector2Int(5, 3), new Vector2Int(6, 3), new Vector2Int(7, 3), new Vector2Int(8, 3),
            new Vector2Int(8, 4), new Vector2Int(8, 5), new Vector2Int(9, 5), new Vector2Int(9, 4),
            new Vector2Int(10, 4), new Vector2Int(11, 4)
        };

        private readonly List<TowerSlot> buildSlots = new()
        {
            new TowerSlot(0, new Vector3(-4.21f, 1.49f, 0f)),
            new TowerSlot(1, new Vector3(-1.14f, 2.46f, 0f)),
            new TowerSlot(2, new Vector3(2.90f, 2.22f, 0f)),
            new TowerSlot(3, new Vector3(0.74f, 0.50f, 0f)),
            new TowerSlot(4, new Vector3(4.65f, 0.83f, 0f)),
            new TowerSlot(5, new Vector3(-2.92f, -1.04f, 0f)),
            new TowerSlot(6, new Vector3(-0.41f, -2.02f, 0f)),
            new TowerSlot(7, new Vector3(2.05f, -2.04f, 0f)),
            new TowerSlot(8, new Vector3(4.38f, -1.77f, 0f)),
        };

        private readonly List<Vector3> pathWaypointPositions = new()
        {
            new Vector3(-7.52f, 0.52f, 0f),
            new Vector3(-2.62f, 0.52f, 0f),
            new Vector3(-2.18f, 0.62f, 0f),
            new Vector3(-2.18f, 1.34f, 0f),
            new Vector3(-1.96f, 1.54f, 0f),
            new Vector3(-0.52f, 1.54f, 0f),
            new Vector3(-0.48f, -0.70f, 0f),
            new Vector3(-0.26f, -0.84f, 0f),
            new Vector3(2.18f, -0.74f, 0f),
            new Vector3(2.52f, -0.74f, 0f),
            new Vector3(2.52f, -0.35f, 0f),
            new Vector3(2.52f, 0.88f, 0f),
            new Vector3(3.2f, 0.88f, 0f),
            new Vector3(3.2f, -0.2f, 0f),
            new Vector3(3.8f, -0.2f, 0f),
            new Vector3(4.4f, -0.2f, 0f),
            new Vector3(5.0f, -0.2f, 0f),
            new Vector3(5.6f, -0.2f, 0f),
            new Vector3(6.2f, -0.2f, 0f),
            new Vector3(6.8f, -0.2f, 0f),
            new Vector3(7.33f, -0.2f, 0f),
        };

        private readonly Dictionary<int, Tower> towersBySlot = new();
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
        private Sprite coinSprite;
        private Sprite hudStatPlateSprite;
        private Sprite hudWaveShieldSprite;
        private Sprite hudActionButtonSprite;
        private Sprite hudSmallButtonSprite;
        private Sprite upgradePlateSprite;
        private Sprite upgradeDamageSprite;
        private Sprite upgradeRangeSprite;
        private Sprite upgradeSpeedSprite;
        private TowerKind selectedTower = TowerKind.Archer;
        private GamePhase phase;
        private Text hudText;
        private Text defenderText;
        private Text messageText;
        private Text attackerText;
        private RectTransform hudRoot;
        private Image leftHudPanel;
        private Image attackerHudPanel;
        private Image goldHudIcon;
        private Image goldHudPlate;
        private Image hpHudPlate;
        private Image waveHudShield;
        private Text goldHudText;
        private Text hpHudText;
        private Text waveHudText;
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
        private Image upgradePopupPanel;
        private Text upgradePopupTitle;
        private Button upgradeDamageButton;
        private Button upgradeRangeButton;
        private Button upgradeSpeedButton;
        private Image upgradeDamageLine;
        private Image upgradeRangeLine;
        private Image upgradeSpeedLine;
        private Button sellTowerButton;
        private Button clearWaveButton;
        private readonly Dictionary<TowerKind, Button> towerButtons = new();
        private readonly Dictionary<EnemyKind, Button> enemyButtons = new();
        private Tower selectedPlacedTower;
        private int selectedPlacedTowerSlotId;
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
            coinSprite = SpriteLoader.Load("Coin");
            hudStatPlateSprite = SpriteLoader.Load("HudStatPlate");
            hudWaveShieldSprite = SpriteLoader.Load("HudWaveShield");
            hudActionButtonSprite = SpriteLoader.Load("HudActionButton");
            hudSmallButtonSprite = SpriteLoader.Load("HudSmallButton");
            upgradePlateSprite = SpriteLoader.Load("UpgradePlate") ?? hudSmallButtonSprite;
            upgradeDamageSprite = SpriteLoader.Load("UpgradeDamage");
            upgradeRangeSprite = SpriteLoader.Load("UpgradeRange");
            upgradeSpeedSprite = SpriteLoader.Load("UpgradeSpeed");
            pathTileSprite = roadHorizontalSprite ?? SpriteLoader.Load("StonePathTile");
            if (fullBackgroundSprite != null && Camera.main != null)
                Camera.main.orthographicSize = 4.45f;
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

            var rewardPosition = enemy.transform.position;
            gold += reward;
            enemiesKilledThisRound++;
            goldEarnedThisRound += reward;
            totalEnemiesKilled++;
            totalGoldEarned += reward;
            SpawnGoldReward(rewardPosition, reward);
            ReleaseEnemy(enemy);
        }

        public void EnemyReachedBase(Enemy enemy)
        {
            if (phase == GamePhase.GameOver)
                return;

            baseHealth = Mathf.Max(0, baseHealth - 1);
            PlaySound(leakClip);
            enemiesLeakedThisRound++;
            totalEnemiesLeaked++;
            ReleaseEnemy(enemy);
            if (baseHealth <= 0 && !stressTestActive)
            {
                gameOverSummary = BuildGameOverSummary(false);
                SetPhase(GamePhase.GameOver);
            }
        }

        private void BuildWaypoints()
        {
            waypoints.Clear();
            if (fullBackgroundSprite != null)
            {
                waypoints.AddRange(pathWaypointPositions);
                return;
            }

            foreach (var cell in pathCells)
                waypoints.Add(CellToWorld(cell));
        }

        private void BuildBoard()
        {
            var root = new GameObject("Board").transform;
            var pathSet = new HashSet<Vector2Int>(pathCells);
            if (fullBackgroundSprite != null)
                return;

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
            renderer.color = fullBackgroundSprite != null ? Color.white : new Color(0.05f, 0.11f, 0.09f);
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
            hudRoot = canvas.GetComponent<RectTransform>();
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            goldHudPlate = CreateHudSpriteImage(canvas.transform, "Gold Plate", hudStatPlateSprite, new Vector2(18, -12), new Vector2(230f, 50f), TextAnchor.UpperLeft);
            hpHudPlate = CreateHudSpriteImage(canvas.transform, "HP Plate", hudStatPlateSprite, new Vector2(256, -12), new Vector2(230f, 50f), TextAnchor.UpperLeft);
            waveHudShield = CreateHudSpriteImage(canvas.transform, "Wave Shield", hudWaveShieldSprite, new Vector2(-36, -8), new Vector2(164f, 115f), TextAnchor.UpperRight);
            goldHudIcon = CreateHudIcon(canvas.transform, "Gold Icon", coinSprite, new Vector2(34, -20), new Vector2(26f, 26f), TextAnchor.UpperLeft);
            goldHudText = CreateText(canvas.transform, "Gold Text", new Vector2(18, -22), TextAnchor.UpperCenter, 16);
            var goldTextRect = goldHudText.GetComponent<RectTransform>();
            goldTextRect.anchorMin = new Vector2(0f, 1f);
            goldTextRect.anchorMax = new Vector2(0f, 1f);
            goldTextRect.pivot = new Vector2(0f, 1f);
            goldTextRect.sizeDelta = new Vector2(230f, 28f);
            hpHudText = CreateText(canvas.transform, "HP Text", new Vector2(256, -22), TextAnchor.UpperCenter, 16);
            var hpTextRect = hpHudText.GetComponent<RectTransform>();
            hpTextRect.anchorMin = new Vector2(0f, 1f);
            hpTextRect.anchorMax = new Vector2(0f, 1f);
            hpTextRect.pivot = new Vector2(0f, 1f);
            hpTextRect.sizeDelta = new Vector2(230f, 28f);
            waveHudText = CreateText(canvas.transform, "Wave Text", new Vector2(-118, -36), TextAnchor.UpperCenter, 16);
            var waveTextRect = waveHudText.GetComponent<RectTransform>();
            waveTextRect.anchorMin = new Vector2(1f, 1f);
            waveTextRect.anchorMax = new Vector2(1f, 1f);
            waveTextRect.pivot = new Vector2(0.5f, 1f);
            waveTextRect.sizeDelta = new Vector2(120f, 58f);
            hudText = CreateText(canvas.transform, "HUD Text", new Vector2(18, -62), TextAnchor.UpperLeft, 14);
            hudText.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 78f);
            defenderText = CreateText(canvas.transform, "Defender Text", new Vector2(18, -112), TextAnchor.UpperLeft, 14);
            defenderText.GetComponent<RectTransform>().sizeDelta = new Vector2(330f, 112f);
            messageText = CreateText(canvas.transform, "Message Text", new Vector2(0, -104), TextAnchor.UpperCenter, 15);
            messageText.GetComponent<RectTransform>().sizeDelta = new Vector2(540f, 64f);
            attackerHudPanel = CreatePanel(canvas.transform, "Attacker HUD Panel", new Vector2(-18, -118), new Vector2(224f, 58f), TextAnchor.UpperRight);
            attackerHudPanel.color = new Color(0.12f, 0.07f, 0.035f, 0.5f);
            attackerText = CreateText(canvas.transform, "Attacker Text", new Vector2(-32, -128), TextAnchor.UpperRight, 11);
            var attackerRect = attackerText.GetComponent<RectTransform>();
            attackerRect.anchorMin = new Vector2(1f, 1f);
            attackerRect.anchorMax = new Vector2(1f, 1f);
            attackerRect.pivot = new Vector2(1f, 1f);
            attackerRect.sizeDelta = new Vector2(202f, 46f);
            attackerHudPanel.gameObject.SetActive(false);
            attackerText.gameObject.SetActive(false);

            var x = 16f;
            foreach (var tower in towerDefinitions)
            {
                var captured = tower;
                var button = CreateButton(canvas.transform, $"{tower.kind} {tower.price}", new Vector2(x, 18), () => SelectTower(captured.kind));
                button.GetComponent<RectTransform>().sizeDelta = GetTowerButtonSize(captured.kind);
                towerButtons[captured.kind] = button;
                x += 152f;
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
                enemyX += 140f;
            }

            clearWaveButton = CreateButton(canvas.transform, "Clear Wave", new Vector2(enemyX, 62), ClearManualWave);
            clearWaveButton.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 36f);

            startButton = CreateButton(canvas.transform, "Start Battle", new Vector2(-166, 18), StartBattle);
            var rect = startButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);

            continueButton = CreateButton(canvas.transform, "Continue", new Vector2(-166, 18), ContinueAfterRound);
            var continueRect = continueButton.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(1, 0);
            continueRect.anchorMax = new Vector2(1, 0);
            continueButton.gameObject.SetActive(false);

            restartButton = CreateButton(canvas.transform, "Restart", new Vector2(-166, 72), RestartGame);
            var restartRect = restartButton.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(1, 0);
            restartRect.anchorMax = new Vector2(1, 0);
            restartButton.gameObject.SetActive(false);

            menuButton = CreateButton(canvas.transform, "Main Menu", new Vector2(-166, 72), ReturnToMenu);
            var menuButtonRect = menuButton.GetComponent<RectTransform>();
            menuButtonRect.anchorMin = new Vector2(1, 0);
            menuButtonRect.anchorMax = new Vector2(1, 0);
            menuButtonRect.sizeDelta = new Vector2(148f, 46f);
            menuButton.gameObject.SetActive(false);

            speedButton = CreateButton(canvas.transform, "Speed x1", new Vector2(-166, 126), ToggleBattleSpeed);
            var speedRect = speedButton.GetComponent<RectTransform>();
            speedRect.anchorMin = new Vector2(1, 0);
            speedRect.anchorMax = new Vector2(1, 0);
            speedRect.sizeDelta = new Vector2(148f, 46f);
            speedButton.gameObject.SetActive(false);

            pauseButton = CreateButton(canvas.transform, "Pause", new Vector2(-166, 180), ToggleBattlePause);
            var pauseRect = pauseButton.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(1, 0);
            pauseRect.anchorMax = new Vector2(1, 0);
            pauseRect.sizeDelta = new Vector2(148f, 46f);
            pauseButton.gameObject.SetActive(false);

            upgradeTowerButton = CreateButton(canvas.transform, "Upgrade", new Vector2(-328, 72), UpgradeSelectedTower);
            var upgradeRect = upgradeTowerButton.GetComponent<RectTransform>();
            upgradeRect.anchorMin = new Vector2(1, 0);
            upgradeRect.anchorMax = new Vector2(1, 0);
            upgradeRect.sizeDelta = new Vector2(148f, 46f);
            upgradeTowerButton.gameObject.SetActive(false);

            upgradePopupPanel = CreateHudSpriteImage(canvas.transform, "Upgrade Popup", hudStatPlateSprite, Vector2.zero, new Vector2(284f, 82f), TextAnchor.MiddleCenter);
            upgradePopupTitle = CreateText(canvas.transform, "Upgrade Popup Title", Vector2.zero, TextAnchor.MiddleCenter, 12);
            var popupTitleRect = upgradePopupTitle.GetComponent<RectTransform>();
            popupTitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupTitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupTitleRect.pivot = new Vector2(0.5f, 0.5f);
            popupTitleRect.sizeDelta = new Vector2(220f, 18f);
            upgradeDamageLine = CreateUpgradeConnector(canvas.transform, "Damage Connector");
            upgradeRangeLine = CreateUpgradeConnector(canvas.transform, "Range Connector");
            upgradeSpeedLine = CreateUpgradeConnector(canvas.transform, "Speed Connector");
            upgradeDamageButton = CreateUpgradeButton(canvas.transform, "Damage Upgrade", upgradeDamageSprite, Vector2.zero, () => UpgradeSelectedTowerDamage());
            upgradeRangeButton = CreateUpgradeButton(canvas.transform, "Range Upgrade", upgradeRangeSprite, Vector2.zero, () => UpgradeSelectedTowerRange());
            upgradeSpeedButton = CreateUpgradeButton(canvas.transform, "Speed Upgrade", upgradeSpeedSprite, Vector2.zero, () => UpgradeSelectedTowerSpeed());
            upgradePopupPanel.gameObject.SetActive(false);
            upgradePopupTitle.gameObject.SetActive(false);
            upgradeDamageLine.gameObject.SetActive(false);
            upgradeRangeLine.gameObject.SetActive(false);
            upgradeSpeedLine.gameObject.SetActive(false);
            upgradeDamageButton.gameObject.SetActive(false);
            upgradeRangeButton.gameObject.SetActive(false);
            upgradeSpeedButton.gameObject.SetActive(false);

            sellTowerButton = CreateButton(canvas.transform, "Sell", new Vector2(-328, 72), SellSelectedTower);
            var sellRect = sellTowerButton.GetComponent<RectTransform>();
            sellRect.anchorMin = new Vector2(1, 0);
            sellRect.anchorMax = new Vector2(1, 0);
            sellRect.sizeDelta = new Vector2(148f, 46f);
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
                var baseColor = hudActionButtonSprite != null
                    ? pair.Key == selectedTower ? new Color(1f, 0.86f, 0.62f, 1f) : Color.white
                    : pair.Key == selectedTower ? new Color(0.34f, 0.22f, 0.12f, 0.98f) : new Color(0.19f, 0.11f, 0.06f, 0.96f);
                image.color = baseColor;
                ApplyButtonColors(pair.Value, baseColor);
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
            text.color = new Color(1f, 0.92f, 0.72f);
            var shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.06f, 0.03f, 0.01f, 0.78f);
            shadow.effectDistance = new Vector2(1.2f, -1.2f);
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
            image.color = new Color(0.12f, 0.07f, 0.035f, 0.76f);
            image.raycastTarget = false;
            var outline = panelObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.72f, 0.52f, 0.25f, 0.75f);
            outline.effectDistance = new Vector2(2f, -2f);

            var rect = image.GetComponent<RectTransform>();
            rect.anchorMin = anchor == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = anchor == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return image;
        }

        private static Image CreateHudIcon(Transform parent, string name, Sprite sprite, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor)
        {
            var image = CreateHudSpriteImage(parent, name, sprite, anchoredPosition, size, anchor);
            image.color = sprite != null ? Color.white : new Color(1f, 0.78f, 0.26f, 1f);
            return image;
        }

        private static Image CreateHudSpriteImage(Transform parent, string name, Sprite sprite, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor)
        {
            var iconObject = new GameObject(name);
            iconObject.transform.SetParent(parent, false);
            var image = iconObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = false;

            var rect = image.GetComponent<RectTransform>();
            rect.anchorMin = GetAnchorVector(anchor);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = GetPivotVector(anchor);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return image;
        }

        private Button CreateUpgradeButton(Transform parent, string name, Sprite icon, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.sprite = icon ?? upgradePlateSprite ?? hudActionButtonSprite ?? hudSmallButtonSprite;
            image.color = Color.white;
            image.preserveAspect = true;
            var button = buttonObject.AddComponent<Button>();
            ApplyButtonColors(button, Color.white);
            button.onClick.AddListener(() =>
            {
                PlaySound(buttonClickClip);
                action?.Invoke();
            });

            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(90f, 90f);

            var text = CreateText(buttonObject.transform, "Cost", new Vector2(0f, 9f), TextAnchor.LowerCenter, 11);
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            text.color = new Color(1f, 0.88f, 0.36f, 1f);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.anchoredPosition = new Vector2(0f, 10f);
            textRect.sizeDelta = new Vector2(0f, 18f);
            return button;
        }

        private static Image CreateUpgradeConnector(Transform parent, string name)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            var image = lineObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.035f, 0.005f, 0.86f);
            image.raycastTarget = false;

            var rect = image.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(10f, 4f);
            return image;
        }

        private static Vector2 GetAnchorVector(TextAnchor anchor)
        {
            return anchor switch
            {
                TextAnchor.UpperCenter => new Vector2(0.5f, 1f),
                TextAnchor.UpperRight => new Vector2(1f, 1f),
                TextAnchor.MiddleLeft => new Vector2(0f, 0.5f),
                TextAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
                TextAnchor.MiddleRight => new Vector2(1f, 0.5f),
                TextAnchor.LowerLeft => new Vector2(0f, 0f),
                TextAnchor.LowerCenter => new Vector2(0.5f, 0f),
                TextAnchor.LowerRight => new Vector2(1f, 0f),
                _ => new Vector2(0f, 1f),
            };
        }

        private static Vector2 GetPivotVector(TextAnchor anchor)
        {
            return anchor switch
            {
                TextAnchor.UpperCenter => new Vector2(0.5f, 1f),
                TextAnchor.UpperRight => new Vector2(1f, 1f),
                TextAnchor.MiddleLeft => new Vector2(0f, 0.5f),
                TextAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
                TextAnchor.MiddleRight => new Vector2(1f, 0.5f),
                TextAnchor.LowerLeft => new Vector2(0f, 0f),
                TextAnchor.LowerCenter => new Vector2(0.5f, 0f),
                TextAnchor.LowerRight => new Vector2(1f, 0f),
                _ => new Vector2(0f, 1f),
            };
        }

        private Button CreateButton(Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.sprite = hudActionButtonSprite;
            image.preserveAspect = false;
            var baseColor = hudActionButtonSprite != null ? Color.white : new Color(0.19f, 0.11f, 0.06f, 0.96f);
            image.color = baseColor;
            if (hudActionButtonSprite == null)
            {
                var outline = buttonObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.74f, 0.55f, 0.3f, 0.95f);
                outline.effectDistance = new Vector2(2f, -2f);
            }
            var button = buttonObject.AddComponent<Button>();
            ApplyButtonColors(button, baseColor);
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
            rect.sizeDelta = new Vector2(138f, 46f);

            var text = CreateText(buttonObject.transform, "Label", Vector2.zero, TextAnchor.MiddleCenter, 15);
            text.text = label;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            return button;
        }

        private static Vector2 GetTowerButtonSize(TowerKind kind)
        {
            return kind switch
            {
                TowerKind.Archer => new Vector2(152f, 46f),
                TowerKind.Freezer => new Vector2(162f, 46f),
                TowerKind.Cannon => new Vector2(160f, 46f),
                _ => new Vector2(144f, 46f),
            };
        }

        private static void ApplyButtonColors(Button button, Color baseColor)
        {
            var colors = button.colors;
            var opaqueBase = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
            colors.normalColor = opaqueBase;
            colors.highlightedColor = Lighten(opaqueBase, 0.08f);
            colors.pressedColor = Darken(opaqueBase, 0.08f);
            colors.selectedColor = Lighten(opaqueBase, 0.04f);
            colors.disabledColor = new Color(0.55f, 0.48f, 0.4f, 0.9f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.04f;
            button.colors = colors;
        }

        private static Color Lighten(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                color.a);
        }

        private static Color Darken(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r - amount),
                Mathf.Clamp01(color.g - amount),
                Mathf.Clamp01(color.b - amount),
                color.a);
        }

        private void SpawnGoldReward(Vector3 position, int reward)
        {
            if (reward <= 0)
                return;

            var rewardObject = new GameObject("Gold Reward");
            rewardObject.transform.SetParent(effectsRoot);
            rewardObject.AddComponent<GoldRewardEffect>().Play(position, reward, coinSprite);
        }

        private void HandlePlacementInput()
        {
            if (!WasPrimaryClickPressed())
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var world = Camera.main.ScreenToWorldPoint(GetPointerPosition());
            if (!TryGetBuildSlot(world, out var slot))
            {
                ClearPlacedTowerSelection();
                messageText.text = "Place towers only on marked stone slots.";
                return;
            }

            if (towersBySlot.TryGetValue(slot.Id, out var existingTower))
            {
                SelectPlacedTower(slot.Id, existingTower);
                return;
            }

            if (hasSelectedPlacedTower)
            {
                ClearPlacedTowerSelection();
                messageText.text = "Tower selection cleared.";
                return;
            }

            ClearPlacedTowerSelection();
            var definition = towerDefinitions.Find(t => t.kind == selectedTower);
            if (definition == null)
                return;

            if (gold < definition.price)
            {
                messageText.text = $"Not enough gold: need {definition.price}, have {gold}.";
                return;
            }

            gold -= definition.price;
            SelectPlacedTower(slot.Id, PlaceTower(slot, definition));
            totalTowersBuilt++;
            PlaySound(buildClip);
        }

        private Tower PlaceTower(TowerSlot slot, TowerDefinition definition)
        {
            var towerObject = new GameObject(definition.kind.ToString());
            towerObject.transform.SetParent(towersRoot);
            towerObject.transform.position = slot.Position;
            var tower = towerObject.AddComponent<Tower>();
            tower.Initialize(definition);
            activeTowers.Add(tower);
            towersBySlot[slot.Id] = tower;
            return tower;
        }

        private bool TryGetBuildSlot(Vector3 world, out TowerSlot slot)
        {
            slot = default;
            var bestDistance = float.MaxValue;
            foreach (var candidate in buildSlots)
            {
                var distance = Vector2.Distance(new Vector2(world.x, world.y), new Vector2(candidate.Position.x, candidate.Position.y));
                if (distance > BuildSlotClickRadius || distance >= bestDistance)
                    continue;

                bestDistance = distance;
                slot = candidate;
            }

            return bestDistance < float.MaxValue;
        }

        private void SellTower(int slotId, Tower tower)
        {
            var refund = tower.SellRefund;
            gold += refund;
            activeTowers.Remove(tower);
            towersBySlot.Remove(slotId);
            totalTowersSold++;
            Destroy(tower.gameObject);
            messageText.text = $"Sold {tower.Definition.kind} for {refund} gold.";
            PlaySound(sellClip);
            UpdateTowerActionButtons();
        }

        private void SelectPlacedTower(int slotId, Tower tower)
        {
            if (phase != GamePhase.Preparation || tower == null)
                return;

            selectedPlacedTower = tower;
            selectedPlacedTowerSlotId = slotId;
            hasSelectedPlacedTower = true;
            UpdateTowerRangeVisibility();
            messageText.text = BuildSelectedTowerMessage(tower);
            UpdateTowerActionButtons();
        }

        private void ClearPlacedTowerSelection()
        {
            selectedPlacedTower = null;
            hasSelectedPlacedTower = false;
            UpdateTowerRangeVisibility();
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

        private void UpgradeSelectedTowerDamage()
        {
            UpgradeSelectedTowerStat(
                selectedPlacedTower != null && selectedPlacedTower.CanUpgradeDamage,
                selectedPlacedTower != null ? selectedPlacedTower.DamageUpgradeCost : 0,
                tower => tower.UpgradeDamage(),
                "damage");
        }

        private void UpgradeSelectedTowerRange()
        {
            UpgradeSelectedTowerStat(
                selectedPlacedTower != null && selectedPlacedTower.CanUpgradeRange,
                selectedPlacedTower != null ? selectedPlacedTower.RangeUpgradeCost : 0,
                tower => tower.UpgradeRange(),
                "range");
        }

        private void UpgradeSelectedTowerSpeed()
        {
            UpgradeSelectedTowerStat(
                selectedPlacedTower != null && selectedPlacedTower.CanUpgradeSpeed,
                selectedPlacedTower != null ? selectedPlacedTower.SpeedUpgradeCost : 0,
                tower => tower.UpgradeSpeed(),
                "attack speed");
        }

        private void UpgradeSelectedTowerStat(bool canUpgrade, int cost, Action<Tower> upgradeAction, string label)
        {
            if (phase != GamePhase.Preparation || !hasSelectedPlacedTower || selectedPlacedTower == null)
                return;

            if (!canUpgrade)
            {
                messageText.text = $"{selectedPlacedTower.Definition.kind} {label} is already maxed.";
                UpdateTowerActionButtons();
                return;
            }

            if (gold < cost)
            {
                messageText.text = $"Need {cost} gold to upgrade {label}.";
                UpdateTowerActionButtons();
                return;
            }

            gold -= cost;
            upgradeAction(selectedPlacedTower);
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
            var slotId = selectedPlacedTowerSlotId;
            ClearPlacedTowerSelection();
            SellTower(slotId, tower);
        }

        private static string BuildSelectedTowerMessage(Tower tower)
        {
            return "";
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
            baseHealth = 20;
            attackBudget = StressTestEnemyCount * 20;
            enemiesKilledThisRound = 0;
            enemiesLeakedThisRound = 0;
            goldEarnedThisRound = 0;

            var buildableSlots = GetBuildableSlots();
            for (var i = 0; i < 20 && i < buildableSlots.Count; i++)
            {
                var definition = towerDefinitions[i % towerDefinitions.Count];
                PlaceTower(buildableSlots[i], definition);
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
            UpdateMainMenuButtonVisibility();
        }

        private void ToggleBattlePause()
        {
            if (phase != GamePhase.Battle)
                return;

            battlePaused = !battlePaused;
            Time.timeScale = battlePaused ? 0f : fastBattleSpeed ? 2f : 1f;
            messageText.text = battlePaused ? "Battle paused." : "Battle in progress.";
            UpdateBattlePauseButton();
            UpdateMainMenuButtonVisibility();
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
            towersBySlot.Clear();
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
            var baseBudget = 150 + (round - 1) * 48;
            var towerPressure = Mathf.Min(130, activeTowers.Count * 12 + totalTowerUpgrades * 9);
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
            var stressEnemies = new List<EnemyDefinition>();
            AddStressEnemy(stressEnemies, EnemyKind.Goblin);
            AddStressEnemy(stressEnemies, EnemyKind.Ghost);
            AddStressEnemy(stressEnemies, EnemyKind.Orc);

            if (stressEnemies.Count == 0)
            {
                var fallback = FindCheapestEnemy();
                if (fallback != null)
                    stressEnemies.Add(fallback);
            }

            if (stressEnemies.Count == 0)
                return result;

            for (var i = 0; i < StressTestEnemyCount; i++)
                result.Enqueue(stressEnemies[i % stressEnemies.Count]);

            return result;
        }

        private void AddStressEnemy(List<EnemyDefinition> stressEnemies, EnemyKind kind)
        {
            var enemy = enemyDefinitions.Find(e => e.kind == kind);
            if (enemy != null && enemy.attackCost > 0)
                stressEnemies.Add(enemy);
        }

        private List<TowerSlot> GetBuildableSlots()
        {
            var result = new List<TowerSlot>();
            foreach (var slot in buildSlots)
            {
                if (!towersBySlot.ContainsKey(slot.Id))
                    result.Add(slot);
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

            if (stressTestActive)
            {
                spawnTimer -= Time.deltaTime;
                while (spawnTimer <= 0f && pendingWave.Count > 0)
                {
                    var stressEnemy = GetEnemy();
                    stressEnemy.Spawn(pendingWave.Dequeue(), this);
                    spawnTimer += StressTestSpawnInterval;
                }

                enemiesToSpawn = pendingWave.Count;
                return;
            }

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
            leakClip = LoadAudioClip("BaseDamage") ?? CreateToneClip("TD Leak", 120f, 0.18f, 0.2f);
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
            var roundBonus = 45 + round * 7;
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
                UpdateMainMenuButtonVisibility();

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
                    if (pendingWave.Count == 0)
                        PrepareNextWave();

                    if (!hasSelectedPlacedTower)
                        messageText.text = playerAttackerMode
                            ? "Defender places towers. Attacker adds enemies, then start battle."
                            : "Click a marked stone slot to place the selected tower.";
                }
                else
                {
                    ClearPlacedTowerSelection();
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
                upgradeTowerButton.gameObject.SetActive(false);
            }

            if (upgradePopupPanel != null)
                upgradePopupPanel.gameObject.SetActive(false);
            if (upgradePopupTitle != null)
            {
                upgradePopupTitle.gameObject.SetActive(false);
                upgradePopupTitle.text = "";
            }
            SetUpgradeConnectorVisible(upgradeDamageLine, show);
            SetUpgradeConnectorVisible(upgradeRangeLine, show);
            SetUpgradeConnectorVisible(upgradeSpeedLine, show);
            if (show)
                PositionUpgradePopup();
            UpdateUpgradeButton(upgradeDamageButton, show, selectedPlacedTower != null && selectedPlacedTower.CanUpgradeDamage, selectedPlacedTower != null ? selectedPlacedTower.DamageUpgradeCost : 0);
            UpdateUpgradeButton(upgradeRangeButton, show, selectedPlacedTower != null && selectedPlacedTower.CanUpgradeRange, selectedPlacedTower != null ? selectedPlacedTower.RangeUpgradeCost : 0);
            UpdateUpgradeButton(upgradeSpeedButton, show, selectedPlacedTower != null && selectedPlacedTower.CanUpgradeSpeed, selectedPlacedTower != null ? selectedPlacedTower.SpeedUpgradeCost : 0);

            if (sellTowerButton != null)
            {
                sellTowerButton.gameObject.SetActive(show);
                sellTowerButton.interactable = show;
                SetButtonLabel(sellTowerButton, show ? $"Sell {selectedPlacedTower.SellRefund}" : "Sell");
            }
        }

        private static void SetUpgradeConnectorVisible(Image line, bool show)
        {
            if (line != null)
                line.gameObject.SetActive(show);
        }

        private void PositionUpgradePopup()
        {
            if (hudRoot == null || selectedPlacedTower == null || Camera.main == null)
                return;

            var screenPoint = Camera.main.WorldToScreenPoint(selectedPlacedTower.PopupAnchorPosition);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRoot, screenPoint, null, out var towerPoint))
                return;

            var left = ClampToHud(towerPoint + new Vector2(-112f, 82f), 58f);
            var right = ClampToHud(towerPoint + new Vector2(112f, 82f), 58f);
            var bottom = ClampToHud(towerPoint + new Vector2(0f, -116f), 58f);

            PositionRect(upgradeDamageButton, left);
            PositionRect(upgradeRangeButton, right);
            PositionRect(upgradeSpeedButton, bottom);
            PositionConnector(upgradeDamageLine, towerPoint, left);
            PositionConnector(upgradeRangeLine, towerPoint, right);
            PositionConnector(upgradeSpeedLine, towerPoint, bottom);
        }

        private Vector2 ClampToHud(Vector2 point, float padding)
        {
            var rect = hudRoot != null ? hudRoot.rect : new Rect(-400f, -300f, 800f, 600f);
            return new Vector2(
                Mathf.Clamp(point.x, rect.xMin + padding, rect.xMax - padding),
                Mathf.Clamp(point.y, rect.yMin + padding, rect.yMax - padding));
        }

        private static void PositionRect(Button button, Vector2 anchoredPosition)
        {
            if (button == null)
                return;

            button.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
        }

        private static void PositionConnector(Image line, Vector2 from, Vector2 to)
        {
            if (line == null)
                return;

            var delta = to - from;
            var rect = line.GetComponent<RectTransform>();
            rect.anchoredPosition = from + delta * 0.5f;
            rect.sizeDelta = new Vector2(Mathf.Max(8f, delta.magnitude - 34f), 4f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void UpdateUpgradeButton(Button button, bool show, bool canUpgrade, int cost)
        {
            if (button == null)
                return;

            button.gameObject.SetActive(show);
            button.interactable = show && canUpgrade && gold >= cost;
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = canUpgrade ? $"{cost} Gold" : "Max";
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

        private void UpdateMainMenuButtonVisibility()
        {
            if (menuButton == null)
                return;

            menuButton.gameObject.SetActive(phase == GamePhase.Preparation || phase == GamePhase.RoundEnd || phase == GamePhase.GameOver || (phase == GamePhase.Battle && battlePaused));
        }

        private static void SetButtonLabel(Button button, string label)
        {
            var text = button != null ? button.GetComponentInChildren<Text>() : null;
            if (text != null)
                text.text = label;
        }

        private void UpdateTowerRangeVisibility()
        {
            foreach (var tower in activeTowers)
                tower.SetRangeVisible(phase == GamePhase.Preparation && hasSelectedPlacedTower && tower == selectedPlacedTower);
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
            hudText.gameObject.SetActive(false);
            if (defenderText != null)
                defenderText.gameObject.SetActive(showDefenderHud && hasSelectedPlacedTower && selectedPlacedTower != null);
            if (leftHudPanel != null)
                leftHudPanel.gameObject.SetActive(false);
            if (goldHudIcon != null)
                goldHudIcon.gameObject.SetActive(showDefenderHud);
            if (goldHudPlate != null)
                goldHudPlate.gameObject.SetActive(showDefenderHud);
            if (hpHudPlate != null)
                hpHudPlate.gameObject.SetActive(showDefenderHud);
            if (waveHudShield != null)
                waveHudShield.gameObject.SetActive(showDefenderHud);
            if (goldHudText != null)
                goldHudText.gameObject.SetActive(showDefenderHud);
            if (hpHudText != null)
                hpHudText.gameObject.SetActive(showDefenderHud);
            if (waveHudText != null)
                waveHudText.gameObject.SetActive(showDefenderHud);

            var mode = stressTestActive ? "Stress" : playerAttackerMode ? "PvP Hot-seat" : "PvE";
            hudText.text = $"{mode} | {phase}";
            if (goldHudText != null)
                goldHudText.text = $"Gold {gold}";
            if (hpHudText != null)
                hpHudText.text = $"HP {baseHealth}";
            if (waveHudText != null)
                waveHudText.text = $"Wave\n{Mathf.Min(round, MaxRounds)}/{MaxRounds}";

            if (defenderText != null)
            {
                var selectedInfo = hasSelectedPlacedTower && selectedPlacedTower != null
                    ? $"{selectedPlacedTower.Definition.kind} L{selectedPlacedTower.Level}\nD {selectedPlacedTower.CurrentDamage:0} | R {selectedPlacedTower.CurrentRange:0.0} | F {selectedPlacedTower.CurrentFireRate:0.0}"
                    : "";
                defenderText.text = selectedInfo;
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
                PositionAttackerHud(phase == GamePhase.Battle);

                var activeText = phase == GamePhase.Battle ? $" | Active {activeEnemies.Count}" : "";
                attackerText.text = $"Atk {attackBudgetText}\nWave {pendingWave.Count}/{MaxWaveEnemies}{activeText}\n{CompactWaveSummary(pendingWave)}";
            }
        }

        private void PositionAttackerHud(bool battleLayout)
        {
            var lowerLeftLayout = battleLayout || phase == GamePhase.Preparation;
            if (attackerHudPanel != null)
            {
                var panelRect = attackerHudPanel.GetComponent<RectTransform>();
                panelRect.anchorMin = lowerLeftLayout ? new Vector2(0f, 0f) : new Vector2(1f, 1f);
                panelRect.anchorMax = panelRect.anchorMin;
                panelRect.pivot = lowerLeftLayout ? new Vector2(0f, 0f) : new Vector2(1f, 1f);
                panelRect.anchoredPosition = battleLayout ? new Vector2(18f, 18f) : phase == GamePhase.Preparation ? new Vector2(18f, 230f) : new Vector2(-18f, -118f);
            }

            if (attackerText != null)
            {
                var textRect = attackerText.GetComponent<RectTransform>();
                textRect.anchorMin = lowerLeftLayout ? new Vector2(0f, 0f) : new Vector2(1f, 1f);
                textRect.anchorMax = textRect.anchorMin;
                textRect.pivot = lowerLeftLayout ? new Vector2(0f, 0f) : new Vector2(1f, 1f);
                textRect.anchoredPosition = battleLayout ? new Vector2(34f, 28f) : phase == GamePhase.Preparation ? new Vector2(32f, 238f) : new Vector2(-32f, -128f);
                attackerText.alignment = lowerLeftLayout ? TextAnchor.LowerLeft : TextAnchor.UpperRight;
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

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            for (var i = 0; i < pathWaypointPositions.Count; i++)
            {
                Gizmos.DrawSphere(pathWaypointPositions[i], 0.08f);
                if (i < pathWaypointPositions.Count - 1)
                    Gizmos.DrawLine(pathWaypointPositions[i], pathWaypointPositions[i + 1]);
            }
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
}

