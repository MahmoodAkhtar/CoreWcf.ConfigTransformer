# Transformation Rules

## Removed Sections

By default, the transformer keeps these `<system.serviceModel>` children:

- `<bindings>`
- `<services>`

All other top-level `<system.serviceModel>` sections are removed. Set `LegacyWcfServiceModelTransformOptions.RemoveUnsupportedConfiguration` to `false` to keep unsupported configuration.

Service `<host>` elements are removed after endpoint addresses are resolved against their matching base addresses.

Binding `<reliableSession>` elements are removed.

Unsupported binding sections and endpoints that reference unsupported bindings are removed. The transformer currently keeps:

- `<basicHttpBinding>`
- `<netTcpBinding>`
- `<webHttpBinding>`
- `<wsHttpBinding>`

## Listener Discovery

Listener addresses are derived from service host base addresses and endpoint bindings:

- `basicHttpBinding`, `wsHttpBinding`, and `webHttpBinding` map to HTTP.
- HTTP bindings with `security mode="Transport"` or `security mode="TransportWithMessageCredential"` map to HTTPS.
- `netTcpBinding` maps to Net.TCP.
- Absolute endpoint addresses are treated as listener addresses directly.

Unsupported bindings are reported as warning diagnostics.
