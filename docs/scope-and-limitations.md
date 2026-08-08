# Scope And Limitations

`CoreWcf.ConfigTransformer` is a migration helper for legacy WCF service configuration. It is not a full WCF-to-CoreWCF migration engine.

## Intended Scope

The transformer focuses on:

- service-side `<system.serviceModel>` XML
- known binding collections used by common CoreWCF migrations
- endpoint address normalization
- listener discovery for HTTP, HTTPS, and Net.TCP
- diagnostics for manual migration review

The transformer deliberately avoids generating application hosting code. A developer still needs to wire CoreWCF services, endpoints, middleware, and packages into the target application.

## Not A CoreWCF Support Matrix

CoreWCF support depends on:

- CoreWCF package version
- installed CoreWCF transport and feature packages
- target .NET runtime
- whether configuration is loaded through CoreWCF configuration APIs or manually mapped in code

This project does not currently maintain a complete CoreWCF configuration compatibility matrix. A removed element should be interpreted as "not handled by this transformer", not as final proof that CoreWCF cannot support it.

## Current Support Assumptions

The current transformer recognizes these binding families:

- `basicHttpBinding`
- `netTcpBinding`
- `webHttpBinding`
- `wsHttpBinding`

It derives listener addresses for:

- HTTP
- HTTPS
- Net.TCP

Other transports or package-backed bindings may be valid for a specific CoreWCF application, but they are not currently modeled by this transformer.

## Recommended Review Workflow

1. Run the transformer with default options.
2. Review every diagnostic.
3. Compare removed or preserved configuration against the CoreWCF package set used by the target application.
4. Add missing CoreWCF packages or manual hosting code.
5. Run integration tests against real WCF clients where possible.

For exploratory migrations, consider setting `RemoveUnsupportedConfiguration` to `false`. This preserves more of the original XML while still reporting unsupported endpoints that the transformer cannot classify.

## When To Extend The Transformer

Extend the transformer when a repeated migration pattern is known and testable. Good candidates are:

- adding a binding descriptor for a CoreWCF-supported binding package
- preserving a known-compatible behavior element
- removing a known-incompatible attribute with a precise diagnostic
- converting a common WCF setting into a CoreWCF-friendly equivalent

Avoid adding broad removal rules without source-backed evidence. Broad cleanup can make generated config look cleaner while hiding migration decisions that should remain visible.

## Future Work

A stronger future version could include a versioned compatibility matrix for CoreWCF packages. That work should be based on CoreWCF source, package tests, and targeted probes rather than assumptions from legacy WCF behavior.
