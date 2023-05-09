using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using Random = UnityEngine.Random;

public class FluidSimulation : MonoBehaviour {
    public static FluidSimulation instance;

    public Vector2Int resolution;
    public FilterMode filterMode;
    public float vortexIntensity = 1f;
    public float maxSpeedAfterAddVortex = 10f;
    public Color injectColor;
    public float injectIntensity = 1;
    public float injectRadius = 1f;
    public float injectSpeed = 1;
    public int fixedWallThickness = 0;
    public float densityDamping = 1;
    public float velocityDamping = 1;
    public ComputeShader initCellCS;
    public ComputeShader initTempBufferCS;
    public ComputeShader initStaggeredPointCS;
    public ComputeShader initSummaryBufferCS;
    public ComputeShader summaryStaggeredPointVelocityCS;
    public ComputeShader averageSummaryStaggeredPointVelocityCS;
    public int projectIteration = 1;
    public ComputeShader projectCS;
    public ComputeShader averageProjectCS;
    public ComputeShader applyProjectCS;
    public int vortexIteration = 1;
    public ComputeShader vortexCS;
    public ComputeShader applyVortexCS;
    public int advectIteration = 1;
    public ComputeShader advectCS;
    public ComputeShader applyAdvectCS;
    public ComputeShader dampingCS;
    public ComputeShader injectCS;
    public ComputeShader refreshTextureCS;

    private Vector2 prevInjectPos;

    private int mainKernel;
    private ComputeBuffer cellDataBuffer;
    private ComputeBuffer advectBuffer;
    private ComputeBuffer vortexBuffer;
    private ComputeBuffer uStaggeredPointBuffer;
    private ComputeBuffer vStaggeredPointBuffer;
    private ComputeBuffer summaryBuffer;

    [NonSerialized] public RenderTexture fluidTexture;

