#ifndef FluidSimulation
#define FluidSimulation

int2 _Resolution;

struct CellData {
    int2 coord;
    float density;
    float2 velocity;
    float4 color;
    int2 leftStaggeredPointCoord;
    int2 rightStaggeredPointCoord;
    int2 upStaggeredPointCoord;
    int2 downStaggeredPointCoord;
    int leftStaggeredPointIndex;
    int rightStaggeredPointIndex;
    int upStaggeredPointIndex;
    int downStaggeredPointIndex;
    int leftStaggeredPointSummaryIndex;
    int rightStaggeredPointSummaryIndex;
    int upStaggeredPointSummaryIndex;
    int downStaggeredPointSummaryIndex;
};

struct AdvectData {
    float density;
    float2 velocity;
    float4 color;
};

struct VortexData {
    float2 velocity;
};

struct InjectData {
    int2 center;
    float radius;
    float density;
    float2 velocity;
    float4 color;
};

struct StaggeredPoint {
    int2 coord;
    float scaler;
    float velocity;
    int summaryNumber;
};

int CellCoordToIndex(int2 coord) {
    return coord.x + coord.y * _Resolution.x;
}

int UStaggeredPointCoordToIndex(int2 coord) {
    return coord.x + coord.y * (_Resolution.x + 1);
}

int VStaggeredPointCoordToIndex(int2 coord) {
    return coord.x + coord.y * _Resolution.x;
}

#endif