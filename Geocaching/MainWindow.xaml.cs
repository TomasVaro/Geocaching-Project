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

    public class Person
    {
        [Key]
        public int ID { get; set; }
        [Required, MaxLength(50)]
        public string FirstName { get; set; }
        [Required, MaxLength(50)]
        public string LastName { get; set; }
        public Coordinates Coordinates { get; set; }
        [Required, MaxLength(50)]
        public string Country { get; set; }
        [Required, MaxLength(50)]
        public string City { get; set; }
        [Required, MaxLength(50)]
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
        [Required, MaxLength(255)]
        public string Content { get; set; }
        [Required, MaxLength(255)]
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
        /*
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        */

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
        public int ActivePersonID;
        static AppDbContext database;
        Color color = new Color();
        Person person = new Person();
        Geocache geocache = new Geocache() { Person = new Person()};
        Coordinates coordinates = new Coordinates();


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

            // Load data from database and populate map here.
            color = Colors.Blue;
            SetPersonPinColor(color);

            color = Colors.Gray;
            SetGeoPinColor(color);
        }

        private static void ClearDatabase()
        {
            database.Person.RemoveRange(database.Person);
            database.Geocache.RemoveRange(database.Geocache);
            database.SaveChanges();
        }
        
        //Behöver inte göras något här
        private void CreateMap()
        {
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = gothenburg;
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);
            WindowState = WindowState.Maximized;

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
            using (database = new AppDbContext())
            {
                database.Person.Include(e => e.Geocache);
                foreach (var e in database.Person.Include(e => e.Geocache))
                {
                    Location location = new Location(e.Coordinates.Latitude, e.Coordinates.Longitude);
                    string personToolTip = e.FirstName + " " + e.LastName + "\n" + e.StreetName + " " + e.StreetNumber + "\n" + e.City + "\n" + e.Country;

                    // Sätter blå färg på vald person-pin och ljusblå på övriga person-pins
                    if (ActivePersonID == e.ID)
                    {
                        color = Colors.Blue;

                        // loopa igenom geochache. Om ActivePersonID == Geocache.PersonID => svart pin
                        foreach (var g in database.Geocache.Include(p => p.Person))
                        {
                            // Om Geocache.PersonID = ActivePersonID så sätt geocachen till svart annars till grått
                            if (ActivePersonID == g.Person.ID)
                            {
                                Location locationGeocache = new Location(g.Coordinates.Latitude, g.Coordinates.Longitude);
                                string geocacheToolTip = "long:" + g.Coordinates.Longitude + "\nlat:    " + g.Coordinates.Latitude + "\nContent: "
                                    + g.Content + "\nMessage: " + g.Message + "\nPlaced by: " + g.Person.FirstName + " " + g.Person.LastName;

                                var pinGeocache = AddPin(locationGeocache, geocacheToolTip, Colors.Black);
                                //pin.Tag = person;
                                /*
                                pinGeocache.MouseDown += (s, a) =>
                                {
                                    // Handle click on geocache pin here.
                                    MessageBox.Show("You clicked a geocache");
                                    UpdateMap();

                                    // Prevent click from being triggered on map.
                                    a.Handled = true;
                                };*/
                            }
                            else
                            {
                                Location locationGeocache = new Location(g.Coordinates.Latitude, g.Coordinates.Longitude);
                                string geocacheToolTip = "long:" + g.Coordinates.Longitude + "\nlat:    " + g.Coordinates.Latitude + "\nContent: "
                                    + g.Content + "\nMessage: " + g.Message + "\nPlaced by: " + g.Person.FirstName + " " + g.Person.LastName;

                                var pinGeocache = AddPin(locationGeocache, geocacheToolTip, Colors.Gray);
                            }
                        }
                    }
                    else
                    {
                        color = Color.FromRgb(0x47, 0x9D, 0xEE);
                    }
                    
                    var pin = AddPin(location, personToolTip, color);

                    //Måste vara kvar för att uppdatera pinsen
                    pin.MouseDown += (s, a) =>
                    {
                        //pin.Tag = e.FirstName;
                        ActivePersonID = e.ID;
                        UpdateMap();

                        // Prevent click from being triggered on map.
                        a.Handled = true;
                    };
                }
            }
        }

        private void OnMapLeftClick()
        {
            // Handle map click here.

            color = Colors.Blue;
            SetPersonPinColor(color);

            color = Colors.Gray;
            SetGeoPinColor(color);

            ActivePersonID = 0;                
        }

        private void SetPersonPinColor(Color color)
        {
            database = new AppDbContext();
            foreach (var e in database.Person.Include(e => e.Geocache))
            {
                Location location = new Location(e.Coordinates.Latitude, e.Coordinates.Longitude);
                string personToolTip = e.FirstName + " " + e.LastName + "\n" + e.StreetName + " " + e.StreetNumber + "\n" + e.City + "\n" + e.Country;

                var pin = AddPin(location, personToolTip, color);

                //Måste vara kvar för att ändra person-pins
                pin.MouseDown += (s, a) =>
                { 
                    // Handle click on person pin here.
                    //pin.Tag = e.FirstName;
                    ActivePersonID = e.ID;
                    UpdateMap();

                    // Prevent click from being triggered on map.
                    a.Handled = true;
                };
            }                          
        }

        private void SetGeoPinColor(Color color)
        {
            database = new AppDbContext();
            foreach (var g in database.Geocache.Include(e => e.Person))
            {
                Location location = new Location(g.Coordinates.Latitude, g.Coordinates.Longitude);
                string geocacheToolTip = "long: " + g.Coordinates.Longitude + "\nlat:    " + g.Coordinates.Latitude + "\nContent: "
                    + g.Content + "\nMessage: " + g.Message + "\nPlaced by: " + g.Person.FirstName + " " + g.Person.LastName;

                var pin = AddPin(location, geocacheToolTip, color);
                //pin.Tag = person;

                //Måste vara kvar för att ändra geocache-pinsen
                pin.MouseDown += (s, a) =>
                {
                    // Handle click on geocache pin here.
                    MessageBox.Show("You clicked a geocache");
                    //UpdateMap();

                    // Prevent click from being triggered on map.
                    a.Handled = true;
                };
            };                
        }

        //Här skapas geocach objektet från kartan och sparas i databasen
        private void OnAddGeocacheClick(object sender, RoutedEventArgs args)
        {
            database = new AppDbContext();
           
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

                // Add geocache to map and database here.
                geocache.Coordinates = coordinates;
                geocache.Content = contents;
                geocache.Message = message;
                coordinates.Longitude = latestClickLocation.Longitude;
                coordinates.Latitude = latestClickLocation.Latitude;

                database.Person.Include(e => e.Geocache);
                var persons = database.Person.Where(p => p.ID == ActivePersonID).ToArray();

                foreach(var p in persons)
                {
                    string geocacheToolTip = "long: " + coordinates.Longitude + "\nlat:    " + coordinates.Latitude + "\nContent: " + contents + "\nMessage: " + message + "\nPlaced by: " + p.FirstName + " " + p.LastName;
                    var pin = AddPin(latestClickLocation, geocacheToolTip, Colors.Gray);

                    geocache.Person.ID = ActivePersonID;  //Geocaching.Geocache.Person.get returned null.
                    geocache.ID = 0;
                    //pin.Tag = person;

                    database.Add(geocache);
                    database.SaveChanges();
                    database.Dispose();
                    /*
                    //En eventhandler. Denna kod körs om man klickar på markören för en geocach
                    pin.MouseDown += (s, a) =>
                    {
                        // Handle click on geocache pin here.
                        MessageBox.Show("You clicked a geocache");
                        UpdateMap();

                        // Prevent click from being triggered on map.
                        a.Handled = true;
                    };*/
                }                
            }            
        }

        //Här läggs personuppgifterna in från kartan
        private void OnAddPersonClick(object sender, RoutedEventArgs args)
        {
            database = new AppDbContext();

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
            person.Coordinates = coordinates;
            person.FirstName = firstName;
            person.LastName = lastName;
            person.Country = country;
            person.City = city;
            person.StreetName = streetName;
            person.StreetNumber = (byte)streetNumber;
            coordinates.Longitude = latestClickLocation.Longitude;
            coordinates.Latitude = latestClickLocation.Latitude;

            string personToolTip = firstName + " " + lastName + "\n" + streetName + " " + streetNumber + "\n" + city + "\n" + country;

            //Sätter ny person-pin till blått
            var pin = AddPin(latestClickLocation, personToolTip, Colors.Blue);
            pin.Tag = person;

            //Sätter gamla person-pins till ljusblått
            color = Color.FromRgb(0x47, 0x9D, 0xEE);
            SetPersonPinColor(color);

            database.Add(person);
            database.SaveChanges();
            //database.Dispose();

            ActivePersonID = person.ID;
            person.ID = 0;

            pin.MouseDown += (s, a) =>
            {
                // Handle click on person pin here.
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
            ToolTipService.SetShowDuration(pin, 10000);
            layer.AddChild(pin, new Location(location.Latitude, location.Longitude));
            //pin.Tag = location;
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