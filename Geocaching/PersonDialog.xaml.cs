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
    /// Interaction logic for PersonDialog.xaml
    /// </summary>
    public partial class PersonDialog : Window
    {
        public string PersonFirstName { get; private set; }
        public string PersonLastName { get; private set; }
        public string AddressCountry { get; private set; }
        public string AddressCity { get; private set; }
        public string AddressStreetName { get; private set; }
        public byte AddressStreetNumber { get; private set; }

        public PersonDialog()
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
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var firstNameLabel = new Label { Content = "First Name:", Margin = spacing };
            grid.Children.Add(firstNameLabel);
            Grid.SetColumn(firstNameLabel, 0);
            Grid.SetRow(firstNameLabel, 0);

            var firstNameTextBox = new TextBox { Margin = spacing, VerticalContentAlignment = VerticalAlignment.Center };
            grid.Children.Add(firstNameTextBox);
            Grid.SetColumn(firstNameTextBox, 1);
            Grid.SetRow(firstNameTextBox, 0);

            var lastNameLabel = new Label { Content = "Last Name:", Margin = spacing };
            grid.Children.Add(lastNameLabel);
            Grid.SetColumn(lastNameLabel, 0);
            Grid.SetRow(lastNameLabel, 1);

            var lastNameTextBox = new TextBox { Margin = spacing, VerticalContentAlignment = VerticalAlignment.Center };
            grid.Children.Add(lastNameTextBox);
            Grid.SetColumn(lastNameTextBox, 1);
            Grid.SetRow(lastNameTextBox, 1);

            var countryLabel = new Label { Content = "Country:", Margin = spacing };
            grid.Children.Add(countryLabel);
            Grid.SetColumn(countryLabel, 0);
            Grid.SetRow(countryLabel, 2);

            var countryTextBox = new TextBox { Margin = spacing, VerticalContentAlignment = VerticalAlignment.Center };
            grid.Children.Add(countryTextBox);
            Grid.SetColumn(countryTextBox, 1);
            Grid.SetRow(countryTextBox, 2);

            var cityLabel = new Label { Content = "City:", Margin = spacing };
            grid.Children.Add(cityLabel);
            Grid.SetColumn(cityLabel, 0);
            Grid.SetRow(cityLabel, 3);

            var cityTextBox = new TextBox { Margin = spacing, VerticalContentAlignment = VerticalAlignment.Center };
            grid.Children.Add(cityTextBox);
            Grid.SetColumn(cityTextBox, 1);
            Grid.SetRow(cityTextBox, 3);

            var streetNameLabel = new Label { Content = "Street Name:", Margin = spacing };
            grid.Children.Add(streetNameLabel);
            Grid.SetColumn(streetNameLabel, 0);
            Grid.SetRow(streetNameLabel, 4);

            var streetNameTextBox = new TextBox { Margin = spacing, VerticalContentAlignment = VerticalAlignment.Center };
            grid.Children.Add(streetNameTextBox);
            Grid.SetColumn(streetNameTextBox, 1);
            Grid.SetRow(streetNameTextBox, 4);

            var streetNumberLabel = new Label { Content = "Street Number:", Margin = spacing };
            grid.Children.Add(streetNumberLabel);
            Grid.SetColumn(streetNumberLabel, 0);
            Grid.SetRow(streetNumberLabel, 5);

            var streetNumberTextBox = new TextBox { Margin = spacing, VerticalContentAlignment = VerticalAlignment.Center };
            grid.Children.Add(streetNumberTextBox);
            Grid.SetColumn(streetNumberTextBox, 1);
            Grid.SetRow(streetNumberTextBox, 5);

            var button = new Button
            {
                Content = "OK",
                IsDefault = true,
                Margin = spacing,
                Padding = spacing
            };
            grid.Children.Add(button);
            Grid.SetColumnSpan(button, 2);
            Grid.SetRow(button, 6);

            button.Click += (sender, args) =>
            {
                PersonFirstName = firstNameTextBox.Text;
                PersonLastName = lastNameTextBox.Text;
                AddressCountry = countryTextBox.Text;
                AddressCity = cityTextBox.Text;
                AddressStreetName = streetNameTextBox.Text;
                try
                {
                    AddressStreetNumber = byte.Parse(streetNumberTextBox.Text);
                    DialogResult = true;
                }
                catch
                {
                    MessageBox.Show("Please enter a valid street number.");
                    Keyboard.Focus(streetNumberTextBox);
                    streetNumberTextBox.SelectAll();
                }
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
                Keyboard.Focus(firstNameTextBox);
            };
        }
    }
}
