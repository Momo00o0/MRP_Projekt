using MediaRating.Api.Controller;
using MediaRating.DTOs;
using MediaRating.Infrastructure;
using MediaRating.Model;
using Moq;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using Xunit;
using Assert = Xunit.Assert;

namespace MediaRating.Tests;

public class MediaControllerTests
{
    private static User MakeUser(int id, Guid guid, string username)
        => new User(id, username, "") { Guid = guid };

    private static MediaEntry MakeMovie(MediaEntryDto dto, Guid mediaGuid, User creator)
    {
        var m = new Movie(dto.Title, dto.Description, dto.ReleaseYear, dto.AgeRestriction, creator) { Guid = mediaGuid };
        return m;
    }

    [Test]
    public void GetAll_Returns_200_And_Items()
    {
        var db = new Mock<IMediaRatingContext>();
        db.Setup(x => x.Media_GetAll()).Returns(new List<MediaEntry>());

        var controller = new MediaController(db.Object);

        var (items, status, err) = controller.GetAll();

        Assert.Equal(200, status);
        Assert.Null(err);
        Assert.NotNull(items);
        db.Verify(x => x.Media_GetAll(), Times.Once);
    }

    [Test]
    public void GetByGuid_Returns_404_When_NotFound()
    {
        var db = new Mock<IMediaRatingContext>();
        db.Setup(x => x.Media_GetByGuid(It.IsAny<Guid>())).Returns((MediaEntry?)null);

        var controller = new MediaController(db.Object);

        var (item, status, err) = controller.GetByGuid(Guid.NewGuid());

        Assert.Null(item);
        Assert.Equal(404, status);
        Assert.Equal("Media not found", err);
    }

    [Test]
    public void CreateMedia_Returns_400_When_DtoNull()
    {
        var db = new Mock<IMediaRatingContext>();
        var controller = new MediaController(db.Object);

        var (item, status, err) = controller.CreateMedia(null!, MakeUser(1, Guid.NewGuid(), "u"));

        Assert.Null(item);
        Assert.Equal(400, status);
        Assert.Equal("Body required", err);
    }

    [Test]
    public void CreateMedia_Returns_400_When_TitleMissing()
    {
        var db = new Mock<IMediaRatingContext>();
        var controller = new MediaController(db.Object);

        var dto = new MediaEntryDto("", "desc", 2020, new List<string>(), 12, Guid.NewGuid(), MediaKind.Movie);
        var (item, status, err) = controller.CreateMedia(dto, MakeUser(1, Guid.NewGuid(), "u"));

        Assert.Null(item);
        Assert.Equal(400, status);
        Assert.Equal("Title required", err);
    }

    [Test]
    public void CreateMedia_Returns_403_When_DtoUserGuid_DoesNotMatchRequester()
    {
        var db = new Mock<IMediaRatingContext>();
        var controller = new MediaController(db.Object);

        var requester = new User(1, "x", "") { Guid = Guid.NewGuid() };
        var dto = new MediaEntryDto("t", "desc", 2020, new List<string>(), 12, Guid.NewGuid(), MediaKind.Movie);

        var (item, status, err) = controller.CreateMedia(dto, requester);

        Assert.Null(item);
        Assert.Equal(403, status);
        Assert.Equal("Forbidden", err);
    }


    [Test]
    public void CreateMedia_Returns_201_When_Ok_And_Calls_Create()
    {
        var creator = MakeUser(5, Guid.NewGuid(), "max");
        var dto = new MediaEntryDto("t", "desc", 2020, new List<string>(), 12, creator.Guid, MediaKind.Movie);
        var created = MakeMovie(dto,Guid.NewGuid(), creator);

        var db = new Mock<IMediaRatingContext>();
        db.Setup(x => x.Media_Create(dto, creator)).Returns(created);

        var controller = new MediaController(db.Object);

        var (item, status, err) = controller.CreateMedia(dto, creator);

        Assert.Equal(201, status);
        Assert.Null(err);
        Assert.NotNull(item);
        Assert.Equal(created.Guid, item!.Guid);

        db.Verify(x => x.Media_Create(dto, creator), Times.Once);
    }

    [Test]
    public void UpdateMedia_Returns_403_When_NotOwner()
    {
        var mediaGuid = Guid.NewGuid();
        var owner = MakeUser(1, Guid.NewGuid(), "owner");
        var other = MakeUser(2, Guid.NewGuid(), "other");
        var existing = new Movie("T","D",2010,12 , owner);

        var dto = new MediaUpdateDto("new", "newdesc", 2022, 16, MediaKind.Movie);
        
        var db = new Mock<IMediaRatingContext>();
        db.Setup(x => x.Media_GetByGuid(mediaGuid)).Returns(existing);

        var controller = new MediaController(db.Object);

        var (item, status, err) = controller.UpdateMedia(mediaGuid, dto, other);

        Assert.Null(item);
        Assert.Equal(403, status);
        Assert.Equal("Forbidden your are not the owner", err);
    }

    [Test]
    public void DeleteMedia_Returns_403_When_NotOwner()
    {
        var mediaGuid = Guid.NewGuid();
        var owner = MakeUser(1, Guid.NewGuid(), "owner");
        var existing = new Movie("T", "D", 2010, 12, owner);

        var db = new Mock<IMediaRatingContext>();
        db.Setup(x => x.Media_GetByGuid(mediaGuid)).Returns(existing);

        var controller = new MediaController(db.Object);

        var (item, status, err) = controller.DeleteMedia(mediaGuid, Guid.NewGuid());

        Assert.Null(item);
        Assert.Equal(403, status);
        Assert.Equal("Forbidden, you are not the owner", err);
    }

    [Test]
    public void GetAverageRating()
    {
        var owner = new User(1, "owner", "") { Guid = Guid.NewGuid() };
        var movie = new Movie("Test", "Desc", 2020, 12, owner) { Guid = Guid.NewGuid() };

        var rater = new User(2, "rater", "") { Guid = Guid.NewGuid() };

        var r1 = new Rating(5, "top", DateTime.UtcNow, confirmed: true, creator: rater, media: movie) { Guid = Guid.NewGuid() };
        var r2 = new Rating(3, "ok", DateTime.UtcNow, confirmed: true, creator: rater, media: movie) { Guid = Guid.NewGuid() };

        movie.Ratings.Add(r1);
        movie.Ratings.Add(r2);

        var avg = movie.GetAverageScore();

        Assert.Equal(4.0, avg);
    }
}
