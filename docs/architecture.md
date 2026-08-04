# Architecture

`CoreWcf.ConfigTransformer` is a small transformation library for moving legacy WCF `<system.serviceModel>` configuration toward CoreWCF-hosted applications.

The transformer:

- Parses a configuration XML document.
- Keeps CoreWCF-relevant service and binding configuration.
- Removes WCF-only sections that are not consumed by a CoreWCF host.
- Derives listener base addresses for HTTP, HTTPS, and Net.TCP endpoints.
- Emits diagnostics instead of hiding lossy or unsupported transformation decisions.

The library targets `netstandard2.0` for broad compatibility and `net8.0` for modern consumers.
