using FirebaseAdmin;
using GenerativeAI.Methods;
using GenerativeAI.Models;
using GenerativeAI.Types;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Duongtddse172132_CloudApp2
{
    public partial class MainWindow : Window
    {
        private HubConnection connection;
        private List<ChatMessage> chatHistory;
        private User MaleUser;
        private User FemaleUser;
        public ChatSession session;
        public string apiKey = "AIzaSyDgEE0rmE5_yAdF-9cJA7BhflNb79VMMOk";

        public MainWindow()
        {
            InitializeComponent();
            MaleUser = new User { Name = "Nam", Age = 24, Gender = "Male" };
            FemaleUser = new User { Name = "Van", Age = 18, Gender = "Female" };
            InitializeSignalR();
            InitializeFirebase();
            ChatLoad();
        }

        public void ChatLoad()
        {
            var model = new GenerativeModel(apiKey);
            session = model.StartChat(new StartChatParams());
        }

        public void InitializeSignalR()
        {
            chatHistory = new List<ChatMessage>();

            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5258/chathub")
                .Build();

            connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    AddMessageToChatHistory(user, FemaleUser.Name, message);
                    if (user != FemaleUser.Name)
                    {
                        SendAutomatedMessage(message);
                    }
                });
            });

            StartConnection();
        }

        public void InitializeFirebase()
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("D:\\Docm\\Dev\\.learning-process\\C#\\PRN221_B3W\\chat-real-time\\App1\\Duongtddse172132_CloudApp1\\Duongtddse172132_CloudApp1\\serviceAccount.json"),
                ProjectId = "fir-chathistory",
            });
        }

        private async void StartConnection()
        {
            try
            {
                await connection.StartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}");
            }
        }

        private void AddMessageToChatHistory(string sender, string receiver, string message)
        {
            ChatMessage chatMessage = new ChatMessage
            {
                Sender = sender,
                Receiver = receiver,
                Message = message,
                Timestamp = DateTime.Now
            };

            chatHistory.Add(chatMessage);

            TextBlock messageTextBlock = new TextBlock
            {
                Text = $"{message}",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5)
            };

            Border messageBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Background = (sender == FemaleUser.Name) ? Brushes.LightBlue : Brushes.LightGray,
                Margin = new Thickness(10, 5, 10, 5),
                Child = messageTextBlock,
                HorizontalAlignment = (sender == FemaleUser.Name) ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            BlockUIContainer blockUIContainer = new BlockUIContainer(messageBorder);

            ChatHistoryRichTextBox.Document.Blocks.Add(blockUIContainer);
            ChatHistoryRichTextBox.ScrollToEnd();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveChatHistory();
        }

        private void SaveChatHistory()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = "chatHistory.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                string json = JsonSerializer.Serialize(chatHistory, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
        }

        //private async void StartAutoConversation(object sender, KeyEventArgs e)
        //{
        //    string mess = $"Hello {FemaleUser.Name} How are you?\nI'm Nam, 24 years-old!";
        //    string aiResponse = await session.SendMessageAsync(mess);
        //    AddMessageToChatHistory(MaleUser.Name, FemaleUser.Name, aiResponse);
        //}

        private async void SendAutomatedMessage(string message)
        {
            if (connection.State == HubConnectionState.Connected)
            {
                try
                {
                    await Task.Delay(5000);
                    string aiResponse = await session.SendMessageAsync(message);
                    //AddMessageToChatHistory(FemaleUser.Name, MaleUser.Name, aiResponse);
                    await connection.InvokeAsync("SendMessage", FemaleUser.Name, aiResponse);
                    await Task.Delay(5000);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error sending message: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Connection is not active. Please wait...");
            }
        }

        public class User
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string Gender { get; set; }
        }

        public class ChatMessage
        {
            public string Sender { get; set; }
            public string Receiver { get; set; }
            public string Message { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