    private void OnEnable() {
        instance = this;

        cellDataBuffer = new ComputeBuffer(resolution.x * resolution.y, FluidSimulationBufferStride.cellData);
        advectBuffer = new ComputeBuffer(cellDataBuffer.count, FluidSimulationBufferStride.advectData);
        vortexBuffer = new ComputeBuffer(cellDataBuffer.count, FluidSimulationBufferStride.vortexData);
        uStaggeredPointBuffer = new ComputeBuffer(resolution.x * resolution.y + resolution.y, FluidSimulationBufferStride.staggeredPoint);
        vStaggeredPointBuffer = new ComputeBuffer(resolution.x * resolution.y + resolution.x, FluidSimulationBufferStride.staggeredPoint);
        summaryBuffer = new ComputeBuffer((uStaggeredPointBuffer.count + vStaggeredPointBuffer.count) * 2, 4);

        fluidTexture = new RenderTexture(resolution.x, resolution.y, 0, RenderTextureFormat.ARGBFloat);
        fluidTexture.enableRandomWrite = true;
        fluidTexture.filterMode = filterMode;
        fluidTexture.wrapMode = TextureWrapMode.Clamp;

        initStaggeredPointCS.SetInts("_Resolution", resolution.x, resolution.y);
        initStaggeredPointCS.SetInt("_ColumnNumber", resolution.x + 1);
        initStaggeredPointCS.SetInt("_WallThickness", fixedWallThickness);
        initStaggeredPointCS.SetBuffer(mainKernel, "_StaggeredPoints", uStaggeredPointBuffer);
        initStaggeredPointCS.Dispatch(mainKernel, resolution.x + 1, resolution.y, 1);

        initStaggeredPointCS.SetInts("_Resolution", resolution.x, resolution.y);
        initStaggeredPointCS.SetInt("_ColumnNumber", resolution.x);
        initStaggeredPointCS.SetInt("_WallThickness", fixedWallThickness);
        initStaggeredPointCS.SetBuffer(mainKernel, "_StaggeredPoints", vStaggeredPointBuffer);
        initStaggeredPointCS.Dispatch(mainKernel, resolution.x, resolution.y + 1, 1);

        initCellCS.SetInts("_Resolution", resolution.x, resolution.y);
        initCellCS.SetBuffer(mainKernel, "_CellDatas", cellDataBuffer);
        initCellCS.SetBuffer(mainKernel, "_UStaggeredPoints", uStaggeredPointBuffer);
        initCellCS.SetBuffer(mainKernel, "_VStaggeredPoints", vStaggeredPointBuffer);
        initCellCS.Dispatch(mainKernel, resolution.x, resolution.y, 1);

        initTempBufferCS.SetBuffer(mainKernel, "_AdvectDatas", advectBuffer);
        initTempBufferCS.SetBuffer(mainKernel, "_VortexDatas", vortexBuffer);
        initTempBufferCS.Dispatch(mainKernel, Mathf.CeilToInt(advectBuffer.count / 256f), 1, 1);

        initSummaryBufferCS.SetBuffer(mainKernel, "_SummaryDatas", summaryBuffer);
        initSummaryBufferCS.Dispatch(mainKernel, Mathf.CeilToInt(summaryBuffer.count / 256f), 1, 1);

        summaryStaggeredPointVelocityCS.SetBuffer(mainKernel, "_CellDatas", cellDataBuffer);
        summaryStaggeredPointVelocityCS.SetBuffer(mainKernel, "_SummaryDatas", summaryBuffer);

        averageSummaryStaggeredPointVelocityCS.SetBuffer(mainKernel, "_UStaggeredPoints", uStaggeredPointBuffer);
        averageSummaryStaggeredPointVelocityCS.SetBuffer(mainKernel, "_VStaggeredPoints", vStaggeredPointBuffer);
        averageSummaryStaggeredPointVelocityCS.SetBuffer(mainKernel, "_SummaryDatas", summaryBuffer);

        projectCS.SetInts("_Resolution", resolution.x, resolution.y);
        projectCS.SetBuffer(mainKernel, "_CellDatas", cellDataBuffer);
        projectCS.SetBuffer(mainKernel, "_UStaggeredPoints", uStaggeredPointBuffer);
        projectCS.SetBuffer(mainKernel, "_VStaggeredPoints", vStaggeredPointBuffer);
        projectCS.SetBuffer(mainKernel, "_SummaryDatas", summaryBuffer);

        averageProjectCS.SetBuffer(mainKernel, "_UStaggeredPoints", uStaggeredPointBuffer);
        averageProjectCS.SetBuffer(mainKernel, "_VStaggeredPoints", vStaggeredPointBuffer);
        averageProjectCS.SetBuffer(mainKernel, "_SummaryDatas", summaryBuffer);

        applyProjectCS.SetBuffer(mainKernel, "_CellDatas", cellDataBuffer);
        applyProjectCS.SetBuffer(mainKernel, "_UStaggeredPoints", uStaggeredPointBuffer);
        applyProjectCS.SetBuffer(mainKernel, "_VStaggeredPoints", vStaggeredPointBuffer);

        vortexCS.SetInts("_Resolution", resolution.x, resolution.y);
        vortexCS.SetBuffer(mainKernel, "_CellDatas", cellDataBuffer);
        vortexCS.SetBuffer(mainKernel, "_VortexDatas", vortexBuffer);

        applyVortexCS.SetBuffer(mainKernel, "_CellDatas", cellDataBuffer);
        applyVortexCS.SetBuffer(mainKernel, "_VortexDatas", vortexBuffer);

        advectCS.SetInts("_Resolution", resolution.x, resolution.y);
        advectCS.SetBuffer(mainKernel, "_CellDatas", cellDataBuffer);
        advectCS.SetBuffer(mainKernel, "_AdvectDatas", advectBuffer);
        advectCS.SetBuffer(mainKernel, "_UStaggeredPoints", uStaggeredPointBuffer);
        advectCS.SetBuffer(mainKernel, "_VStaggeredPoints", vStaggeredPointBuffer);

        applyAdvectCS.SetBuffer(mainKernel, "_CellDatas", cellDataBuffer);
        applyAdvectCS.SetBuffer(mainKernel, "_AdvectDatas", advectBuffer);

        dampingCS.SetBuffer(mainKernel, "_CellDatas", cellDataBuffer);

        injectCS.SetInts("_Resolution", resolution.x, resolution.y);
        injectCS.SetBuffer(mainKernel, "_CellDatas", cellDataBuffer);
        injectCS.SetBuffer(mainKernel, "_UStaggeredPoints", uStaggeredPointBuffer);
        injectCS.SetBuffer(mainKernel, "_VStaggeredPoints", vStaggeredPointBuffer);

        refreshTextureCS.SetInts("_Resolution", resolution.x, resolution.y);
        refreshTextureCS.SetBuffer(mainKernel, "_CellDatas", cellDataBuffer);
        refreshTextureCS.SetTexture(mainKernel, "_FluidTexture", fluidTexture);
    }

    private void OnDisable() {
        cellDataBuffer.Release();
        advectBuffer.Release();
        vortexBuffer.Release();
        uStaggeredPointBuffer.Release();
        vStaggeredPointBuffer.Release();
        summaryBuffer.Release();
        fluidTexture.Release();
    }

    private void Update() {
        Refresh();
    }

    public void Refresh() {
        UpdateParameters();

        VorticityConfinement();

        PrepareStaggeredPointVelocity();

        Project();

        Advect();

        Damping();

        RefreshTexture();

        Inject();
    }

