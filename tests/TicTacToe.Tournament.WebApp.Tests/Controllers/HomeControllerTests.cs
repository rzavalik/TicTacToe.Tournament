namespace TicTacToe.Tournament.WebApp.Tests.Controllers
{
    using global::TicTacToe.Tournament.WebApp.Controllers;
    using global::TicTacToe.Tournament.WebApp.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Shouldly;
    public class HomeControllerTests
    {
        private HomeController MakeSut(out Mock<ILogger<HomeController>> loggerMock)
        {
            loggerMock = new Mock<ILogger<HomeController>>();
            return new HomeController(loggerMock.Object);
        }

        [Fact]
        public void Index_ShouldRedirectToTournament()
        {
            var sut = MakeSut(out _);
            var result = sut.Index();

            var redirect = result.ShouldBeOfType<RedirectResult>();
            redirect.Url.ShouldBe("/tournament");
        }

        [Fact]
        public void Privacy_ShouldReturnView()
        {
            var sut = MakeSut(out _);
            var result = sut.Privacy();

            result.ShouldBeOfType<ViewResult>();
        }

        [Fact]
        public void Error_ShouldReturnViewWithErrorModel()
        {
            var sut = MakeSut(out _);

            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "trace-123";
            sut.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = sut.Error();
            var viewResult = result.ShouldBeOfType<ViewResult>();
            var model = viewResult.Model.ShouldBeOfType<ErrorViewModel>();
            model.RequestId.ShouldBe("trace-123");
        }
    }
}