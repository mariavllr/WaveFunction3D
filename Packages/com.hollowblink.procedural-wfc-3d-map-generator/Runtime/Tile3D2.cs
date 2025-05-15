using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile3D2 : MonoBehaviour
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

    public List<Tile3D2> upNeighbours = new List<Tile3D2>();
    public List<Tile3D2> rightNeighbours = new List<Tile3D2>();
    public List<Tile3D2> downNeighbours = new List<Tile3D2>();
    public List<Tile3D2> leftNeighbours = new List<Tile3D2>();
    public List<Tile3D2> aboveNeighbours = new List<Tile3D2>();    // Y+
    public List<Tile3D2> belowNeighbours = new List<Tile3D2>();    // Y-

    [Header("Excluded neighbours")]
    public List<string> excludedNeighboursUp = new();
    public List<string> excludedNeighboursRight = new();
    public List<string> excludedNeighboursDown = new();
    public List<string> excludedNeighboursLeft = new();

    [Tooltip("Para definir la direccion la derecha siempre ser� el eje X (rojo) y arriba ser� el eje Z (azul)")]
    [Header("Sockets")]
    public Socket upSocket;
    public Socket rightSocket;
    public Socket leftSocket;
    public Socket downSocket;
    public Socket aboveSocket;
    public Socket belowSocket;


}
