// Datei: Infrastructure/IMediaRatingContext.cs
using System;
using System.Collections.Generic;
using MediaRating.DTOs;
using MediaRating.Model;

namespace MediaRating.Infrastructure
{
    public interface IMediaRatingContext
    {
        // Users
        List<User> Users_GetAll();
        User? Users_FindByUsername(string username);
        User? Users_FindByGuid(Guid guid);
        User Users_Insert(string username, string passwordHash);

        // Media
        List<MediaEntry> Media_GetAll();
        MediaEntry? Media_GetByGuid(Guid guid);
        MediaEntry Media_Create(MediaEntryDto dto, User creator);
        MediaEntry? Media_Update(Guid mediaGuid, MediaUpdateDto dto, Guid ownerGuid);
        MediaEntry? Media_Delete(Guid guid, Guid ownerGuid);
        (double Avg, int Count)? Get_MediaAvg(Guid mediaGuid);

        // Ratings
        bool Ratings_Exists(Guid userGuid, Guid mediaGuid);
        void Ratings_Insert(Guid userGuid, Guid mediaGuid, int stars, string? comment);
        List<Rating> Ratings_GetForMedia(Guid mediaGuid);
        List<Rating> Ratings_GetForUser(Guid userGuid);
        Rating? Rating_Update(Guid ratingGuid, Guid ownerGuid, RatingUpdateDto dto);
        bool Rating_Delete(Guid ratingGuid, Guid ownerGuid);
    }
}
