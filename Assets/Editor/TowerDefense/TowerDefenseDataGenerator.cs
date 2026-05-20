using System.IO;
using TowerDefense;
using UnityEditor;
using UnityEngine;

public static class TowerDefenseDataGenerator
{
    private const string TowerFolder = "Assets/Resources/TowerDefense/Towers";
    private const string EnemyFolder = "Assets/Resources/TowerDefense/Enemies";

    [MenuItem("Tower Defense/Create Default Data Assets")]
    public static void CreateDefaultDataAssets()
    {
        Directory.CreateDirectory(TowerFolder);
        Directory.CreateDirectory(EnemyFolder);
        AssetDatabase.Refresh();

        CreateOrUpdateTower("Archer", new TowerDefinition { kind = TowerKind.Archer, price = 100, range = 2.9f, fireRate = 1.45f, damage = 20f, color = new Color(0.36f, 0.76f, 0.42f) });
        CreateOrUpdateTower("Mage", new TowerDefinition { kind = TowerKind.Mage, price = 150, range = 1.9f, fireRate = 0.7f, damage = 18f, splashRadius = 0.85f, color = new Color(0.55f, 0.4f, 0.95f) });
        CreateOrUpdateTower("Freezer", new TowerDefinition { kind = TowerKind.Freezer, price = 120, range = 2.4f, fireRate = 0.95f, damage = 4f, slowPercent = 0.45f, slowDuration = 1.8f, color = new Color(0.32f, 0.78f, 0.94f) });
        CreateOrUpdateTower("Cannon", new TowerDefinition { kind = TowerKind.Cannon, price = 200, range = 3.25f, fireRate = 0.45f, damage = 55f, color = new Color(0.93f, 0.56f, 0.26f) });

        CreateOrUpdateEnemy("Goblin", new EnemyDefinition { kind = EnemyKind.Goblin, maxHealth = 45f, speed = 1.65f, attackCost = 10, goldReward = 8, color = new Color(0.44f, 0.86f, 0.34f) });
        CreateOrUpdateEnemy("Orc", new EnemyDefinition { kind = EnemyKind.Orc, maxHealth = 140f, speed = 0.72f, attackCost = 25, goldReward = 18, color = new Color(0.74f, 0.47f, 0.31f) });
        CreateOrUpdateEnemy("Ghost", new EnemyDefinition { kind = EnemyKind.Ghost, maxHealth = 80f, speed = 1.05f, attackCost = 20, goldReward = 14, color = new Color(0.78f, 0.86f, 1f), ignoresSlow = true });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Tower Defense default data assets are ready.");
    }

    private static void CreateOrUpdateTower(string fileName, TowerDefinition definition)
    {
        var path = $"{TowerFolder}/{fileName}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TowerData>(path);
        if (asset == null && AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<TowerData>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.definition = definition;
        EditorUtility.SetDirty(asset);
    }

    private static void CreateOrUpdateEnemy(string fileName, EnemyDefinition definition)
    {
        var path = $"{EnemyFolder}/{fileName}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (asset == null && AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.definition = definition;
        EditorUtility.SetDirty(asset);
    }
}
