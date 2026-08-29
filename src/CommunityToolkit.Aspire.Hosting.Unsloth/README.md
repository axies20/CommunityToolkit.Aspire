# Unsloth hosting integration

Use this integration to model, configure, and orchestrate [Unsloth](https://github.com/unslothai/unsloth) Studio, Jupyter Lab, and its OpenAI-compatible inference API in an Aspire application.

## Getting started

Add the package to an Aspire AppHost:

```dotnetcli
aspire add CommunityToolkit.Aspire.Hosting.Unsloth
```

The official `unsloth/unsloth` image currently targets Linux on AMD64. NVIDIA GPU workloads require a compatible NVIDIA driver and NVIDIA Container Toolkit or CDI configuration on the host.

## Usage example

```csharp
var unsloth = builder.AddUnsloth("unsloth")
    .WithDataVolume()
    .WithModelCacheVolume();

builder.AddProject<Projects.Api>("api")
    .WithReference(unsloth)
    .WaitFor(unsloth);
```

This starts Unsloth without requesting a GPU, which supports the CPU-capable Studio features. To enable NVIDIA GPU inference and training, add `WithGPUSupport()` after configuring NVIDIA Container Toolkit on the host:

```csharp
var unsloth = builder.AddUnsloth("unsloth")
    .WithGPUSupport()
    .WithDataVolume()
    .WithModelCacheVolume();
```

Docker uses `--gpus all`. Podman uses CDI and requires `nvidia-ctk cdi list` to include `nvidia.com/gpu=all`. If the device is absent, install NVIDIA Container Toolkit and generate or refresh the CDI specification before starting the AppHost. See the official NVIDIA Container Toolkit documentation linked below.

Aspire exposes links for Unsloth Studio and Jupyter Lab in the dashboard. SSH is also exposed as a dynamically allocated TCP endpoint. Pass `port`, `jupyterPort`, or `sshPort` to `AddUnsloth` when fixed host ports are required.

`WithDataVolume` persists `/workspace/work`. `WithModelCacheVolume` separately persists `/workspace/.cache`, preventing downloaded models from being lost when the container is recreated. The corresponding `WithDataBindMount` and `WithModelCacheBindMount` methods mount host directories instead.

### Authentication

`AddUnsloth` creates secret parameters for `JUPYTER_PASSWORD` and `USER_PASSWORD`. Existing parameters can be supplied when credentials must be shared or externally configured:

```csharp
var jupyterPassword = builder.AddParameter("unsloth-jupyter-password", secret: true);
var userPassword = builder.AddParameter("unsloth-user-password", secret: true);

var unsloth = builder.AddUnsloth(
    "unsloth",
    jupyterPassword: jupyterPassword,
    userPassword: userPassword);
```

Unsloth Studio protects inference endpoints with an API key managed by Studio. Create the key in the Studio settings and store it as an Aspire secret for the consuming application; it is distinct from the Jupyter and container-user passwords.

### OpenAI-compatible API

After loading a model in Studio, applications can use these endpoints on the primary HTTP resource:

- `POST /v1/chat/completions`
- `POST /v1/responses`
- `POST /v1/completions` (primarily for loaded GGUF models)

Unsloth also provides the Anthropic-compatible `POST /v1/messages` endpoint. Streaming, tool calling, and vision inputs are supported by compatible models and backends.

When the resource is referenced, Aspire injects the standard `ConnectionStrings__unsloth` value and connection properties. The `OpenAIEndpoint` connection property includes the `/v1` suffix expected by OpenAI clients.

Example request from a consuming service:

```bash
curl "$UNSLOTH_OPENAIENDPOINT/chat/completions" \
  -H "Authorization: Bearer $UNSLOTH_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "default",
    "messages": [{ "role": "user", "content": "Hello from Aspire" }],
    "stream": false
  }'
```

The exact environment-variable prefix is derived from the Aspire resource name. For a resource named `unsloth`, connection properties use the `UNSLOTH_` prefix.

## Connection Properties

| Property | Description | Format |
|---|---|---|
| `Host` | Host of the Studio/API endpoint | `host` |
| `Port` | Port of the Studio/API endpoint | `port` |
| `Uri` | Base URI for Studio and its HTTP APIs | `http://host:port` |
| `OpenAIEndpoint` | OpenAI-compatible base URI | `http://host:port/v1` |

## Additional documentation

- [Unsloth repository](https://github.com/unslothai/unsloth)
- [Official Unsloth Docker image](https://hub.docker.com/r/unsloth/unsloth)
- [Unsloth Docker installation](https://unsloth.ai/docs/get-started/install-and-update/docker)
- [NVIDIA Container Toolkit installation](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/latest/install-guide.html)
- [NVIDIA CDI support](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/latest/cdi-support.html)

## Feedback & contributing

https://github.com/CommunityToolkit/Aspire
