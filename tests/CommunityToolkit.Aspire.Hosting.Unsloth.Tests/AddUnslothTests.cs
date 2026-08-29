using Aspire.Hosting;

namespace CommunityToolkit.Aspire.Hosting.Unsloth.Tests;

public class AddUnslothTests
{
    [Fact]
    public void AddUnslothConfiguresContainer()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddUnsloth("unsloth");

        using var app = builder.Build();
        var resource = Assert.Single(app.Services.GetRequiredService<DistributedApplicationModel>().Resources.OfType<UnslothResource>());

        Assert.Equal("unsloth", resource.Name);

        var image = Assert.Single(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(UnslothContainerImageTags.Registry, image.Registry);
        Assert.Equal(UnslothContainerImageTags.Image, image.Image);
        Assert.Equal(UnslothContainerImageTags.Tag, image.Tag);

        var endpoints = resource.Annotations.OfType<EndpointAnnotation>().ToList();
        Assert.Equal(3, endpoints.Count);
        Assert.Equal(8000, Assert.Single(endpoints, e => e.Name == "http").TargetPort);
        Assert.Equal(8888, Assert.Single(endpoints, e => e.Name == "jupyter").TargetPort);
        Assert.Equal(22, Assert.Single(endpoints, e => e.Name == "ssh").TargetPort);

        Assert.Single(resource.Annotations.OfType<HealthCheckAnnotation>());
    }

    [Fact]
    public void AddUnslothUsesCustomPorts()
    {
        var builder = DistributedApplication.CreateBuilder();
        var unsloth = builder.AddUnsloth("unsloth", port: 8001, jupyterPort: 8889, sshPort: 2222);

        var endpoints = unsloth.Resource.Annotations.OfType<EndpointAnnotation>();
        Assert.Equal(8001, Assert.Single(endpoints, e => e.Name == "http").Port);
        Assert.Equal(8889, Assert.Single(endpoints, e => e.Name == "jupyter").Port);
        Assert.Equal(2222, Assert.Single(endpoints, e => e.Name == "ssh").Port);
    }

    [Fact]
    public async Task ConnectionStringAndOpenAIEndpointAreExposed()
    {
        var builder = DistributedApplication.CreateBuilder();
        var unsloth = builder.AddUnsloth("unsloth")
            .WithEndpoint("http", endpoint => endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", 8000));

        Assert.Equal("http://localhost:8000", await unsloth.Resource.ConnectionStringExpression.GetValueAsync(default));
        Assert.Equal("http://localhost:8000/v1", await unsloth.Resource.OpenAIEndpointExpression.GetValueAsync(default));

        var properties = ((IResourceWithConnectionString)unsloth.Resource).GetConnectionProperties().ToDictionary();
        Assert.Equal(["Host", "OpenAIEndpoint", "Port", "Uri"], properties.Keys.Order().ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unsloth-data")]
    public void WithDataVolumePersistsWorkDirectory(string? volumeName)
    {
        var builder = DistributedApplication.CreateBuilder();
        var unsloth = builder.AddUnsloth("unsloth").WithDataVolume(volumeName);

        var mount = Assert.Single(unsloth.Resource.Annotations.OfType<ContainerMountAnnotation>());
        Assert.Equal("/workspace/work", mount.Target);
        Assert.NotNull(mount.Source);
        if (volumeName is not null)
        {
            Assert.Equal(volumeName, mount.Source);
        }
    }

    [Fact]
    public void BindMountAndModelCacheHelpersUseCorrectTargets()
    {
        var builder = DistributedApplication.CreateBuilder();
        var unsloth = builder.AddUnsloth("unsloth")
            .WithDataBindMount("./work")
            .WithModelCacheBindMount("./cache");

        var mounts = unsloth.Resource.Annotations.OfType<ContainerMountAnnotation>().ToList();
        Assert.Contains(mounts, mount => mount.Source?.EndsWith("/work", StringComparison.Ordinal) == true && mount.Target == "/workspace/work");
        Assert.Contains(mounts, mount => mount.Source?.EndsWith("/cache", StringComparison.Ordinal) == true && mount.Target == "/workspace/.cache");
    }

    [Fact]
    public void WithModelCacheVolumePersistsModelCache()
    {
        var builder = DistributedApplication.CreateBuilder();
        var unsloth = builder.AddUnsloth("unsloth").WithModelCacheVolume("unsloth-models");

        var mount = Assert.Single(unsloth.Resource.Annotations.OfType<ContainerMountAnnotation>());
        Assert.Equal("unsloth-models", mount.Source);
        Assert.Equal("/workspace/.cache", mount.Target);
    }

    [Fact]
    public void WithHostPortUpdatesPrimaryEndpoint()
    {
        var builder = DistributedApplication.CreateBuilder();
        var unsloth = builder.AddUnsloth("unsloth").WithHostPort(8010);

        Assert.Equal(8010, Assert.Single(unsloth.Resource.Annotations.OfType<EndpointAnnotation>(), e => e.Name == "http").Port);
    }

    [Theory]
    [InlineData("docker", "--gpus", "all")]
    [InlineData("podman", "--device", "nvidia.com/gpu=all")]
    public async Task WithGpuSupportUsesContainerRuntimeConvention(string runtime, string firstArgument, string secondArgument)
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.Configuration["ASPIRE_CONTAINER_RUNTIME"] = runtime;

        var unsloth = builder.AddUnsloth("unsloth").WithGPUSupport();
        Assert.True(unsloth.Resource.TryGetLastAnnotation(out ContainerRuntimeArgsCallbackAnnotation? annotation));

        var context = new ContainerRuntimeArgsCallbackContext([]);
        await annotation.Callback(context);

        Assert.Equal([firstArgument, secondArgument], context.Args);
    }
}