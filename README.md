# 🚗 Car Rental Management System (ASP.NET Core MVC)
<style>
h2, h3 {
    font-size: 2em;
}
</style>

<p align="left">
  <img src="https://img.shields.io/badge/ASP.NET%20Core%20MVC-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" />
  <img src="https://img.shields.io/badge/Entity%20Framework-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/Architecture-N--Tier-blue?style=for-the-badge" />
</p>

## 📖 Présentation
Ce projet est une application de gestion de location de voitures développée en **ASP.NET Core MVC**. L'objectif est de fournir une plateforme robuste permettant aux utilisateurs de louer des véhicules et aux administrateurs de gérer efficacement la flotte automobile.

L'application repose sur une **architecture en couches** (N-Tier) pour garantir une séparation nette entre la logique métier, l'accès aux données et l'interface utilisateur.

---

## 🏗️ Architecture Technique
Le projet est structuré de manière modulaire :

* **`CarRental.Core`** : Contient les **Entités** (Car, Client, Rental) et la logique métier centrale.
* **`CarRental.Data`** : Gère la persistance des données via **Entity Framework Core** (Migrations, Context, Repositories).
* **`CarRental.Web`** : La couche de présentation utilisant le pattern **MVC (Model-View-Controller)**.
  * **Views** : Interfaces dynamiques en Razor (HTML/CSS/JS).
  * **Controllers** : Gestion des requêtes HTTP et coordination avec la couche Data.

---

## 🚀 Fonctionnalités Clés
- ✅ **Gestion du Catalogue** : Visualisation des véhicules disponibles avec filtres.
- ✅ **Système de Réservation** : Processus complet de location de voiture.
- ✅ **Administration** : Dashboard pour la gestion des stocks, des clients et des retours.
- ✅ **Base de données SQL** : Persistance fiable des informations transactionnelles.

---

## 🛠️ Stack Technique
- **Backend** : .NET / C#
- **Frontend** : Razor Pages, HTML5, CSS3, Bootstrap
- **Accès aux données** : Entity Framework Core (Code First)
- **Base de données** : SQL Server

---

## ⚙️ Installation & Lancement

  ### 1.Cloner le projet 
   `
   git clone [https://github.com/BADIAMOHAMEDAYMANE/Project-dotNet.git](https://github.com/BADIAMOHAMEDAYMANE/Project-dotNet.git)
   cd Project-dotNet

  ### 2.Appliquer les Migrations (Base de données) 
         dotnet ef database update --project CarRental.Data --startup-project CarRental.Web

   ### 3.Lancer l'application 
        dotnet run --project CarRental.Web

     👤 Auteur
        Mohamed Aymane Badia
Étudiant en 4ème année – IA & Data Science
