using System;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Events.Consumers.SyncPlay
{
    /// <summary>
    /// Creates an entry in the activity log henever a user starts playback.
    /// </summary>
    public class SyncPlayStartLogger : IEventConsumer<SyncPlayStartEventArgs>
    {
        private readonly ILogger<SyncPlayStartLogger> _logger;
        private readonly ILocalizationManager _localizationManager;
        private readonly IActivityManager _activityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncPlayStartLogger"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public SyncPlayStartLogger(ILogger<SyncPlayStartLogger> logger, ILocalizationManager localizationManager, IActivityManager activityManager)
        {
            _logger = logger;
            _localizationManager = localizationManager;
            _activityManager = activityManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(SyncPlayStartEventArgs eventArgs)
        {
            _logger.LogWarning("SyncPlayStart event triggered.");

            if (eventArgs.MediaInfo is null)
            {
                _logger.LogWarning("PlaybackStart reported with null media info.");
                return;
            }

            if (eventArgs.Item is not null && eventArgs.Item.IsThemeMedia)
            {
                // Don't report theme song or local trailer playback
                return;
            }

            if (eventArgs.Users.Count == 0)
            {
                return;
            }

            var user = eventArgs.Users[0];

            await _activityManager.CreateAsync(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("UserStartedPlayingItemWithValues"),
                    user.Username,
                    GetItemName(eventArgs.MediaInfo),
                    eventArgs.DeviceName),
                GetPlaybackNotificationType(eventArgs.MediaInfo.MediaType),
                user.Id)
            {
                ItemId = eventArgs.Item?.Id.ToString("N", CultureInfo.InvariantCulture),
            })
            .ConfigureAwait(false);
        }

        private static string GetItemName(BaseItemDto item)
        {
            var name = item.Name;

            if (!string.IsNullOrEmpty(item.SeriesName))
            {
                name = item.SeriesName + " - " + name;
            }

            if (item.Artists is not null && item.Artists.Count > 0)
            {
                name = item.Artists[0] + " - " + name;
            }

            return name;
        }

        private static string GetPlaybackNotificationType(MediaType mediaType)
        {
            if (mediaType == MediaType.Audio)
            {
                return NotificationType.AudioPlayback.ToString();
            }

            if (mediaType == MediaType.Video)
            {
                return NotificationType.VideoPlayback.ToString();
            }

            return "Playback";
        }
    }
}
