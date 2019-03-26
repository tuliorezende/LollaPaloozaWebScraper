using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LollapaloozaSelenium.Model;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace LollapaloozaSelenium
{
    class Program
    {
        public static List<Stage> stages = new List<Stage>
        {
              new Stage { NomePalco = "Budweiser", IdPalcoGuid = Guid.NewGuid().ToString(), IdPalcoSite = 18160 }
            , new Stage { NomePalco = "Onix", IdPalcoGuid = Guid.NewGuid().ToString(), IdPalcoSite = 18159 }
            , new Stage { NomePalco = "Adidas", IdPalcoGuid = Guid.NewGuid().ToString(), IdPalcoSite = 20108 }
            , new Stage { NomePalco = "Perry's by Doritos", IdPalcoGuid = Guid.NewGuid().ToString(), IdPalcoSite = 20109 }
        };

        public static List<Atracao> atracoes = new List<Atracao>();
        public static List<Show> shows = new List<Show>();

        public static ChromeDriver webDriver = new ChromeDriver(@"C:\ProgramData\chocolatey\bin");

        public static string _idEvento = "039d42ab-3cc6-43cb-9e0e-244176821071";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            webDriver.Navigate().GoToUrl("https://www.lollapaloozabr.com/lineup-2019/");

            ManipulateTables("schedule-date-friday-2019-04-05");
            ManipulateTables("schedule-date-saturday-2019-04-06");
            ManipulateTables("schedule-date-sunday-2019-04-07");

            UpdateUserPhoto();

            webDriver.Quit();
            GenerateSQLInserts();

            Console.WriteLine("Finalizando WebScrapper, aperte enter para sair...");
            Console.ReadKey();
        }

        private static void GenerateSQLInserts()
        {
            using (StreamWriter writer = new StreamWriter($"{Directory.GetCurrentDirectory()}/EventManagementInserst.txt", false))
            {
                Console.WriteLine("Gravando Evento no Arquivo");
                writer.WriteLine("----------------------------------------------Event Insert----------------------------------------------");
                writer.Write(CreateEventInsert());

                Console.WriteLine("Gravando Stage no Arquivo");
                //Stage Insert
                writer.WriteLine(Environment.NewLine);
                writer.WriteLine("----------------------------------------------Stage Insert----------------------------------------------");
                writer.Write(CreateStageInserts());

                Console.WriteLine("Gravando Speaker no Arquivo");
                //Speaker Insert
                writer.WriteLine(Environment.NewLine);
                writer.WriteLine("----------------------------------------------Speaker Insert----------------------------------------------");
                writer.Write(CreateSpeakerInserts());

                Console.WriteLine("Gravando Show no Arquivo");
                //Activity Insert
                writer.WriteLine(Environment.NewLine);
                writer.WriteLine("----------------------------------------------Activity Insert----------------------------------------------");
                writer.Write(CreateActivitiesInserts());

                Console.WriteLine("Finalizando gravação do arquivo");
                writer.Flush();
            }

        }


        #region Inserts

        private static string CreateActivitiesInserts()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var item in shows)
                builder.AppendLine($"INSERT INTO [BotEventManagement].[Activity] ([ActivityId], [StartDate], [Name], [Description], [EventId], [SpeakerId], [EndDate], [StageId]) VALUES ('{item.IdShow}','{item.DataHoraInicio.ToString("dd/MM/yyyy HH:mm")}','{item.NomeShow}','{item.DescricaoShow}','{_idEvento}','{item.IdCantor}','{item.DataHoraFim.ToString("dd/MM/yyyy HH:mm")}','{item.IdPalco}')");

            return builder.ToString();
        }

        private static string CreateSpeakerInserts()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var item in atracoes)
                builder.AppendLine($"INSERT INTO [BotEventManagement].[Speaker] ([SpeakerId], [Name], [Biography], [UploadedPhoto], [EventId]) VALUES ('{item.AtracaoId}','{item.NomeAtracao}','{item.BiografiaAtracao}','{item.FotoAtracao}','{_idEvento}')");

            return builder.ToString();
        }

        private static string CreateEventInsert()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("INSERT INTO [BotEventManagement].[Event] ([EventId], [Name], [Description], [StartDate], [EndDate], [Street], [Latitude] ,[Longitude])");
            builder.AppendLine("VALUES");
            builder.AppendLine($"('{_idEvento}','Lollapalooza Brasil 2019'");

            builder.AppendLine(",'O Lollapalooza Brasil oferece muito mais do que shows incríveis dos principais artistas do momento. Aqui a experiência também conta, fazendo a situação perfeita para que jovens possam se expressar nos três dias de festival. As marcas sabem disso, e aproveitam a esta grande oportunidade de aproximação com seu target, gerando automaticamente relevância, empatia e lembrança. Para que isso aconteça, nossos patrocinadores oferecem diversas ativações para que o público tenha uma experiência inesquecível com cada marca.'");
            builder.AppendLine(",'2019-04-05T10:00:00.0000000','2019-04-07T23:00:00.0000000','Autódromo de Interlagos, em São Paulo.','-23.701018','-46.697951')");

            return builder.ToString();
        }

        private static string CreateStageInserts()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var item in stages)
                builder.AppendLine($"INSERT INTO [BotEventManagement].[Stages] ([StageId], [Name], [EventId]) VALUES ('{item.IdPalcoGuid}','{item.NomePalco.Replace("'","''")}','{_idEvento}')");

            return builder.ToString();
        }
        #endregion

        #region Sellenium
        private static void UpdateUserPhoto()
        {
            foreach (var item in atracoes)
                item.FotoAtracao = GetArtistProfile(item.FotoAtracao);
        }

        private static void ManipulateTables(string tableId)
        {
            Console.WriteLine($"----------------------------Id da Tabela: {tableId}------------------------------------");

            var match = Regex.Match(tableId, @"\d{4}-\d{2}-\d{2}");

            var showDate = DateTime.Parse(match.Value);

            var table = webDriver.FindElementById(tableId);

            //Busca todas as colunas abaixo da tabela
            var stages = FindAllColumnElements(table);

            foreach (var item in stages)
            {
                //Recupera o ID do Palco baseado nas colunas da tabela
                var stageId = item.GetAttribute("data-location-id");

                Console.WriteLine($"{Environment.NewLine}Id Palco: {stageId}");

                //Busca os shows
                var contents = FindAllShowsByClass(item);
                CreateNewShow(contents, stageId, showDate);
            }
        }

        private static ReadOnlyCollection<IWebElement> FindAllColumnElements(IWebElement table) => table.FindElements(By.TagName("td"));
        private static ReadOnlyCollection<IWebElement> FindAllShowsByClass(IWebElement stageColumn) => stageColumn.FindElements(By.ClassName("o-session"));

        private static string GetArtistProfile(string artistUrl)
        {
            webDriver.Navigate().GoToUrl(artistUrl);

            var imageElement = webDriver.FindElementByXPath("//img[contains(@class,'o-entry__featured-img') and contains(@class,'wp-post-image')]").GetAttribute("src");
            return imageElement;
        }

        private static void CreateNewShow(ReadOnlyCollection<IWebElement> showsDiv, string stageId, DateTime showDate)
        {
            foreach (var item in showsDiv)
            {
                var activityName = item.FindElement(By.ClassName("o-session__title")).GetAttribute("innerText");
                var activityHour = item.FindElement(By.ClassName("o-session__time")).GetAttribute("innerText");

                var artistProfile = item.FindElement(By.ClassName("o-session__link")).GetAttribute("href");

                var separatedActivityHour = activityHour.Split("-");

                var startHour = separatedActivityHour[0];
                var endHour = separatedActivityHour[1];

                DateTime startDate;

                if (startHour.StartsWith("11:"))
                    startDate = DateTime.Parse($"{showDate.ToLongDateString()} {startHour} AM");
                else
                    startDate = DateTime.Parse($"{showDate.ToLongDateString()} {startHour} PM");

                DateTime endDate = DateTime.Parse($"{showDate.ToLongDateString()} {endHour} PM");


                var atracao = new Atracao()
                {
                    BiografiaAtracao = $"Biografia de {activityName}",
                    FotoAtracao = artistProfile,
                    NomeAtracao = $"{activityName}"
                };

                atracoes.Add(atracao);


                var show = new Show
                {
                    DataHoraInicio = startDate,
                    DataHoraFim = endDate,
                    DescricaoShow = $"Biografia de {activityName}",
                    NomeShow = $"Show: {activityName}",
                    IdCantor = atracao.AtracaoId,
                    IdPalco = stages.Where(x => x.IdPalcoSite == int.Parse(stageId)).First().IdPalcoGuid,

                };

                shows.Add(show);

                Console.WriteLine($"{Environment.NewLine}Show: {activityName} // Horário de Início: {startHour}{Environment.NewLine}Horário de Fim: {endHour}");
            }
        }
        #endregion

    }
}
