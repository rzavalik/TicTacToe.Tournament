using global::TicTacToe.Tournament.Auth;
using global::TicTacToe.Tournament.Server.DTOs;
using global::TicTacToe.Tournament.Server.Interfaces;
using global::TicTacToe.Tournament.WebApp.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Shouldly;

namespace TicTacToe.Tournament.WebApp.Tests.Controllers;

public class TournamentControllerTests
{
    private TournamentController MakeSut(
        out Mock<ITournamentOrchestratorService> orchestratorMock,
        out Mock<IConfiguration> configMock)
    {
        orchestratorMock = new Mock<ITournamentOrchestratorService>();
        configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Azure:SignalR:Endpoint"]).Returns("https://signalr.endpoint");
        configMock.Setup(c => c["Azure:SignalR:AccessKey"]).Returns("mysupersecurekey!mysupersecurekey!mysupersecurekey!");

        return new TournamentController(configMock.Object, orchestratorMock.Object);
    }

    [Fact]
    public void Index_ShouldReturnView()
    {
        var sut = MakeSut(out _, out _);
        var result = sut.Index();
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task List_ShouldReturnOkWithTournaments()
    {
        var sut = MakeSut(out var orchestratorMock, out _);
        orchestratorMock.Setup(o => o.GetAllTournamentsAsync())
            .ReturnsAsync(new List<TournamentSummaryDto> { new() { Id = Guid.NewGuid(), Name = "Test", Status = "Planned" } });

        var result = await sut.List();
        result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public void Details_ShouldSetViewDataAndReturnView()
    {
        var tournamentId = Guid.NewGuid();
        var sut = MakeSut(out var orchestratorMock, out _);

        var result = sut.Details(tournamentId);

        orchestratorMock.Verify(o => o.SpectateTournamentAsync(tournamentId), Times.Once);
        result.ShouldBeOfType<ViewResult>();
        sut.ViewData["TournamentId"].ShouldBe(tournamentId);
    }

    [Fact]
    public async Task GetTournament_WhenNotFound_ShouldReturnNotFound()
    {
        var sut = MakeSut(out var orchestratorMock, out _);
        orchestratorMock.Setup(o => o.GetTournamentAsync(It.IsAny<Guid>()))
            .ReturnsAsync((TournamentDto?)null);

        var result = await sut.GetTournament(Guid.NewGuid());
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetTournament_WhenFound_ShouldReturnOk()
    {
        var tournamentId = Guid.NewGuid();
        var sut = MakeSut(out var orchestratorMock, out _);
        orchestratorMock.Setup(o => o.GetTournamentAsync(tournamentId))
            .ReturnsAsync(new TournamentDto { Id = tournamentId });

        var result = await sut.GetTournament(tournamentId);
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ((TournamentDto)ok.Value!).Id.ShouldBe(tournamentId);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenTournamentDoesNotExist_ShouldReturnNotFound()
    {
        var tournamentId = Guid.NewGuid();
        var request = new TournamentAuthRequest { PlayerName = "Test" };

        var sut = MakeSut(out var orchestratorMock, out _);
        orchestratorMock.Setup(o => o.TournamentExistsAsync(tournamentId)).ReturnsAsync(false);

        var result = await sut.AuthenticateAsync(tournamentId, request);
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenTournamentExists_ShouldReturnOkWithToken()
    {
        var tournamentId = Guid.NewGuid();
        var request = new TournamentAuthRequest { PlayerName = "Test" };

        var sut = MakeSut(out var orchestratorMock, out var configMock);
        orchestratorMock.Setup(o => o.TournamentExistsAsync(tournamentId)).ReturnsAsync(true);

        var controllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext()
        };
        controllerContext.HttpContext.Request.Headers["X-PlayerId"] = Guid.NewGuid().ToString();
        sut.ControllerContext = controllerContext;

        var result = await sut.AuthenticateAsync(tournamentId, request);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<TournamentAuthResponse>();
        response.Success.ShouldBeTrue();
        response.TournamentId.ShouldBe(tournamentId);
    }
}
