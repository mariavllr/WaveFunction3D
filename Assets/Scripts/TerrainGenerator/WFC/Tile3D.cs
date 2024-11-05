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

    public int probability;
    public bool isHorizontalSymetric;
    public bool isVerticalSymetric;
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

    public List<Tile3D> excludedNeighboursUp = new List<Tile3D>();
    public List<Tile3D> excludedNeighboursRight = new List<Tile3D>();
    public List<Tile3D> excludedNeighboursDown = new List<Tile3D>();
    public List<Tile3D> excludedNeighboursLeft = new List<Tile3D>();
    public List<Tile3D> excludedNeighboursAbove = new List<Tile3D>();
    public List<Tile3D> excludedNeighboursBelow  = new List<Tile3D>();

    [Tooltip("Para definir la direccion la derecha siempre será el eje X (rojo) y arriba será el eje Z (azul)")]
    [Header("Borders")]
    
   /* public Border upBorder; //Z
    public Border rightBorder; //X
    public Border leftBorder; //-X
    public Border downBorder; //-Z

    public Border aboveBorder;   // Y+
    public Border belowBorder;   // Y-*/


    public Socket upSocket;
    public Socket rightSocket;
    public Socket leftSocket;
    public Socket downSocket;
    public Socket aboveSocket;
    public Socket belowSocket;


}
