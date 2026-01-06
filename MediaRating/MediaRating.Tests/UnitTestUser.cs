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

public class UnitTestUser
{
    private static User MakeUser(int id, Guid guid, string username, string plainPassword)
    {
        var u = new User(id, username, "") { Guid = guid };
        u.Password = u.HashPassword(plainPassword); // hashed in Password field (so ComparePassword works)
        return u;
    }

    [Test]
    public void GetAllUsers_Returns_List()
    {
        var db = new Mock<IMediaRatingContext>();
        db.Setup(x => x.Users_GetAll()).Returns(new List<User> { new User(1, "a", "") { Guid = Guid.NewGuid() } });

        var controller = new UserController(db.Object);

        var users = controller.GetAllUsers();

        Assert.Single(users);
        db.Verify(x => x.Users_GetAll(), Times.Once);
    }

    [Test]
    public void AddUser_Returns_400_When_UsernameOrPasswordMissing()
    {
        var db = new Mock<IMediaRatingContext>();
        var controller = new UserController(db.Object);

        var (user, code, err) = controller.AddUser(new UserDto("", "", Guid.NewGuid()));

        Assert.Null(user);
        Assert.Equal(400, code);
        Assert.Equal("Username and password are required.", err);
    }

    [Test]
    public void AddUser_Returns_409_When_UserAlreadyExists()
    {
        var db = new Mock<IMediaRatingContext>();
        db.Setup(x => x.Users_FindByUsername("max")).Returns(new User(1, "max", ""));

        var controller = new UserController(db.Object);

        var (user, code, err) = controller.AddUser(new UserDto("max", "pw", Guid.NewGuid()));

        Assert.Null(user);
        Assert.Equal(409, code);
        Assert.Equal("User already exists.", err);
    }

    [Test]
    public void AddUser_Returns_201_And_Calls_Insert()
    {
        var fixedGuid = Guid.NewGuid();

        var db = new Mock<IMediaRatingContext>();
        db.Setup(x => x.Users_FindByUsername("max")).Returns((User?)null);
        db.Setup(x => x.Users_Insert(
                "max",
                It.Is<string>(h => !string.IsNullOrWhiteSpace(h) && h != "pw"),
                fixedGuid))
          .Returns(new User(7, "max", "") { Guid = fixedGuid });

        var controller = new UserController(db.Object);

        var (user, code, err) = controller.AddUser(new UserDto("max", "pw", fixedGuid));

        Assert.NotNull(user);
        Assert.Equal(201, code);
        Assert.Null(err);
        Assert.Equal("max", user!.Username);
        Assert.Equal(fixedGuid, user.Guid);

        db.Verify(x => x.Users_Insert("max", It.IsAny<string>(), fixedGuid), Times.Once);
    }

    [Test]
    public void Login_Returns_404_When_UserNotFound()
    {
        var db = new Mock<IMediaRatingContext>();
        db.Setup(x => x.Users_FindByUsername("max")).Returns((User?)null);

        var controller = new UserController(db.Object);

        var (resp, code, err) = controller.Login(new UserDto("max", "pw", Guid.Empty));

        Assert.Null(resp);
        Assert.Equal(404, code);
        Assert.Equal("User Not Found", err);
    }

    [Test]
    public void Login_Returns_200_And_Token_When_PasswordOk()
    {
        var guid = Guid.NewGuid();
        var user = MakeUser(3, guid, "max", "pw");

        var db = new Mock<IMediaRatingContext>();
        db.Setup(x => x.Users_FindByUsername("max")).Returns(user);

        var controller = new UserController(db.Object);

        var (resp, code, err) = controller.Login(new UserDto("max", "pw", Guid.Empty));

        Assert.Equal(200, code);
        Assert.Null(err);
        Assert.NotNull(resp);

        var tokenProp = resp!.GetType().GetProperty("token");
       

        var token = (string)tokenProp!.GetValue(resp!)!;

        Assert.StartsWith("mrpx.", token);
        Assert.Contains(guid.ToString("N"), token); // token contains Guid in "N" format
    }
}
