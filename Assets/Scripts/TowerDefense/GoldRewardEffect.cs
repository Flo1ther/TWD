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
public sealed class GoldRewardEffect : MonoBehaviour
    {
        private const float Duration = 0.85f;

        private SpriteRenderer coinRenderer;
        private TextMesh rewardText;
        private Vector3 startPosition;
        private Vector3 drift;
        private float age;

        public void Play(Vector3 position, int reward, Sprite coinSprite)
        {
            age = 0f;
            startPosition = position + new Vector3(0f, 0.18f, -0.25f);
            drift = new Vector3(UnityEngine.Random.Range(-0.18f, 0.18f), 0.92f, 0f);
            transform.position = startPosition;
            transform.localScale = Vector3.one;

            var coinObject = new GameObject("Coin");
            coinObject.transform.SetParent(transform, false);
            coinRenderer = coinObject.AddComponent<SpriteRenderer>();
            coinRenderer.sprite = coinSprite != null ? coinSprite : SpriteFactory.Circle;
            coinRenderer.color = coinSprite != null ? Color.white : new Color(1f, 0.78f, 0.16f, 1f);
            coinRenderer.sortingOrder = 9;
            coinObject.transform.localScale = Vector3.one * CalculateCoinScale(coinSprite);

            var textObject = new GameObject("Reward Text");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = new Vector3(0.22f, 0.02f, -0.02f);
            rewardText = textObject.AddComponent<TextMesh>();
            rewardText.text = $"+{reward}";
            rewardText.fontSize = 34;
            rewardText.characterSize = 0.045f;
            rewardText.anchor = TextAnchor.MiddleLeft;
            rewardText.alignment = TextAlignment.Left;
            rewardText.color = new Color(1f, 0.87f, 0.24f, 1f);
            var textRenderer = textObject.GetComponent<MeshRenderer>() ?? textObject.AddComponent<MeshRenderer>();
            textRenderer.sortingOrder = 10;
        }

        private void Update()
        {
            age += Time.deltaTime;
            var t = Mathf.Clamp01(age / Duration);
            var lift = Mathf.Sin(t * Mathf.PI) * 0.28f;
            transform.position = startPosition + drift * t + new Vector3(0f, lift, 0f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.82f, 1.08f, 1f - Mathf.Abs(0.5f - t) * 2f);

            var alpha = 1f - Mathf.SmoothStep(0.58f, 1f, t);
            if (coinRenderer != null)
                coinRenderer.color = new Color(coinRenderer.color.r, coinRenderer.color.g, coinRenderer.color.b, alpha);
            if (rewardText != null)
                rewardText.color = new Color(rewardText.color.r, rewardText.color.g, rewardText.color.b, alpha);

            if (age >= Duration)
                Destroy(gameObject);
        }

        private static float CalculateCoinScale(Sprite coinSprite)
        {
            if (coinSprite == null)
                return 0.16f;

            var largestSpriteAxis = Mathf.Max(coinSprite.bounds.size.x, coinSprite.bounds.size.y);
            return largestSpriteAxis > 0f ? 0.24f / largestSpriteAxis : 0.16f;
        }
    }
}

