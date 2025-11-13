using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using IndexPage = HorizonChat.Pages.Index;

namespace HorizonChat.Tests;

public class IndexPageTests : BunitContext
{
    [Fact]
    public void UsernameInput_UpdatesCorrectly_WhenUserTypes()
    {
        // Arrange
        var cut = Render<IndexPage>();
        var input = cut.Find("input[type='text']");

        // Act
        input.Change("TestUser");

        // Assert
        Assert.Equal("TestUser", input.GetAttribute("value"));
    }

    [Fact]
    public void EnterChatButton_IsEnabled_Always()
    {
        // Arrange
        var cut = Render<IndexPage>();
        var button = cut.Find("button");

        // Assert - Button is always enabled (guest username will be generated if empty)
        Assert.False(button.HasAttribute("disabled"));
    }

    [Fact]
    public void EnterChat_GeneratesGuestUsername_WhenNoUsernameProvided()
    {
        // Arrange
        var cut = Render<IndexPage>();
        var button = cut.Find("button");
        var navManager = Services.GetRequiredService<NavigationManager>();

        // Act
        button.Click();

        // Assert - Should navigate with a guest username
        var uri = new Uri(navManager.Uri);
        Assert.Contains("/chat", uri.AbsolutePath);
        Assert.Contains("username=", uri.Query);
        
        // Extract username from query
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var username = query["username"];
        
        Assert.NotNull(username);
        Assert.NotEmpty(username);
        // Guest username format: AdjectiveNoun### (e.g., HappyPanda123)
        Assert.Matches(@"^[A-Z][a-z]+[A-Z][a-z]+\d{3}$", username);
    }

    [Fact]
    public void EnterChat_UsesProvidedUsername_WhenUsernameEntered()
    {
        // Arrange
        var cut = Render<IndexPage>();
        var input = cut.Find("input[type='text']");
        var button = cut.Find("button");
        var navManager = Services.GetRequiredService<NavigationManager>();

        // Act
        input.Change("CustomUser");
        button.Click();

        // Assert
        var uri = new Uri(navManager.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var username = query["username"];
        
        Assert.Equal("CustomUser", username);
    }

    [Fact]
    public void EnterChat_NavigatesToChatPage_WithUsernameParameter()
    {
        // Arrange
        var cut = Render<IndexPage>();
        var input = cut.Find("input[type='text']");
        var button = cut.Find("button");
        var navManager = Services.GetRequiredService<NavigationManager>();

        // Act
        input.Change("TestUser123");
        button.Click();

        // Assert
        Assert.Contains("/chat", navManager.Uri);
        Assert.Contains("username=TestUser123", navManager.Uri);
    }

    [Fact]
    public void EnterChat_EncodesSpecialCharacters_InUsername()
    {
        // Arrange
        var cut = Render<IndexPage>();
        var input = cut.Find("input[type='text']");
        var button = cut.Find("button");
        var navManager = Services.GetRequiredService<NavigationManager>();

        // Act
        input.Change("User Name!");
        button.Click();

        // Assert - Special characters should be URL encoded
        Assert.Contains("/chat", navManager.Uri);
        var uri = new Uri(navManager.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var username = query["username"];
        
        Assert.Equal("User Name!", username); // HttpUtility.ParseQueryString decodes it
    }
}


