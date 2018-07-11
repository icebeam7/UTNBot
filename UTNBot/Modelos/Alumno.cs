using System;

namespace UTNBot.Modelos
{
    [Serializable]
    public class Alumno
    {
        public string Nombre { get; set; }
        public string Sexo { get; set; }
        public string FotoURL { get; set; }
        public string TiempoViaje { get; set; }
        public string TiempoEstudio { get; set; }
        public string Internet { get; set; }
        public string G1 { get; set; }
        public string G2 { get; set; }
    }
}
