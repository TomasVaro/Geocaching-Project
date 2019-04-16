using Microsoft.EntityFrameworkCore;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Device.Location;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace Geocaching
{
    #region Classes
    public class Person
    {
        public int ID { get; set; }
        [Required, MaxLength(50)]
        public string FirstName { get; set; }
        [Required, MaxLength(50)]
        public string LastName { get; set; }
        public GeoCoordinate Coordinates { get; set; }
        [Required, MaxLength(50)]
        public string Country { get; set; }
        [Required, MaxLength(50)]
        public string City { get; set; }
        [Required, MaxLength(50)]
        public string StreetName { get; set; }
        [Required]
        public byte StreetNumber { get; set; }
        public List<FoundGeocache> FoundGeocaches { get; set; }
        public List<Geocache> Geocaches { get; set; }
    }

    public class Geocache
    {
        public int ID { get; set; }
        public Person Person { get; set; }
        public GeoCoordinate Coordinates { get; set; }
        [Required, MaxLength(255)]
        public string Content { get; set; }
        [Required, MaxLength(255)]
        public string Message { get; set; }
        public List<FoundGeocache> FoundGeocaches { get; set; }
    }

    public class FoundGeocache
    {
        public int PersonID { get; set; }
        public Person Person { get; set; }
        public int GeocacheID { get; set; }
        public Geocache Geocache { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<Person> Person { get; set; }
        public DbSet<Geocache> Geocache { get; set; }
        public DbSet<FoundGeocache> FoundGeocache { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(@"Data Source=(local)\SQLEXPRESS;Initial Catalog=Geocaching;Integrated Security=True");
        }
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<FoundGeocache>().HasKey(fg => new { fg.PersonID, fg.GeocacheID });

            model.Entity<Person>().OwnsOne(o => o.Coordinates, a =>
            {
                a.Property(p => p.Latitude).HasColumnName("Latitude");
                a.Property(p => p.Longitude).HasColumnName("Longitude");
                a.Ignore(p => p.Altitude).Ignore(p => p.Course).Ignore(p => p.HorizontalAccuracy).Ignore(p => p.Speed).Ignore(p => p.VerticalAccuracy);
            });

            model.Entity<Geocache>().OwnsOne(o => o.Coordinates, a =>
            {
                a.Property(p => p.Latitude).HasColumnName("Latitude");
                a.Property(p => p.Longitude).HasColumnName("Longitude");
                a.Ignore(p => p.Altitude).Ignore(p => p.Course).Ignore(p => p.HorizontalAccuracy).Ignore(p => p.Speed).Ignore(p => p.VerticalAccuracy);
            });
        }
    }
    #endregion

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Startup
        public int ActivePersonID;
        Color color = new Color();
        Person person = new Person();
        Geocache geocache = new Geocache() { Person = new Person() };
        FoundGeocache foundGeocache = new FoundGeocache();
        GeoCoordinate coordinates = new GeoCoordinate();

        // Contains the ID string needed to use the Bing map.
        private const string applicationId = "AsAxqI2jwZYAKchAD0G8TytQ0ZavSgXYlloAUiMBEIqXLq93ERbTF2pgs2V2e6tT";

        private MapLayer layer;

        // Contains the location of the latest click on the map.
        private Location latestClickLocation;
        private Location gothenburg = new Location(57.719021, 11.991202);
        #endregion

        #region Start and create map
        public MainWindow()
        {
            InitializeComponent();
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if (applicationId == null)
            {
                MessageBox.Show("Please set the applicationId variable before running this program.");
                Environment.Exit(0);
            }
            CreateMap();
            UpdateMap();
        }

        private void CreateMap()
        {
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = gothenburg;
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);
            WindowState = WindowState.Maximized;

            // Resets color on all pins to blue/gray on map click.
            MouseDown += (sender, e) =>
            {
                var point = e.GetPosition(this);
                latestClickLocation = map.ViewportPointToLocation(point);

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    OnMapLeftClick();
                }
            };

            map.ContextMenu = new ContextMenu();

            var addPersonMenuItem = new MenuItem { Header = "Add Person" };
            map.ContextMenu.Items.Add(addPersonMenuItem);
            addPersonMenuItem.Click += BeginOnAddPersonClick;

            var addGeocacheMenuItem = new MenuItem { Header = "Add Geocache" };
            map.ContextMenu.Items.Add(addGeocacheMenuItem);
            addGeocacheMenuItem.Click += BeginOnAddGeocacheClick;
        }
        #endregion

        #region Update Pins and set pin-colors and tooltip
        private void UpdateMap()
        {
            BeginColorPersonPins();
            BeginColorGeocachePins();
        }

        // Loops through Person and gives the right color to the pins, asynchronously.
        private async void BeginColorPersonPins()
        {
            using (var database = new AppDbContext())
            {
                IEnumerable<Person> PersonAsync = new List<Person>();
                var task = Task.Run(() =>
                {
                    PersonAsync = database.Person.Include(e => e.Geocaches).Include(p => p.FoundGeocaches).AsNoTracking();
                });
                await (task);

                foreach (var p in PersonAsync)
                {
                    Location location = new Location(p.Coordinates.Latitude, p.Coordinates.Longitude);
                    string personToolTip = p.FirstName + " " + p.LastName + "\n" + p.StreetName + " " + p.StreetNumber + "\n" + p.City + "\n" + p.Country;

                    if (ActivePersonID == p.ID || ActivePersonID == 0)
                    {
                        color = Colors.Blue;
                    }
                    else
                    {
                        color = Color.FromRgb(0x47, 0x9D, 0xEE);
                    }
                    var pinPerson = AddPin(location, personToolTip, color);

                    pinPerson.MouseDown += (s, a) =>
                    {
                        ActivePersonID = p.ID;
                        UpdateMap();
                        a.Handled = true;
                    };
                    // Put delay here to check asynchronisity on person-pins
                    await Task.Delay(0);
                }
            }
        }

        // Loops through Geocache and gives the right color to the pins, asynchronously.
        private async void BeginColorGeocachePins()
        {
            var database = new AppDbContext();

            IEnumerable<Geocache> GeocacheAsync = new List<Geocache>();
            var task = Task.Run(() =>
            {
                GeocacheAsync = database.Geocache.Include(g => g.Person).Include(g => g.FoundGeocaches);
            });
            await (task);

            foreach (var g in GeocacheAsync)
            {
                Location locationGeocache = new Location(g.Coordinates.Latitude, g.Coordinates.Longitude);
                string geocacheToolTip = "long:" + g.Coordinates.Longitude + "\nlat:    " + g.Coordinates.Latitude + "\nContent: "
                    + g.Content + "\nMessage: " + g.Message + "\nPlaced by: " + g.Person.FirstName + " " + g.Person.LastName;
                if (ActivePersonID == 0)
                {
                    color = Colors.Gray;
                }
                else if (g.Person.ID == ActivePersonID)
                {
                    color = Colors.Black;
                }
                else
                {
                    color = Colors.Red;
                }
                var pinGeocache = AddPin(locationGeocache, geocacheToolTip, color);

                foreach (var f in g.FoundGeocaches)
                {
                    if (f.PersonID == ActivePersonID)
                    {
                        pinGeocache = AddPin(locationGeocache, geocacheToolTip, Colors.Green);
                    }
                }
                // Put delay here to check asynchronisity on geocache-pins.
                await Task.Delay(0);

                // Change color on Geocache-pins on click.
                pinGeocache.MouseDown += async (s, a) =>
                {
                    a.Handled = true;
                    Geocache activeGeocache = new Geocache();
                    List<int> ActivePersonFoundGeocachIDs = new List<int>();

                    var task1 = Task.Run(() =>
                    {
                        activeGeocache = database.Geocache.Include(e => e.Person).First(gc => gc.Coordinates.Latitude == locationGeocache.Latitude && g.Coordinates.Longitude == locationGeocache.Longitude);
                        ActivePersonFoundGeocachIDs = database.FoundGeocache.Where(f => f.PersonID == ActivePersonID).Select(f => f.GeocacheID).ToList();
                    });
                    await (task1);

                    // If ActivePerson is selected and is not the one who placed the geocache...
                    if (ActivePersonID != activeGeocache.Person.ID && ActivePersonID != 0)
                    {
                        //... and ActivePerson already found this geocachen, then change the pin-color to red and remove from FoundGeocache.
                        if (ActivePersonFoundGeocachIDs.Contains(activeGeocache.ID))
                        {
                            pinGeocache = AddPin(locationGeocache, geocacheToolTip, Colors.Red);
                            var task2 = Task.Run(() =>
                            {
                                var foundGeocacheToDelete = database.FoundGeocache.First(fg => (fg.PersonID == ActivePersonID) && (fg.GeocacheID == activeGeocache.ID));
                                database.Remove(database.FoundGeocache.Single(fg => fg.GeocacheID == foundGeocacheToDelete.GeocacheID && fg.PersonID == ActivePersonID));
                            });
                            await (task2);
                        }
                        // Else change pin-color to green and add to FoundGeocache.
                        else
                        {
                            pinGeocache = AddPin(locationGeocache, geocacheToolTip, Colors.Green);
                            var foundGeocache = new FoundGeocache
                            {
                                GeocacheID = activeGeocache.ID,
                                PersonID = ActivePersonID
                            };
                            database.Add(foundGeocache);
                        }
                        var task3 = Task.Run(() =>
                        {
                            database.SaveChanges();
                        });
                        await (task3);

                        UpdateMap();
                    }
                };
            }
        }

        // Handle map click here.
        private void OnMapLeftClick()
        {
            ActivePersonID = 0;
            UpdateMap();
        }

        // Creates a pin with color and tooltip
        private Pushpin AddPin(Location location, string tooltip, Color color)
        {
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            ToolTipService.SetShowDuration(pin, 3000);
            layer.AddChild(pin, new Location(location.Latitude, location.Longitude));
            return pin;
        }
        #endregion

        #region On map-click create new Person & Geocache objects & pins
        // Here the geocache object and pin is created and saved to database on map right-click.
        private async void BeginOnAddGeocacheClick(object sender, RoutedEventArgs args)
        {
            using (var database = new AppDbContext())
            {
                if (ActivePersonID == 0)
                {
                    MessageBox.Show("Please select/add a person before adding a geocache.");
                }
                else
                {
                    var dialog = new GeocacheDialog();
                    dialog.Owner = this;
                    dialog.ShowDialog();
                    if (dialog.DialogResult == false)
                    {
                        return;
                    }
                    string contents = dialog.GeocacheContents;
                    string message = dialog.GeocacheMessage;

                    geocache.Coordinates = coordinates;
                    geocache.Content = contents;
                    geocache.Message = message;
                    coordinates.Longitude = latestClickLocation.Longitude;
                    coordinates.Latitude = latestClickLocation.Latitude;

                    var persons = database.Person.Include(p => p.Geocaches).Where(p => p.ID == ActivePersonID).ToArray();

                    foreach (var p in persons)
                    {
                        string geocacheToolTip = "long: " + coordinates.Longitude + "\nlat:    " + coordinates.Latitude + "\nContent: " + contents
                            + "\nMessage: " + message + "\nPlaced by: " + p.FirstName + " " + p.LastName;
                        var pin = AddPin(latestClickLocation, geocacheToolTip, Colors.Black);
                    }

                    geocache.Person.ID = ActivePersonID;
                    geocache.ID = 0;

                    var task = Task.Run(() =>
                    {
                        database.Add(geocache);
                        database.SaveChanges();
                    });
                    await (task);
                }
            }
        }

        // Here the person object and pin is created and saved to database on map right-click.
        private async void BeginOnAddPersonClick(object sender, RoutedEventArgs args)
        {
            using (var database = new AppDbContext())
            {
                var dialog = new PersonDialog();
                dialog.Owner = this;
                dialog.ShowDialog();
                if (dialog.DialogResult == false)
                {
                    return;
                }

                string firstName = dialog.PersonFirstName;
                string lastName = dialog.PersonLastName;
                string city = dialog.AddressCity;
                string country = dialog.AddressCountry;
                string streetName = dialog.AddressStreetName;
                int streetNumber = dialog.AddressStreetNumber;

                person.Coordinates = coordinates;
                person.FirstName = firstName;
                person.LastName = lastName;
                person.Country = country;
                person.City = city;
                person.StreetName = streetName;
                person.StreetNumber = (byte)streetNumber;
                coordinates.Longitude = latestClickLocation.Longitude;
                coordinates.Latitude = latestClickLocation.Latitude;

                string personToolTip = person.FirstName + " " + person.LastName + "\n" + person.StreetName + " " + person.StreetNumber + "\n" + person.City + "\n" + person.Country;
                var pin = AddPin(latestClickLocation, personToolTip, Colors.Blue);

                var task = Task.Run(() =>
                {
                    database.Add(person);
                    database.SaveChanges();
                });
                await (task);

                ActivePersonID = person.ID;
                person.ID = 0;
                UpdateMap();
            }
        }
        #endregion

        #region Load/save from/to database
        // Load from file.
        private async void OnLoadFromFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }
            string path = dialog.FileName;

            // Clear database and reset map.
            using (var database = new AppDbContext())
            {
                var task = Task.Run(() =>
                {
                    database.Person.RemoveRange(database.Person);
                    database.Geocache.RemoveRange(database.Geocache);
                    database.SaveChanges();
                });
                await (task);

                ActivePersonID = 0;
                layer.Children.Clear();
            }

            // Read and split selected file here.
            List<Person> personsTotalList = new List<Person> { };
            string textFromFile = File.ReadAllText(path, Encoding.Default);
            string[] textParts = textFromFile.Split(new string[] { Environment.NewLine + Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            // Loop to get person objects and there placed geocaches to lists.
            foreach (string part in textParts)
            {
                List<Geocache> geocaches = new List<Geocache> { };
                string[] lines = part.Split('\n');

                foreach (string line in lines)
                {
                    if (char.IsDigit(line[0]))
                    {
                        string[] values = line.Split('|');
                        Geocache g = new Geocache
                        {
                            Content = values[3].Trim(),
                            Message = values[4].Trim(),
                            Coordinates = new GeoCoordinate
                            {
                                Latitude = Convert.ToDouble(values[1]),
                                Longitude = Convert.ToDouble(values[2])
                            }
                        };
                        geocaches.Add(g);
                    }
                    else if (line.StartsWith("Found:") == false && char.IsDigit(line[0]) == false)
                    {
                        string[] values = line.Split('|');
                        Person p = new Person
                        {
                            FirstName = values[0].Trim(),
                            LastName = values[1].Trim(),
                            Country = values[2].Trim(),
                            City = values[3].Trim(),
                            StreetName = values[4].Trim(),
                            StreetNumber = Convert.ToByte(values[5]),
                            Geocaches = geocaches,
                            Coordinates = new GeoCoordinate
                            {
                                Latitude = Convert.ToDouble(values[6]),
                                Longitude = Convert.ToDouble(values[7])
                            }
                        };
                        personsTotalList.Add(p);
                    }
                }
            }

            // Loop to put all geocaches in geocachesTotalList.
            List<Geocache> geocachesTotalList = new List<Geocache> { };
            foreach (Person p in personsTotalList)
            {
                foreach (Geocache g in p.Geocaches)
                {
                    geocachesTotalList.Add(g);
                }
            }

            // Loop to get FoundGeocaches and connections to join-tabel to a dictionary.
            Dictionary<Person, List<int>> foundGeocachesDictionary = new Dictionary<Person, List<int>>();
            foreach (string part in textParts)
            {
                List<int> indexes = new List<int>();
                string[] lines = part.Split('\n');

                foreach (string line in lines)
                {
                    string ln1 = line.Substring(6);
                    if (line.StartsWith("Found:") && ln1 != " " && ln1 != "")
                    {
                        string[] numbers = ln1.Split(',');
                        foreach (string n in numbers)
                        {
                            int n1 = Convert.ToInt32(n);
                            indexes.Add(n1);
                        }
                    }
                    else if (line.StartsWith("Found:") == false && char.IsDigit(line[0]) == false)
                    {
                        string[] values = line.Split('|');
                        foreach (Person p in personsTotalList)
                        {
                            if (p.FirstName == values[0].Trim() && p.LastName == values[1].Trim())
                            {
                                foundGeocachesDictionary[p] = indexes;
                            }
                        };
                    }
                }
            }

            // Loop through dictionary and make a lista of FoundGeocaches.
            List<FoundGeocache> foundGeocaches = new List<FoundGeocache>();
            foreach (KeyValuePair<Person, List<int>> pair in foundGeocachesDictionary)
            {
                foreach (int i in pair.Value)
                {
                    FoundGeocache fg = new FoundGeocache
                    {
                        Person = pair.Key,
                        Geocache = geocachesTotalList[i - 1]
                    };
                    foundGeocaches.Add(fg);
                }
            }

            // Loop through all persons and add to foundGeocaches list.
            foreach (Person p in personsTotalList)
            {
                p.FoundGeocaches = new List<FoundGeocache>();
                foreach (FoundGeocache fg in foundGeocaches)
                {
                    if (fg.Person.FirstName == p.FirstName && fg.Person.LastName == p.LastName)
                    {
                        p.FoundGeocaches.Add(fg);
                    }
                }
            }

            // Loop through all geocaches and add to foundGeocaches list.
            foreach (Geocache g in geocachesTotalList)
            {
                g.FoundGeocaches = new List<FoundGeocache>();
                foreach (FoundGeocache fg in foundGeocaches)
                {
                    if (fg.Geocache.Message == g.Message && fg.Geocache.Content == g.Content)
                    {
                        g.FoundGeocaches.Add(fg);
                    }
                }
            }

            // Save all persons and geocaches to database, asynchronously.
            using (var database = new AppDbContext())
            {
                foreach (Person p in personsTotalList)
                {
                    database.Add(p);
                }

                foreach (Geocache g in geocachesTotalList)
                {
                    database.Add(g);
                }

                var task = Task.Run(() =>
                {
                    database.SaveChanges();
                });
                await (task);
            }
            UpdateMap();
        }

        // Save to file
        private async void OnSaveToFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            dialog.FileName = "Geocaches";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            string path = dialog.FileName;

            using (var database = new AppDbContext())
            {
                string all = "";

                int minGeocacheID = 0;
                var task = Task.Run(() =>
                {
                    minGeocacheID = database.Geocache.Min(g => (int)g.ID);
                });
                await (task);

                IEnumerable<Person> PersonAsync = new List<Person>();
                var task1 = Task.Run(() =>
                {
                    PersonAsync = database.Person.Include(e => e.Geocaches).Include(p => p.FoundGeocaches).ThenInclude(fg => fg.Person).Include(p => p.FoundGeocaches).ThenInclude(fg => fg.Geocache).AsNoTracking();
                });
                await (task1);

                foreach (var x in PersonAsync)
                {
                    string geocachesString = "";
                    string foundString = "";

                    string personString = (x.FirstName + " | " + x.LastName + " | " + x.Country + " | " + x.City + " | " + x.StreetName + " | " + x.StreetNumber + " | " +
                            x.Coordinates.Latitude + " | " + x.Coordinates.Longitude);

                    foreach (Geocache g in x.Geocaches)
                    {
                        geocachesString = geocachesString + ((g.ID - minGeocacheID + 1) + " | " + g.Coordinates.Latitude + " | " + g.Coordinates.Longitude + " | " + g.Content + " | " + g.Message + "\r\n");
                    }

                    foreach (FoundGeocache fg in x.FoundGeocaches)
                    {
                        foundString = foundString + (fg.Geocache.ID - minGeocacheID + 1 + ", ");
                    }

                    if (foundString != "")
                    {
                        all = all + (personString + "\r\n" + geocachesString + "Found: " + foundString.Substring(0, foundString.Length - 2) + "\r\n") + "\r\n";
                    }
                    else
                    {
                        all = all + (personString + "\r\n" + geocachesString + "Found: " + foundString + "\r\n") + "\r\n";
                    }
                }
                File.WriteAllText(path, all, Encoding.Default);
            }
        }
        #endregion
    }
}