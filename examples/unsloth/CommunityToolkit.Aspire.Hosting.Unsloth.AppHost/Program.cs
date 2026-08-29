var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
    .WithGPUSupport()
    .WithDataVolume();

var qwen = ollama.AddModel("qwen", "qwen2.5:0.5b");

var unsloth = builder.AddUnsloth("unsloth")
    .WithDataVolume()
    .WithModelCacheVolume()
    .WithReference(ollama)
    .WaitFor(qwen);

// Enable NVIDIA GPU access after configuring NVIDIA Container Toolkit on Docker
// or the nvidia.com/gpu=all CDI device on Podman:
unsloth.WithGPUSupport();

builder.Build().Run();
