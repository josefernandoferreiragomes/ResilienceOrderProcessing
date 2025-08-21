using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace OrderProcessing.Api.Tests.Helpers;
public class TestEndpointRouteBuilder : IEndpointRouteBuilder
{
    public IServiceProvider ServiceProvider { get; }
    public ICollection<EndpointDataSource> DataSources { get; }
    public IApplicationBuilder CreateApplicationBuilder() => throw new NotImplementedException();

    public TestEndpointRouteBuilder(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        DataSources = new List<EndpointDataSource>();
    }
}