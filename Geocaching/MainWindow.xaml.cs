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

namespace Geocaching
{
    class AppDbContext : DbContext
    {
        public DbSet<Person> Person { get; set; }
        public DbSet<Geocache> Geocache { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(@"Data Source=(local)\SQLEXPRESS;Initial Catalog=Geocaching;Integrated Security=True");
        }
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<FoundGeocache>().HasKey(ap => new { ap.PersonID, ap.GeocacheID });
                                 
            model.Entity<Person>().OwnsOne(o => o.Coordinates, a =>
            {
                a.Property(p => p.Latitude).HasColumnName("Latitude");
                a.Property(p => p.Longitude).HasColumnName("Longitude");
                a.Ignore(p => p.Altitude);
                a.Ignore(p => p.Course);
                a.Ignore(p => p.HorizontalAccuracy);
                a.Ignore(p => p.Speed);
                a.Ignore(p => p.VerticalAccuracy);
            });

            model.Entity<Geocache>().OwnsOne(o => o.Coordinates, a =>
            {
                a.Property(p => p.Latitude).HasColumnName("Latitude");
                a.Property(p => p.Longitude).HasColumnName("Longitude");
                a.Ignore(p => p.Altitude);
                a.Ignore(p => p.Course);
                a.Ignore(p => p.HorizontalAccuracy);
                a.Ignore(p => p.Speed);
                a.Ignore(p => p.VerticalAccuracy);
            });
        }
    }

    public class Person
    {
        [Key]
        public int ID { get; set; }
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }
        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }
        public Coordinates Coordinates { get; set; }
        [Required]
        [MaxLength(50)]
        public string Country { get; set; }
        [Required]
        [MaxLength(50)]
        public string City { get; set; }
        [Required]
        [MaxLength(50)]
        public string StreetName { get; set; }
        [Required]
        public byte StreetNumber { get; set; }
        public List<FoundGeocache> FoundGeocache { get; set; }
        public List<Geocache> Geocache { get; set; }
    }

    public class Geocache
    {
        [Key]
        public int ID { get; set; }
        public Person Person { get; set; }
        public Coordinates Coordinates { get; set; }
        [Required]
        [MaxLength(255)]
        public string Content { get; set; }
        [Required]
        [MaxLength(255)]
        public string Message { get; set; }
        public List<FoundGeocache> FoundGeocache { get; set; }
    }

    public class FoundGeocache
    {
        public int PersonID { get; set; }
        public Person Person { get; set; }
        public int GeocacheID { get; set; }
        public Geocache Geocache { get; set; }
    }

    public class Coordinates : GeoCoordinate
    {
        private double latitude;
        [Required]
        public double Latitude
        {
            get
            {
                return latitude;
            }
            set
            {
                //Avrundar latitude till 6 decimaler
                latitude = double.Parse(value.ToString("0.000000"));
            }
        }
        private double longitude;
        [Required]
        public double Longitude
        {
            get
            {
                return longitude;
            }
            set
            {
                longitude = double.Parse(value.ToString("0.000000"));
            }
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static AppDbContext database;

        // Contains the ID string needed to use the Bing map.
        // Instructions here: https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-dev-center-help/getting-a-bing-maps-key
        private const string applicationId = "AsAxqI2jwZYAKchAD0G8TytQ0ZavSgXYlloAUiMBEIqXLq93ERbTF2pgs2V2e6tT";

        private MapLayer layer;

        // Contains the location of the latest click on the map.
        // The Location object in turn contains information like longitude and latitude.
        private Location latestClickLocation;

        private Location gothenburg = new Location(57.719021, 11.991202);

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            //Gör att komma och punkt inte ställer till med problem
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if (applicationId == null)
            {
                MessageBox.Show("Please set the applicationId variable before running this program.");
                Environment.Exit(0);
            }

            CreateMap();

            //Härifrån anropas databasen
            using (database = new AppDbContext())
            {
                // Load data from database and populate map here.

                //PopulateDatabase();
            }
        }

        private static void ClearDatabase()
        {
            database.Person.RemoveRange(database.Person);
            database.Geocache.RemoveRange(database.Geocache);
            database.SaveChanges();
        }

        /*
        private static void PopulateDatabase()
        {
            var persons = ReadPersons();
            var geocaches = ReadGeocaches();
            foreach (var person in songs.Values)
            {
                database.Add(song);
                database.SaveChanges();
            }
        }
        */

        /*
        //Härifrån anropas databasen
        private AppDbContext db = new AppDbContext()
        {

        };
        */
        
        //Behöver inte göras något här
        private void CreateMap()
        {
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = gothenburg;
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);

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
            addPersonMenuItem.Click += OnAddPersonClick;

            var addGeocacheMenuItem = new MenuItem { Header = "Add Geocache" };
            map.ContextMenu.Items.Add(addGeocacheMenuItem);
            addGeocacheMenuItem.Click += OnAddGeocacheClick;
        }

        private void UpdateMap()
        {
            // It is recommended (but optional) to use this method for setting the color and opacity of each pin after every user interaction that might change something.
            // This method should then be called once after every significant action, such as clicking on a pin, clicking on the map, or clicking a context menu option.
        }

        private void OnMapLeftClick()
        {
            // Handle map click here.
            UpdateMap();
        }

        private void OnAddGeocacheClick(object sender, RoutedEventArgs args)
        {
            //MenuItem map = (MenuItem)sender;
            database = new AppDbContext();
            Geocache geocache = new Geocache();
            Coordinates coordinates = new Coordinates();

            if (geocache.Person == null)
                {
                    MessageBox.Show("Please select a person before adding a geocache.");
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

                //Här skapas geocach objektet som sedan kan sparas i databasen
                string contents = dialog.GeocacheContents;
                string message = dialog.GeocacheMessage;
                // Add geocache to map and database here.
                var pin = AddPin(latestClickLocation, "Geocache", Colors.Gray);
                geocache.Coordinates = coordinates;
                geocache.Message = message;
                geocache.Content = contents;
                coordinates.Longitude = latestClickLocation.Longitude;
                coordinates.Latitude = latestClickLocation.Latitude;

                database.Add(geocache);
                database.SaveChanges();
                database.Dispose();

                //En eventhandler. Denna kod körs om man klickar på markören för en geocach
                pin.MouseDown += (s, a) =>
                {
                    // Handle click on geocache pin here.
                    MessageBox.Show("You clicked a geocache");
                    UpdateMap();

                    // Prevent click from being triggered on map.
                    a.Handled = true;
                };
            }            
        }

        //Här läggs personuppgifterna in
        private void OnAddPersonClick(object sender, RoutedEventArgs args)
        {
            database = new AppDbContext();
            Person person = new Person();
            Coordinates coordinates = new Coordinates();

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
            // Add person to map and database here.
            
            var pin = AddPin(latestClickLocation, "Person", Colors.Blue);

            person.Coordinates = coordinates;
            person.FirstName = firstName;
            person.LastName = lastName;
            person.Country = country;
            person.City = city;
            person.StreetName = streetName;
            person.StreetNumber = (byte)streetNumber;
            coordinates.Longitude = latestClickLocation.Longitude;
            coordinates.Latitude = latestClickLocation.Latitude;

            database.Add(person);
            database.SaveChanges();
            database.Dispose();

            pin.MouseDown += (s, a) =>
            {
                // Handle click on person pin here.
                MessageBox.Show("You clicked a person");
                UpdateMap();

                // Prevent click from being triggered on map.
                a.Handled = true;
            };
        }

        //Skapar en pin med färg
        private Pushpin AddPin(Location location, string tooltip, Color color)
        {
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            layer.AddChild(pin, new Location(location.Latitude, location.Longitude));
            return pin;
        }

        //Läser in från en fil
        private void OnLoadFromFileClick(object sender, RoutedEventArgs args)
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
            ClearDatabase();
                
            // Read the selected file here.

            List<Person> persons = new List<Person>();
            List<Geocache> geocaches = new List<Geocache>();
            


            /*
            var songs = new Dictionary<int, Song>();

            string[] lines = File.ReadAllLines("Songs.csv").Skip(1).ToArray();
            foreach (string line in lines)
            {
                try
                {
                    string[] values = line.Split('|').Select(v => v.Trim()).ToArray();

                    int id = int.Parse(values[0]);
                    byte trackNumber = byte.Parse(values[1]);
                    string title = values[2];

                    string[] lengthParts = values[3].Split(':');
                    int minutes = int.Parse(lengthParts[0]);
                    int seconds = int.Parse(lengthParts[1]);
                    Int16 length = Convert.ToInt16(minutes * 60 + seconds);

                    bool hasMusicVideo;
                    if (values[4].ToUpper() == "Y") hasMusicVideo = true;
                    else if (values[4].ToUpper() == "N") hasMusicVideo = false;
                    else throw new FormatException("Boolean string must be either Y or N.");

                    int albumId = int.Parse(values[5]);

                    // If there are lyrics, add them, otherwise let them be null.
                    string lyrics = null;
                    if (values.Length == 7)
                    {
                        lyrics = values[6];
                    }

                    songs[id] = new Song
                    {
                        TrackNumber = trackNumber,
                        Title = title,
                        Length = length,
                        HasMusicVideo = hasMusicVideo,
                        Lyrics = lyrics,
                        Album = albums[albumId]
                    };
                }
                catch
                {
                    Console.WriteLine("Could not read song: " + line);
                }
            }
            return songs;
            */
        }


        //Sparar till en fil
        private void OnSaveToFileClick(object sender, RoutedEventArgs args)
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
            // Write to the selected file here.
        }
    }
}
