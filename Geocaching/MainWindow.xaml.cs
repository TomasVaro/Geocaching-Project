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
    class AppDbContext : DbContext
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
        Geocache geocache = new Geocache() { Person = new Person() };
        FoundGeocache foundGeocache = new FoundGeocache();
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
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if (applicationId == null)
            {
                MessageBox.Show("Please set the applicationId variable before running this program.");
                Environment.Exit(0);
            }

            CreateMap();
            ColorPersonPinsAsync();
            ColorGeocachePinsAsync();
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

            // Återställer alla pins till blå/grå vid klick på kartan.
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
            addPersonMenuItem.Click += OnAddPersonClickAsync;

            var addGeocacheMenuItem = new MenuItem { Header = "Add Geocache" };
            map.ContextMenu.Items.Add(addGeocacheMenuItem);
            addGeocacheMenuItem.Click += OnAddGeocacheClickAsync;
        }

        private void UpdateMap()
        {
            // It is recommended (but optional) to use this method for setting the color and opacity of each pin after every user interaction that might change something.
            // This method should then be called once after every significant action, such as clicking on a pin, clicking on the map, or clicking a context menu option.
            ColorPersonPinsAsync();
            ColorGeocachePinsAsync();
        }

        // Loopar igenom Person och sätter rätt färg på pinsen asynkront.
        private async void ColorPersonPinsAsync()
        {
            database = new AppDbContext();
            foreach (var p in database.Person.Include(e => e.Geocache).Include(p => p.FoundGeocache))
            {
                Location location = new Location(p.Coordinates.Latitude, p.Coordinates.Longitude);
                string personToolTip = p.FirstName + " " + p.LastName + "\n" + p.StreetName + " " + p.StreetNumber + "\n" + p.City + "\n" + p.Country;

                // Sätter blå färg på vald person-pin och ljusblå på övriga person-pins
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
                    // Prevent click from being triggered on map.
                    a.Handled = true;
                };
                await Task.Delay(1000);
            }
        }

        // Loopar igenom Geocache och sätter rätt färg på pinsen asynkront.
        private async void ColorGeocachePinsAsync()
        {
            database = new AppDbContext();
            foreach (var g in database.Geocache.Include(g => g.Person).Include(g => g.FoundGeocache))
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

                foreach (var f in g.FoundGeocache)
                {
                    if (f.PersonID == ActivePersonID)
                    {
                        pinGeocache = AddPin(locationGeocache, geocacheToolTip, Colors.Green);
                    }
                }

                // Körs vid klick på geocache-pinsen
                pinGeocache.MouseDown += (s, a) =>
                {
                    // Tar fram alla gånger som klickad Geocache hittats av vem som helst
                    var activeGeocache = database.Geocache.Include(e => e.Person).First(gc => gc.Coordinates.Latitude == locationGeocache.Latitude && g.Coordinates.Longitude == locationGeocache.Longitude);
                    // Lista med geocacheIDs som ActivePerson hittat
                    var ActivePersonFoundGeocachIDs = database.FoundGeocache.Where(f => f.PersonID == ActivePersonID).Select(f => f.GeocacheID).ToList();

                    // Om ActivePerson inte är den som själv placerat ut geocachen och ActivePerson är vald...
                    if (ActivePersonID != activeGeocache.Person.ID && ActivePersonID != 0)
                    {
                        //... och ActivePerson redan hittat geocachen så byt färg till rött och ta bort från FoundGeocache tabellen.
                        if (ActivePersonFoundGeocachIDs.Contains(activeGeocache.ID))
                        {
                            pinGeocache = AddPin(locationGeocache, geocacheToolTip, Colors.Red);
                            var foundGeocacheToDelete = database.FoundGeocache.First(fg => (fg.PersonID == ActivePersonID) && (fg.GeocacheID == activeGeocache.ID));
                            database.Remove(database.FoundGeocache.Single(fg => fg.GeocacheID == foundGeocacheToDelete.GeocacheID && fg.PersonID == ActivePersonID));
                        }
                        // Annars byt färg till grönt och lägg till i FoundGeocache tabellen.
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
                        database.SaveChanges();
                        UpdateMap();
                        a.Handled = true;
                    }
                    // Om klickar på en svart eller grå geo-pin så ändra inte geopin-färgerna.
                    else
                    {
                        a.Handled = true;
                    }
                };
                await Task.Delay(500);
            }
        }

        // Handle map click here.
        private void OnMapLeftClick()
        {
            ActivePersonID = 0;
            UpdateMap();
        }

        //Här skapas geocache objektet och sparas till databasen vid höger-klick på kartan.
        private async void OnAddGeocacheClickAsync(object sender, RoutedEventArgs args)
        {
            using (database = new AppDbContext())
            {
                if (ActivePersonID == 0)
                {
                    MessageBox.Show("Please select/add a person before adding a geocache.");
                }
                else
                {
                    //Läser in från kartans AddGeocache-fönster.
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

                    var persons = database.Person.Include(p => p.Geocache).Where(p => p.ID == ActivePersonID).ToArray();

                    foreach (var p in persons)
                    {
                        string geocacheToolTip = "long: " + coordinates.Longitude + "\nlat:    " + coordinates.Latitude + "\nContent: " + contents
                            + "\nMessage: " + message + "\nPlaced by: " + p.FirstName + " " + p.LastName;
                        var pin = AddPin(latestClickLocation, geocacheToolTip, Colors.Black);
                    }

                    geocache.Person.ID = ActivePersonID;
                    geocache.ID = 0; //Får ej tas bort. Måste nollställas om man lägger till flera geopins efter varandra.

                    database.Add(geocache);
                    database.SaveChanges();

                    await Task.Delay(0);
                }
            }
        }

        //Här skapas person objektet och sparas till databasen vid höger-klick på kartan.
        private async void OnAddPersonClickAsync(object sender, RoutedEventArgs args)
        {
            using (database = new AppDbContext())
            {
                //Läser in från kartans AddPerson-fönster.
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

                database.Add(person);
                database.SaveChanges();

                await Task.Delay(0);

                ActivePersonID = person.ID;
                person.ID = 0;  //Får ej tas bort. Måste nollställas om man lägger till flera personpins efter varandra.

                //Uppdaterar alla pins
                UpdateMap();
            }
        }

        //Skapar en pin med färg och tooltip
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

            List<Person> persons = new List<Person> { };
            //List<Geocache> geocaches = new List<Geocache> { };
            string text = File.ReadAllText(path);
            string[] parts = text.Split(new string[] { Environment.NewLine + Environment.NewLine },
                              StringSplitOptions.RemoveEmptyEntries);

            // NOVAK: 1.loop : att få person objekter med sina egna geocaches listor 
            foreach (string part in parts)
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
                            Content = values[3],
                            Message = values[4],
                            Coordinates = new Coordinates
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
                            FirstName = values[0],
                            LastName = values[1],
                            Country = values[2],
                            City = values[3],
                            StreetName = values[4],
                            StreetNumber = Convert.ToByte(values[5]),
                            Geocache = geocaches,
                            Coordinates = new Coordinates
                            {
                                Latitude = Convert.ToDouble(values[6]),
                                Longitude = Convert.ToDouble(values[7])
                            }


                        };
                        persons.Add(p);

                    }
                    else
                    {

                    }


                }
            }
            // NOVAK : 2.Loop :att få alla geocaches i en lista geocachesTotalList
            List<Geocache> geocachesTotalList = new List<Geocache> { };
            foreach (string part in parts)
            {


                string[] lines = part.Split('\n');

                foreach (string line in lines)
                {
                    if (char.IsDigit(line[0]))

                    {
                        string[] values = line.Split('|');
                        Geocache g = new Geocache
                        {
                            Content = values[3],
                            Message = values[4],
                            Coordinates = new Coordinates
                            {
                                Latitude = Convert.ToDouble(values[1]),
                                Longitude = Convert.ToDouble(values[2])
                            }
                        };

                        geocachesTotalList.Add(g);
                    }

                    else
                    {

                    }


                }
            }

            // NOVAK : 3.Loop :att få FoundGeocaches och kåplingar för join tabell att få dictionary
            Dictionary<Person, List<int>> fgdict = new Dictionary<Person, List<int>>();

            foreach (string part in parts)
            {
                List<int> indexes = new List<int>();

                string[] lines = part.Split('\n');

                foreach (string line in lines)
                {
                    if (line.StartsWith("Found:"))

                    {
                        string ln1 = line.Substring(6);
                        if (ln1 != " ")
                        {
                            string[] numbers = ln1.Split(',');
                            foreach (string n in numbers)
                            {
                                int n1 = Convert.ToInt32(n);
                                indexes.Add(n1);
                            }
                        }

                    }
                    else if (line.StartsWith("Found:") == false && char.IsDigit(line[0]) == false)
                    {
                        string[] values = line.Split('|');
                        foreach (Person p in persons)
                        {
                            if (p.FirstName == values[0] && p.LastName == values[1])
                            {
                                fgdict[p] = indexes;
                            }



                        };


                    }
                    else
                    {

                    }


                }
            }

            //NOVAK:loopa dictionary och gjora en lista med FoundGeocaches
            List<FoundGeocache> foundGeocaches = new List<FoundGeocache>();
            foreach (KeyValuePair<Person, List<int>> pair in fgdict)
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

            //NOVAK:loopa alla personer och läga till dem foundGeocaches listan
            FoundGeocache FoundGeocache = new FoundGeocache();
            foreach (FoundGeocache fg in foundGeocaches)
            {
                //ge fg
                foreach (Person p in persons)
                {

                    if (fg.Person.FirstName == p.FirstName && fg.Person.LastName == p.LastName)
                    {
                        p.FoundGeocache.Add(fg);
                    }
                }
            }

            //NOVAK:loopa alla geocaches och läga till dem foundGeocaches listan
            foreach (FoundGeocache fg in foundGeocaches)
            {

                foreach (Geocache g in geocachesTotalList)
                {
                    if (fg.Geocache.Message == g.Message)
                    {
                        g.FoundGeocache.Add(fg);
                    }
                }
            }

            //NOVAK: add persons till databas
            var database = new AppDbContext();
            foreach (Person p in persons)
            {
                database.Add(p);
            }
            //NOVAK: add geocaches till databas

            foreach (Geocache g in geocachesTotalList)
            {
                database.Add(g);
            }
            database.SaveChanges();

        }

        private async void ClearDatabase()
        {
            database.Person.RemoveRange(database.Person);
            database.Geocache.RemoveRange(database.Geocache);
            database.SaveChanges();
            ActivePersonID = 0;
            //layer.Children.Clear();
            await Task.Delay(0);
        }

        //Sparar till en fil
        private async void OnSaveToFileClickAsync(object sender, RoutedEventArgs args)
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
            using (database = new AppDbContext())
            {
                string all = "";

                foreach (var x in database.Person.Include(e => e.Geocache).Include(p => p.FoundGeocache).ThenInclude(fg => fg.Person).Include(p => p.FoundGeocache).ThenInclude(fg => fg.Geocache))
                {
                    string geocachesString = "";
                    string foundString = "";

                    string personString = (x.FirstName + " | " + x.LastName + " | " + x.Country + " | " + x.City + " | " + x.StreetName + " | " + x.StreetNumber + " | " +
                            x.Coordinates.Latitude + " | " + x.Coordinates.Longitude);

                    foreach (Geocache g in x.Geocache)
                    {
                        geocachesString = geocachesString + (g.ID + " | " + g.Coordinates.Latitude + " | " + g.Coordinates.Longitude + " | " + g.Content + " | " + g.Message + "\r\n");
                    }

                    foreach (FoundGeocache fg in x.FoundGeocache)
                    {
                        foundString = foundString + (fg.Geocache.ID + ", ");
                    }

                    if (foundString != "")
                    {
                        all = all + (personString + "\r\n" + geocachesString + "Found: " + foundString.Substring(0, foundString.Length - 2) + "\r\n") + "\r\n";
                    }
                    else
                    {
                        all = all + (personString + "\r\n" + geocachesString + "Found: " + foundString + "\r\n") + "\r\n";
                    }

                    await Task.Delay(0);
                }
                File.WriteAllText(path, all);
            }
        }
    }
}