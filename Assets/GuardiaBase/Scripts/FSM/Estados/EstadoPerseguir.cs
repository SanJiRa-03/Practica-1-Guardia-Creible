using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class EstadoPerseguir : State
    {
        public EstadoPerseguir(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            // Empezamos a perseguir hacia la última posición conocida.
            // Execute se encargará de refrescarla cada frame.
            if (Pizarra.Get(ClavesBB.TieneUltimaPos, false))
            {
                Vector3 objetivo = Pizarra.Get(ClavesBB.UltimaPos, Vector3.zero);
                Pizarra.Set(ClavesBB.Destino, objetivo);
                Pizarra.Set(ClavesBB.TieneDestino, true);
                Pizarra.Set(ClavesBB.HaLlegado, false);
            }
            else
            {
                UnityEngine.Debug.LogWarning(
                    "[" + Agente.name + "] persigue sin tener posición del jugador.", Agente);
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
            }

            // Aquí iría un bark tipo "¡Alto ahí!" o animación de correr.
            // Si lo añadís, apagadlo en Exit().
        }

        internal override void Execute()
        {
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);

            // 1. Seguimos en Alerta: el jugador está a la vista (o se sabe dónde está).
            //    Actualizamos el destino cada frame para que el guardia lo siga.
            if (nivel == NivelSospecha.Alerta)
            {
                Vector3 objetivo = Pizarra.Get(ClavesBB.UltimaPos, Vector3.zero);
                Pizarra.Set(ClavesBB.Destino, objetivo);
                Pizarra.Set(ClavesBB.TieneDestino, true);
                Pizarra.Set(ClavesBB.HaLlegado, false);
                return;
            }

            // 2. Hemos perdido al jugador de vista pero aún queda sospecha:
            //    vamos a investigar el último sitio donde estaba.
            if (nivel == NivelSospecha.Sospecha)
            {
                StateMachine.ChangeState(new EstadoBuscar(StateMachine));
                return;
            }

            //// 3. Tranquilo: se acabó. Volvemos a patrullar.
            //StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
        }

        internal override void Exit()
        {
            // Si en Enter() lanzáis animación, bark o corrutina, se apagan aquí.
            // Ojo: NO limpiamos TieneUltimaPosicion, porque EstadoInvestigar
            // la necesita justo después de perder al jugador.
        }
    }
}