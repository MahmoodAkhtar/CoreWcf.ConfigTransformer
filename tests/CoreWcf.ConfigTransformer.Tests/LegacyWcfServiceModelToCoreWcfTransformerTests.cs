using System.Xml.Linq;
using Xunit;

namespace CoreWcf.ConfigTransformer.Tests;

public sealed class LegacyWcfServiceModelToCoreWcfTransformerTests
{
    [Fact]
    public void Transform_DerivesHttpListener()
    {
        var result = TransformFixture("BasicHttpOnly.config");

        var listener = Assert.Single(result.HttpListeners);
        Assert.Equal("http://localhost:8080", listener.GetLeftPart(UriPartial.Authority));
        Assert.Equal(
            "http://localhost:8080/Service",
            (string)Assert.Single(result.ServiceModel.Element("services")?.Element("service")?.Elements("endpoint") ?? Enumerable.Empty<XElement>()).Attribute("address"));
        Assert.Empty(result.NetTcpListeners);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Transform_DerivesHttpAndNetTcpListeners()
    {
        var result = TransformFixture("HttpAndNetTcp.config");

        Assert.Single(result.HttpListeners);
        Assert.Single(result.NetTcpListeners);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Transform_DerivesHttpsFromTransportSecurity()
    {
        var result = TransformFixture("HttpsAndNetTcp.config");

        Assert.Single(result.HttpsListeners);
        Assert.Single(result.NetTcpListeners);
        Assert.Empty(result.HttpListeners);
    }

    [Fact]
    public void Transform_ReportsMissingBaseAddress()
    {
        var result = TransformFixture("MissingBaseAddress.config");

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.MissingBaseAddress);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void Transform_ReportsInvalidBaseAddress()
    {
        var result = TransformFixture("InvalidBaseAddress.config");

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.InvalidBaseAddress);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void Transform_RemovesUnsupportedSections()
    {
        var result = TransformFixture("ReaderQuotasAndSecurity.config");

        Assert.Null(result.ServiceModel.Element("behaviors"));
        Assert.Null(result.ServiceModel.Element("client"));
        Assert.Null(result.ServiceModel.Element("diagnostics"));
        Assert.Null(result.ServiceModel.Element("extensions"));
        Assert.Null(result.ServiceModel.Element("protocolMapping"));
        Assert.Null(result.ServiceModel.Element("serviceHostingEnvironment"));
        Assert.Null(result.ServiceModel.Element("bindings")?.Element("netTcpBinding")?.Element("binding")?.Element("reliableSession"));
        Assert.NotNull(result.ServiceModel.Element("bindings"));
        Assert.NotNull(result.ServiceModel.Element("services"));
        Assert.Null(result.ServiceModel.Element("services")?.Element("service")?.Element("host"));
    }

    [Fact]
    public void Transform_UsesBaseAddressForRelativeEndpointAddresses()
    {
        var result = TransformFixture("RelativeEndpointAddresses.config");

        var listener = Assert.Single(result.HttpListeners);
        Assert.Equal("http://localhost:8080", listener.GetLeftPart(UriPartial.Authority));
        Assert.Equal(
            "http://localhost:8080/relative",
            (string)Assert.Single(result.ServiceModel.Element("services")?.Element("service")?.Elements("endpoint") ?? Enumerable.Empty<XElement>()).Attribute("address"));
    }

    [Fact]
    public void Transform_RemovesUnsupportedBindingsAndEndpoints()
    {
        var result = TransformFixture("UnsupportedBindings.config");

        Assert.Null(result.ServiceModel.Element("bindings")?.Element("netMsmqBinding"));
        Assert.NotNull(result.ServiceModel.Element("bindings")?.Element("basicHttpBinding"));
        Assert.DoesNotContain(
            result.ServiceModel.Element("services")?.Element("service")?.Elements("endpoint") ?? Enumerable.Empty<XElement>(),
            endpoint => string.Equals((string)endpoint.Attribute("binding"), "netMsmqBinding", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.UnsupportedBinding);
    }

    [Fact]
    public void Transform_PopulatesMissingServiceNamesFromContracts()
    {
        var result = Transform(
            """
            <system.serviceModel>
              <services>
                <service>
                  <endpoint contract="Contracts.IFoo" />
                </service>
                <service>
                  <endpoint contract="Contracts.IBar" />
                </service>
              </services>
            </system.serviceModel>
            """);

        var serviceNames = result.ServiceModel.Element("services")?.Elements("service").Select(service => (string)service.Attribute("name"));

        Assert.Equal(new[] { "Contracts.IFoo", "Contracts.IBar" }, serviceNames);
    }

    [Fact]
    public void Transform_MakesServiceNamesUnique()
    {
        var result = Transform(
            """
            <system.serviceModel>
              <services>
                <service name="Services.Duplicate" />
                <service name="Services.Duplicate" />
                <service />
                <service />
              </services>
            </system.serviceModel>
            """);

        var serviceNames = result.ServiceModel.Element("services")?.Elements("service").Select(service => (string)service.Attribute("name"));

        Assert.Equal(new[] { "Services.Duplicate", "Services.Duplicate_2", "Service3", "Service4" }, serviceNames);
    }

    [Fact]
    public void Transform_PopulatesMissingEndpointNamesFromBindingAndContract()
    {
        var result = Transform(
            """
            <system.serviceModel>
              <services>
                <service name="Services.Foo">
                  <endpoint binding="basicHttpBinding" contract="Contracts.IFoo" />
                  <endpoint binding="netTcpBinding" contract="Contracts.IFoo" />
                </service>
              </services>
            </system.serviceModel>
            """);

        var endpointNames = result.ServiceModel.Element("services")?
            .Element("service")?
            .Elements("endpoint")
            .Select(endpoint => (string)endpoint.Attribute("name"));

        Assert.Equal(new[] { "basicHttpBinding_Contracts.IFoo", "netTcpBinding_Contracts.IFoo" }, endpointNames);
    }

    [Fact]
    public void Transform_MakesEndpointNamesUnique()
    {
        var result = Transform(
            """
            <system.serviceModel>
              <services>
                <service name="Services.Foo">
                  <endpoint binding="basicHttpBinding" contract="Contracts.IFoo" />
                  <endpoint binding="basicHttpBinding" contract="Contracts.IFoo" />
                  <endpoint name="Existing" binding="netTcpBinding" contract="Contracts.IFoo" />
                  <endpoint name="Existing" binding="netTcpBinding" contract="Contracts.IBar" />
                </service>
              </services>
            </system.serviceModel>
            """);

        var endpointNames = result.ServiceModel.Element("services")?
            .Element("service")?
            .Elements("endpoint")
            .Select(endpoint => (string)endpoint.Attribute("name"));

        Assert.Equal(new[] { "basicHttpBinding_Contracts.IFoo", "basicHttpBinding_Contracts.IFoo_2", "Existing", "Existing_2" }, endpointNames);
    }

    [Fact]
    public void Transform_FromFilePaths_WritesTransformedServiceModel()
    {
        var legacyConfigurationPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "BasicHttpOnly.config");
        var generatedConfigurationPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.config");

        try
        {
            var transformer = new LegacyWcfServiceModelToCoreWcfTransformer();
            var result = transformer.Transform(
                legacyConfigurationPath,
                generatedConfigurationPath,
                new LegacyWcfServiceModelTransformOptions());

            var generatedConfiguration = XDocument.Load(generatedConfigurationPath);
            var generatedServiceModel = generatedConfiguration.Root?.Element("system.serviceModel");

            Assert.True(File.Exists(generatedConfigurationPath));
            Assert.Equal("configuration", generatedConfiguration.Root?.Name.LocalName);
            Assert.NotNull(generatedServiceModel);
            Assert.Equal(
                NormalizeXml(result.ServiceModel),
                NormalizeXml(generatedServiceModel));
            Assert.Null(generatedServiceModel.Element("services")?.Element("service")?.Element("host"));
        }
        finally
        {
            if (File.Exists(generatedConfigurationPath))
            {
                File.Delete(generatedConfigurationPath);
            }
        }
    }

    private static LegacyWcfServiceModelTransformResult TransformFixture(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var serviceModel = document.Root?.Element("system.serviceModel");
        var transformer = new LegacyWcfServiceModelToCoreWcfTransformer();

        Assert.NotNull(serviceModel);

        return transformer.Transform(serviceModel);
    }

    private static LegacyWcfServiceModelTransformResult Transform(string serviceModelXml)
    {
        var transformer = new LegacyWcfServiceModelToCoreWcfTransformer();
        return transformer.Transform(XElement.Parse(serviceModelXml));
    }

    private static string NormalizeXml(XElement element)
    {
        return XElement.Parse(element.ToString(SaveOptions.DisableFormatting))
            .ToString(SaveOptions.DisableFormatting);
    }
}
