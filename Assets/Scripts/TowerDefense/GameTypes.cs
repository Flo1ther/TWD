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
}

