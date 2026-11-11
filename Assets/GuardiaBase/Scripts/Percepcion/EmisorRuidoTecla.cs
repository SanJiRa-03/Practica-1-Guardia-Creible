using UnityEngine;
using UnityEngine.InputSystem;

namespace UDIT.IA3.Guardia
{
    public class EmisorRuidoTecla : MonoBehaviour
    {
        public float distancia = 8f;
        public float volumen = 1.0f;

        private void Update()
        {
            //Asignamos que la tecla para tirar un objeto sea la tecla "G":
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            {
                // Emitimos un ruido en la dirección hacia adelante del jugador, a una distancia específica y con un volumen determinado.
                Vector3 posicionRuido = transform.position + transform.forward * distancia;
                // Emitimos el ruido en el mundo, lo que permitirá que los guardias lo perciban.
                RuidoMundo.Emitir(posicionRuido, volumen);
            }
        }
    }
}