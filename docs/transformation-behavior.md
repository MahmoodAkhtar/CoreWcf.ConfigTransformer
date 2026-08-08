# Transformation Behavior

This document describes what the transformer currently does. It is not a complete statement of CoreWCF support.

When this document says "unsupported", it means "unsupported by this transformer rule set", not necessarily "unsupported by every CoreWCF package or version".

## Rule Order

Rules currently run in this order:

1. Remove unsupported configuration when enabled.
2. Ensure service names are present and unique.
3. Remove or report endpoints that use unrecognized bindings.
4. Ensure endpoint names are present and unique.
5. Resolve endpoint addresses and derive listener addresses.

Rule order matters. For example, unsupported endpoints are removed before endpoint names are generated, so removed endpoints do not receive generated names.

## Recognized Bindings

The transformer currently recognizes these binding collections:

- `<basicHttpBinding>`
- `<netTcpBinding>`
- `<webHttpBinding>`
- `<wsHttpBinding>`

Recognized bindings are used for endpoint filtering and listener transport discovery.

Bindings outside this set are treated as unrecognized by the transformer. If `RemoveUnsupportedConfiguration` is `true`, their binding collections and endpoints are removed. If it is `false`, endpoints are preserved and diagnostics are emitted.

## Removed Top-Level Sections

When `RemoveUnsupportedConfiguration` is `true`, the transformer preserves only these direct children of `<system.serviceModel>`:

- `<bindings>`
- `<services>`

Other top-level sections are removed with diagnostics. Examples include:

- `<behaviors>`
- `<client>`
- `<diagnostics>`
- `<extensions>`
- `<protocolMapping>`
- `<serviceHostingEnvironment>`

This behavior is intentionally conservative for this transformer. It does not mean every removed section is impossible to use with CoreWCF.

## Removed Binding Content

The transformer removes these known binding details:

- `<reliableSession>` elements under binding configurations.
- `proxyCredentialType` attributes under `<security><transport>`.

Other binding elements and attributes may be preserved even if they require manual CoreWCF review.

## Service Host Elements

Service `<host>` elements are used to read base addresses. After endpoint addresses are resolved, `<host>` is removed when `RemoveUnsupportedConfiguration` is `true`.

Set `RemoveUnsupportedConfiguration` to `false` if you want to preserve the original `<host>` element for manual review.

## Name Generation

Services without a `name` attribute receive one from the first endpoint `contract` value when possible. If no contract is available, the transformer generates `Service{n}`.

Endpoints without a `name` attribute receive one from `{binding}_{contract}` when both values are available. If either value is missing, the transformer generates `Endpoint{n}`.

Duplicate service and endpoint names are made unique by appending numeric suffixes such as `_2`.

## Address Resolution

Absolute endpoint addresses are preserved and used directly for listener discovery.

Relative or empty endpoint addresses are resolved against the first matching base address for the endpoint transport:

- HTTP bindings resolve against `http://` base addresses.
- HTTP bindings with `security mode="Transport"` or `security mode="TransportWithMessageCredential"` resolve against `https://` base addresses.
- Net.TCP bindings resolve against `net.tcp://` base addresses.

If a relative endpoint has no matching base address, the transformer emits an error diagnostic.

## Output File Modes

The path-based overload supports two output modes:

- `ReplaceServiceModelInConfiguration`: preserves the original configuration document and replaces only `<system.serviceModel>`.
- `ServiceModelOnly`: writes a new `<configuration>` document containing only the transformed `<system.serviceModel>`.

`ReplaceServiceModelInConfiguration` is the default because it is the least surprising behavior when the input is a full `.config` file.

## Diagnostics

Diagnostics are emitted for:

- unsupported or removed configuration
- duplicate binding configurations
- invalid or missing base addresses
- generated or changed service names
- generated or changed endpoint names
- resolved relative endpoint addresses

Warnings generally indicate lossy transformation decisions. Errors indicate cases where the transformer could not produce a reliable result for part of the configuration.
