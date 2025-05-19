using System.Collections.Generic;
using UnityEngine;

namespace WFC3DMapGenerator
{
    public class Tileset : ScriptableObject
    {
        [HideInInspector][SerializeField] public List<Tile3D> tiles;
        [HideInInspector][SerializeField] public int tileSize;
        [HideInInspector][SerializeField] public int tileCount;
        [HideInInspector][SerializeField] public List<string> tileTypes;
        [HideInInspector][SerializeField] public List<string> socketTypes;
    }
}
