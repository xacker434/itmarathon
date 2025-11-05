using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Aggregate.Room;
using Epam.ItMarathon.ApiService.Domain.Entities.User;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentAssertions;
using FluentValidation.Results;
using NSubstitute;
using NSubstitute.Core;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Epam.ItMarathon.ApiService.Application.Tests.UserCases.Commands
{
    /// <summary>
    /// Unit tests for the <see cref="DeleteUserHandler"/> class.
    /// </summary>
    public class DeleteUserHandlerTests
    {
        private readonly IRoomRepository _roomRepositoryMock;
        private readonly DeleteUserHandler _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteUserHandlerTests"/> class with mocked dependencies.
        /// </summary>
        public DeleteUserHandlerTests()
        {
            _roomRepositoryMock = Substitute.For<IRoomRepository>();
            _handler = new DeleteUserHandler(_roomRepositoryMock);
        }

        /// <summary>
        /// Tests that the handler returns a NotFoundError when the room by provided UserCode is not found.
        /// </summary>
        [Fact]
        public async Task Handle_Should_ReturnFailure_WhenRoomNotFound()
        {
            // Arrange
            var authUser = DataFakers.UserFaker
            .RuleFor(user => user.RoomId, _ => 1UL)
            .Generate();
            var request = new DeleteUserRequest(string.Empty, authUser.Id);

            _roomRepositoryMock
                .GetByUserCodeAsync(Arg.Any<string>(), CancellationToken.None)
                .Returns(new NotFoundError([
                    new ValidationFailure("userCode", string.Empty)
                ]));

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            result.Error.Errors.Should().Contain(error =>
                error.PropertyName.Equals("userCode"));
        }

        /// <summary>
        /// Tests that the handler returns a BadRequestError when the room is already closed.
        /// </summary>
        [Fact]
        public async Task Handle_Should_ReturnFailure_WhenRoomIsClosed()
        {
            // Arrange
            var authUser = DataFakers.UserFaker
            .RuleFor(user => user.RoomId, _ => 1UL)
            .Generate();
            var room = DataFakers.RoomFaker
                .RuleFor(r => r.ClosedOn, faker => faker.Date.Past())
                .Generate();
            room.Users.Add(authUser);
            var request = new DeleteUserRequest(string.Empty, authUser.Id);

            _roomRepositoryMock
                .GetByUserCodeAsync(Arg.Any<string>(), CancellationToken.None)
                .Returns(room);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<BadRequestError>();
            result.Error.Errors.Should().Contain(error =>
                error.PropertyName.Equals("room.ClosedOn"));
        }

        /// <summary>
        /// Tests that the handler returns a NotFoundError when the user to be deleted is not found in the room.
        /// </summary>
        [Fact]
        public async Task Handle_Should_ReturnFailure_WhenUserToDeleteNotFoundInRoom()
        {
            // Arrange
            var adminUser = DataFakers.UserFaker
                .RuleFor(user => user.RoomId, _ => 1UL)
                .RuleFor(user => user.IsAdmin, _ => true)
                .Generate();
            var room = DataFakers.RoomFaker
                .Generate();
            room.Users.Add(adminUser);
            var request = new DeleteUserRequest(adminUser.AuthCode, null);

            _roomRepositoryMock
                .GetByUserCodeAsync(Arg.Any<string>(), CancellationToken.None)
                .Returns(room);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            result.Error.Errors.Should().Contain(error =>
                error.PropertyName.Equals("userId"));
        }

        /// <summary>
        /// Tests that the handler returns a NotAuthorizedError when not admin tries to delete another user.
        /// </summary>
        [Fact]
        public async Task Handle_Should_ReturnFailure_WhenNotAdminTriesToDeleteAnotherUser()
        {
            // Arrange
            var adminUser = DataFakers.UserFaker
                .RuleFor(user => user.RoomId, _ => 1UL)
                .RuleFor(user => user.IsAdmin, _ => true)
                .Generate();
            var anotherUser = DataFakers.UserFaker
                .RuleFor(user => user.RoomId, _ => 1UL)
                .Generate();
            var room = DataFakers.RoomFaker
                .Generate();
            room.Users.Add(adminUser);
            room.Users.Add(anotherUser);
            var request = new DeleteUserRequest(anotherUser.AuthCode, anotherUser.Id);

            _roomRepositoryMock
                .GetByUserCodeAsync(Arg.Any<string>(), CancellationToken.None)
                .Returns(room);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotAuthorizedError>();
            result.Error.Errors.Should().Contain(error =>
                error.PropertyName.Equals("UserCode"));
        }

        /// <summary>
        /// Tests that the handler returns a BadRequestError when the Id and UserCode refer to the same user.
        /// </summary>
        [Fact]
        public async Task Handle_Should_ReturnFailure_WhenIdAndUserCodeReferToSameUser()
        {
            // Arrange
            var authUser = DataFakers.UserFaker
                .RuleFor(user => user.RoomId, _ => 1UL)
                .RuleFor(user => user.IsAdmin, _ => true)
                .Generate();
            var room = DataFakers.RoomFaker
                .Generate();
            room.Users.Add(authUser);

            var request = new DeleteUserRequest(authUser.AuthCode, authUser.Id);

            _roomRepositoryMock
                .GetByUserCodeAsync(Arg.Any<string>(), CancellationToken.None)
                .Returns(room);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<BadRequestError>();
            result.Error.Errors.Should().Contain(error =>
                error.PropertyName.Equals("userId"));
        }

        /// <summary>
        /// Tests that the handler successfully deletes a user when valid inputs are provided by an admin.
        /// </summary>
        [Fact]
        public async Task Handle_Should_DeleteUserSuccessfully_WhenValidInputsProvidedByAdmin()
        {
            // Arrange
            var adminUser = DataFakers.UserFaker
                .RuleFor(user => user.RoomId, _ => 1UL)
                .RuleFor(user => user.IsAdmin, _ => true)
                .RuleFor(user => user.Id, _ => 1UL)
                .Generate();
            var userToDelete = DataFakers.UserFaker
                .RuleFor(user => user.RoomId, _ => 1UL)
                .RuleFor(user => user.Id, _ => 2UL)
                .Generate();
            var room = DataFakers.RoomFaker
                .Generate();
            room.Users.Add(adminUser);
            room.Users.Add(userToDelete);

            var request = new DeleteUserRequest(adminUser.AuthCode, userToDelete.Id);

            _roomRepositoryMock
                .GetByUserCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(room);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Users.Should().NotContain(user => user.Id == userToDelete.Id);
        }
    }
}