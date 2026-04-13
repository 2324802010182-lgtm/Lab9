using ASC.Web.Configuration;
using ASC.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ASC.Tests
{
    public class HomeControllerTests
    {
        private readonly Mock<IOptions<ApplicationSettings>> _mockOptions;
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            var appSettings = new ApplicationSettings
            {
                ApplicationTitle = "ASC Demo App",
                AdminEmail = "admin@test.com",
                AdminName = "Admin",
                AdminPassword = "123",
                Roles = "Admin,Engineer",
                EngineerEmail = "engineer@test.com",
                EngineerName = "Engineer",
                EngineerPassword = "123",
                SMTPServer = "smtp.gmail.com",
                SMTPPort = 587,
                SMTPAccount = "smtp@test.com",
                SMTPPassword = "123"
            };

            _mockOptions = new Mock<IOptions<ApplicationSettings>>();
            _mockOptions.Setup(x => x.Value).Returns(appSettings);

            _mockLogger = new Mock<ILogger<HomeController>>();

           // _controller = new HomeController(_mockLogger.Object, _mockOptions.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new FakeSession();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public void HomeController_Index_ReturnsViewResult_Test()
        {
            var result = _controller.Index();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void HomeController_Index_ModelIsNotNull_Test()
        {
            var result = _controller.Index() as ViewResult;

            Assert.NotNull(result);
            Assert.NotNull(result.Model);
        }

        [Fact]
        public void HomeController_Index_ModelHasNoValidationErrors_Test()
        {
            var result = _controller.Index() as ViewResult;

            Assert.NotNull(result);
            Assert.True(_controller.ModelState.IsValid);
        }

        [Fact]
        public void HomeController_Index_ReturnsApplicationSettingsModel_Test()
        {
            var result = _controller.Index() as ViewResult;

            Assert.NotNull(result);
            Assert.IsType<ApplicationSettings>(result.Model);
        }
    }
}