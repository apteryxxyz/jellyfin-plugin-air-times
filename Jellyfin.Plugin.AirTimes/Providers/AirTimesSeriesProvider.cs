using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using J2N;
using Jellyfin.Plugin.AirTimes.Services;
using Jellyfin.Plugin.AirTimes.Utilities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AirTimes.Providers;

/// <summary>
/// Air Times series provider.
/// </summary>
public class AirTimesSeriesProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
  : IRemoteMetadataProvider<Series, SeriesInfo>
{
  private readonly ILogger<AirTimesSeriesProvider> logger = loggerFactory.CreateLogger<AirTimesSeriesProvider>();
  private readonly Tvdb tvdb = new(httpClientFactory, loggerFactory);

  /// <inheritdoc/>
  public string Name => "Air Times (TheTVDB)";

  /// <inheritdoc/>
  public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
  {
    var metadata = new MetadataResult<Series>();

    int seriesId = info.ProviderIds.GetPropertyAsInt32("Tvdb");
    if (seriesId is 0)
    {
      var searchTerm = info.Year is null ? info.Name : $"{info.Name} ({info.Year})";
      var newSeriesId = await tvdb.GetSeriesId(searchTerm, cancellationToken).ConfigureAwait(false);
      if (newSeriesId is null)
      {
        logger.LogWarning("Could not retrieve TVDB ID for \"{Name}\"", searchTerm);
        return metadata;
      }
      seriesId = newSeriesId.Value;
    }

    var airDates = await tvdb
      .GetSeriesSeasonAirDates(seriesId, -1, cancellationToken)
      .ConfigureAwait(false);
    if (airDates.Count == 0)
    {
      logger.LogWarning("No air dates found for series \"{Name}\"", info.Name);
      return metadata;
    }

    var utcSchedule = Schedule.FromDateTimes(airDates);
    var localSchedule = Schedule.FromSchedule(utcSchedule, TimeZoneInfo.Local);

    metadata.Item = new Series
    {
      AirDays = [.. localSchedule.Days],
      AirTime = localSchedule.Time.ToString("hh\\:mm", CultureInfo.InvariantCulture)
    };
    metadata.HasMetadata = true;

    return metadata;
  }

  /// <summary>
  /// Not implemented.
  /// </summary>
  public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo info, CancellationToken cancellationToken)
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