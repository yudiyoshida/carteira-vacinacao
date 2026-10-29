using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using VaccinationCard.Api.Infra.Http;

namespace VaccinationCard.UnitTests.Infra.Http;

public sealed class InvalidModelStateResponseTests
{
    private const string InvalidValue = "O valor informado é inválido.";
    private const string InvalidBody = "O corpo da requisição é inválido.";

    [Fact]
    public void Should_Answer_Validation_Problem_When_Model_State_Is_Invalid()
    {
        // Arrange
        var context = MakeActionContext();
        context.ModelState.AddModelError("$.number", "The JSON value could not be converted to System.Int32.");

        // Act
        var result = InvalidModelStateResponse.Create(context);

        // Assert
        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        objectResult.ContentTypes.ShouldBe(["application/problem+json"]);
        var problem = objectResult.Value.ShouldBeOfType<HttpValidationProblemDetails>();
        problem.Status.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Title.ShouldBe("Requisição inválida");
        problem.Detail.ShouldBe("Um ou mais campos são inválidos.");
        problem.Extensions["code"].ShouldBe("validation.failed");
    }

    [Theory]
    [InlineData("$.primaryDoses", "primaryDoses", InvalidValue)]
    [InlineData("$.dose.type", "dose.type", InvalidValue)]
    [InlineData("$.Dose.Type", "dose.type", InvalidValue)]
    [InlineData("Page", "page", InvalidValue)]
    [InlineData("$", "body", InvalidBody)]
    [InlineData("", "body", InvalidBody)]
    public void Should_Name_The_Json_Field_When_A_Value_Cannot_Be_Read(string key, string field, string message)
    {
        // Arrange
        var context = MakeActionContext();
        context.ModelState.AddModelError(key, "The JSON value could not be converted.");

        // Act
        var result = InvalidModelStateResponse.Create(context);

        // Assert
        var problem = result.ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<HttpValidationProblemDetails>();
        problem.Errors.Keys.ShouldBe([field]);
        problem.Errors[field].ShouldBe([message]);
    }

    private static ActionContext MakeActionContext() => new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
}
