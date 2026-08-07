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
    public void Transform_DerivesHttpsFromDefaultTransportSecurityBinding()
    {
        var result = Transform(
            """
            <system.serviceModel>
              <bindings>
                <basicHttpBinding>
                  <binding>
                    <security mode="Transport" />
                  </binding>
                </basicHttpBinding>
              </bindings>
              <services>
                <service name="Services.Foo">
                  <host>
                    <baseAddresses>
                      <add baseAddress="https://localhost:8443/Service" />
                    </baseAddresses>
                  </host>
                  <endpoint address="" binding="basicHttpBinding" contract="Contracts.IFoo" />
                </service>
              </services>
            </system.serviceModel>
            """);

        Assert.Single(result.HttpsListeners);
        Assert.Empty(result.HttpListeners);
        Assert.False(result.HasErrors);
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
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.UnsupportedSectionRemoved);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.UnsupportedBindingElementRemoved);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.HostElementRemoved);
    }

    [Fact]
    public void Transform_UsesBaseAddressForRelativeEndpointAddresses()
    {
        var result = TransformFixture("RelativeEndpointAddresses.config");

        var listener = Assert.Single(result.HttpListeners);
        Assert.Equal("http://localhost:8080", listener.GetLeftPart(UriPartial.Authority));
        Assert.Equal(
            "http://localhost:8080/Service/relative",
            (string)Assert.Single(result.ServiceModel.Element("services")?.Element("service")?.Elements("endpoint") ?? Enumerable.Empty<XElement>()).Attribute("address"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.EndpointAddressResolved);
    }

    [Fact]
    public void Transform_AppendsRelativeEndpointAddressesToBaseAddressPath()
    {
        var result = Transform(
            """
            <system.serviceModel>
              <services>
                <service name="Services.Foo">
                  <host>
                    <baseAddresses>
                      <add baseAddress="http://localhost:8080/Service" />
                    </baseAddresses>
                  </host>
                  <endpoint address="/relative" binding="basicHttpBinding" contract="Contracts.IFoo" />
                </service>
              </services>
            </system.serviceModel>
            """);

        var endpoint = Assert.Single(result.ServiceModel.Element("services")?.Element("service")?.Elements("endpoint") ?? Enumerable.Empty<XElement>());

        Assert.Equal("http://localhost:8080/Service/relative", (string)endpoint.Attribute("address"));
    }

    [Fact]
    public void Transform_ClassifiesListenersFromResolvedEndpointScheme()
    {
        var result = Transform(
            """
            <system.serviceModel>
              <services>
                <service name="Services.Foo">
                  <endpoint address="https://localhost:8443/Service" binding="basicHttpBinding" contract="Contracts.IFoo" />
                </service>
              </services>
            </system.serviceModel>
            """);

        Assert.Empty(result.HttpListeners);
        Assert.Single(result.HttpsListeners);
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
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.UnsupportedBindingRemoved);
    }

    [Fact]
    public void Transform_WhenUnsupportedRemovalDisabled_ReportsUnsupportedEndpointsWithoutRemovingThem()
    {
        var result = Transform(
            """
            <system.serviceModel>
              <services>
                <service name="Services.Foo">
                  <endpoint address="" binding="netMsmqBinding" contract="Contracts.IQueue" />
                </service>
              </services>
            </system.serviceModel>
            """,
            new LegacyWcfServiceModelTransformOptions
            {
                RemoveUnsupportedConfiguration = false
            });

        Assert.NotNull(result.ServiceModel.Element("services")?.Element("service")?.Element("endpoint"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.UnsupportedBinding);
    }

    [Fact]
    public void Transform_WhenUnsupportedRemovalDisabled_PreservesHostElement()
    {
        var result = Transform(
            """
            <system.serviceModel>
              <services>
                <service name="Services.Foo">
                  <host>
                    <baseAddresses>
                      <add baseAddress="http://localhost:8080/Service" />
                    </baseAddresses>
                  </host>
                  <endpoint address="" binding="basicHttpBinding" contract="Contracts.IFoo" />
                </service>
              </services>
            </system.serviceModel>
            """,
            new LegacyWcfServiceModelTransformOptions
            {
                RemoveUnsupportedConfiguration = false
            });

        Assert.NotNull(result.ServiceModel.Element("services")?.Element("service")?.Element("host"));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.HostElementRemoved);
    }

    [Fact]
    public void Transform_ReportsDuplicateBindingConfigurations()
    {
        var result = Transform(
            """
            <system.serviceModel>
              <bindings>
                <basicHttpBinding>
                  <binding name="Duplicate" />
                  <binding name="Duplicate" />
                </basicHttpBinding>
              </bindings>
            </system.serviceModel>
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.DuplicateBindingConfiguration);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void Transform_RemovesUnsupportedProxyCredentialTypeFromSecurityTransport()
    {
        var result = Transform(
            """
            <system.serviceModel>
              <bindings>
                <basicHttpBinding>
                  <binding name="Basic">
                    <security mode="Transport">
                      <transport clientCredentialType="None" proxyCredentialType="None" realm="example" />
                    </security>
                  </binding>
                </basicHttpBinding>
                <wsHttpBinding>
                  <binding name="Ws">
                    <security mode="Transport">
                      <transport clientCredentialType="None" proxyCredentialType="Ntlm" realm="example" />
                    </security>
                  </binding>
                </wsHttpBinding>
                <webHttpBinding>
                  <binding name="Web">
                    <security mode="Transport">
                      <transport clientCredentialType="None" proxyCredentialType="Windows" realm="example" />
                    </security>
                  </binding>
                </webHttpBinding>
              </bindings>
            </system.serviceModel>
            """);

        var transports = result.ServiceModel
            .Element("bindings")?
            .Elements()
            .Elements("binding")
            .Elements("security")
            .Elements("transport")
            .ToArray() ?? Array.Empty<XElement>();

        Assert.Equal(3, transports.Length);
        Assert.All(transports, transport =>
        {
            Assert.Null(transport.Attribute("proxyCredentialType"));
            Assert.NotNull(transport.Attribute("clientCredentialType"));
            Assert.NotNull(transport.Attribute("realm"));
        });
        Assert.Equal(
            3,
            result.Diagnostics.Count(diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.UnsupportedBindingAttributeRemoved));
        Assert.Contains(
            result.Diagnostics,
            diagnostic =>
                diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.UnsupportedBindingAttributeRemoved &&
                diagnostic.Message.Contains("proxyCredentialType", StringComparison.Ordinal) &&
                diagnostic.Message.Contains("transport", StringComparison.Ordinal) &&
                diagnostic.Message.Contains("basicHttpBinding", StringComparison.Ordinal) &&
                diagnostic.Message.Contains("Basic", StringComparison.Ordinal));
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
        Assert.Equal(2, result.Diagnostics.Count(diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.ServiceNameGenerated));
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
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.ServiceNameChanged);
        Assert.Equal(2, result.Diagnostics.Count(diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.ServiceNameGenerated));
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
        Assert.Equal(2, result.Diagnostics.Count(diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.EndpointNameGenerated));
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
        Assert.Equal(2, result.Diagnostics.Count(diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.EndpointNameGenerated));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.EndpointNameChanged);
    }

    [Fact]
    public void Transform_DiagnosticsIncludeFormattedTransformationContext()
    {
        var result = Transform(
            """
            <system.serviceModel>
              <services>
                <service name="Services.Foo">
                  <host>
                    <baseAddresses>
                      <add baseAddress="http://localhost:8080/" />
                    </baseAddresses>
                  </host>
                  <endpoint binding="basicHttpBinding" contract="Contracts.IFoo" address="relative" />
                </service>
              </services>
            </system.serviceModel>
            """);

        Assert.Contains(
            result.Diagnostics,
            diagnostic =>
                diagnostic.Code == LegacyWcfServiceModelDiagnosticCodes.EndpointAddressResolved &&
                diagnostic.Message.Contains("Services.Foo", StringComparison.Ordinal) &&
                diagnostic.Message.Contains("basicHttpBinding_Contracts.IFoo", StringComparison.Ordinal) &&
                diagnostic.Message.Contains("relative", StringComparison.Ordinal) &&
                diagnostic.Message.Contains("http://localhost:8080/relative", StringComparison.Ordinal));
    }

    [Fact]
    public void Transform_FromFilePaths_ReplacesServiceModelAndPreservesConfigurationByDefault()
    {
        var legacyConfigurationPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.config");
        var generatedConfigurationPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.config");
        File.WriteAllText(
            legacyConfigurationPath,
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <appSettings>
                <add key="Environment" value="Test" />
              </appSettings>
              <system.serviceModel>
                <services>
                  <service name="Sample.Service">
                    <host>
                      <baseAddresses>
                        <add baseAddress="http://localhost:8080/Service" />
                      </baseAddresses>
                    </host>
                    <endpoint address="" binding="basicHttpBinding" contract="Sample.IService" />
                  </service>
                </services>
              </system.serviceModel>
              <connectionStrings>
                <add name="Default" connectionString="Data Source=.;Initial Catalog=Sample;" />
              </connectionStrings>
            </configuration>
            """);

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
            Assert.NotNull(generatedConfiguration.Root?.Element("appSettings"));
            Assert.NotNull(generatedConfiguration.Root?.Element("connectionStrings"));
            Assert.NotNull(generatedServiceModel);
            Assert.Equal(
                NormalizeXml(result.ServiceModel),
                NormalizeXml(generatedServiceModel));
            Assert.Null(generatedServiceModel.Element("services")?.Element("service")?.Element("host"));
        }
        finally
        {
            if (File.Exists(legacyConfigurationPath))
            {
                File.Delete(legacyConfigurationPath);
            }

            if (File.Exists(generatedConfigurationPath))
            {
                File.Delete(generatedConfigurationPath);
            }
        }
    }

    [Fact]
    public void Transform_FromFilePaths_WhenServiceModelOnly_WritesOnlyTransformedServiceModel()
    {
        var legacyConfigurationPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.config");
        var generatedConfigurationPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.config");
        File.WriteAllText(
            legacyConfigurationPath,
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <appSettings>
                <add key="Environment" value="Test" />
              </appSettings>
              <system.serviceModel>
                <services>
                  <service name="Sample.Service">
                    <host>
                      <baseAddresses>
                        <add baseAddress="http://localhost:8080/Service" />
                      </baseAddresses>
                    </host>
                    <endpoint address="" binding="basicHttpBinding" contract="Sample.IService" />
                  </service>
                </services>
              </system.serviceModel>
            </configuration>
            """);

        try
        {
            var transformer = new LegacyWcfServiceModelToCoreWcfTransformer();
            var result = transformer.Transform(
                legacyConfigurationPath,
                generatedConfigurationPath,
                new LegacyWcfServiceModelTransformOptions
                {
                    GeneratedConfigurationMode = GeneratedConfigurationMode.ServiceModelOnly
                });

            var generatedConfiguration = XDocument.Load(generatedConfigurationPath);
            var generatedServiceModel = generatedConfiguration.Root?.Element("system.serviceModel");

            Assert.True(File.Exists(generatedConfigurationPath));
            Assert.Equal("configuration", generatedConfiguration.Root?.Name.LocalName);
            Assert.Null(generatedConfiguration.Root?.Element("appSettings"));
            Assert.NotNull(generatedServiceModel);
            Assert.Equal(
                NormalizeXml(result.ServiceModel),
                NormalizeXml(generatedServiceModel));
        }
        finally
        {
            if (File.Exists(legacyConfigurationPath))
            {
                File.Delete(legacyConfigurationPath);
            }

            if (File.Exists(generatedConfigurationPath))
            {
                File.Delete(generatedConfigurationPath);
            }
        }
    }

    [Fact]
    public void Diagnostics_WriteTo_WritesEachDiagnostic()
    {
        var diagnostics = new[]
        {
            new LegacyWcfServiceModelDiagnostic(
                LegacyWcfServiceModelDiagnosticSeverity.Warning,
                "CWCF999",
                "Test diagnostic."),
        };
        using var writer = new StringWriter();

        diagnostics.WriteTo(writer);

        Assert.Equal(
            $"CWCF999 Warning: Test diagnostic.{Environment.NewLine}",
            writer.ToString());
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

    private static LegacyWcfServiceModelTransformResult Transform(
        string serviceModelXml,
        LegacyWcfServiceModelTransformOptions options = null)
    {
        var transformer = new LegacyWcfServiceModelToCoreWcfTransformer();
        return transformer.Transform(XElement.Parse(serviceModelXml), options);
    }

    private static string NormalizeXml(XElement element)
    {
        return XElement.Parse(element.ToString(SaveOptions.DisableFormatting))
            .ToString(SaveOptions.DisableFormatting);
    }
}
