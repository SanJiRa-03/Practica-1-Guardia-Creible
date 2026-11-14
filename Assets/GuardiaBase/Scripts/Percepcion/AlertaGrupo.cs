using System;
using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public static class AlertaGrupo
    {
        // emisor, posición donde está el jugador
        public static event Action<GameObject, Vector3> AlEmitir;

        public static void Emitir(GameObject emisor, Vector3 posicionJugador)
        {
            AlEmitir?.Invoke(emisor, posicionJugador);
        }
    }
}