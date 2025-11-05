using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFaceTracking.Core.Library;
using VRCFaceTracking.Core.Params.Data;

[assembly: TypeForwardedTo(typeof(VRCFaceTracking.ExtTrackingModule))]
[assembly: TypeForwardedTo(typeof(VRCFaceTracking.ModuleMetadata))]
[assembly: TypeForwardedTo(typeof(ModuleState))]

namespace VRCFaceTracking.Core;

public class MainStandalone : IMainService
{
    private readonly ILogger<MainStandalone> _logger;
    private readonly ILibManager _libManager;
    private readonly UnifiedTrackingMutator _mutator;

    public Action<string, float> ParameterUpdate { get; set; } = (_, _) => { };

    public MainStandalone(
        ILogger<MainStandalone> logger,
        ILibManager libManager,
        UnifiedTrackingMutator mutator
        )
    {
        _logger = logger;
        _libManager = libManager;
        _mutator = mutator;
    }

    public async Task Teardown()
    {
        _logger.LogInformation("SomniumSpaceFaceTracking Standalone Exiting!");
        await _mutator.Save();

        _libManager.TeardownAllAndResetAsync();

        if (OperatingSystem.IsWindows())
        {
            _logger.LogDebug("Resetting our time end period...");
            var timeEndRes = Utils.TimeEndPeriod(1);
            if (timeEndRes != 0)
            {
                _logger.LogWarning($"TimeEndPeriod failed with HRESULT {timeEndRes}");
            }
        }

        _logger.LogDebug("Teardown complete. Awaiting exit...");
    }

    public Task InitializeAsync()
    {
        SomniumSpace.EnsureVRCOSCDirectory();

        // Ensure OSC is enabled
        var isWindows = OperatingSystem.IsWindows();

        //if (isWindows && SomniumSpace.ForceEnableOsc()) // If osc was previously not enabled
        //{
        //    _logger.LogWarning("SomniumSpaceFaceTracking detected OSC was disabled and automatically enabled it.");
        //    // If we were launched after VRChat
        //    if (SomniumSpace.IsSomniumSpaceRunning())
        //        _logger.LogError(
        //            "However, SomniumSpace was running while this change was made.\n" +
        //            "If parameters do not update, please restart SomniumSpace or manually enable OSC yourself in your avatar's expressions menu.");
        //}

        _mutator.Load();

        // Begin main OSC update loop
        _logger.LogDebug("Starting OSC update loop...");

        if (isWindows)
        {
            Utils.TimeBeginPeriod(1);
        }

        return Task.CompletedTask;
    }
}
