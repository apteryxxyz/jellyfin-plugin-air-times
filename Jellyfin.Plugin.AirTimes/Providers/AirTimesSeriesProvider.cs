using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AirTimes.Providers;

/// <summary>
/// Air Times series provider.
/// </summary>
public class AirTimesSeriesProvider(IHttpClientFactory httpClientFactory, ILogger<AirTimesSeriesProvider> logger)
  : IRemoteMetadataProvider<Series, SeriesInfo>, IHasOrder
{
  /// <inheritdoc/>
  public string Name => "Air Times (TheTVDB)";

  /// <inheritdoc/>
  public int Order => -1;

  /// <inheritdoc/>
  public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
  {
    var metadata = new MetadataResult<Series>();

    var seriesId = await GetSeriesId(info.Name, info.Year, cancellationToken).ConfigureAwait(false);
    if (seriesId == null)
    {
      logger.LogWarning("Could not retrieve series by \"{Name}\"", info.Name);
      return metadata;
    }

    var airTimes = await GetSeriesSeasonAirDates(seriesId.Value, -1, cancellationToken).ConfigureAwait(false);
    if (airTimes.Count == 0)
    {
      logger.LogWarning("No air dates found for series \"{Name}\"", info.Name);
      return metadata;
    }

    var utcSchedule = Schedule.FromDateTimes(airTimes);
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
  /// Get the Tvdb series ID from a name and optional year.
  /// </summary>
  /// <param name="name">Name of series</param>
  /// <param name="year">Optional year of release for series</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns></returns>
  private async Task<int?> GetSeriesId(string name, int? year, CancellationToken cancellationToken)
  {
    var term = year is null ? name : $"{name} ({year})";
    var url = $"https://skyhook.sonarr.tv/v1/tvdb/search/en/?term={term}";
    var document = await GetJson(url, cancellationToken).ConfigureAwait(false);
    var shows = document.RootElement;

    if (shows.ValueKind == JsonValueKind.Array && shows.GetArrayLength() > 0)
    {
      var show = shows[0];
      if (show.TryGetProperty("tvdbId", out var tvdbId))
      {
        return tvdbId.GetInt32();
      }
    }

    return null;
  }

  /// <summary>
  /// Get the air dates for a series season.
  /// </summary>
  /// <param name="seriesId">The Tvdb series ID.</param>
  /// <param name="seasonNumber">A season number, -1 for the latest.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>A list of air dates.</returns>
  private async Task<List<DateTime>> GetSeriesSeasonAirDates(int seriesId, int seasonNumber, CancellationToken cancellationToken)
  {
    var url = $"https://skyhook.sonarr.tv/v1/tvdb/shows/en/{seriesId}";
    var document = await GetJson(url, cancellationToken).ConfigureAwait(false);
    var show = document.RootElement;

    // Get the latest season number if -1 is passed
    if (seasonNumber == -1)
    {
      if (!show.TryGetProperty("seasons", out var seasons) || seasons.GetArrayLength() == 0)
      {
        logger.LogWarning("No seasons found for series {SeriesId}", seriesId);
        return [];
      }

      seasonNumber = seasons.EnumerateArray()
        .Last().GetProperty("seasonNumber").GetInt32();
    }

    var episodes = show.GetProperty("episodes").EnumerateArray()
      .Where(e => e.GetProperty("seasonNumber").GetInt32() == seasonNumber) // Only get episodes for the season
      .Where(e => e.TryGetProperty("airDateUtc", out _)); // Only get episodes with air dates

    var airDates = episodes
      .Select(e => e.GetProperty("airDateUtc").GetString()!) // Map by air date UTC
      .Select(s => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)) // Parse to DateTime
      .ToList();

    return airDates;
  }

  private async Task<JsonDocument> GetJson(string url, CancellationToken cancellationToken)
  {
    try
    {
      using var client = httpClientFactory.CreateClient();
      var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

      response.EnsureSuccessStatusCode();
      var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

      return JsonDocument.Parse(content);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting data from {Url}", url);
      throw;
    }
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