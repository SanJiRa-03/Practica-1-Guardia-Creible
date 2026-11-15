using UnityEngine;
using UnityEngine.AI;

namespace UDIT.IA3.Guardia
{
    public class EstadoBuscar : State
    {
        private const float TiempoTotalBusqueda = 0.5f;  // cuánto busca antes de rendirse
        private const float RadioBusqueda = 4f;        // radio alrededor del último punto
        private const float PausaEnCadaPunto = 1.5f;   // se queda mirando al llegar
        private const float UmbralNuevaPos = 0.5f;     // para detectar una posición distinta

        private Vector3 _centro;
        private float _tiempoTotal;
        private float _pausa;
        private bool _mirando;

        public EstadoBuscar(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            _tiempoTotal = 0f;
            _pausa = 0f;
            _mirando = false;

            if (!Pizarra.Get(ClavesBB.TieneUltimaPos, false))
            {
                UnityEngine.Debug.LogWarning(
                    "[" + Agente.name + "] busca sin tener última posición conocida.", Agente);
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
                return;
            }

            // Guardamos el centro de la búsqueda para no depender de la pizarra después.
            _centro = Pizarra.Get(ClavesBB.UltimaPos, Vector3.zero);
            IrAPuntoAleatorio();
        }

        internal override void Execute()
        {
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);

            if (Pizarra.Get(ClavesBB.AlertaDeGrupo, false))
            {
                StateMachine.ChangeState(new EstadoInvestigar(StateMachine));
                return;
            }

            // 1. Lo hemos vuelto a ver: a perseguir.
            if (nivel == NivelSospecha.Alerta)
            {
                StateMachine.ChangeState(new EstadoPerseguir(StateMachine));
                return;
            }

            // 2. Nueva sospecha EN OTRO SITIO: investigar ese punto.
            //    Comparamos posiciones para no rebotar Investigar <-> Buscar
            //    cuando el nivel se queda en Sospecha sin que haya nada nuevo.
            if (nivel == NivelSospecha.Sospecha)
            {
                Vector3 nueva = Pizarra.Get(ClavesBB.UltimaPos, Vector3.zero);
                if ((nueva - _centro).sqrMagnitude > UmbralNuevaPos * UmbralNuevaPos)
                {
                    StateMachine.ChangeState(new EstadoInvestigar(StateMachine));
                    return;
                }
            }

            // 3. Se acabó el tiempo de búsqueda: nos rendimos.
            _tiempoTotal += Time.deltaTime;
            if (_tiempoTotal >= TiempoTotalBusqueda)
            {
                // Olvidamos la posición: aquí sí, porque ya no hay nada que investigar.
                Pizarra.Set(ClavesBB.TieneUltimaPos, false);
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
                return;
            }

            // 4. Al llegar a un punto, mira un momento y luego va al siguiente.
            if (Pizarra.Get(ClavesBB.HaLlegado, false))
            {
                if (!_mirando)
                {
                    _mirando = true;
                    _pausa = 0f;
                    Pizarra.Set(ClavesBB.TieneDestino, false); // quieto mirando
                }

                _pausa += Time.deltaTime;
                if (_pausa >= PausaEnCadaPunto)
                {
                    _mirando = false;
                    IrAPuntoAleatorio();
                }
            }

        }

        internal override void Exit()
        {
            // No limpiamos TieneUltimaPos aquí: si salimos hacia Investigar o
            // Perseguir, esa posición hace falta. Solo se borra en la rama de
            // "rendirse" de Execute.
            _mirando = false;
        }

        private void IrAPuntoAleatorio()
        {
            Vector3 candidato = _centro + Random.insideUnitSphere * RadioBusqueda;
            candidato.y = _centro.y;

            // Aseguramos que el punto esté sobre el NavMesh.
            if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, RadioBusqueda, NavMesh.AllAreas))
                candidato = hit.position;
            else
                candidato = _centro;

            Pizarra.Set(ClavesBB.Destino, candidato);
            Pizarra.Set(ClavesBB.TieneDestino, true);
            Pizarra.Set(ClavesBB.HaLlegado, false);
        }
    }
}