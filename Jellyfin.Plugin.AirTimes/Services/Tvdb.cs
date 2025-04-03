using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AirTimes.Services;

/// <summary>
/// Tvdb service.
/// </summary>
/// <param name="httpClientFactory">Http client factory.</param>
/// <param name="loggerFactory">Logger factory.</param>
public class Tvdb(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
{
  private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
  private readonly ILogger<Tvdb> logger = loggerFactory.CreateLogger<Tvdb>();

  /// <summary>
  /// Get the Tvdb series ID from a name and optional year.
  /// </summary>
  /// <param name="term">Name and year of series.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns></returns>
  public async Task<int?> GetSeriesId(string term, CancellationToken cancellationToken)
  {
    var url = $"https://skyhook.sonarr.tv/v1/tvdb/search/en/?term={term}";
    var document = await GetJson(url, cancellationToken).ConfigureAwait(false);
    var shows = document.RootElement;

    if (shows.GetArrayLength() <= 0)
    {
      logger.LogWarning("No series found for \"{Term}\"", term);
      return null;
    }

    var show = shows[0];
    var tvdbId = show.GetProperty("tvdbId").GetInt32();

    return tvdbId;
  }

  /// <summary>
  /// Get the air dates for a series season.
  /// </summary>
  /// <param name="seriesId">The Tvdb series ID.</param>
  /// <param name="seasonNumber">A season number, -1 for the latest.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>A list of air dates.</returns>
  public async Task<List<DateTime>> GetSeriesSeasonAirDates(int seriesId, int seasonNumber, CancellationToken cancellationToken)
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

  /// <summary>
  /// Get the air date for a given episode.
  /// </summary>
  /// <param name="seriesId">Series ID the episode belongs to.</param>
  /// <param name="episodeResolvable">ID or title of the episode.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The air date.</returns>
  public async Task<DateTime?> GetEpisodeAirDate(int seriesId, object episodeResolvable, CancellationToken cancellationToken)
  {
    var url = $"https://skyhook.sonarr.tv/v1/tvdb/shows/en/{seriesId}";
    var document = await GetJson(url, cancellationToken).ConfigureAwait(false);
    var show = document.RootElement;

    if (!show.TryGetProperty("episodes", out var episodes) || episodes.GetArrayLength() == 0)
    {
      logger.LogWarning("No episodes found for series {SeriesId}", seriesId);
      return null;
    }

    JsonElement? relevantEpisode = null;
    if (episodeResolvable is int episodeId)
    {
      relevantEpisode = episodes.EnumerateArray()
        .First(e => e.GetProperty("tvdbId").GetInt32() == episodeId);
    }
    else if (episodeResolvable is string episodeTitle)
    {
      relevantEpisode = episodes.EnumerateArray()
        .First(e => e.GetProperty("title").GetString() == episodeTitle);
    }

    if (relevantEpisode is null)
    {
      logger.LogWarning("No episode found for {EpisodeTitle}", episodeResolvable);
      return null;
    }

    if (!relevantEpisode.Value.TryGetProperty("airDateUtc", out var airDateUtc))
    {
      logger.LogWarning("No air date found for episode {EpisodeTitle}", episodeResolvable);
      return null;
    }

    var airDate = airDateUtc.GetString()!;
    var airDateTime = DateTime.Parse(airDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

    return airDateTime;
  }

  private async Task<JsonDocument> GetJson(string url, CancellationToken cancellationToken)
  {
    try
    {
      using var httpClient = httpClientFactory.CreateClient();

      var response = await httpClient
        .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
        .ConfigureAwait(false);
      response.EnsureSuccessStatusCode();

      var content = await response.Content
        .ReadAsStringAsync(cancellationToken)
        .ConfigureAwait(false);
      var jsonDoc = JsonDocument.Parse(content);

      return jsonDoc;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting data from {Url}", url);
      throw;
    }
  }
}