using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class EstadoInvestigar : State
    {
        // Tiempo que se queda mirando alrededor al llegar, antes de rendirse.
        private const float TiempoBusqueda = 3f;

        private float _temporizador;
        private bool _buscando;

        public EstadoInvestigar(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            _temporizador = 0f;
            _buscando = false;

            // Vamos a la última posición donde se vio al jugador.
            if (Pizarra.Get(ClavesBB.TieneUltimaPos, false))
            {
                Vector3 ultima = Pizarra.Get(ClavesBB.UltimaPos, Vector3.zero);
                Pizarra.Set(ClavesBB.Destino, ultima);
                Pizarra.Set(ClavesBB.TieneDestino, true);
                Pizarra.Set(ClavesBB.HaLlegado, false);
            }
            else
            {
                // Sin posición que investigar no tiene sentido seguir aquí.
                UnityEngine.Debug.LogWarning(
                    "[" + Agente.name + "] investiga sin tener última posición conocida.", Agente);
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
            }
        }

        internal override void Execute()
        {
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);

            // 1. Si lo vemos de nuevo con claridad, a perseguir.
            if (nivel == NivelSospecha.Alerta)
            {
                StateMachine.ChangeState(new EstadoPerseguir(StateMachine));
                return;
            }

            // 2. Si hay una nueva sospecha (vista u oída) en otro sitio,
            //    actualizamos el destino SIN reiniciar el estado.
            if (nivel == NivelSospecha.Sospecha &&
                Pizarra.Get(ClavesBB.TieneUltimaPos, false))
            {
                Vector3 ultima = Pizarra.Get(ClavesBB.UltimaPos, Vector3.zero);
                Vector3 destinoActual = Pizarra.Get(ClavesBB.Destino, Vector3.zero);

                if ((ultima - destinoActual).sqrMagnitude > 0.25f)
                {
                    Pizarra.Set(ClavesBB.Destino, ultima);
                    Pizarra.Set(ClavesBB.HaLlegado, false);
                    _buscando = false;
                    _temporizador = 0f;
                }
            }

            // 3. Al llegar, nos quedamos un rato "buscando"; luego volvemos a patrullar.
            if (Pizarra.Get(ClavesBB.HaLlegado, false))
            {
                if (!_buscando)
                {
                    _buscando = true;
                    _temporizador = 0f;
                    Pizarra.Set(ClavesBB.TieneDestino, false); // quieto
                }

                _temporizador += Time.deltaTime;

                if (_temporizador >= TiempoBusqueda)
                {
                    StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
                }
            }
        }

        internal override void Exit()
        {
            // Limpiamos para que no quede una posición "vieja" que reactive
            // la investigación más tarde.
            Pizarra.Set(ClavesBB.TieneUltimaPos, false);
            _buscando = false;
        }
    }
}