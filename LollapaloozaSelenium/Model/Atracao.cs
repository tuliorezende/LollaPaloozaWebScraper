using System;
using System.Collections.Generic;
using System.Text;

namespace LollapaloozaSelenium.Model
{
    public class Atracao
    {
        public string AtracaoId { get; set; } = Guid.NewGuid().ToString();
        public string NomeAtracao { get; set; }
        public string BiografiaAtracao { get; set; }
        public string FotoAtracao { get; set; }       
    }
}
