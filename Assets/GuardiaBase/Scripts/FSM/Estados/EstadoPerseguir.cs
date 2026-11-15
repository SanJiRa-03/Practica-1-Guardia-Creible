using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class EstadoPerseguir : State
    {
        public EstadoPerseguir(StateMachine machine) : base(machine) { }

        private const float IntervaloAviso = 1f;
        private float _tiempoAviso;

        internal override void Enter()
        {
            _tiempoAviso = 0f;
            Agente.PuntosVisitados.Clear();

            if (Pizarra.Get(ClavesBB.TieneUltimaPos, false))
            {
                Vector3 objetivo = Pizarra.Get(ClavesBB.UltimaPos, Vector3.zero);
                RutaPatrulla ruta = Agente.GetComponent<RutaPatrulla>();
                if (ruta != null) ruta.MarcarInteres(objetivo, 1f);
                Pizarra.Set(ClavesBB.Destino, objetivo);
                Pizarra.Set(ClavesBB.TieneDestino, true);
                Pizarra.Set(ClavesBB.HaLlegado, false);

                AlertaGrupo.Emitir(Agente.gameObject, objetivo); // ← aviso inicial
            }
            else
            {
                UnityEngine.Debug.LogWarning(
                    "[" + Agente.name + "] persigue sin tener posición del jugador.", Agente);
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
            }
        }

        internal override void Execute()
        {
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);

            if (nivel == NivelSospecha.Alerta)
            {
                Vector3 objetivo = Pizarra.Get(ClavesBB.UltimaPos, Vector3.zero);
                Pizarra.Set(ClavesBB.Destino, objetivo);
                Pizarra.Set(ClavesBB.TieneDestino, true);
                Pizarra.Set(ClavesBB.HaLlegado, false);

                _tiempoAviso += Time.deltaTime;
                if (_tiempoAviso >= IntervaloAviso)
                {
                    _tiempoAviso = 0f;
                    AlertaGrupo.Emitir(Agente.gameObject, objetivo); // ← aviso periódico
                }
                return;
            }

            StateMachine.ChangeState(new EstadoInvestigar(StateMachine));

            // Tranquilo: ver nota al final.
        }

        internal override void Exit()
        {
            // Si en Enter() lanzáis animación, bark o corrutina, se apagan aquí.
            // Ojo: NO limpiamos TieneUltimaPosicion, porque EstadoInvestigar
            // la necesita justo después de perder al jugador.
        }
    }
}