using System;
using UnityEngine;

namespace UDIT.IA3.Guardia
{
    //Al ser una clase estática, no hereda de MonoBehaviour y por lo tanto no hay que asignarla a un GameObject. Esto es útil para eventos globales que no dependen de un objeto específico en la escena.
    public static class RuidoMundo
    {
        //Al emitir un ruido, se pasa la posición y el volumen del mismo. Los guardias escucharán este evento y reaccionarán en consecuencia.
        //Con la opción de hacer un evento, optimizamos ya que solo salta si el jugador emite un ruido y simplemente el guardia se suscribe a este evento y no tiene que estar comprobando constantemente si hay un ruido o no.
        public static event Action<Vector3, float> AlEmitir;

        public static void Emitir(Vector3 posicion, float volumen)
        {
            //El ? significa que solo se invocará el evento si hay suscriptores, evitando así una excepción si no hay ninguno.
            AlEmitir?.Invoke(posicion, volumen);
        }
    }
}