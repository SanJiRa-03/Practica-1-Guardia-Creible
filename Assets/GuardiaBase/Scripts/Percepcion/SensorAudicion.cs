using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class SensorAudicion : MonoBehaviour, ISensor
    {
        //Variables:
        Vector3 posicionRuido;
        private float volumen;
        private bool hayRuido = false;
        public float alcance = 15f;

        //Para evitar que el guardia persigue constantemente al jugador:
        [Tooltip("UmbralRuidoFuerte")]
        public float umbralRuidoFuerte = 0.5f;

        //La función Percibir se encarga de determinar si el guardia puede percibir un ruido emitido en el mundo. Calcula la distancia entre el guardia y la posición del ruido, y luego aplica una atenuación basada en esa distancia para determinar el volumen percibido. Si hay un ruido y su volumen es significativo, se actualiza el Blackboard con la posición y el volumen del ruido, y se resetea la variable hayRuido para evitar que el guardia siga escuchando un ruido que ya no existe.
        public void Percibir(Blackboard bb)
        {            
            if (hayRuido)
            {
                float distancia = Vector3.Distance(transform.position, posicionRuido);
                float atenuacion = 1 - Mathf.Clamp01(distancia / alcance);
                float volumenAtenuado = volumen * atenuacion;

                hayRuido = false; // Resetea la variable para que no se quede escuchando un ruido que ya no existe.


                if (volumenAtenuado > 0.1f) // Umbral para considerar que el guardia ha oído el ruido.
                {
                    bb.Set(ClavesBB.VolumenRuido, volumenAtenuado);
                    bb.Set(ClavesBB.PosicionRuido, posicionRuido);

                    if (volumenAtenuado > umbralRuidoFuerte)
                    {
                        bb.Set(ClavesBB.TieneRuidoPendiente, true);
                    }
                }
                else
                {
                    bb.Set(ClavesBB.VolumenRuido, 0f);
                }                               
            } 
            else
            {
                bb.Set(ClavesBB.VolumenRuido, 0f);
            }
        }

        private void AlOirRuido(Vector3 posicion, float volumen)
        {
            posicionRuido = posicion;
            this.volumen = volumen;
            hayRuido = true;
        }
        //Se ejecuta cuando el guardia se activa.
        private void OnEnable()
        {
            RuidoMundo.AlEmitir += AlOirRuido;
        }
        //Cuando se desactiva o se destruye.
        private void OnDisable()
        {
            RuidoMundo.AlEmitir -= AlOirRuido;
        }
    }
}