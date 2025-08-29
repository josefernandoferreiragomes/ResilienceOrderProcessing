using LoggingApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoggingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeatureToggleController : ControllerBase
{
    private readonly IFeatureToggleService _featureToggleService;

    public FeatureToggleController(IFeatureToggleService featureToggleService)
    {
        _featureToggleService = featureToggleService;
    }

    [HttpGet]
    public IActionResult GetAllFeatures()
    {
        var features = _featureToggleService.GetAllFeatures();
        return Ok(features);
    }

    [HttpGet("{featureName}")]
    public IActionResult GetFeature(string featureName)
    {
        var isEnabled = _featureToggleService.IsFeatureEnabled(featureName);
        return Ok(new { FeatureName = featureName, Enabled = isEnabled });
    }

    [HttpPost("{featureName}/toggle")]
    public IActionResult ToggleFeature(string featureName, [FromBody] bool enabled)
    {
        _featureToggleService.SetFeatureState(featureName, enabled);
        return Ok(new { FeatureName = featureName, Enabled = enabled, Message = "Feature state updated successfully" });
    }
}
