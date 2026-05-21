using TowerDefense;
using UnityEditor;
using UnityEngine;

public static class TowerDefenseArtImporter
{
    private const string SpriteFolder = "Assets/Resources/TowerDefense/Sprites";

    [MenuItem("Tower Defense/Import Generated Art")]
    public static void ImportGeneratedArt()
    {
        ConfigureSprite("ArcherTower");
        ConfigureSprite("ArcherTowerAlt");
        ConfigureSprite("MageTower");
        ConfigureSprite("FreezerTower");
        ConfigureSprite("CannonTower");
        ConfigureSprite("Goblin");
        ConfigureSprite("Orc");
        ConfigureSprite("Ghost");
        ConfigureSprite("ArrowProjectile");
        ConfigureSprite("MagicOrbProjectile");
        ConfigureSprite("IceShardProjectile");
        ConfigureSprite("CannonballProjectile");
        ConfigureSprite("GrassTile");
        ConfigureSprite("BuildableGrassTile");
        ConfigureSprite("StonePathTile");
        ConfigureSprite("RoadHorizontal");
        ConfigureSprite("RoadVertical");
        ConfigureSprite("RoadCorner");
        ConfigureSprite("RoadCornerNE");
        ConfigureSprite("RoadCornerES");
        ConfigureSprite("RoadCornerWN");
        ConfigureSprite("RoadCornerTopRight");
        ConfigureSprite("RoadCornerRightBottom");
        ConfigureSprite("RoadCornerBottomLeft");
        ConfigureSprite("RoadCornerLeftTop");
        ConfigureSprite("RoadTJunction");
        ConfigureSprite("ForestBorderTile");
        ConfigureSprite("DirtTile");
        ConfigureSprite("ForestTile");
        ConfigureSprite("EntryMarker");
        ConfigureSprite("BaseMarker");
        ConfigureSprite("EntryPortal");
        ConfigureSprite("DefenderBase");
        ConfigureSprite("Coin");
        ConfigureSprite("HudStatPlate");
        ConfigureSprite("HudWaveShield");
        ConfigureSprite("HudActionButton");
        ConfigureSprite("HudSmallButton");
        ConfigureSprite("UpgradePlate");
        ConfigureSprite("UpgradeDamage");
        ConfigureSprite("UpgradeRange");
        ConfigureSprite("UpgradeSpeed");

        TowerDefenseDataGenerator.CreateDefaultDataAssets();
        AssignTower("Archer", "ArcherTowerAlt", "ArrowProjectile");
        AssignTower("Mage", "MageTower", "MagicOrbProjectile");
        AssignTower("Freezer", "FreezerTower", "IceShardProjectile");
        AssignTower("Cannon", "CannonTower", "CannonballProjectile");
        AssignEnemy("Goblin", "Goblin");
        AssignEnemy("Orc", "Orc");
        AssignEnemy("Ghost", "Ghost");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Tower Defense generated art imported and assigned.");
    }

    private static void ConfigureSprite(string name)
    {
        var path = $"{SpriteFolder}/{name}.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 512f;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{name}.png");
    }

    private static void AssignTower(string assetName, string towerSpriteName, string projectileSpriteName)
    {
        var data = AssetDatabase.LoadAssetAtPath<TowerData>($"Assets/Resources/TowerDefense/Towers/{assetName}.asset");
        if (data == null)
            return;

        data.definition.sprite = LoadSprite(towerSpriteName);
        data.definition.projectileSprite = LoadSprite(projectileSpriteName);
        EditorUtility.SetDirty(data);
    }

    private static void AssignEnemy(string assetName, string spriteName)
    {
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/Resources/TowerDefense/Enemies/{assetName}.asset");
        if (data == null)
            return;

        data.definition.sprite = LoadSprite(spriteName);
        EditorUtility.SetDirty(data);
    }
}
