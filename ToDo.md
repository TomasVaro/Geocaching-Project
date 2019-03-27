# Projekt Geocaching
## Att göra:
### Krav för G
- [ ] Programmet ska kunna läsa in information om geocaches och personer från en textfil med ett förutbestämt format (exempeldata finns i Geocaches.txt). Informationen ska dels visas upp i programmets GUI och dels sparas i databasen.
  - [ ] Ni får inte använda SQL-kod för att skriva informationen till databasen utan måste använda EF Core till detta. (Med andra ord får ni inte importera och använda er av System.Data.SqlClient.)
  - [ ] Informationen som läses in ska ersätta all existerande information i databasen.
- [ ] Programmet ska kunna spara ner all information om geocaches och personer till en textfil med samma format som programmet kan läsa in.
  - [ ] Ni behöver inte hantera strängar som innehåller tecknet |. (Ni kan alltså anta att detta tecken inte förekommer i något av värdena som användaren matar in.)

Programmet ska visa upp all information i sitt GUI på följande sätt:
- [x] Samtliga geocaches ska visas upp som gråa markörer på kartan. När användaren håller musen över en sådan ska information om den geocachen visas i en tooltip: koordinater, meddelande, innehåll och vilken person som har placerat den.
- [x] Samtliga personer ska visas upp som blåa markörer på kartan. När användaren håller musen över en sådan ska information om den personen visas i en tooltip: namn och adress.
- [ ] När användaren klickar på en personmarkör (blå) ska alla andra personmarkörer på kartan bli halvt genomskinliga (Opacity = 0.5). Varje geocachemarkör ska bli antingen grön, röd eller svart: grön om den valda personen har hittat den geocachen, röd om den valda personen inte har hittat den geocachen och svart om den valda personen var den som placerade ut den geocachen.
- [ ] När användaren klickar på en grön geocachemarkör ska dess färg ändras till röd och vice versa. Med andra ord ska geocachen antingen läggas till i eller tas bort från listan över geocaches som den valda personen har hittat.
- [x] När användaren klickar utanför en markör ska ingen person längre vara vald och kartan ska återgå till utgångsläget (vilket beskrivs i den första punkten ovan).
- [x] När användaren högerklickar på kartan ska två alternativ visas i en kontextmeny: "Add Person" och "Add Geocache".
  - [x] Om användaren väljer "Add Person" ska en ny person skapas på de koordinater där användaren klickade. En dialogruta som låter användaren mata in information om den nya personen finns i filen PersonDialog.xaml.cs.
  - [x] Om användaren väljer "Add Geocache" ska en ny geocache skapas på de koordinater där användaren klickade. En dialogruta som låter användaren mata in information om den nya geocachen finns i filen PersonDialog.xaml.cs. Om ingen person på kartan är vald ska ett felmeddelande istället visas: "Please select a person before adding a geocache."
- [x] Varje gång programmets data ändras (exempelvis när en ny person läggs till) ska den nya datan sparas i databasen direkt.
- [x] Programmet ska skrivas "Code First", det vill säga att tabellerna ska definieras och skapas helt utifrån er C#-kod.

### Krav för VG
- [x] Utöka programmet så att det använder sig av den inbyggda GeoCoordinate-klassen från System.Device.Location i alla entity-klasser som använder sig av longitud och latitud (istället för att ta fram en egen, använda Location från Microsoft.Maps.MapControl.WPF eller ha Longitude- och Latitude-variabler i de klasser som behöver dem). Programmets databasdesign ska dock förbli oförändrad, trots att GeoCoordinate-klassen dels innehåller variabler som inte återspeglas i den förutbestämda designen och dels per automatik mappas till en separat tabell.
- [ ] Utöka programmet så att alla databasoperationer (läsa och skriva) görs asynkront med hjälp av metoderna för detta som finns i EF Core. Syftet är bland annat att säkerställa att långsamma databasoperationer (på exempelvis stora mängder data eller opålitliga databasuppkopplingar) inte påverkar användarupplevelsen.
