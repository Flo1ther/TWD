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
public static class SpriteLoader
    {
        private static readonly Dictionary<string, Sprite> cache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCache()
        {
            cache.Clear();
        }

        public static Sprite Load(string spriteName)
        {
            if (cache.TryGetValue(spriteName, out var cached))
                return cached;

            var filterMode = FilterMode.Bilinear;
            var resourcePath = $"TowerDefense/Sprites/{spriteName}";
            Sprite sprite = null;
            if (ShouldUseFullTextureSprite(spriteName))
            {
                var fullTexture = Resources.Load<Texture2D>(resourcePath);
                if (fullTexture != null)
                    sprite = CreateFullTextureSprite(fullTexture);
            }

            sprite ??= Resources.Load<Sprite>(resourcePath);
            if (ShouldCenterSpritePivot(spriteName))
                sprite = CreateCenteredSprite(sprite);

            if (sprite == null)
            {
                var sprites = Resources.LoadAll<Sprite>(resourcePath);
                if (sprites != null && sprites.Length > 0)
                {
                    sprite = SelectBestSprite(sprites);
                    if (ShouldCenterSpritePivot(spriteName))
                        sprite = CreateCenteredSprite(sprite);
                }
            }

            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    texture.filterMode = filterMode;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    sprite = CreateFullTextureSprite(texture);
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

        private static bool ShouldUseFullTextureSprite(string spriteName)
        {
            return spriteName == "MapBackground"
                || spriteName == "MageTower"
                || spriteName == "FreezerTower"
                || spriteName == "CannonTower"
                || spriteName == "ArcherTower"
                || spriteName == "ArcherTowerAlt";
        }

        private static bool ShouldCenterSpritePivot(string spriteName)
        {
            return spriteName == "Goblin"
                || spriteName == "Orc"
                || spriteName == "Ghost";
        }

        private static Sprite CreateFullTextureSprite(Texture2D texture)
        {
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 512f);
        }

        private static Sprite SelectBestSprite(Sprite[] sprites)
        {
            var best = sprites[0];
            var bestArea = best.rect.width * best.rect.height;
            for (var i = 1; i < sprites.Length; i++)
            {
                var area = sprites[i].rect.width * sprites[i].rect.height;
                if (area <= bestArea)
                    continue;

                best = sprites[i];
                bestArea = area;
            }

            return best;
        }

        private static Sprite CreateCenteredSprite(Sprite source)
        {
            if (source == null || source.texture == null)
                return source;

            return Sprite.Create(
                source.texture,
                source.rect,
                new Vector2(0.5f, 0.5f),
                source.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                source.border);
        }

        private static Sprite LoadFromProjectFile(string spriteName)
        {
            var path = Path.Combine(Application.dataPath, "Resources", "TowerDefense", "Sprites", $"{spriteName}.png");
            if (!File.Exists(path))
                return null;

            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            if (!ImageConversion.LoadImage(texture, bytes))
                return null;

            texture.filterMode = FilterMode.Bilinear;
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 512f);
        }
    }
}

