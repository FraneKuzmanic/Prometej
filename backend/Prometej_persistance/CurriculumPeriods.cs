using Prometej_core.Models.efModels;

namespace Prometej_persistance
{
    // The literary periods of the school curriculum for Croatian (NN 10/2019), which are also
    // the period terms of the state matura's exam catalogue, in chronological order. Ids 1 to 6
    // predate the list, which is why they are not in order.
    public static class CurriculumPeriods
    {
        public static readonly Period[] All =
        [
            new()
            {
                Id = 1,
                SortOrder = 1,
                Name = "Antika",
                TimeFrame = "8. st. pr. Kr. – 5. st.",
                Image = "antika.webp",
                Description = "Zaplovite u književnost stare Grčke i Rima: od Homerovih epova i Sofoklovih tragedija do Plautovih komedija i Vergilijeve Eneide.",
            },
            new()
            {
                Id = 2,
                SortOrder = 2,
                Name = "Srednji vijek",
                TimeFrame = "5. – 15. st.",
                Image = "srednji-vijek.webp",
                Description = "Upoznajte najdulje razdoblje europske književnosti: junačke epove i viteške romane, crkvena prikazanja i prve hrvatske spomenike pisane glagoljicom.",
            },
            new()
            {
                Id = 7,
                SortOrder = 3,
                Name = "Humanizam i predrenesansa",
                TimeFrame = "14. – 15. st.",
                Image = "humanizam-i-predrenesansa.webp",
                Description = "Upoznajte Dantea, Petrarcu i Boccaccia te humaniste koji su, vraćajući se antici, čovjeka ponovno stavili u središte zanimanja.",
            },
            new()
            {
                Id = 3,
                SortOrder = 4,
                Name = "Renesansa",
                TimeFrame = "15. – 16. st.",
                Image = "renesansa.webp",
                Description = "Otkrijte književnost koja u središte stavlja čovjeka: Marulićevu Juditu, Držićeve komedije, Shakespeareova Hamleta i Cervantesova Don Quijotea.",
            },
            new()
            {
                Id = 8,
                SortOrder = 5,
                Name = "Barok",
                TimeFrame = "17. st.",
                Image = "barok.webp",
                Description = "Zavirite u književnost kićenoga stila, kontrasta i prolaznosti: od Gundulićeva Osmana i Dubravke do Calderónova kazališta.",
            },
            new()
            {
                Id = 9,
                SortOrder = 6,
                Name = "Klasicizam",
                TimeFrame = "17. – 18. st.",
                Image = "klasicizam.webp",
                Description = "Otkrijte doba razuma, reda i strogih pravila, Molièreovih komedija i prosvjetiteljskih ideja koje su mijenjale Europu.",
            },
            new()
            {
                Id = 10,
                SortOrder = 7,
                Name = "Predromantizam",
                TimeFrame = "druga polovica 18. st.",
                Image = "predromantizam.webp",
                Description = "Pratite kako osjećaji potiskuju razum: Goetheov Werther, Sturm und Drang i prvi nagovještaji romantizma.",
            },
            new()
            {
                Id = 5,
                SortOrder = 8,
                Name = "Romantizam",
                TimeFrame = "kraj 18. – sredina 19. st.",
                Image = "romantizam.webp",
                Description = "Osjetite romantizam, razdoblje koje obilježava osjećajnost, maštu, prirodu, fokus na pojedinca, njegovu impresiju i ekspresiju.",
            },
            new()
            {
                Id = 4,
                SortOrder = 9,
                Name = "Realizam",
                TimeFrame = "druga polovica 19. st.",
                Image = "realizam.webp",
                Description = "Realizam obilježava realan opis svijeta. Fokus je na dugačkom i detaljnom opisu likova i prostora što donosi brojne zanimljivosti iz tog vremena.",
            },
            new()
            {
                Id = 6,
                SortOrder = 10,
                Name = "Modernizam",
                TimeFrame = "sredina 19. – početak 20. st.",
                Image = "modernizam.webp",
                Description = "Modernizam obilježava razdoblje estetike u književnosti, umjetnost radi same sebe. Zavirite u čari modernizma, njegove teme, motive i najpoznatije autore.",
            },
            new()
            {
                Id = 11,
                SortOrder = 11,
                Name = "Ekspresionizam",
                TimeFrame = "1910. – 1925.",
                Image = "ekspresionizam.webp",
                Description = "Uđite u književnost krika, pobune i slobodnoga stiha, uz A. B. Šimića, Kafku i ranoga Krležu.",
            },
            new()
            {
                Id = 12,
                SortOrder = 12,
                Name = "Suvremena književnost",
                TimeFrame = "od kraja 1920-ih do danas",
                Image = "suvremena-knjizevnost.webp",
                Description = "Istražite književnost nakon ekspresionizma: Krležine Glembajeve, egzistencijalizam i Camusova Stranca, Marinkovićeva Kiklopa i postmodernizam.",
            },
        ];
    }
}
