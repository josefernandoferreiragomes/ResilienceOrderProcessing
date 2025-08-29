namespace LoggingApi.Services;

public interface IFeatureToggleService
{
    bool IsFeatureEnabled(string featureName);
    void SetFeatureState(string featureName, bool enabled);
    Dictionary<string, bool> GetAllFeatures();
}

public class FeatureToggleService : IFeatureToggleService
{
    private readonly Dictionary<string, bool> _features = new()
    {
        { "RealTimeLogging", true },
        { "DetailedErrorLogging", true },
        { "PerformanceLogging", false },
        { "SecurityLogging", true }
    };

    public bool IsFeatureEnabled(string featureName)
    {
        return _features.GetValueOrDefault(featureName, false);
    }

    public void SetFeatureState(string featureName, bool enabled)
    {
        _features[featureName] = enabled;
    }

    public Dictionary<string, bool> GetAllFeatures()
    {
        return new Dictionary<string, bool>(_features);
    }
}