using System.Collections.Generic;
using UnityEngine;

namespace WFC3DMapGenerator
{
    [CreateAssetMenu(fileName = "Tileset", menuName = "WFC3DMapGenerator/Tileset", order = 1)]
    public class Tileset : ScriptableObject
    {
        [SerializeField] public List<Tile3D> tiles;
    }
}
