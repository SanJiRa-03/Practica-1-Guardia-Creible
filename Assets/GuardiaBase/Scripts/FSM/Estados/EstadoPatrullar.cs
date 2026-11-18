// ═══════════════════════════════════════════════════════════════════════════
//  EstadoPatrullar.cs — EJEMPLO COMPLETO del patrón Estado
//
//  Inteligencia Artificial (IA3) · GDDVV 3º · UDIT · Curso 2026/2027
//  Dr. Luis Peña Sánchez
//
//  Éste OS LO DOY ENTERO, para que veáis la forma. Los otros tres estados
//  —Investigar, Perseguir, Buscar— los escribís vosotros copiando esta
//  estructura.
//
//  Fijaos en las tres cosas que hace y en las tres que NO hace:
//
//    SÍ:  lee la pizarra · escribe intenciones (destino) · transita
//    NO:  mirar el mundo (eso es del sensor)
//         llamar al NavMeshAgent (eso es de LocomocionNavMesh)
//         guardar «en qué estado estoy» en la pizarra (para eso está la FSM)
// ═══════════════════════════════════════════════════════════════════════════
using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class EstadoPatrullar : State
    {
        public EstadoPatrullar(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            // Al entrar, apuntamos al punto de patrulla que toque.
            RutaPatrulla ruta = Agente.GetComponent<RutaPatrulla>();

            if (ruta != null && ruta.TieneRuta)
            {
                Pizarra.Set(ClavesBB.Destino, ruta.PuntoActual);
                Pizarra.Set(ClavesBB.TieneDestino, true);
            }
            else
            {
                // Sin ruta configurada nos quedamos quietos, pero avisamos:
                // es un fallo de montaje de escena, no de código.
                Pizarra.Set(ClavesBB.TieneDestino, false);
                Debug.LogWarning(
                    "[" + Agente.name + "] no tiene RutaPatrulla con puntos. " +
                    "Mirad los apartados 3.4 y 3.5 del README_Montaje.", Agente);
            }
        }

        internal override void Execute()
        {
            // ── 1. ¿Hay razón para dejar de patrullar? ──────────────────────
            //
            // Se pregunta a la PIZARRA, nunca al sensor. El estado no sabe si
            // la sospecha viene de la vista, del oído o de que otro guardia dio
            // la alarma, y no le hace falta saberlo. Eso es la separación de
            // capas funcionando.
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);

            if (Pizarra.Get(ClavesBB.AlertaDeGrupo, false))
            {
                StateMachine.ChangeState(new EstadoInvestigar(StateMachine));
                return;
            }

            if (nivel == NivelSospecha.Alerta)
            {
                // TODO · descomentad cuando tengáis EstadoPerseguir
                 StateMachine.ChangeState(new EstadoPerseguir(StateMachine));
                return;
            }

            if (nivel == NivelSospecha.Sospecha)
            {
                // Si lo estoy viendo y la sospecha sube, me paro a observar:
                // si sigue subiendo entrará en Alerta y perseguirá. Solo
                // investigo cuando ya no lo veo.
                if (Pizarra.Get(ClavesBB.Visible, false))
                {
                    Pizarra.Set(ClavesBB.TieneDestino, false);
                    return;
                }

                StateMachine.ChangeState(new EstadoInvestigar(StateMachine));
                return;
            }

            // ── 2. Si no, seguir la ruta ────────────────────────────────────
            RutaPatrulla ruta = Agente.GetComponent<RutaPatrulla>();
            if (ruta == null || !ruta.TieneRuta) return;

            if (!Pizarra.Get(ClavesBB.TieneDestino, false))
            {
                // Me había parado a observar: retomo el punto en el que iba.
                Pizarra.Set(ClavesBB.Destino, ruta.PuntoActual);
                Pizarra.Set(ClavesBB.TieneDestino, true);
            }
            else if (Pizarra.Get(ClavesBB.HaLlegado, false))
            {
                // He llegado: elijo el siguiente con criterio.
                Pizarra.Set(ClavesBB.Destino, ElegirSiguiente(ruta));
                Pizarra.Set(ClavesBB.TieneDestino, true);
            }

        }

        internal override void Exit()
        {
            // Aquí no hay nada que limpiar. Pero el método existe y está vacío
            // a propósito: cuando en Enter() arranquéis una animación, un bark
            // o una corrutina, Exit() es donde se apaga. Si no lo hacéis, os
            // quedaréis con guardias diciendo «¿quién anda ahí?» mientras
            // persiguen. Pasa todos los años.
        }
        private Vector3 ElegirSiguiente(RutaPatrulla ruta)
        {
            int mejor = -1;
            float mejorNota = float.NegativeInfinity;

            for (int i = 0; i < ruta.NumPuntos; i++)
            {
                if (i == ruta.IndiceActual) continue;   // no repetir el mismo

                float tiempo = Mathf.Clamp01(ruta.TiempoSinVisitar(i) / 60f);   // 60 s sin pasar = máximo
                float interes = ruta.Interes(i);                                  // ya es 0..1
                float distancia = Mathf.Clamp01(
                    Vector3.Distance(Agente.transform.position, ruta.Posicion(i)) / 30f);

                float nota = 1.5f * tiempo + interes - 0.5f * distancia;
                Debug.Log(Agente.name + " | punto " + i +
                  " tiempo=" + tiempo.ToString("F2") +
                  " interes=" + interes.ToString("F2") +
                  " dist=" + distancia.ToString("F2") +
                  " NOTA=" + nota.ToString("F2"));

                if (nota > mejorNota)
                {
                    mejorNota = nota;
                    mejor = i;
                }
            }

            if (mejor == -1) return ruta.PuntoActual;
            Debug.Log(Agente.name + " elige el punto " + mejor);
            return ruta.IrA(mejor);  // marca la visita y devuelve la posición
        }
    }
}
