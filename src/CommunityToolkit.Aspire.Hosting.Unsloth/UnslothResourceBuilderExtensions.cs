using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using CommunityToolkit.Aspire.Hosting.Unsloth;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Unsloth resources to the application model.
/// </summary>
public static class UnslothResourceBuilderExtensions
{
    private const string WorkPath = "/workspace/work";
    private const string ModelCachePath = "/workspace/.cache";

    /// <summary>
    /// Adds an Unsloth Studio container with OpenAI- and Anthropic-compatible inference APIs.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource. This name is also used as the connection string name.</param>
    /// <param name="jupyterPassword">The parameter used for the Jupyter Lab password. A secret parameter is generated when omitted.</param>
    /// <param name="userPassword">The parameter used for the container user's password and sudo access. A secret parameter is generated when omitted.</param>
    /// <param name="port">The host port for Unsloth Studio and its inference API.</param>
    /// <param name="jupyterPort">The host port for Jupyter Lab.</param>
    /// <param name="sshPort">The host port for SSH.</param>
    /// <returns>A builder for the Unsloth resource.</returns>
    [AspireExport]
    public static IResourceBuilder<UnslothResource> AddUnsloth(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        IResourceBuilder<ParameterResource>? jupyterPassword = null,
        IResourceBuilder<ParameterResource>? userPassword = null,
        int? port = null,
        int? jupyterPort = null,
        int? sshPort = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var jupyterPasswordParameter = jupyterPassword?.Resource ??
            ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-jupyter-password");
        var userPasswordParameter = userPassword?.Resource ??
            ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-user-password");
        var resource = new UnslothResource(name, jupyterPasswordParameter, userPasswordParameter);

        return builder.AddResource(resource)
            .WithImage(UnslothContainerImageTags.Image, UnslothContainerImageTags.Tag)
            .WithImageRegistry(UnslothContainerImageTags.Registry)
            .WithHttpEndpoint(port: port, targetPort: UnslothResource.PrimaryTargetPort, name: UnslothResource.PrimaryEndpointName)
            .WithHttpEndpoint(port: jupyterPort, targetPort: UnslothResource.JupyterTargetPort, name: UnslothResource.JupyterEndpointName)
            .WithEndpoint(port: sshPort, targetPort: UnslothResource.SshTargetPort, name: UnslothResource.SshEndpointName, scheme: "tcp")
            .WithUrlForEndpoint(UnslothResource.PrimaryEndpointName, annotation => annotation.DisplayText = "Unsloth Studio")
            .WithUrlForEndpoint(UnslothResource.JupyterEndpointName, annotation => annotation.DisplayText = "Jupyter Lab")
            .WithEnvironment("JUPYTER_PORT", UnslothResource.JupyterTargetPort.ToString())
            .WithEnvironment("JUPYTER_PASSWORD", resource.JupyterPasswordParameter)
            .WithEnvironment("USER_PASSWORD", resource.UserPasswordParameter)
            .WithHttpHealthCheck("/api/health", endpointName: UnslothResource.PrimaryEndpointName);
    }

    /// <summary>Adds a named volume for persistent work files.</summary>
    /// <param name="builder">The Unsloth resource builder.</param>
    /// <param name="name">The volume name, or an automatically generated name when omitted.</param>
    /// <param name="isReadOnly">Whether the volume is mounted read-only.</param>
    /// <returns>The Unsloth resource builder.</returns>
    [SuppressMessage("ApiDesign", "RS0026", Justification = "The method is named WithDataVolume to be consistent with other integrations.")]
    [AspireExport]
    public static IResourceBuilder<UnslothResource> WithDataVolume(this IResourceBuilder<UnslothResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), WorkPath, isReadOnly);
    }

    /// <summary>Adds a bind mount for persistent work files.</summary>
    /// <param name="builder">The Unsloth resource builder.</param>
    /// <param name="source">The source directory on the host.</param>
    /// <param name="isReadOnly">Whether the bind mount is read-only.</param>
    /// <returns>The Unsloth resource builder.</returns>
    [AspireExport]
    public static IResourceBuilder<UnslothResource> WithDataBindMount(this IResourceBuilder<UnslothResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return builder.WithBindMount(source, WorkPath, isReadOnly);
    }

    /// <summary>Adds a named volume for the Hugging Face and model cache.</summary>
    /// <param name="builder">The Unsloth resource builder.</param>
    /// <param name="name">The volume name, or an automatically generated name when omitted.</param>
    /// <param name="isReadOnly">Whether the volume is mounted read-only.</param>
    /// <returns>The Unsloth resource builder.</returns>
    [AspireExport]
    public static IResourceBuilder<UnslothResource> WithModelCacheVolume(this IResourceBuilder<UnslothResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "models"), ModelCachePath, isReadOnly);
    }

    /// <summary>Adds a bind mount for the Hugging Face and model cache.</summary>
    /// <param name="builder">The Unsloth resource builder.</param>
    /// <param name="source">The source directory on the host.</param>
    /// <param name="isReadOnly">Whether the bind mount is read-only.</param>
    /// <returns>The Unsloth resource builder.</returns>
    [AspireExport]
    public static IResourceBuilder<UnslothResource> WithModelCacheBindMount(this IResourceBuilder<UnslothResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return builder.WithBindMount(source, ModelCachePath, isReadOnly);
    }

    /// <summary>Configures the host port for Unsloth Studio and its inference API.</summary>
    /// <param name="builder">The Unsloth resource builder.</param>
    /// <param name="port">The host port, or <see langword="null"/> for dynamic allocation.</param>
    /// <returns>The Unsloth resource builder.</returns>
    [AspireExport]
    public static IResourceBuilder<UnslothResource> WithHostPort(this IResourceBuilder<UnslothResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint(UnslothResource.PrimaryEndpointName, endpoint => endpoint.Port = port);
    }

    /// <summary>Adds NVIDIA GPU access to the Unsloth container.</summary>
    /// <param name="builder">The Unsloth resource builder.</param>
    /// <returns>The Unsloth resource builder.</returns>
    /// <remarks>Docker uses <c>--gpus all</c>; Podman uses the NVIDIA CDI device.</remarks>
    [AspireExport]
    public static IResourceBuilder<UnslothResource> WithGPUSupport(this IResourceBuilder<UnslothResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ApplicationBuilder.GetContainerRuntime() switch
        {
            "podman" => builder.WithContainerRuntimeArgs("--device", "nvidia.com/gpu=all"),
            _ => builder.WithContainerRuntimeArgs("--gpus", "all"),
        };
    }

    private static string? GetContainerRuntime(this IDistributedApplicationBuilder builder)
        => (builder.Configuration["ASPIRE_CONTAINER_RUNTIME"] ??
            builder.Configuration["DOTNET_ASPIRE_CONTAINER_RUNTIME"])?.ToLowerInvariant();
}