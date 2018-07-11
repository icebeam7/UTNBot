using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis.Models;
using UTNBot.Helpers;
using UTNBot.Modelos;
using UTNBot.Servicios;

namespace UTNBot.Dialogs
{
    [LuisModel(Constantes.LuisApplicationId, Constantes.LuisSubscriptionKey, domain: "westus.api.cognitive.microsoft.com")]
    [Serializable]
    public class DialogoUTNBot : LuisDialog<object>
    {
        public DialogoUTNBot()
        {

        }

        public DialogoUTNBot(ILuisService service) : base(service)
        {
        }

        Alumno alumno;

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"Hola, soy UTNBot. No pude entender tu mensaje. Intenta otro mensaje, por ejemplo dime cómo te llamas y si eres hombre o mujer. También puedes decir 'Quiero registrar una foto' o 'Analiza imagen' para detectar la emoción de una persona en una foto.\n\n Intención detectada: " + string.Join(", ", result.Intents.Select(i => i.Intent));
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("Saludar")]
        public Task Saludar(IDialogContext context, LuisResult result)
        {
            EntityRecommendation EntidadNombre, EntidadSexo;

            if (result.TryFindEntity("Nombre", out EntidadNombre))
            {
                alumno = new Alumno() { Nombre = EntidadNombre.Entity };

                if (result.TryFindEntity("Sexo", out EntidadSexo))
                {
                    alumno.Sexo = EntidadSexo.Entity;
                    SolicitarGenero(context, null);
                }
                else
                    PromptDialog.Text(context, SolicitarGenero, "¿Cuál es tu sexo?");
            }
            else
                PromptDialog.Text(context, SolicitarNombre, "¿Cómo te llamas?");

            return Task.CompletedTask;
        }

        private async Task SolicitarNombre(IDialogContext context, IAwaitable<string> result)
        {
            var nombre = await result;

            var entidadNombre = new EntityRecommendation(type: "Nombre")
            {
                Entity = (nombre != null) ? nombre : "Juan Perez"
            };

            alumno = new Alumno() { Nombre = nombre };
            PromptDialog.Text(context, SolicitarGenero, "¿Eres hombre o mujer?");
        }

        private async Task SolicitarGenero(IDialogContext context, IAwaitable<string> result)
        {
            if (string.IsNullOrWhiteSpace(alumno.Sexo))
            {
                alumno.Sexo = await result;
            }

            switch (alumno.Sexo.ToLower())
            {
                case "hombre":
                case "macho":
                case "masculino":
                    alumno.Sexo = "M";
                    break;
                case "mujer":
                case "femenino":
                case "hembra":
                    alumno.Sexo = "F";
                    break;
                default:
                    alumno.Sexo = "M";
                    break;
            }

            await context.PostAsync($"Bienvenido **{this.alumno.Nombre}**. Eres del sexo **{this.alumno.Sexo}** ");
        }

        [LuisIntent("EnviarImagen")]
        public async Task EnviarImagen(IDialogContext context, LuisResult result)
        {
            PromptDialog.Attachment(context, SolicitarImagen, "Muy bien. Envía la imagen");
        }

        private async Task SolicitarImagen(IDialogContext context, IAwaitable<IEnumerable<Attachment>> result)
        {
            var imagen = await result;

            if (imagen.Count() > 0)
            {
                alumno.FotoURL = imagen.First().ContentUrl;
                await context.PostAsync($"Imagen recibida.");
                var stream = await GetImageStream(alumno.FotoURL);
                var emocion = await ServicioFace.ObtenerEmocion(stream);
                await context.PostAsync($"**{alumno.Nombre}** tu emoción es **{emocion.Nombre}** (Score: {emocion.Score})");
            }
            else
                await context.PostAsync("Error. Fotografía no detectada");
        }

        private static async Task<Stream> GetImageStream(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var uri = new Uri(url);
                return await httpClient.GetStreamAsync(uri);
            }
        }

        [LuisIntent("Describir")]
        public async Task Describir(IDialogContext context, LuisResult result)
        {
            EntityRecommendation EntidadAtributo, EntidadNumero;

            bool tieneAtributo = result.TryFindEntity("Atributo", out EntidadAtributo);
            bool tieneNumero = result.TryFindEntity("builtin.number", out EntidadNumero);

            if (tieneAtributo)
            {
                var atributo = (EntidadAtributo.Resolution["values"] as List<object>)[0].ToString();
                var numero = tieneNumero ? EntidadNumero.Resolution["value"].ToString() : "0";
                var informacion = $"{atributo}: {numero}";

                switch (atributo)
                {
                    case "tiempo de estudio":
                        alumno.TiempoEstudio = numero;
                        break;
                    case "tiempo de viaje":
                        alumno.TiempoViaje = numero;
                        break;
                    case "internet":
                        PromptDialog.Text(context, SolicitarInternet, "¿Tienes servicio de internet?");
                        informacion = $"{atributo}: {alumno.Internet}";
                        break;
                    case "calificacion uno":
                        alumno.G1 = numero;
                        break;
                    case "calificacion dos":
                        alumno.G2 = numero;
                        break;
                    default:
                        break;
                }

                await context.PostAsync($"Información recibida y asignada: {informacion}");
            }
            else
                await context.PostAsync("Error: Atributo no identificado. Intenta de nuevo.");
        }

        private async Task SolicitarInternet(IDialogContext context, IAwaitable<string> result)
        {
            var respuesta = await result;

            switch (respuesta.ToLower())
            {
                case "si":
                    alumno.Internet = "yes";
                    break;
                case "no":
                default:
                    alumno.Internet = "no";
                    break;
            }
        }

        [LuisIntent("Predecir")]
        public async Task Predecir(IDialogContext context, LuisResult result)
        {
            if (string.IsNullOrWhiteSpace(alumno.TiempoViaje) ||
                string.IsNullOrWhiteSpace(alumno.TiempoEstudio) ||
                string.IsNullOrWhiteSpace(alumno.Internet) ||
                string.IsNullOrWhiteSpace(alumno.G1) ||
                string.IsNullOrWhiteSpace(alumno.G2))
            {
                await context.PostAsync($"Para predecir tu calificación final necesito los siguientes datos faltantes: " +
                    (string.IsNullOrWhiteSpace(alumno.TiempoViaje) ? "\n\nCategoría de tiempo de viaje" : "") +
                    (string.IsNullOrWhiteSpace(alumno.TiempoEstudio) ? "\n\nCategoría de tiempo de estudio" : "") +
                    (string.IsNullOrWhiteSpace(alumno.Internet) ? "\n\nSi cuentas o no con servicio de internet" : "") +
                    (string.IsNullOrWhiteSpace(alumno.G1) ? "\n\nCalificación del primer periodo" : "") +
                    (string.IsNullOrWhiteSpace(alumno.G2) ? "\n\nCalificación del segundo periodo." : "") +
                    "\n\nEnvía estos datos indicando el tipo y el valor e intenta la predicción de nuevo");
            }
            else
            {
                await context.PostAsync($"Enviando datos...");
                var calificacionFinal = await ServicioPrediccion.PredecirCalificacion(alumno);
                await context.PostAsync($"Tu calificación esperada es: {calificacionFinal}");
            }
        }
    }
}
