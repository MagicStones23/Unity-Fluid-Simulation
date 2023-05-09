using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.Burst.Intrinsics.X86.Avx;

public class FluidSimulationRenderPass : ScriptableRenderPass {
    private FluidSimulationRenderPassSetting setting;

    public FluidSimulationRenderPass(FluidSimulationRenderPassSetting setting) {
        this.setting = setting;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        FluidSimulation simulation = FluidSimulation.instance;

        if (simulation == null) {
            return;
        }

        renderPassEvent = setting.renderPassEvent;

        CommandBuffer cmd = CommandBufferPool.Get("FluidSimulation");

        cmd.Blit(simulation.fluidTexture, renderingData.cameraData.renderer.cameraColorTarget);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}