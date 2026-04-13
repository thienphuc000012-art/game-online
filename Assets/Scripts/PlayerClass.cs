using System;
using UnityEngine;

public enum PlayerClass 
{
   Mage, Archer , Barbarian , Elf
}

[Serializable]
public struct PlayerClassInfo
{
    public string Name;
    public PlayerClass Class;
}
