import { createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

const unsloth = await builder.addUnsloth("unsloth");
await unsloth.withDataVolume();
await unsloth.withModelCacheVolume();

// Enable NVIDIA GPU access after configuring NVIDIA Container Toolkit on Docker
// or the nvidia.com/gpu=all CDI device on Podman:
// await unsloth.withGPUSupport();

await builder.build().run();
