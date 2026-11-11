using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class EmisorPasos : MonoBehaviour
    {
        //Variables:
        public float volumenPaso = 1.0f;

        //Método idéntico al de ThirdPersonController.cs, que sirve para emitir el ruido cada vez que el jugador da un paso.
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                RuidoMundo.Emitir(transform.position, volumenPaso);
            }
        }

    }
}