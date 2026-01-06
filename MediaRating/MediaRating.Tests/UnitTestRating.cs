using System;
using System.Collections.Generic;
using Moq;
using Xunit;
using MediaRating.Api.Controller;
using MediaRating.DTOs;
using MediaRating.Infrastructure;
using MediaRating.Model;
using Assert = Xunit.Assert;

namespace MediaRating.Tests;

public class RatingControllerTests
{
    private static User MakeUser(Guid guid, string username)
        => new User(1, username, "") { Guid = guid };

    [Test]
    public void Create_Returns_400_When_StarsOutOfRange()
    {
        var db = new Mock<IMediaRatingContext>();
        var controller = new RatingController(db.Object);

        var dto = new RatingCreateDto(Guid.NewGuid(), Guid.NewGuid(), 6, "x");
        var (msg, status, err) = controller.Create(dto);

        Assert.Equal(400, status);
        Assert.Equal("Stars must be 1 to 5", err);
    }

    [Test]
    public void Create_Returns_409_When_AlreadyExists()
    {
        var u = Guid.NewGuid();
        var m = Guid.NewGuid();

        var db = new Mock<IMediaRatingContext>();
        db.Setup(x => x.Ratings_Exists(u, m)).Returns(true);

        var controller = new RatingController(db.Object);

        var dto = new RatingCreateDto(u, m, 5, "nice");
        var (msg, status, err) = controller.Create(dto);

        Assert.Equal(409, status);
        Assert.Equal("You already gave a rating to this MediaEntry", err);
    }

    [Test]
    public void Create_Returns_201_When_Ok_And_Calls_Insert()
    {
        var u = Guid.NewGuid();
        var m = Guid.NewGuid();

        var db = new Mock<IMediaRatingContext>();
        db.Setup(x => x.Ratings_Exists(u, m)).Returns(false);
        db.Setup(x => x.Ratings_Insert(u, m, 4, "ok"));

        var controller = new RatingController(db.Object);

        var dto = new RatingCreateDto(u, m, 4, "ok");
        var (msg, status, err) = controller.Create(dto);

        Assert.Equal(201, status);
        Assert.Null(err);
        Assert.Equal("created", msg);

        db.Verify(x => x.Ratings_Insert(u, m, 4, "ok"), Times.Once);
    }

    [Test]
    public void UpdateRating_Returns_400_When_StarsInvalid()
    {
        var db = new Mock<IMediaRatingContext>();
        var controller = new RatingController(db.Object);

        var requester = MakeUser(Guid.NewGuid(), "max");
        var dto = new RatingUpdateDto(0, "bad");

        var (item, status, err) = controller.UpdateRating(Guid.NewGuid(), dto, requester);

        Assert.Equal(400, status);
        Assert.Equal("Stars must be between 1 and 5", err);
        Assert.Null(item);
    }

    [Test]
    public void DeleteRating_Returns_204_When_Ok()
    {
        var ratingGuid = Guid.NewGuid();
        var requester = MakeUser(Guid.NewGuid(), "max");

        var db = new Mock<IMediaRatingContext>();
        db.Setup(x => x.Rating_Delete(ratingGuid, requester.Guid)).Returns(true);

        var controller = new RatingController(db.Object);

        var (ok, status, err) = controller.DeleteRating(ratingGuid, requester);

        Assert.True(ok);
        Assert.Equal(204, status);
        Assert.Null(err);

        db.Verify(x => x.Rating_Delete(ratingGuid, requester.Guid), Times.Once);
    }
}
