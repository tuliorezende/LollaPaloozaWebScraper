using System;
using System.Collections.Generic;
using System.Text;

namespace LollapaloozaSelenium.Model
{
    public class Show
    {
        public string IdShow { get; set; } = Guid.NewGuid().ToString();
        public DateTime DataHoraInicio { get; set; }
        public DateTime DataHoraFim { get; set; }
        public string NomeShow { get; set; }
        public string DescricaoShow { get; set; }
        public string IdCantor { get; set; }
        public string IdPalco { get; set; }
    }
}
