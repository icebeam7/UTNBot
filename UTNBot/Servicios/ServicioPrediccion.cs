using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Collections.Generic;
using Newtonsoft.Json;
using UTNBot.Helpers;
using UTNBot.Modelos;

namespace UTNBot.Servicios
{
    public static class ServicioPrediccion
    {
        public async static Task<string> PredecirCalificacion(Alumno alumno)
        {
            try
            {
                var datos = new
                {
                    Inputs = new Dictionary<string, StringTable>()
                {
                    {
                        "input1",
                            new StringTable()
                            {
                                ColumnNames = new string[]
                                {
                                    "sex",
                                    "traveltime",
                                    "studytime",
                                    "internet",
                                    "G1",
                                    "G2",
                                    "G3"
                                },
                                Values = new string[,]
                                {
                                    {
                                        alumno.Sexo,
                                        alumno.TiempoViaje,
                                        alumno.TiempoEstudio,
                                        alumno.Internet,
                                        alumno.G1,
                                        alumno.G2,
                                        "0",
                                    },
                                }
                            }
                    },
                },
                    GlobalParameters = new Dictionary<string, string>()
                    {
                    }
                };

                var json = JsonConvert.SerializeObject(datos);

                using (var cliente = new HttpClient())
                {
                    cliente.DefaultRequestHeaders.Authorization = new
                        AuthenticationHeaderValue("Bearer", Constantes.PredictionServiceKey);
                    cliente.BaseAddress = new Uri(Constantes.PredictionServiceURL);

                    var content = new StringContent(json);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var response = await cliente.PostAsync("", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var resultado = await response.Content.ReadAsStringAsync();
                        var prediccion = JsonConvert.DeserializeObject<Prediccion>(resultado);

                        return prediccion.Results.output1.value.Values[0][0];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
