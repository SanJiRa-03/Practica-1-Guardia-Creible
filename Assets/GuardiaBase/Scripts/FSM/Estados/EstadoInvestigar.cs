using System.Collections.Generic;   // para List<>
using UnityEngine;
using UnityEngine.AI;

namespace UDIT.IA3.Guardia
{
    public class EstadoInvestigar : State
    {
        // Tiempo que se queda mirando alrededor al llegar, antes de rendirse.
        //private const float TiempoBusqueda = 3f;

        private float _temporizador;
        private bool _buscando;

        private const int NumCandidatos = 10;
        private const float RadioBusqueda = 10f;
        private Vector3 _centro;
        private List<Candidato> _candidatos = new List<Candidato>();
        private const int MaxIntentos = 30;
     

        private int _indice;                          // candidato actual
        private const int MaxPuntosAVisitar = 3;      // cuántos recorre como máximo
        private const float PausaEnCadaPunto = 0.5f;  // se queda mirando al llegar

        private struct Candidato
        {
            public Vector3 Posicion;
            public float Nota;
        }

        public EstadoInvestigar(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            Pizarra.Set(ClavesBB.AlertaDeGrupo, false);

            if (!Pizarra.Get(ClavesBB.TieneUltimaPos, false))
            {
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
                return;
            }

            _centro = Pizarra.Get(ClavesBB.UltimaPos, Vector3.zero);
            RutaPatrulla ruta = Agente.GetComponent<RutaPatrulla>();
            if (ruta != null) ruta.MarcarInteres(_centro, 1f);
            GenerarCandidatos();
            PuntuarCandidatos();

            _indice = 0;
            _buscando = false;

            if (_candidatos.Count > 0)
                IrAlCandidatoActual();
            else
                StateMachine.ChangeState(new EstadoBuscar(StateMachine));
        }

        internal override void Execute()
        {
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);

            if (Pizarra.Get(ClavesBB.AlertaDeGrupo, false))
            {
                StateMachine.ChangeState(new EstadoInvestigar(StateMachine));  // regenera candidatos alrededor de la nueva UltimaPos
                return;
            }

            // Si lo vemos, a perseguir.
            if (nivel == NivelSospecha.Alerta)
            {
                StateMachine.ChangeState(new EstadoPerseguir(StateMachine));
                return;
            }

            // Al llegar al candidato, esperamos un poco y volvemos a patrullar.
            if (Pizarra.Get(ClavesBB.HaLlegado, false))
            {
                // Primera vez que detectamos la llegada a este punto: nos quedamos quietos.
                if (!_buscando)
                {
                    _buscando = true;
                    _temporizador = 0f;
                    Pizarra.Set(ClavesBB.TieneDestino, false);
                }

                _temporizador += Time.deltaTime;

                // Ya hemos mirado el tiempo suficiente: pasamos al siguiente punto.
                if (_temporizador >= PausaEnCadaPunto)
                {
                    Agente.PuntosVisitados.Add(_candidatos[_indice].Posicion);   // guardar el actual
                    _indice++;                                                    // luego avanzar
                    _buscando = false;

                    if (_indice < _candidatos.Count && _indice < MaxPuntosAVisitar)
                        IrAlCandidatoActual();
                    else
                        StateMachine.ChangeState(new EstadoBuscar(StateMachine));   // sin borrar TieneUltimaPos
                }
            }
        }

        internal override void Exit()
        {
            _buscando = false;
        }
        private void GenerarCandidatos()
        {
            _candidatos.Clear();
            int intentos = 0;

            while (_candidatos.Count < NumCandidatos && intentos < MaxIntentos)
            {
                intentos++;

                Vector3 punto = _centro + Random.insideUnitSphere * RadioBusqueda;
                punto.y = _centro.y;

                if (!NavMesh.SamplePosition(punto, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    continue;

                NavMeshPath camino = new NavMeshPath();
                bool calculado = NavMesh.CalculatePath(
                    Agente.transform.position,
                    hit.position,
                    NavMesh.AllAreas,
                    camino);

                if (!calculado || camino.status != NavMeshPathStatus.PathComplete)
                    continue;

                Candidato c = new Candidato();
                c.Posicion = hit.position;
                c.Nota = 0f;
                _candidatos.Add(c);
            }

            Debug.Log("Candidatos: " + _candidatos.Count + " en " + intentos + " intentos");
        }
        private float Escondite(Vector3 punto)
        {
            Vector3 desde = _centro + Vector3.up * 1f;   // altura de ojos
            Vector3 hasta = punto + Vector3.up * 1f;

            // Linecast devuelve true si algo bloquea la línea
            if (Physics.Linecast(desde, hasta))
                return 1f;   // hay muro en medio: buen escondite
            return 0f;       // a la vista: mal escondite
        }
        private float Cercania(Vector3 punto)
        {
            float distancia = Vector3.Distance(punto, _centro);
            return Mathf.Clamp01(1f - (distancia / RadioBusqueda));
        }
        private void PuntuarCandidatos()
        {
            for (int i = 0; i < _candidatos.Count; i++)
            {
                Candidato c = _candidatos[i];
                c.Nota = Cercania(c.Posicion) + Escondite(c.Posicion) - Visitado(c.Posicion);
                _candidatos[i] = c;
            }

            // Ya están todos puntuados: ahora sí, ordenamos una sola vez.
            _candidatos.Sort((a, b) => b.Nota.CompareTo(a.Nota));

            // Y el log, también fuera, para ver la lista ya ordenada.
            for (int i = 0; i < _candidatos.Count; i++)
            {
                Debug.Log("Candidato " + i + " en " + _candidatos[i].Posicion +
                          " nota: " + _candidatos[i].Nota);
            }
        }
        private void IrAlCandidatoActual()
        {
            Pizarra.Set(ClavesBB.Destino, _candidatos[_indice].Posicion);
            Pizarra.Set(ClavesBB.TieneDestino, true);
            Pizarra.Set(ClavesBB.HaLlegado, false);
        }
        private float Visitado(Vector3 punto)
        {
            foreach (Vector3 v in Agente.PuntosVisitados)
            {
                if (Vector3.Distance(punto, v) < 2f)
                    return 1f;
            }
            return 0f;
        }
    }
}