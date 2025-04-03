using System;
using Jellyfin.Plugin.AirTimes.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.AirTimes;

/// <summary>
/// Jellyfin Air Times plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>
{
  /// <summary>
  /// Initialises a new instance of the Plugin class.
  /// </summary>
  public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
    : base(applicationPaths, xmlSerializer)
  {
    Instance = this;
  }

  /// <summary>
  /// Gets the singleton instance of the plugin.
  /// </summary>
  public static Plugin? Instance { get; private set; }

  /// <inheritdoc/>
  public override Guid Id => Guid.Parse("27b03c3b-9e2a-4f47-941a-82bbb5beedd8");

  /// <inheritdoc/>
  public override string Name => "Air Times";

  /// <inheritdoc/>
  public override string Description => "Localised series air times from TheTVDB.";
}
