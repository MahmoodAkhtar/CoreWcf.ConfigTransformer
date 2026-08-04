using CoreWcf.ConfigTransformer.SampleHost.Contracts;

namespace CoreWcf.ConfigTransformer.SampleHost.Services;

public sealed class EvalService : IEvalService
{
    public string Echo(string value) => value;
}
