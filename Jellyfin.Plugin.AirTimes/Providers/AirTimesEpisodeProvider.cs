using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using J2N;
using Jellyfin.Plugin.AirTimes.Services;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AirTimes.Providers;

/// <summary>
/// Air Times episodes provider.
/// </summary>
public class AirTimesEpisodeProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
  : IRemoteMetadataProvider<Episode, EpisodeInfo>
{
  private readonly ILogger<AirTimesEpisodeProvider> logger = loggerFactory.CreateLogger<AirTimesEpisodeProvider>();
  private readonly Tvdb tvdb = new(httpClientFactory, loggerFactory);

  /// <inheritdoc/>
  public string Name => "Air Times (TheTVDB)";

  /// <inheritdoc/>
  public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
  {
    var metadata = new MetadataResult<Episode>();

    int seriesId = info.SeriesProviderIds.GetPropertyAsInt32("Tvdb");
    if (seriesId is 0)
    {
      logger.LogWarning("Could not retrieve TVDB ID for \"{Name}\"", info.Name);
      return metadata;
    }

    object episodeResolvable = info.ProviderIds.GetPropertyAsInt32("Tvdb") is 0
      ? info.Name
      : info.ProviderIds.GetPropertyAsInt32("Tvdb");
    var episodeAirDate = await tvdb
      .GetEpisodeAirDate(seriesId, episodeResolvable, cancellationToken)
      .ConfigureAwait(false);
    if (episodeAirDate is null)
    {
      logger.LogWarning("Could not retrieve episode air date for \"{Name}\"", episodeResolvable);
      return metadata;
    }

    metadata.Item = new Episode
    {
      PremiereDate = episodeAirDate
    };
    metadata.HasMetadata = true;

    return metadata;
  }

  /// <summary>
  /// Not implemented.
  /// </summary>
  public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo info, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  /// <summary>
  /// Not implemented.
  /// </summary>
  public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}