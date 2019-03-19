using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Geocaching
{
    /// <summary>
    /// Interaction logic for GeocacheDialog.xaml
    /// </summary>
    public partial class GeocacheDialog : Window
    {
        public string GeocacheContents { get; private set; }
        public string GeocacheMessage { get; private set; }

        public GeocacheDialog()
        {
            InitializeComponent();

            Width = 400;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var spacing = new Thickness(5);
            Padding = spacing;

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var contentsLabel = new Label { Content = "Contents:", Margin = spacing };
            grid.Children.Add(contentsLabel);
            Grid.SetColumn(contentsLabel, 0);
            Grid.SetRow(contentsLabel, 0);

            var contentsTextBox = new TextBox { Margin = spacing, VerticalContentAlignment = VerticalAlignment.Center };
            grid.Children.Add(contentsTextBox);
            Grid.SetColumn(contentsTextBox, 1);
            Grid.SetRow(contentsTextBox, 0);

            var messageLabel = new Label { Content = "Message:", Margin = spacing };
            grid.Children.Add(messageLabel);
            Grid.SetColumn(messageLabel, 0);
            Grid.SetRow(messageLabel, 1);

            var messageTextBox = new TextBox { Margin = spacing, VerticalContentAlignment = VerticalAlignment.Center };
            grid.Children.Add(messageTextBox);
            Grid.SetColumn(messageTextBox, 1);
            Grid.SetRow(messageTextBox, 1);

            var button = new Button
            {
                Content = "OK",
                IsDefault = true,
                Margin = spacing,
                Padding = spacing
            };
            grid.Children.Add(button);
            Grid.SetColumnSpan(button, 2);
            Grid.SetRow(button, 2);

            button.Click += (sender, args) =>
            {
                GeocacheContents = contentsTextBox.Text;
                GeocacheMessage = messageTextBox.Text;
                DialogResult = true;
            };

            PreviewKeyDown += (sender, args) =>
            {
                if (args.Key == Key.Escape)
                {
                    Close();
                }
            };

            Loaded += (sender, args) =>
            {
                // Restrict window to actual height, effectively preventing vertical resizing.
                MinHeight = MaxHeight = ActualHeight;
                Keyboard.Focus(contentsTextBox);
            };
        }
    }
}
