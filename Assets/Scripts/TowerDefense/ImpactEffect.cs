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
}

