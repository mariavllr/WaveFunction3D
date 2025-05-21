using System.Collections.Generic;
using UnityEngine;

namespace WFC3DMapGenerator
{
    public class Tileset : ScriptableObject
    {
        [HideInInspector][SerializeField] public List<Tile3D> tiles = new List<Tile3D>();
        [HideInInspector][SerializeField] public int tileSize = 1;
        [HideInInspector][SerializeField] public int tileCount = 0;
        [HideInInspector][SerializeField] public List<string> tileTypes = new List<string>();
        [HideInInspector][SerializeField] public List<string> socketTypes = new List<string>();
    }
}
