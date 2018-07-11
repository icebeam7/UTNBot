using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using UTNBot.Helpers;
using UTNBot.Modelos;

namespace UTNBot.Servicios
{
    public static class ServicioFace
    {
        public static async Task<Emocion> ObtenerEmocion(Stream foto)
        {
            Emocion emocion = null;

            try
            {
                if (foto != null)
                {
                    var clienteFace = new FaceServiceClient(Constantes.FaceApiKey, Constantes.FaceApiEndpointURL);
                    var atributosFace = new FaceAttributeType[] { FaceAttributeType.Emotion, FaceAttributeType.Age };

                    using (var stream = foto)
                    {
                        Face[] rostros = await clienteFace.DetectAsync(stream, false, false, atributosFace);

                        if (rostros.Any())
                        {
                            var analisisEmocion = rostros.FirstOrDefault().FaceAttributes.Emotion.ToRankedList().FirstOrDefault();
                            emocion = new Emocion()
                            {
                                Nombre = analisisEmocion.Key,
                                Score = analisisEmocion.Value
                            };
                        }

                        foto.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return emocion;
        }
    }
}
