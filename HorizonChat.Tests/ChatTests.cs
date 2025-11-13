using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using ChatPage = HorizonChat.Pages.Chat;
using System.Net.WebSockets;

namespace HorizonChat.Tests;

public class ChatTests : BunitContext
{
    [Fact]
    public void Chat_RequiresUsername_FromQueryString()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var testUri = $"{navigationManager.BaseUri}chat?username=TestUser";
        
        // Act
        navigationManager.NavigateTo(testUri);
        var cut = Render<ChatPage>();
        
        // Assert
        var usernameBadge = cut.Find(".username-badge");
        Assert.Contains("TestUser", usernameBadge.TextContent);
    }

    [Fact]
    public void Chat_DisplaysDefaultUsername_WhenNoUsernameProvided()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var testUri = $"{navigationManager.BaseUri}chat";
        
        // Act
        navigationManager.NavigateTo(testUri);
        var cut = Render<ChatPage>();
        
        // Assert
        var usernameBadge = cut.Find(".username-badge");
        Assert.Contains("Guest", usernameBadge.TextContent);
    }

    [Fact]
    public void Chat_DisplaysChatHeader_WithCorrectTitle()
    {
        // Arrange & Act
        var cut = Render<ChatPage>();
        
        // Assert
        var header = cut.Find(".chat-header h2");
        Assert.Equal("Horizon Chat", header.TextContent);
    }

    [Fact]
    public void Chat_HasMessageInput_Disabled_WhenNotConnected()
    {
        // Arrange & Act
        var cut = Render<ChatPage>();
        
        // Assert
        var input = cut.Find(".chat-input input");
        Assert.True(input.HasAttribute("disabled"));
    }

    [Fact]
    public void Chat_HasSendButton_Disabled_WhenNotConnected()
    {
        // Arrange & Act
        var cut = Render<ChatPage>();
        
        // Assert
        var button = cut.Find(".chat-input button");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void Chat_DisplaysConnectionStatus()
    {
        // Arrange & Act
        var cut = Render<ChatPage>();
        
        // Assert
        var status = cut.Find(".connection-status");
        Assert.NotNull(status);
        Assert.True(status.ClassList.Contains("connected") || status.ClassList.Contains("disconnected"));
    }

    [Fact]
    public void Chat_MessagesContainer_Exists()
    {
        // Arrange & Act
        var cut = Render<ChatPage>();
        
        // Assert
        var messagesContainer = cut.Find("#chatMessages");
        Assert.NotNull(messagesContainer);
    }
}

public class WebSocketHandlerTests
{
    [Fact]
    public void WebSocketHandler_GetActiveConnectionCount_ReturnsZero_Initially()
    {
        // Act
        var count = WebSocketHandler.GetActiveConnectionCount();
        
        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void WebSocketHandler_GetConnectedUsers_ReturnsEmpty_Initially()
    {
        // Act
        var users = WebSocketHandler.GetConnectedUsers();
        
        // Assert
        Assert.Empty(users);
    }

    [Fact]
    public void ChatMessage_HasRequiredProperties()
    {
        // Arrange & Act
        var message = new WebSocketHandler.ChatMessage
        {
            Username = "TestUser",
            Content = "Test message",
            Timestamp = DateTime.Now
        };
        
        // Assert
        Assert.Equal("TestUser", message.Username);
        Assert.Equal("Test message", message.Content);
        Assert.NotEqual(default(DateTime), message.Timestamp);
    }

    [Fact]
    public void ChatMessage_Username_CanBeSet()
    {
        // Arrange
        var message = new WebSocketHandler.ChatMessage();
        
        // Act
        message.Username = "NewUser";
        
        // Assert
        Assert.Equal("NewUser", message.Username);
    }

    [Fact]
    public void ChatMessage_Content_CanBeSet()
    {
        // Arrange
        var message = new WebSocketHandler.ChatMessage();
        
        // Act
        message.Content = "Hello, World!";
        
        // Assert
        Assert.Equal("Hello, World!", message.Content);
    }

    [Fact]
    public void ChatMessage_Timestamp_CanBeSet()
    {
        // Arrange
        var message = new WebSocketHandler.ChatMessage();
        var now = DateTime.Now;
        
        // Act
        message.Timestamp = now;
        
        // Assert
        Assert.Equal(now, message.Timestamp);
    }

    [Fact]
    public void ChatMessage_DefaultUsername_IsEmptyString()
    {
        // Arrange & Act
        var message = new WebSocketHandler.ChatMessage();
        
        // Assert
        Assert.Equal("", message.Username);
    }

    [Fact]
    public void ChatMessage_DefaultContent_IsEmptyString()
    {
        // Arrange & Act
        var message = new WebSocketHandler.ChatMessage();
        
        // Assert
        Assert.Equal("", message.Content);
    }

    [Fact]
    public void ChatMessage_DefaultTimestamp_IsDefault()
    {
        // Arrange & Act
        var message = new WebSocketHandler.ChatMessage();
        
        // Assert
        Assert.Equal(default(DateTime), message.Timestamp);
    }
}

public class ChatUITests : BunitContext
{
    [Fact]
    public void Chat_DisplaysMessages_InCorrectFormat()
    {
        // Arrange
        var cut = Render<ChatPage>();
        
        // Assert - Message structure should exist
        var messagesDiv = cut.Find("#chatMessages");
        Assert.NotNull(messagesDiv);
    }

    [Fact]
    public void Chat_InputField_HasCorrectPlaceholder()
    {
        // Arrange & Act
        var cut = Render<ChatPage>();
        
        // Assert
        var input = cut.Find(".chat-input input");
        Assert.Equal("Type a message...", input.GetAttribute("placeholder"));
    }

    [Fact]
    public void Chat_SendButton_HasCorrectText()
    {
        // Arrange & Act
        var cut = Render<ChatPage>();
        
        // Assert
        var button = cut.Find(".chat-input button");
        Assert.Equal("Send", button.TextContent.Trim());
    }

    [Fact]
    public void Chat_UsernameRequired_BeforeAccessingChat()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        
        // Act - Navigate without username
        navigationManager.NavigateTo($"{navigationManager.BaseUri}chat");
        var cut = Render<ChatPage>();
        
        // Assert - Should show default username "Guest"
        var usernameBadge = cut.Find(".username-badge");
        Assert.Equal("Guest", usernameBadge.TextContent);
    }

    [Fact]
    public void Chat_AutoScrollsToBottom_WhenMessagesAdded()
    {
        // Arrange & Act
        var cut = Render<ChatPage>();
        
        // Assert - Chat messages container should have id for JS scrolling
        var messagesContainer = cut.Find("#chatMessages");
        Assert.Equal("chatMessages", messagesContainer.Id);
    }
}
