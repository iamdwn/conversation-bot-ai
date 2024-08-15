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

        public MainWindow()
        {
            InitializeComponent();
            chatHistory = new List<ChatMessage>();

            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5258/chathub")
                .Build();

            SendMessageButton.IsEnabled = false;

            connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    AddMessageToChatHistory(user, "User1", message);
                });
            });

            StartConnection();

            this.Closing += MainWindow_Closing;
        }

        private async void StartConnection()
        {
            try
            {
                await connection.StartAsync();
                SendMessageButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}");
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(MessageTextBox.Text))
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    try
                    {
                        string user = "User2";
                        string message = MessageTextBox.Text;

                        await connection.InvokeAsync("SendMessage", user, message);
                        //AddMessageToChatHistory(user, "Receiver", message);
                        MessageTextBox.Clear();
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
                Background = (sender == "User2") ? Brushes.LightBlue : Brushes.LightGray,
                Margin = new Thickness(10, 5, 10, 5),
                Child = messageTextBlock,
                HorizontalAlignment = (sender == "User2") ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            BlockUIContainer blockUIContainer = new BlockUIContainer(messageBorder);

            ChatHistoryRichTextBox.Document.Blocks.Add(blockUIContainer);
            ChatHistoryRichTextBox.ScrollToEnd();
        }

        private void UserInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage_Click(sender, e);
            }
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


        public class ChatMessage
        {
            public string Sender { get; set; }
            public string Receiver { get; set; }
            public string Message { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var firestoreDb = FirestoreDb.Create("fir-chathistory");

                var chatHistoryJson = JsonSerializer.Serialize(chatHistory, new JsonSerializerOptions { WriteIndented = true });

                var documentData = new Dictionary<string, object>
        {
            { "chatHistory", chatHistoryJson },
            { "timestamp", Timestamp.FromDateTime(DateTime.UtcNow) }
        };

                CollectionReference chatHistoryCollection = firestoreDb.Collection("ChatHistories");

                await chatHistoryCollection.AddAsync(documentData);

                MessageBox.Show("Chat history uploaded successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to upload chat history: {ex.Message}");
            }
        }
    }
}
