using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile3D : MonoBehaviour
{
    public enum Border
    {
        GRASS,
        PATH,
        WATER,
        EMPTY,
        WALL_LATERAL,
        WALL_TOP,
        WALL_CORNER_EXT,
        WALL_CORNER_INT,
        BORDER,
        GRASS_BORDER,
        SOLID

    }
    [Serializable]
    public struct Socket
    {
        public Border socket_name;
        //for horizontal faces
        [Header("For HORIZONTAL faces")]
        public bool horizontalFace;
        public bool isSymmetric;
        public bool isFlipped;
        //for vertical faces
        [Header("For VERTICAL faces")]
        public bool verticalFace;
        public int rotationIndex;
        public bool rotationallyInvariant;
    }

    public string tileType;
    public int probability;

    [Header("Create rotated tiles")]
    public bool rotateRight;
    public bool rotate180;
    public bool rotateLeft;

    public Vector3 rotation;
    public Vector3 scale;
    public Vector3 positionOffset;

    public List<Tile3D> upNeighbours = new List<Tile3D>();
    public List<Tile3D> rightNeighbours = new List<Tile3D>();
    public List<Tile3D> downNeighbours = new List<Tile3D>();
    public List<Tile3D> leftNeighbours = new List<Tile3D>();
    public List<Tile3D> aboveNeighbours = new List<Tile3D>();    // Y+
    public List<Tile3D> belowNeighbours = new List<Tile3D>();    // Y-

    [Header("Excluded neighbours")]
    public List<string> excludedNeighboursUp = new();
    public List<string> excludedNeighboursRight = new();
    public List<string> excludedNeighboursDown = new();
    public List<string> excludedNeighboursLeft = new();

    public bool excludeInTopLayer;

    [Tooltip("Para definir la direccion la derecha siempre ser� el eje X (rojo) y arriba ser� el eje Z (azul)")]
    [Header("Sockets")]
    public Socket upSocket;
    public Socket rightSocket;
    public Socket leftSocket;
    public Socket downSocket;
    public Socket aboveSocket;
    public Socket belowSocket;


}
