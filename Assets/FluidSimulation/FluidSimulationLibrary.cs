using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FluidSimulationBufferStride {
    public static int cellData = 100;
    public static int advectData = 28;
    public static int vortexData = 8;
    public static int injectData = 40;
    public static int staggeredPoint = 20;
}

public struct CellData {
    public Vector2Int coord;
    public float density;
    public Vector2 velocity;
    public Color color;
    public Vector2Int leftStaggeredPointCoord;
    public Vector2Int rightStaggeredPointCoord;
    public Vector2Int upStaggeredPointCoord;
    public Vector2Int downStaggeredPointCoord;
    public int leftStaggeredPointIndex;
    public int rightStaggeredPointIndex;
    public int upStaggeredPointIndex;
    public int downStaggeredPointIndex;
    public int leftStaggeredPointSummaryIndex;
    public int rightStaggeredPointSummaryIndex;
    public int upStaggeredPointSummaryIndex;
    public int downStaggeredPointSummaryIndex;
}

public struct AdvectData {
    public float density;
    public Vector2 velocity;
    public Color color;
}

public struct VortexData {
    public Vector2 velocity;
}

public struct InjectData {
    public Vector2Int center;
    public float radius;
    public float density;
    public Vector2 velocity;
    public Color color;
}

public struct StaggeredPoint {
    public Vector2Int coord;
    public float scaler;
    public float velocity;
    public int summaryNumber;
}

public class FluidSimulationLibrary {

}