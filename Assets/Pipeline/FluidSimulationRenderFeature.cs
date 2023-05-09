using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEditor.MaterialProperty;

public class FluidSimulationRenderFeature : ScriptableRendererFeature {
    public FluidSimulationRenderPassSetting setting;

    private FluidSimulationRenderPass pass;

    public override void Create() {

    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (renderingData.cameraData.camera.name != "Main Camera")
            return;

        if (pass == null)
            pass = new FluidSimulationRenderPass(setting);

        renderer.EnqueuePass(pass);
    }
}