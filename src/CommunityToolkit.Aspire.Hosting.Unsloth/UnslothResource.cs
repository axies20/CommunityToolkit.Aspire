namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an Unsloth Studio container and its OpenAI-compatible API.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="jupyterPassword">The parameter containing the Jupyter Lab password.</param>
/// <param name="userPassword">The parameter containing the password for the container's <c>unsloth</c> user.</param>
[AspireExport(ExposeProperties = true)]
public sealed class UnslothResource(string name, ParameterResource jupyterPassword, ParameterResource userPassword)
    : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "http";
    internal const string JupyterEndpointName = "jupyter";
    internal const string SshEndpointName = "ssh";

    internal const int PrimaryTargetPort = 8000;
    internal const int JupyterTargetPort = 8888;
    internal const int SshTargetPort = 22;

    private EndpointReference? _primaryEndpoint;
    private EndpointReference? _jupyterEndpoint;
    private EndpointReference? _sshEndpoint;

    /// <summary>Gets the parameter containing the Jupyter Lab password.</summary>
    public ParameterResource JupyterPasswordParameter { get; } = jupyterPassword;

    /// <summary>Gets the parameter containing the password for the container's <c>unsloth</c> user.</summary>
    public ParameterResource UserPasswordParameter { get; } = userPassword;

    /// <summary>Gets the Unsloth Studio and inference API endpoint.</summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>Gets the Jupyter Lab endpoint.</summary>
    public EndpointReference JupyterEndpoint => _jupyterEndpoint ??= new(this, JupyterEndpointName);

    /// <summary>Gets the SSH endpoint.</summary>
    public EndpointReference SshEndpoint => _sshEndpoint ??= new(this, SshEndpointName);

    /// <summary>Gets the host expression for the primary endpoint.</summary>
    public EndpointReferenceExpression Host => PrimaryEndpoint.Property(EndpointProperty.Host);

    /// <summary>Gets the port expression for the primary endpoint.</summary>
    public EndpointReferenceExpression Port => PrimaryEndpoint.Property(EndpointProperty.Port);

    /// <summary>Gets the URI expression for Unsloth Studio and its HTTP API.</summary>
    public ReferenceExpression UriExpression => ReferenceExpression.Create($"{PrimaryEndpoint.Property(EndpointProperty.Url)}");

    /// <summary>Gets the OpenAI-compatible base URI expression ending in <c>/v1</c>.</summary>
    public ReferenceExpression OpenAIEndpointExpression => ReferenceExpression.Create($"{PrimaryEndpoint.Property(EndpointProperty.Url)}/v1");

    /// <summary>Gets the connection string expression for Unsloth Studio.</summary>
    public ReferenceExpression ConnectionStringExpression => UriExpression;

    IEnumerable<KeyValuePair<string, ReferenceExpression>> IResourceWithConnectionString.GetConnectionProperties()
    {
        yield return new("Host", ReferenceExpression.Create($"{Host}"));
        yield return new("Port", ReferenceExpression.Create($"{Port}"));
        yield return new("Uri", UriExpression);
        yield return new("OpenAIEndpoint", OpenAIEndpointExpression);
    }
}