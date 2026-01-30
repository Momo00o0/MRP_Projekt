# Media Ratings Platform (MRP)

Eine eigenständige RESTful-Backend-Applikation zur Verwaltung, Bewertung und Empfehlung von  
Medieninhalten (Filme, Serien und Spiele).

Dieses Projekt wurde im Rahmen eines Hochschulprojekts an der **FH Technikum Wien** umgesetzt.

---

## 🚀 Projektübersicht

Die **Media Ratings Platform (MRP)** ist ein HTTP/REST-basierter Server, der eine API für mögliche  
Frontends (z. B. Web, Mobile, Konsole) bereitstellt.  
Der Fokus liegt ausschließlich auf der **Backend-Entwicklung** – ein Frontend ist **nicht Bestandteil**  
des Projekts.

Schwerpunkte des Projekts sind:
- RESTful API Design
- Authentifizierung & Autorisierung
- Geschäftslogik
- Datenbankanbindung
- Testbarkeit und saubere Architektur

---

## 🛠️ Technologie-Stack

- **Programmiersprache:** C#  
- **HTTP-Server:** Eigener HTTP-Service (kein ASP.NET)  
- **Datenbank:** PostgreSQL  
- **Datenbankzugriff:** ADO.NET  
- **Serialisierung:** JSON  
- **Authentifizierung:** Token-basierte Authentifizierung (Bearer Token)  
- **Containerisierung:** Docker & Docker Compose  
- **Testing:** Unit-Tests (Geschäftslogik)  
- **API-Tests:** Postman / curl  

---

## ✨ Funktionalitäten

### Benutzerverwaltung
- Registrierung und Login mit eindeutigen Zugangsdaten
- Token-basierte Authentifizierung
- Benutzerprofil mit persönlichen Statistiken:
  - Anzahl abgegebener Bewertungen

### Medienverwaltung
- Erstellen, Bearbeiten und Löschen von Medien (CRUD)
- Medientypen: **Filme, Serien, Spiele**
- Medienattribute:
  - Titel, Beschreibung
  - Medientyp
  - Erscheinungsjahr
  - Genre(s)
  - Altersfreigabe
- Medien können nur vom Ersteller bearbeitet oder gelöscht werden
- Automatische Berechnung der durchschnittlichen Bewertung

### Bewertungssystem
- Bewertungen von **1–5 Sternen**
- Optionale Kommentare
- Eigene Bewertungen bearbeiten oder löschen

### Verlauf
- Eigener Bewertungsverlauf einsehbar

---

## 🔐 Authentifizierung

Alle Endpunkte (außer Registrierung und Login) sind durch eine token-basierte Authentifizierung geschützt.

### Login-Beispiel
POST /api/users/login
Content-Type: application/json

{
"Username": "mustermann",
"Password": "max"
}

**Antwort:**  
Ein Token (z. B. `mustermann-mrpToken`)

### Beispiel für einen authentifizierten Request
GET /api/users/mustermann/profile
Authentication: Bearer mustermann-mrpToken
Accept: application/json

---

## 🐘 Datenbank & Docker

- Persistente Speicherung der Daten in **PostgreSQL**
- Vollständig containerisierte Umgebung mit **Docker Compose**
- Eine SQL-Datei initialisiert:
  - Datenbankschema
  - Tabellen
  - Relationen

### Start der Datenbank
```bash
docker-compose up -d
```
## 🧪 Tests

Mindestens 20 Unit-Tests

Fokus auf:

- Zentrale Geschäftslogik

- Bewertungssystem

- Empfehlungssystem

- Autorisierungslogik

## 📦 API-Tests

Bereitgestellte Postman Collection bzw. curl-Skripte

Demonstration aller relevanten Endpunkte:

- Authentifizierung

- Medienverwaltung

- Bewertungen & Favoriten

- Filter- und Empfehlungsfunktionen