    private void UpdateParameters() {
        vortexCS.SetFloat("_VortexIntensity", vortexIntensity);

        applyVortexCS.SetFloat("_MaxSpeed", maxSpeedAfterAddVortex);

        dampingCS.SetFloat("_DensityDamping", densityDamping);
        dampingCS.SetFloat("_VelocityDamping", velocityDamping);
    }

    private void Inject() {
        ComputeBuffer injectDataBuffer;

        if (Input.GetMouseButton(0)) {
            Vector2 injectPosition = Input.mousePosition;

            injectPosition.x = Mathf.Clamp(injectPosition.x, 0, Screen.width);
            injectPosition.y = Mathf.Clamp(injectPosition.y, 0, Screen.width);

            Vector2 injectUV = injectPosition;
            injectUV.x = Mathf.Clamp01(injectUV.x / Screen.width);
            injectUV.y = Mathf.Clamp01(injectUV.y / Screen.height);

            Vector2Int injectCenter = Vector2Int.zero;
            injectCenter.x = Mathf.FloorToInt(Mathf.Lerp(0, resolution.x, injectUV.x));
            injectCenter.y = Mathf.FloorToInt(Mathf.Lerp(0, resolution.y, injectUV.y));

            InjectData injectData = new InjectData();
            injectData.center = injectCenter;
            injectData.radius = injectRadius;
            injectData.density = injectIntensity;

            Vector2 dir = injectPosition - prevInjectPos;

            injectData.velocity = dir.normalized * injectSpeed;
            injectData.color = injectColor * injectData.density;

            injectDataBuffer = new ComputeBuffer(1, FluidSimulationBufferStride.injectData);
            injectDataBuffer.SetData(new InjectData[1] { injectData });

            injectCS.SetBuffer(mainKernel, "_InjectDatas", injectDataBuffer);
            injectCS.Dispatch(mainKernel, resolution.x, resolution.y, 1);

            injectDataBuffer.Release();

            prevInjectPos = injectPosition;
        }

        List<InjectData> injectDatas = new List<InjectData>();

        if (injectDatas.Count == 0) {
            return;
        }

        injectDataBuffer = new ComputeBuffer(injectDatas.Count, FluidSimulationBufferStride.injectData);
        injectDataBuffer.SetData(injectDatas);

        injectCS.SetBuffer(mainKernel, "_InjectDatas", injectDataBuffer);
        injectCS.Dispatch(mainKernel, resolution.x, resolution.y, 1);

        injectDataBuffer.Release();
    }

    private void PrepareStaggeredPointVelocity() {
        summaryStaggeredPointVelocityCS.Dispatch(mainKernel, Mathf.CeilToInt(cellDataBuffer.count / 256f), 1, 1);

        averageSummaryStaggeredPointVelocityCS.Dispatch(mainKernel, Mathf.CeilToInt((uStaggeredPointBuffer.count + vStaggeredPointBuffer.count) / 256f), 1, 1);
    }

    private void Project() {
        for (int i = 0; i < projectIteration; i++) {
            projectCS.Dispatch(mainKernel, Mathf.CeilToInt(cellDataBuffer.count / 256f), 1, 1);
            averageProjectCS.Dispatch(mainKernel, Mathf.CeilToInt((uStaggeredPointBuffer.count + vStaggeredPointBuffer.count) / 256f), 1, 1);
        }

        if (projectIteration > 0) {
            applyProjectCS.Dispatch(mainKernel, Mathf.CeilToInt(cellDataBuffer.count / 256f), 1, 1);
        }
    }

    private void VorticityConfinement() {
        for (int i = 0; i < vortexIteration; i++) {
            vortexCS.Dispatch(mainKernel, Mathf.CeilToInt(cellDataBuffer.count / 256f), 1, 1);

            applyVortexCS.Dispatch(mainKernel, Mathf.CeilToInt(cellDataBuffer.count / 256f), 1, 1);
        }
    }

    private void Advect() {
        for (int i = 0; i < advectIteration; i++) {
            advectCS.Dispatch(mainKernel, Mathf.CeilToInt(cellDataBuffer.count / 256f), 1, 1);

            applyAdvectCS.Dispatch(mainKernel, Mathf.CeilToInt(cellDataBuffer.count / 256f), 1, 1);
        }
    }

    private void Damping() {
        dampingCS.Dispatch(mainKernel, Mathf.CeilToInt(cellDataBuffer.count / 256f), 1, 1);
    }

    private void RefreshTexture() {
        refreshTextureCS.Dispatch(mainKernel, resolution.x, resolution.y, 1);
    }
}