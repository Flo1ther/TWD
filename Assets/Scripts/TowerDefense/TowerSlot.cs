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
public readonly struct TowerSlot
    {
        public TowerSlot(int id, Vector3 position)
        {
            Id = id;
            Position = position;
        }

        public int Id { get; }
        public Vector3 Position { get; }
    }
}

