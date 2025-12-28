using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using TMPro;

public class GPTClient : MonoBehaviour
{
    public OpenAIConfiguration configuration;
    public Text resultText;

    public async void SubmitImage(Texture2D image)
    {
        var api = new OpenAIClient(configuration);

        var messages = new List<Message>();

        Message systemMessage = new Message(Role.System, "You are a robotics specialist. Your role is to support an explanable human-robot interatcion system. You will be shown an image from a passthrough AR headset taken from the user's perspective. Given the following information and the user's view, provide a resolution describing how the robot can automatically resolve the constraint. ");
        List<Content> imageContents = new List<Content>();

        string textContent = "The user is instructing a Stretch 3 robot to complete a task, but there is a contraint.";
        Texture2D imageContent = image;

        imageContents.Add(textContent);
        imageContents.Add(imageContent);

        Message imageMessage = new Message(Role.User, imageContents);

        messages.Add(systemMessage);
        messages.Add(imageMessage);

        var chatRequest = new ChatRequest(messages, model: Model.GPT4o);
        var result = await api.ChatEndpoint.GetCompletionAsync(chatRequest);

        Debug.Log(result.FirstChoice);
        resultText.text = result.FirstChoice;
    }
}
