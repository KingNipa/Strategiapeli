using System.Collections.Generic;
using UnityEngine;

public static class NationNameGenerator
{
    private static List<string> availableNames = new List<string>
    {
        // 1–34: Major historical empires/dynasties keep "Empire" or "Dynasty"
        "Roman Empire",
        "Byzantine Empire",
        "Ottoman Empire",
        "British Empire",
        "Mongol Empire",
        "Qing Dynasty",
        "Tang Dynasty",
        "Han Dynasty",
        "Sui Dynasty",
        "Ming Dynasty",
        "Yuan Dynasty",
        "Achaemenid Empire",
        "Seleucid Empire",
        "Parthian Empire",
        "Sassanid Empire",
        "Timurid Empire",
        "Mughal Empire",
        "Maurya Empire",
        "Gupta Empire",
        "Chola Empire",
        "Vijayanagara Empire",
        "Tavastians",
        "Karelia Empire",
        "Srivijaya Empire",
        "Khmer Empire",
        "Carthaginian Empire",
        "Kingdom of Sweden",      // changed from "Sweden"
        "French Empire",
        "Austrian Empire",
        "Holy Roman Empire",
        "Dutch Empire",
        "Portuguese Empire",
        "Spanish Empire",
        "German Empire",
        "Umayyad Caliphate",
        "Abbasid Caliphate",

        // 35–49: Renamed some “lesser” historical "Empires" to simpler forms
        "Bulgaria",
        "Serbia",
        "Ethiopia",
        "Haiti",
        "Mexico",
        "Brazil",
        "Kanem-Bornu",
        "Ashanti",
        "Tibet",
        "Korea",
        "Songhai",
        "Mali",
        "Ghana",
        "Benin",
        "Kongo",

        // 50–53: Large modern nations (Asia / Oceania)
        "People's Republic of China",
        "Republic of India",
        "Japan",
        "Commonwealth of Australia",

        // 54–88: European countries (not previously listed)
        "Kingdom of Belgium",     // changed from "Belgium"
        "Kingdom of Denmark",     // changed from "Denmark"
        "Kingdom of Norway",      // changed from "Norway"
        "Atlantis",
        "Finland",
        "Iceland",
        "Poland",
        "Hungary",
        "Slovenia",
        "Croatia",
        "Bosnia and Herzegovina",
        "Montenegro",
        "North Macedonia",
        "Albania",
        "Greece",
        "Italy",
        "Switzerland",
        "Liechtenstein",
        "Luxembourg",
        "Andorra",
        "Monaco",
        "Malta",
        "San Marino",
        "Vatican City",
        "Turkey",
        "Russia",
        "Romania",
        "Ukraine",
        "Belarus",
        "Moldova",
        "Latvia",
        "Lithuania",
        "Estonia",
        "Slovakia",
        "Czech Republic",
        "Ireland",

        // 89–100: American countries not yet in the list
        "United States of America",
        "Canada",
        "Argentina",
        "Chile",
        "Peru",
        "Colombia",
        "Venezuela",
        "Bolivia",
        "Ecuador",
        "Paraguay",
        "Uruguay",

        // --- New 50 Countries (101–150) ---
        // Africa
        "Algeria",
        "Angola",
        "Botswana",
        "Cameroon",
        "Chad",
        "Congo",
        "Egypt",
        "Eritrea",
        "Gabon",
        "Kenya",
        "Libya",
        "Madagascar",
        "Kingdom of Morocco",     // changed from "Morocco"
        "Nigeria",
        "Senegal",
        "South Africa",
        "Tanzania",
        "Tunisia",
        "Uganda",
        "Zimbabwe",

        // Middle East / West Asia
        "Afghanistan",
        "Iran",
        "Iraq",
        "Israel",
        "Hashemite Kingdom of Jordan", // changed from "Jordan"
        "Kuwait",
        "Qatar",
        "Kingdom of Saudi Arabia",     // changed from "Saudi Arabia"
        "Syria",
        "United Arab Emirates",
        "Yemen",

        // Rest of Asia
        "Indonesia",
        "Malaysia",
        "Pakistan",
        "Philippines",
        "Singapore",
        "Kingdom of Thailand",    // changed from "Thailand"
        "Vietnam",
        "Bangladesh",
        "Kingdom of Bhutan",      // changed from "Bhutan"
        "Kingdom of Cambodia",    // changed from "Cambodia"

        // Americas / Caribbean
        "Bahamas",
        "Barbados",
        "Dominican Republic",
        "Guatemala",
        "Honduras",
        "Jamaica",
        "Panama",
        "Mammals",
        "Trinidad and Tobago",
        "Costa Rica"

    };

    public static string GetUniqueName()
    {
        if (availableNames.Count == 0)
        {
            //Debug.LogWarning("NationNameGenerator: No more available names!");
            return "UnnamedNation";
        }

        int index = Random.Range(0, availableNames.Count);
        string chosenName = availableNames[index];
        availableNames.RemoveAt(index);
        return chosenName;
    }
}

